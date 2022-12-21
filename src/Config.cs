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
    public ImmutableArray<string> Tags { get; }
    public ImmutableArray<SiteData> Sites { get; }


    private Config(string importedPath, string fakeRelayUrl, string fakeRelayApiKey, string? mastodonPostgresConnectionString,
                    ImmutableArray<string> tags, ImmutableArray<SiteData> sites)
    {
        ImportedPath = importedPath;
        FakeRelayUrl = fakeRelayUrl;
        FakeRelayApiKey = fakeRelayApiKey;
        MastodonPostgresConnectionString = mastodonPostgresConnectionString;
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
            Console.WriteLine("Warning: Sites is deprecated, please use Instances instead");
        }

        data.Tags ??= Array.Empty<string>();
        if (data.MastodonPostgresConnectionString.HasValue() && data.Tags.Length > 0)
        {
            throw new Exception("You can't specify both MastodonPostgresConnectionString and Tags");
        }

        Instance = new Config(importedPath, data.FakeRelayUrl, apiKey, data.MastodonPostgresConnectionString,
            data.Tags.ToImmutableArray(), data.GetImmutableSites());
    }

    public class ConfigData
    {
        public string FakeRelayUrl { get; set; }
        public string? FakeRelayApiKey { get; set; }
        public string? MastodonPostgresConnectionString { get; set; }
        public string[]? Instances { get; set; }
        public string[]? Tags { get; set; }
        public InternalSiteData[]? Sites { get; set; }

        public ImmutableArray<SiteData> GetImmutableSites()
        {
            // the plan is to stop supporting Sites in favor of Instances. SiteSpecificTags add complexity and 
            // don't make sense when pulling tags from Mastodon. Also, pulling is fast and multithreaded!
            if (Instances != null)
            {
                return Instances
                    .Select(i => new SiteData { Host = i, SiteSpecificTags = ImmutableArray<string>.Empty })
                    .ToImmutableArray();
            }
            
            return Sites == null
                ? ImmutableArray<SiteData>.Empty
                : Sites.Select(s => s.ToSiteData())
                    .ToImmutableArray();
        }

        public class InternalSiteData
        {
            public string Host { get; set; }
            public string[]? SiteSpecificTags { get; set; }

            public SiteData ToSiteData() =>
                new()
                {
                    Host = Host,
                    SiteSpecificTags =
                        SiteSpecificTags == null
                        ? ImmutableArray<string>.Empty
                        : SiteSpecificTags.ToImmutableArray()
                };
        }
    }

    public class SiteData
    {
        public string Host { get; init; }
        public ImmutableArray<string> SiteSpecificTags { get; init; }
    }
}
