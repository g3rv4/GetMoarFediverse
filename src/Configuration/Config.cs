using System.Collections.Immutable;

namespace GetMoarFediverse.Configuration;

public record Config(
    string ImportedPath,
    string FakeRelayUrl,
    string FakeRelayApiKey,
    string? MastodonPostgresConnectionString,
    MastodonApi? Api,
    bool PinnedTags,
    ImmutableArray<string> Tags,
    ImmutableArray<SiteData> Sites
);

public record MastodonApi(string Url, ImmutableArray<MastodonApiAccessToken> Tokens);

public record MastodonApiAccessToken(string Owner, string Token);

public record SiteData(string Host, ImmutableArray<string> SiteSpecificTags);