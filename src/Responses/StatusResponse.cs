namespace GetMoarFediverse.Responses;

public class StatusResponse
{
    public string Uri { get; }

    public StatusResponse(string uri)
    {
        Uri = uri;
    }
}