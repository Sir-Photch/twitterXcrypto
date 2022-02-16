using System.Text;
using System.Data;
using MySql.Data.MySqlClient;
using twitterXcrypto.twitter;
using twitterXcrypto.util;
using twitterXcrypto.crypto;
using twitterXcrypto.imaging;

namespace twitterXcrypto.data;

internal class MySqlClient : IAsyncDisposable, IDisposable
{
    private readonly MySqlConnection _con;

    internal MySqlClient(string serverIp, int? port, string dbName, string username, string password)
    {
        if (string.IsNullOrEmpty(serverIp) || string.IsNullOrEmpty(dbName) || string.IsNullOrEmpty(username))
            throw new ArgumentException("Bad connection details");

        StringBuilder sb = new();
        sb.Append($"server={serverIp};database={dbName};userid={username};");

        if (!string.IsNullOrEmpty(password)) sb.Append($"password={password};");
        if (port.HasValue) sb.Append($"port={port.Value};");

        _con = new(sb.ToString());
    }

    internal async Task Open()
    {
        await _con.OpenAsync();
        Log.Write($"Opened Database with {_con.ConnectionString}");
    }

    #region tweet-related

    /* describe tweet;
     * +----------------+------------+------+-----+---------------------+-------------------------------+
     * | Field          | Type       | Null | Key | Default             | Extra                         |
     * +----------------+------------+------+-----+---------------------+-------------------------------+
     * | time           | timestamp  | NO   | MUL | current_timestamp() | on update current_timestamp() |
     * | text           | text       | NO   |     | NULL                |                               |
     * | userid         | bigint(20) | NO   | MUL | NULL                |                               |
     * | containsImages | tinyint(1) | NO   |     | NULL                |                               |
     * | id             | bigint(20) | NO   | PRI | NULL                | auto_increment                |
     * +----------------+------------+------+-----+---------------------+-------------------------------+
     */

    internal async Task WriteTweet(Tweet tweet)
    {
        Task userEnsurer = EnsureUserExists(tweet.User);

        await using MySqlCommand cmd = _con.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = $"insert into tweet(time, text, userid, containsImages) values {tweet.ToSqlTuple()};";

        try
        {
            await userEnsurer;
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            Log.Write("Could not insert tweet into db", e);
        }
    }

    /* describe mentions;
     * +-----------+-------------+------+-----+---------+-------+
     * | Field     | Type        | Null | Key | Default | Extra |
     * +-----------+-------------+------+-----+---------+-------+
     * | tweetid   | bigint(20)  | NO   | PRI | NULL    |       |
     * | crypto    | varchar(32) | NO   | PRI | NULL    |       |
     * | price     | double      | YES  |     | NULL    |       |
     * | change1h  | double      | YES  |     | NULL    |       |
     * | change24h | double      | YES  |     | NULL    |       |
     * | change7d  | double      | YES  |     | NULL    |       |
     * | change30d | double      | YES  |     | NULL    |       |
     * | change60d | double      | YES  |     | NULL    |       |
     * | change90d | double      | YES  |     | NULL    |       |
     * +-----------+-------------+------+-----+---------+-------+
     */
    internal async Task EnrichTweet(Tweet tweet, IEnumerable<CoinmarketcapClient.Asset> assets)
    {
        if (assets.Empty())
            return;

        long? tweetId = await GetTweetId(tweet);

        if (tweetId is null)
            return;

        var tasks = assets.Select(ass => EnsureAssetExists(ass));
        await Task.WhenAll(tasks);

        StringBuilder sb = new("insert into mentions values ");
        foreach (var ass in assets)
        {
            sb.Append($"({tweetId},'{ass.Symbol.Sanitize()}',{ass.Price}");
            CoinmarketcapClient.Asset.Change.Intervals.ForEach(interval => sb.Append($",{ass.PercentChange[interval]}"));
            sb.Append("),");
        }
        sb.Remove(sb.Length - 1, 1); // remove last ','
        sb.Append(';');

        await using MySqlCommand insertMentions = _con.CreateCommand();
        insertMentions.CommandType = CommandType.Text;
        insertMentions.CommandText = sb.ToString();

        try
        {
            await insertMentions.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Log.Write($"Could not insert into mentions:\nQuery:\t\t{sb}", ex);
        }
    }

    /* describe image;
     * +---------+--------------+------+-----+---------+-------+
     * | Field   | Type         | Null | Key | Default | Extra |
     * +---------+--------------+------+-----+---------+-------+
     * | tweetid | bigint(20)   | NO   | PRI | NULL    |       |
     * | path    | varchar(255) | NO   |     | NULL    |       |
     * | name    | varchar(64)  | NO   | PRI | NULL    |       |
     * +---------+--------------+------+-----+---------+-------+
     */
    internal async Task EnrichTweet(Tweet tweet, IAsyncEnumerable<Image> images, DirectoryInfo imageDir)
    {
        if (!await images.AnyAsync())
            return;

        long? tweetId = await GetTweetId(tweet);

        if (tweetId is null)
            return;

        if (!imageDir.Exists)
            throw new DirectoryNotFoundException($"{imageDir.FullName} does not exist");

        StringBuilder insertImageQuery = new("insert into image values");
        await foreach (var image in images)
        {
            string path = image.Save(imageDir);
            insertImageQuery.AppendFormat("({0},'{1}','{2}'),", tweetId, path, image.Name);
        }
        insertImageQuery.Remove(insertImageQuery.Length - 1, 1); // remove last ','
        insertImageQuery.Append(';');

        await using MySqlCommand insertImage = _con.CreateCommand();
        insertImage.CommandType = CommandType.Text;
        insertImage.CommandText = insertImageQuery.ToString();

        try
        {
            await insertImage.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            Log.Write($"Could not insert into images with query {insertImageQuery}", e);
        }
    }

    /* describe asset;
     * +--------+-------------+------+-----+---------+-------+
     * | Field  | Type        | Null | Key | Default | Extra |
     * +--------+-------------+------+-----+---------+-------+
     * | symbol | varchar(32) | NO   | PRI | NULL    |       |
     * | name   | text        | NO   |     | NULL    |       |
     * +--------+-------------+------+-----+---------+-------+
     */
    internal async Task EnsureAssetExists(CoinmarketcapClient.Asset asset)
    {
        await using MySqlCommand cmd = _con.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = $"insert into asset values ('{asset.Symbol.Sanitize()}','{asset.Name.Sanitize()}')";

        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch { }
    }

    private async Task<long?> GetTweetId(Tweet tweet)
    {
        await using MySqlCommand idQuery = _con.CreateCommand();
        idQuery.CommandType = CommandType.Text;
        idQuery.CommandText = $"select id from tweet where time = '{tweet.Timestamp.ToSql()}' and userid = {tweet.User.Id}";

        object? data;
        try
        {
            data = await idQuery.ExecuteScalarAsync();
        }
        catch (Exception e)
        {
            Log.Write($"Could not retrieve id from tweet \"{tweet}\"", e);
            return null;
        }

        if (data is null)
        {
            Log.Write($"Tweet \"{tweet}\" did not exist in database");
            return null;
        }

        return (long)data;
    }

    #endregion

    #region user-related

    /* describe user;
     * +-------+--------------+------+-----+---------+-------+
     * | Field | Type         | Null | Key | Default | Extra |
     * +-------+--------------+------+-----+---------+-------+
     * | name  | varchar(255) | NO   |     | NULL    |       |
     * | id    | bigint(20)   | NO   | PRI | NULL    |       |
     * +-------+--------------+------+-----+---------+-------+
     */
    private async Task EnsureUserExists(User user)
    {
        await using MySqlCommand cmd = _con.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = $"insert into user values {user.ToSqlTuple()};";

        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch { }
    }

    #endregion

    #region interfaces

    public void Dispose()
    {
        _con.Close();
        _con.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await _con.CloseAsync();
        await _con.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    #endregion
}
