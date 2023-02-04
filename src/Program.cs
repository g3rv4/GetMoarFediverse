using System.Collections.Concurrent;
using System.Text.Json;
using GetMoarFediverse;
using GetMoarFediverse.Configuration;
using TurnerSoftware.RobotsExclusionTools;

var configPath = Environment.GetEnvironmentVariable("CONFIG_PATH");
if (args.Length == 1){
    configPath = args[0];
}

if (configPath.IsNullOrEmpty())
{
    throw new Exception("Missing config path");
}

Context.Load(configPath);

var client = new HttpClient();
client.DefaultRequestHeaders.Add("User-Agent", "GetMoarFediverse");

var authClient = new HttpClient
{
    BaseAddress = new Uri(Context.Configuration.FakeRelayUrl)
};
authClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Context.Configuration.FakeRelayApiKey);

var importedPath = Context.Configuration.ImportedPath;
if (!File.Exists(importedPath))
{
    File.WriteAllText(importedPath, "");
}

var robotsFileParser = new RobotsFileParser();
var sitesRobotFile = new ConcurrentDictionary<string, RobotsFile>();
await Parallel.ForEachAsync(Context.Configuration.Sites,
    new ParallelOptions { MaxDegreeOfParallelism = Context.Configuration.Sites.Length },
    async (site, _) =>
    {
        sitesRobotFile[site.Host] = await robotsFileParser.FromUriAsync(new Uri($"http://{site.Host}/robots.txt"));
    }
);

List<(string host, string tag)> sitesTags;
int numberOfTags;

var tags = new List<string>();

if (Context.Configuration.MastodonPostgresConnectionString.HasValue() || Context.Configuration.Api != null)
{
    tags.AddRange(await MastodonConnectionHelper.GetFollowedTagsAsync());
}

if (Context.Configuration.MastodonPostgresConnectionString.HasValue())
{
    if (Context.Configuration.PinnedTags)
    {
        tags = tags.Concat(await MastodonConnectionHelper.GetPinnedTagsAsync()).Distinct().ToList();
    }
}

if (tags.Any())
{
    numberOfTags = tags.Count;
    sitesTags = Context.Configuration.Sites
        .SelectMany(s => tags.Select(t => (s.Host, t)))
        .OrderBy(e => e.t)
        .ToList();
}
else
{
    numberOfTags = Context.Configuration.Tags.Length;
    sitesTags = Context.Configuration.Sites
        .SelectMany(s => Context.Configuration.Tags.Select(tag => (s.Host, tag)))
        .Concat(Context.Configuration.Sites.SelectMany(s => s.SiteSpecificTags.Select(tag => (s.Host, tag))))
        .OrderBy(t => t.tag)
        .ToList();
}

var importedList = File.ReadAllLines(importedPath).ToList();
var imported = importedList.ToHashSet();
var statusesToLoadBag = new ConcurrentBag<string>();
await Parallel.ForEachAsync(sitesTags, new ParallelOptions{MaxDegreeOfParallelism = numberOfTags * 2}, async (st, _) =>
{
    var (site, tag) = st;
    Console.WriteLine($"Fetching tag #{tag} from {site}");

    var url = $"https://{site}/tags/{tag}.json";
    if (sitesRobotFile.TryGetValue(site, out var robotsFile))
    {
        var allowed = robotsFile.IsAllowedAccess(new Uri(url), "GetMoarFediverse");
        if (!allowed)
        {
            Console.WriteLine($"Scraping {url} is not allowed based on their robots.txt file");
            return;
        }
    }
    
    HttpResponseMessage? response = null;
    try
    {
        response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error fetching tag {tag} from {site}, status code: {response?.StatusCode}. Error: {e.Message}");
        return;
    }

    var json = await response.Content.ReadAsStringAsync();
    var data = JsonSerializer.Deserialize(json, CamelCaseJsonContext.Default.TagResponse);
    if (data == null)
    {
        Console.WriteLine($"Error deserializing the response when pulling #{tag} posts from {site}");
        return;
    }

    int count = 0;
    foreach (var statusLink in data.OrderedItems.Where(i=>!imported.Contains(i)))
    {
        statusesToLoadBag.Add(statusLink);
        count++;
    }
    
    Console.WriteLine($"Retrieved {count} new statuses from {site} with hashtag #{tag}");
});

var statusesToLoad = statusesToLoadBag.ToHashSet();
Console.WriteLine($"Originally retrieved {statusesToLoadBag.Count} statuses. After removing duplicates, I got {statusesToLoad.Count} really unique ones");
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

if (importedList.Count > 5000)
{
    importedList = importedList
        .Skip(importedList.Count - 5000)
        .ToList();
}

File.WriteAllLines(importedPath, importedList);
