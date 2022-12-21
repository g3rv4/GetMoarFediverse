using Npgsql;

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
}
