using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using twitterXcrypto.util;

[assembly: InternalsVisibleTo("twitterXcrypto_tests")]

namespace twitterXcrypto.text;

internal class KeywordFinder
{
    private readonly Regex _regex;
    private readonly Dictionary<string, string> _keywords;

    /*
     * https://stackoverflow.com/questions/71194117/c-sharp-regex-whitespace-between-capturing-groups
     */
    internal KeywordFinder(IEnumerable<(string Key, string Val)> pairs)
    {
        var orderedEscapedConcatenated = pairs.Select(p => p.Key)
                                              .Concat(pairs.Select(p => p.Val))
                                              .Distinct()
                                              .OrderByDescending(s => s.Length)
                                              .Select(Regex.Escape);

        _keywords = new(pairs.Select(p => new KeyValuePair<string, string>(p.Key, p.Val)));

        _regex = new Regex($@"(?!\B\w)(?:{string.Join('|', orderedEscapedConcatenated)})(?<!\w\B)",
                           RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
    }

    internal IReadOnlySet<string> Match(string text)
    {
        if (_regex is null)
            return new HashSet<string>();

        HashSet<string> retval = new();

        MatchCollection matches = _regex.Matches(text);

        for (int i = 0; i < matches.Count; i++)
        {
            string trimmed = TrimAndLower(matches[i].Value);

            var keysInDictionary = _keywords.Keys.Where(k => trimmed == k.ToLowerInvariant());
            var valInDictionary = _keywords.Values.Where(k => trimmed == k.ToLowerInvariant());

            if (keysInDictionary.Any())
                retval.Add(_keywords[keysInDictionary.First()]);
            else if (valInDictionary.Any())
                retval.Add(valInDictionary.First());
        }

        return retval;
    }

    private static string TrimAndLower(ReadOnlySpan<char> s)
    {
        var noWhiteSpace = s.Trim();
        if (!char.IsLetter(noWhiteSpace[0]))
            noWhiteSpace = noWhiteSpace[1..];
        if (!char.IsLetter(noWhiteSpace[^1]))
            noWhiteSpace = noWhiteSpace[0..^1];

        Span<char> lower = stackalloc char[noWhiteSpace.Length];
        noWhiteSpace.ToLowerInvariant(lower);
        return new string(lower);
    }
}
