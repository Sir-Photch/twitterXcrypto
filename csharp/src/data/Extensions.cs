using System.Text;
using twitterXcrypto.twitter;

namespace twitterXcrypto.data
{
    internal static class Extensions
    {
        internal static string Sanitize(this string input)
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

        internal static string ToSql(this DateTimeOffset dateTimeOffset) => dateTimeOffset.UtcDateTime.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");

        /*
         * insert into tweet (time, text, userid, containsImages)
         */
        internal static string ToSql(this Tweet tweet) => $"('{tweet.Timestamp.ToSql()}', '{tweet.Text.Sanitize()}', {tweet.User.Id}, {tweet.ContainsImages})";

        /*
         * insert into user (name, id)
         */
        internal static string ToSql(this User user) => $"('{user.Name.Sanitize()}', {user.Id})";

    }
}
