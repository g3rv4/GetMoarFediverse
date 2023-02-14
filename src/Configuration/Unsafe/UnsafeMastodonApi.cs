using System.Collections.Immutable;

namespace GetMoarFediverse.Configuration.Unsafe;

public class UnsafeMastodonApi
{
    public string? Url { get; set; }
    public UnsafeMastodonApiAccessToken[]? Tokens { get; set; }

    public MastodonApi ToMastodonApi()
    {
        if (Url.IsNullOrEmpty())
            throw new Exception("A valid Url must be provided for the Api");

        if (!Url!.EndsWith("/api/"))
            throw new Exception("The Url must end with /api/");

        return new MastodonApi(Url!,
            Tokens == null
                ? ImmutableArray<MastodonApiAccessToken>.Empty
                : Tokens.Select(t => t.ToAccessToken()).ToImmutableArray());
    }
}