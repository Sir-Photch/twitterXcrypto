using System.Text;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("twitterXcrypto_tests")]

namespace twitterXcrypto.data;

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
}
