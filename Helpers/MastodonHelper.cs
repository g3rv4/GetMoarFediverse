using System.Net.Http.Headers;

namespace ImportDataAsRelay.Helpers;

public static class MastodonHelper
{
    private static string TargetHost = Environment.GetEnvironmentVariable("TARGET_HOST");
    private static string RelayHost = Environment.GetEnvironmentVariable("RELAY_HOST");
    
    public static async Task EnqueueStatusToFetch(string statusUrl)
    {
        var client = new HttpClient();

        var date = DateTime.UtcNow;
        
        var content = $@"{{
    ""@context"": ""https://www.w3.org/ns/activitystreams"",
    ""actor"": ""https://{RelayHost}/actor"",
    ""id"": ""https://{RelayHost}/activities/23af173e-e1fd-4283-93eb-514f1e5e5408"",
    ""object"": ""{statusUrl}"",
    ""to"": [
        ""https://{RelayHost}/followers""
    ],
    ""type"": ""Announce""
}}";
        var digest = CryptoHelper.GetSHA256Digest(content);
        var requestContent = new StringContent(content);

        requestContent.Headers.Add("Digest", "SHA-256=" + digest);

        var stringToSign = $"(request-target): post /inbox\ndate: {date.ToString("R")}\nhost: {TargetHost}\ndigest: SHA-256={digest}\ncontent-length: {content.Length}";
        var signature = CryptoHelper.Sign(stringToSign);
        requestContent.Headers.Add("Signature", $@"keyId=""https://{RelayHost}/actor#main-key"",algorithm=""rsa-sha256"",headers=""(request-target) date host digest content-length"",signature=""{signature}""");

        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/activity+json");
        client.DefaultRequestHeaders.Date = date;

        var response = await client.PostAsync($"https://{TargetHost}/inbox", requestContent);
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
            Console.WriteLine("Status code: " + response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response content: " + body);

            throw;
        }
    }
}
