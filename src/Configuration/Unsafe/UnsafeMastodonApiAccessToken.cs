namespace GetMoarFediverse.Configuration.Unsafe;

public class UnsafeMastodonApiAccessToken
{
    public string? Owner { get; set; }
    public string? Token { get; set; }

    public MastodonApiAccessToken ToAccessToken()
    {
        if (Owner.IsNullOrEmpty() || Token.IsNullOrEmpty())
            throw new Exception("An Owner and Token must both be specified for an API Access Token.");

        return new MastodonApiAccessToken(Owner!, Token!);
    }
}