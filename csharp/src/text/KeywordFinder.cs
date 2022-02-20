using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using twitterXcrypto.util;

[assembly: InternalsVisibleTo("twitterXcrypto_tests")]

namespace twitterXcrypto.text;

internal class KeywordFinder
{
    private Regex? _regex = null;
    private Dictionary<string, string> _keywords = new();

    /*
     * https://stackoverflow.com/questions/71194117/c-sharp-regex-whitespace-between-capturing-groups
     */
    internal KeywordFinder(IEnumerable<(string Key, string Val)> pairs)
    {
        var concatenatedPairs = pairs.Select(p => p.Key)
                                     .Concat(pairs.Select(p => p.Val))
                                     .Distinct()
                                     // strings should already be escaped via Regex.Escape
                                     //.Where(s => s.All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)))
                                     .OrderBy(s => s, StringComparer.OrdinalIgnoreCase);

        pairs.ForEach(p => _keywords[p.Key] = p.Val);

        _regex = new Regex($@"\b(?:{string.Join('|', concatenatedPairs.Select(Regex.Escape))})\b",
                           RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    internal IReadOnlySet<string> Match(string text)
    {
        if (_regex is null)
            return new HashSet<string>();

        HashSet<string> retval = new();

        MatchCollection matches = _regex.Matches(text);

        for (int i = 0; i < matches.Count; i++)
        {
            string trimmed = Trim(matches[i].Value);

            if (_keywords.ContainsKey(trimmed))
                retval.Add(_keywords[trimmed]);
            else
                retval.Add(trimmed);
        }

        return retval;
    }

    private static string Trim(ReadOnlySpan<char> s)
    {
        var noWhiteSpace = s.Trim();
        if (!char.IsLetter(noWhiteSpace[0]))
            noWhiteSpace = noWhiteSpace[1..];
        if (!char.IsLetter(noWhiteSpace[^1]))
            noWhiteSpace = noWhiteSpace[0..^1];
        return new string(noWhiteSpace);
    }
}
