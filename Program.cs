using ImportDataAsRelay.Helpers;
using Jil;

var interestingTagsEverywhere = new[] { "dotnet", "csharp" };
var sources = new Dictionary<string, string[]>
{
    ["hachyderm.io"] = new [] { "hachyderm" },
    ["mastodon.social"] = Array.Empty<string>(),
    ["dotnet.social"] = Array.Empty<string>(),
};

var client = new HttpClient();

foreach (var (site, specificTags) in sources)
{
    var tags = specificTags.Concat(interestingTagsEverywhere).ToList();
    foreach (var tag in tags)
    {
        Console.WriteLine($"Fetching tag #{tag} from {site}");
        var response = await client.GetAsync($"https://{site}/tags/{tag}.json");
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

        foreach (var statusLink in data.OrderedItems)
        {
            Console.WriteLine($"Bringing in {statusLink}");
            try
            {
                await MastodonHelper.EnqueueStatusToFetch(statusLink);
                await Task.Delay(500);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}");
            }
        }
    }
}

public class TagResponse
{
    public string[] OrderedItems { get; private set; }
}