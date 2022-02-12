using System.Text;
using System.Data;
using MySql.Data.MySqlClient;
using twitterXcrypto.twitter;
using twitterXcrypto.util;

namespace twitterXcrypto.data
{
    internal class MySqlClient : IAsyncDisposable, IDisposable
    {
        private readonly MySqlConnection _con;

        internal MySqlClient(string serverIp, int? port, string dbName, string username, string password)
        {
            if (string.IsNullOrEmpty(serverIp) || string.IsNullOrEmpty(dbName) || string.IsNullOrEmpty(username))
                throw new ArgumentException("Bad connection details");

            StringBuilder sb = new();
            sb.Append($"server={serverIp};database={dbName};userid={username};");

            if (!string.IsNullOrEmpty(password))    sb.Append($"password={password};");
            if (port.HasValue)                      sb.Append($"port={port.Value};");

            _con = new(sb.ToString());
        }

        internal async Task Open()
        {
            await _con.OpenAsync();
            Log.Write($"Opened Database with {_con.ConnectionString}");
        }

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
            await EnsureUserExists(tweet.User);

            using MySqlCommand cmd = _con.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = $"insert into tweet(time, text, userid, containsImages) " +
                              $"values ('{tweet.Timestamp.UtcDateTime:yyyy'-'MM'-'dd' 'HH':'mm':'ss}', '{Sanitize(tweet.Text)}', {tweet.User.Id}, {tweet.ContainsImages});";

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                Log.Write("Could not insert tweet into db", e);
            }
        }

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
            using MySqlCommand cmd = _con.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = $"insert into user values ('{Sanitize(user.Name)}',{user.Id});";

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                Log.Write($"Error inserting {user} into db", e, Log.Level.WRN);
            }
        }

        private static string Sanitize(ReadOnlySpan<char> input)
        {
            StringBuilder sb = new();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] is '\'' or '\"')
                    continue;

                sb.Append(input[i]);
            }

            return sb.ToString();
        }

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
}
