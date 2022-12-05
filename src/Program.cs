using System.Collections.Concurrent;
using ImportDataAsRelay;
using Jil;

Config.Init(Environment.GetEnvironmentVariable("CONFIG_PATH"));

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

var imported = File.ReadAllLines(importedPath).ToHashSet();
var statusesToLoadBag = new ConcurrentBag<string>();

ParallelOptions parallelOptions = new()
{
    MaxDegreeOfParallelism = 8
};

await Parallel.ForEachAsync(Config.Instance.Sites, parallelOptions, async (site, _) =>
{
    var tags = site.SiteSpecificTags.Concat(Config.Instance.Tags).ToList();
    foreach (var tag in tags)
    {
        Console.WriteLine($"Fetching tag #{tag} from {site.Host}");
        var response = await client.GetAsync($"https://{site.Host}/tags/{tag}.json");
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error fetching tag, status code: {response.StatusCode}. Error: {e.Message}");
            continue;
        }

        var json = await response.Content.ReadAsStringAsync();
        var data = JSON.Deserialize<TagResponse>(json, Options.CamelCase);

        foreach (var statusLink in data.OrderedItems.Where(i=>!imported.Contains(i)))
        {
            statusesToLoadBag.Add(statusLink);
        }
    }
});

var statusesToLoad = statusesToLoadBag.ToHashSet();
var importedOnThisRun = new List<string>();
foreach (var statusLink in statusesToLoad)
{
    Console.WriteLine($"Bringing in {statusLink}");
    try
    {
        var content = new List<KeyValuePair<string, string>>();
        content.Add(new KeyValuePair<string, string>("statusUrl", statusLink));

        var res = await authClient.PostAsync("index", new FormUrlEncodedContent(content));
        res.EnsureSuccessStatusCode();
        importedOnThisRun.Add(statusLink);
    }
    catch (Exception e)
    {
        Console.WriteLine($"{e.Message}");
    }
}

File.AppendAllLines(importedPath, importedOnThisRun);

public class TagResponse
{
    public string[] OrderedItems { get; private set; }
}