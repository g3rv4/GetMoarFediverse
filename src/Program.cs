using System.Collections.Concurrent;
using System.Text.Json;
using GetMoarFediverse;

var configPath = Environment.GetEnvironmentVariable("CONFIG_PATH");
if (args.Length == 1){
    configPath = args[0];
}

Config.Init(configPath);

var client = new HttpClient();
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

var importedList = File.ReadAllLines(importedPath).ToList();
var imported = importedList.ToHashSet();
var statusesToLoadBag = new ConcurrentBag<string>();

var sitesTags = Config.Instance.Sites
    .SelectMany(s => Config.Instance.Tags.Select(tag => (s.Host, tag)))
    .Concat(Config.Instance.Sites.SelectMany(s => s.SiteSpecificTags.Select(tag => (s.Host, tag))))
    .OrderBy(t => t.tag)
    .ToList();

ParallelOptions parallelOptions = new()
{
    MaxDegreeOfParallelism = 8
};

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
    var data = JsonSerializer.Deserialize<TagResponse>(json, CamelCaseJsonContext.Default.TagResponse);

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
        var content = new List<KeyValuePair<string, string>>();
        content.Add(new KeyValuePair<string, string>("statusUrl", statusLink));

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
    public string[] OrderedItems { get; set; }
}