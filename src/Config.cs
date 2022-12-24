using System.Collections.Immutable;
using System.Text.Json;

namespace GetMoarFediverse;

public class Config
{
    public static Config? Instance { get; private set; }
    
    public string ImportedPath { get; }
    public string FakeRelayUrl { get; }
    public string FakeRelayApiKey { get; }
    public string? MastodonPostgresConnectionString { get; }
    public bool PinnedTags { get; }
    public ImmutableArray<string> Tags { get; }
    public ImmutableArray<SiteData> Sites { get; }


    private Config(string importedPath, string fakeRelayUrl, string fakeRelayApiKey, string? mastodonPostgresConnectionString,
                    bool pinnedTags, ImmutableArray<string> tags, ImmutableArray<SiteData> sites)
    {
        ImportedPath = importedPath;
        FakeRelayUrl = fakeRelayUrl;
        FakeRelayApiKey = fakeRelayApiKey;
        MastodonPostgresConnectionString = mastodonPostgresConnectionString;
        PinnedTags = pinnedTags;
        Tags = tags;
        Sites = sites;
    }

    public static void Init(string path)
    {
        if (Instance != null)
        {
            return;
        }

        var data = JsonSerializer.Deserialize(File.ReadAllText(path), JsonContext.Default.ConfigData);
        if (data == null)
        {
            throw new Exception("Could not deserialize the config file");
        }
        
        var importedPath = Path.Join(Path.GetDirectoryName(path), "imported.txt");
        var apiKey = string.IsNullOrEmpty(data.FakeRelayApiKey)
            ? Environment.GetEnvironmentVariable("FAKERELAY_APIKEY")
            : data.FakeRelayApiKey;

        if (apiKey == null)
        {
            throw new Exception("The api key is missing");
        }
        
        if (data.Sites is { Length: > 0 })
        {
            Console.WriteLine("|============================================================|");
            Console.WriteLine("| Warning: Sites is deprecated, please use Instances instead |");
            Console.WriteLine("|============================================================|\n");
        }

        data.Tags ??= Array.Empty<string>();
        if (data.MastodonPostgresConnectionString.HasValue() && data.Tags.Length > 0)
        {
            throw new Exception("You can't specify both MastodonPostgresConnectionString and Tags");
        }

        if (data.FakeRelayUrl.IsNullOrEmpty())
        {
            throw new Exception("Missing FakeRelayUrl");
        }

        Instance = new Config(importedPath, data.FakeRelayUrl, apiKey, data.MastodonPostgresConnectionString,
            data.PinnedTags, data.Tags.ToImmutableArray(), data.GetImmutableSites());
    }

    public class ConfigData
    {
        public string? FakeRelayUrl { get; set; }
        public string? FakeRelayApiKey { get; set; }
        public string? MastodonPostgresConnectionString { get; set; }
        public bool PinnedTags { get; set; }
        public string[]? Instances { get; set; }
        public string[]? Tags { get; set; }
        public InternalSiteData[]? Sites { get; set; }

        public ImmutableArray<SiteData> GetImmutableSites()
        {
            // the plan is to stop supporting Sites in favor of Instances. SiteSpecificTags add complexity and 
            // don't make sense when pulling tags from Mastodon. Also, pulling is fast and multi threaded!
            if (Instances != null)
            {
                return Instances
                    .Select(i => new SiteData(i, ImmutableArray<string>.Empty))
                    .ToImmutableArray();
            }
            
            return Sites == null
                ? ImmutableArray<SiteData>.Empty
                : Sites.Select(s => s.ToSiteData())
                    .ToImmutableArray();
        }

        public class InternalSiteData
        {
            public InternalSiteData(string host, string[]? siteSpecificTags)
            {
                Host = host;
                SiteSpecificTags = siteSpecificTags;
            }

            public string Host { get; }
            public string[]? SiteSpecificTags { get; }
            public SiteData ToSiteData() =>
                new(Host, SiteSpecificTags?.ToImmutableArray() ?? ImmutableArray<string>.Empty);
        }
    }

    public record SiteData(string Host, ImmutableArray<string> SiteSpecificTags);
}
