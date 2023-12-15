using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using GetMoarFediverse;
using GetMoarFediverse.Configuration;
using GetMoarFediverse.Responses;
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
        try
        {
            sitesRobotFile[site.Host] = await robotsFileParser.FromUriAsync(new Uri($"http://{site.Host}/robots.txt"));
        }
        catch
        {
            Console.WriteLine($"Ignoring {site.Host} because had issues fetching its robots data (is the site down?)");
        }
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

    var url = $"https://{site}/api/v1/timelines/tag/{tag}?limit=40";
    if (sitesRobotFile.TryGetValue(site, out var robotsFile))
    {
        var allowed = robotsFile.IsAllowedAccess(new Uri(url), "GetMoarFediverse");
        if (!allowed)
        {
            Console.WriteLine($"Scraping {url} is not allowed based on their robots.txt file");
            return;
        }
    }
    else
    {
        Console.WriteLine($"Not scraping {url} because I couldn't fetch robots data.");
        return;
    }
    
    HttpResponseMessage? response = null;
    string? json = null;
    try
    {
        response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        json = await response.Content.ReadAsStringAsync();
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error fetching tag {tag} from {site}, status code: {response?.StatusCode}. Error: {e.Message}");
        return;
    }

    StatusResponse[]? data;
    try
    {
        data = JsonSerializer.Deserialize(json, CamelCaseJsonContext.Default.StatusResponseArray);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error deserializing the response when pulling #{tag} posts from {site}. Error: {e.Message}");
        Console.WriteLine($"Got the following response while I expected json content: {json}");
        return;
    }
    
    if (data == null)
    {
        Console.WriteLine($"Error deserializing the response when pulling #{tag} posts from {site}");
        return;
    }

    var count = 0;
    foreach (var statusLink in data.Where(i => !imported.Contains(i.Uri)))
    {
        statusesToLoadBag.Add(statusLink.Uri);
        count++;
    }

    Console.WriteLine($"Retrieved {count} new statuses from {site} with hashtag #{tag}");
});

var statusesToLoad = statusesToLoadBag.ToHashSet();
Console.WriteLine($"Originally retrieved {statusesToLoadBag.Count} statuses. After removing duplicates, I got {statusesToLoad.Count} really unique ones");
var rateLimited = false;
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
        if (res.StatusCode == HttpStatusCode.TooManyRequests)
        {
            // fakerelay.gervas.io has a token bucket rate limit of 30 tokens per minute, allowing bursts of up to
            // 60 requests per minute. Once we hit a 429, we should do a request every 2 seconds. 
            Console.WriteLine("Got a 429 from FakeRelay, waiting 2 second and retrying");
            rateLimited = true;
            await Task.Delay(TimeSpan.FromSeconds(2));
            res = await authClient.PostAsync("index", new FormUrlEncodedContent(content));
        }
        res.EnsureSuccessStatusCode();
        importedList.Add(statusLink);
        if (rateLimited)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"{e.Message}");
    }
}

var maxFileLines = sitesTags.Count * 40;
if (importedList.Count > maxFileLines)
{
    Console.WriteLine($"Keeping the last {maxFileLines} on the status file");
    importedList = importedList
        .Skip(importedList.Count - maxFileLines)
        .ToList();
}

File.WriteAllLines(importedPath, importedList);
