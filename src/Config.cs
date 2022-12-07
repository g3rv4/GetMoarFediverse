using System.Collections.Immutable;
using Jil;

namespace GetMoarFediverse;

public class Config
{
    public static Config? Instance { get; private set; }
    
    public string ImportedPath { get; }
    public string FakeRelayUrl { get; }
    public string FakeRelayApiKey { get; }
    public ImmutableArray<string> Tags { get; }
    public ImmutableArray<SiteData> Sites { get; }


    private Config(string importedPath, string fakeRelayUrl, string fakeRelayApiKey, ImmutableArray<string> tags, ImmutableArray<SiteData> sites)
    {
        ImportedPath = importedPath;
        FakeRelayUrl = fakeRelayUrl;
        FakeRelayApiKey = fakeRelayApiKey;
        Tags = tags;
        Sites = sites;
    }

    public static void Init(string path)
    {
        if (Instance != null)
        {
            return;
        }

        var data = JSON.Deserialize<ConfigData>(File.ReadAllText(path));

        var importedPath = Path.Join(Path.GetDirectoryName(path), "imported.txt");
        var apiKey = string.IsNullOrEmpty(data.FakeRelayApiKey)
            ? Environment.GetEnvironmentVariable("FAKERELAY_APIKEY")
            : data.FakeRelayApiKey;

        Instance = new Config(importedPath, data.FakeRelayUrl, apiKey, data.Tags.ToImmutableArray(), data.ImmutableSites);
    }

    private class ConfigData
    {
        public string FakeRelayUrl { get; set; }
        public string? FakeRelayApiKey { get; set; }
        public string[] Tags { get; set; }
        public InternalSiteData[]? Sites { get; set; }

        public ImmutableArray<SiteData> ImmutableSites =>
            Sites == null
                ? ImmutableArray<SiteData>.Empty
                : Sites.Select(s => s.ToSiteData())
                       .ToImmutableArray();

        public class InternalSiteData
        {
            public string Host { get; private set; }
            public string[]? SiteSpecificTags { get; private set; }

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
