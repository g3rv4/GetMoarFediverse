using System.Collections.Immutable;

namespace GetMoarFediverse.Configuration.Unsafe;

public class UnsafeSiteData
{
    public UnsafeSiteData(string host, string[]? siteSpecificTags)
    {
        Host = host;
        SiteSpecificTags = siteSpecificTags;
    }

    public string Host { get; }
    public string[]? SiteSpecificTags { get; }
    public SiteData ToSiteData() =>
        new(Host, SiteSpecificTags?.ToImmutableArray() ?? ImmutableArray<string>.Empty);
}