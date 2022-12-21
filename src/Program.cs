using System.Collections.Concurrent;
using System.Text.Json;
using GetMoarFediverse;
using TurnerSoftware.RobotsExclusionTools;

var configPath = Environment.GetEnvironmentVariable("CONFIG_PATH");
if (args.Length == 1){
    configPath = args[0];
}

if (configPath.IsNullOrEmpty())
{
    throw new Exception("Missing config path");
}

Config.Init(configPath);

if (Config.Instance == null)
{
    throw new Exception("Error initializing config object");
}

var client = new HttpClient();
client.DefaultRequestHeaders.Add("User-Agent", "GetMoarFediverse");

var authClient = new HttpClient
{
    BaseAddress = new Uri(Config.Instance.FakeRelayUrl)
};
authClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Config.Instance.FakeRelayApiKey);

var importedPath = Config.Instance.ImportedPath;
if (!File.Exists(importedPath))
{
    File.WriteAllText(importedPath, "");
}

ParallelOptions parallelOptions = new()
{
    MaxDegreeOfParallelism = 8
};

var robotsFileParser = new RobotsFileParser();
var sitesRobotStatus = new ConcurrentDictionary<string, bool>();
await Parallel.ForEachAsync(Config.Instance.Sites, parallelOptions, async (site, _) =>
{
    var robotsFile = await robotsFileParser.FromUriAsync(new Uri($"http://{site.Host}/robots.txt"));
    var allowedAccess = robotsFile.IsAllowedAccess(
        new Uri($"https://{site.Host}/tags/example.json"),
        "GetMoarFediverse"
    );
    sitesRobotStatus[site.Host] = allowedAccess;
});

var allowedSites = sitesRobotStatus
    .Where(i => i.Value)
    .Select(i => i.Key)
    .ToList();

List<(string host, string tag)> sitesTags;
if (Config.Instance.MastodonPostgresConnectionString.HasValue())
{
    var tags = await MastodonConnectionHelper.GetFollowedTagsAsync();
    sitesTags = allowedSites
        .SelectMany(s => tags.Select(t => (s, t)))
        .ToList();
}
else
{
    sitesTags = allowedSites
        .SelectMany(s => Config.Instance.Tags.Select(tag => (s, tag)))
        .Concat(Config.Instance.Sites.SelectMany(s => s.SiteSpecificTags.Select(tag => (s.Host, tag))))
        .OrderBy(t => t.tag)
        .ToList();
}

var importedList = File.ReadAllLines(importedPath).ToList();
var imported = importedList.ToHashSet();
var statusesToLoadBag = new ConcurrentBag<string>();
await Parallel.ForEachAsync(sitesTags, parallelOptions, async (st, _) =>
{
    var (site, tag) = st;
    Console.WriteLine($"Fetching tag #{tag} from {site}");
    HttpResponseMessage? response = null;
    try
    {
        response = await client.GetAsync($"https://{site}/tags/{tag}.json");
        response.EnsureSuccessStatusCode();
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error fetching tag, status code: {response?.StatusCode}. Error: {e.Message}");
        return;
    }

    var json = await response.Content.ReadAsStringAsync();
    var data = JsonSerializer.Deserialize(json, CamelCaseJsonContext.Default.TagResponse);
    if (data == null)
    {
        Console.WriteLine($"Error deserializing the response when pulling #{tag} posts from {site}");
        return;
    }

    foreach (var statusLink in data.OrderedItems.Where(i=>!imported.Contains(i)))
    {
        statusesToLoadBag.Add(statusLink);
    }
});

var statusesToLoad = statusesToLoadBag.ToHashSet();
foreach (var statusLink in statusesToLoad)
{
    Console.WriteLine($"Bringing in {statusLink}");
    try
    {
        var content = new List<KeyValuePair<string, string>>
        {
            new("statusUrl", statusLink)
        };

        var res = await authClient.PostAsync("index", new FormUrlEncodedContent(content));
        res.EnsureSuccessStatusCode();
        importedList.Add(statusLink);
    }
    catch (Exception e)
    {
        Console.WriteLine($"{e.Message}");
    }
}

if (importedList.Count > 1000)
{
    importedList = importedList
        .Skip(importedList.Count - 1000)
        .ToList();
}

File.WriteAllLines(importedPath, importedList);

public class TagResponse
{
    public string[] OrderedItems { get; }
    
    public TagResponse(string[] orderedItems)
    {
        OrderedItems = orderedItems;
    }
}
