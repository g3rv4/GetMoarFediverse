using Npgsql;

namespace GetMoarFediverse;

public static class MastodonConnectionHelper
{
    public static async Task<List<string>> GetFollowedTagsAsync()
    {
        var res = new List<string>();
        
        await using var conn = new NpgsqlConnection(Config.Instance.MastodonPostgresConnectionString);
        await conn.OpenAsync();

        await using (var cmd = new NpgsqlCommand("SELECT DISTINCT tags.name FROM tag_follows JOIN tags ON tag_id = tags.id ORDER BY tags.name ASC;", conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                res.Add(reader.GetString(0));
        }

        return res;
    }
}
