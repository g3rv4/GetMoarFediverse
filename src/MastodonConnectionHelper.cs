using Npgsql;
using System.Text.Json;

namespace GetMoarFediverse;

public static class MastodonConnectionHelper
{
    public static async Task<List<string>> GetFollowedTagsAsync()
    {
        if (Config.Instance == null) throw new Exception("Config object is not initialized");
        if (Config.Instance.MastodonPostgresConnectionString.IsNullOrEmpty())
        {
            throw new Exception("Missing mastodon postgres connection string");
        }
        
        await using var conn = new NpgsqlConnection(Config.Instance.MastodonPostgresConnectionString);
        await conn.OpenAsync();

        var res = new List<string>();
        await using var cmd = new NpgsqlCommand("SELECT DISTINCT tags.name FROM tag_follows JOIN tags ON tag_id = tags.id ORDER BY tags.name ASC;", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            res.Add(reader.GetString(0));

        return res;
    }

    public static async Task<List<string>> GetPinnedTagsAsync()
    {
        if (Config.Instance == null) throw new Exception("Config object is not initialized");
        if (Config.Instance.MastodonPostgresConnectionString.IsNullOrEmpty())
        {
            throw new Exception("Missing mastodon postgres connection string");
        }
        
        await using var conn = new NpgsqlConnection(Config.Instance.MastodonPostgresConnectionString);
        await conn.OpenAsync();

        var res = new List<string>();
        // Column 0: the 'original' tag with was pinned
        // Column 1: Config of 'Include additional tags for this column' this includes the the tags in 'any' array.
        await using var cmd = new NpgsqlCommand(@"
SELECT DISTINCT col->'params'->>'id', col->'params'->>'tags'
FROM   web_settings, json_array_elements(data->'columns') col
WHERE  col->>'id' = 'HASHTAG'
AND    col->'params'->>'id' IS NOT NULL
ORDER BY col->'params'->>'id' ASC", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            res.Add(reader.GetString(0));
            if (reader.IsDBNull(1)) continue;
            var doc = JsonDocument.Parse(reader.GetString(1));
            var anyArray = doc.RootElement.GetProperty("any");
            foreach (var item in anyArray.EnumerateArray())
            {
                var value = item.GetProperty("value");
                if (value.ValueKind != JsonValueKind.Null)
                {
                    var valuestring = value.GetString();
                    if (valuestring.HasValue())
                        res.Add(valuestring);
                }
            }
        }
     
        return res;
    }

}
