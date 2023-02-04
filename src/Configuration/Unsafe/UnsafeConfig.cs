using System.Collections.Immutable;
using System.Text.Json;

namespace GetMoarFediverse.Configuration.Unsafe;

public class UnsafeConfig
{
    public string? ImportedPath { get; set; }
    public string? FakeRelayUrl { get; set; }
    public string? FakeRelayApiKey { get; set; }
    public string? MastodonPostgresConnectionString { get; set; }
    public UnsafeMastodonApi? Api { get; set; }
    public bool PinnedTags { get; set; }
    public string[]? Instances { get; set; }
    public string[]? Tags { get; set; }
    public UnsafeSiteData[]? Sites { get; set; }

    public static Config ToConfig(string path)
    {
        var data = JsonSerializer.Deserialize(File.ReadAllText(path), JsonContext.Default.UnsafeConfig);
        if (data == null)
        {
            throw new Exception("Could not deserialize the config file");
        }

        data.ImportedPath = Path.Join(Path.GetDirectoryName(path), "imported.txt");
        data.FakeRelayApiKey ??= Environment.GetEnvironmentVariable("FAKERELAY_APIKEY");
        
        if (data.FakeRelayApiKey == null)
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
        
        if ((data.MastodonPostgresConnectionString.HasValue() || data.Api != null) && data.Tags.Length > 0)
        {
            throw new Exception("You can't specify both MastodonPostgresConnectionString / API and Tags");
        }

        if (data.FakeRelayUrl.IsNullOrEmpty())
        {
            throw new Exception("Missing FakeRelayUrl");
        }

        return new Config(
            data.ImportedPath!, 
            data.FakeRelayUrl!, 
            data.FakeRelayApiKey!, 
            data.MastodonPostgresConnectionString, 
            data.Api?.ToMastodonApi(),
            data.PinnedTags, 
            data.Tags?.ToImmutableArray() ?? ImmutableArray<string>.Empty, 
            data.GetImmutableSites());
    }
    
    private ImmutableArray<SiteData> GetImmutableSites()
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
}