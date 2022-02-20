using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

[assembly: InternalsVisibleTo("twitterXcrypto_tests")]

namespace twitterXcrypto.text;

internal class KeywordFinder
{
    private readonly ConcurrentDictionary<string, string> _keywordMap = new();

    internal void ReadKeywords(IEnumerable<(string Key, string Val)> pairs)
    {
        foreach (var kvPair in pairs)
        {
            _keywordMap.TryAdd(kvPair.Key, kvPair.Val);
        }
    }

    internal IReadOnlySet<string> Match(string text)
    {
        HashSet<string> matches = new();

        Parallel.ForEach(_keywordMap.Chunk(Environment.ProcessorCount), 
        chunk =>
        {
            foreach (var kvp in chunk)
            {
                string lower = text.ToLowerInvariant();
                if (lower.Contains(kvp.Key.ToLowerInvariant()) 
                 || lower.Contains(kvp.Value.ToLowerInvariant()))
                {
                    lock (matches)
                        matches.Add(kvp.Value);
                }
            }
        });

        return matches;
    }

    internal async Task<IReadOnlySet<string>> MatchAsync(string text)
    {
        HashSet<string> matches = new();

        await Parallel.ForEachAsync(_keywordMap.Chunk(Environment.ProcessorCount), 
        (chunk, token) =>
        {
            if (token.IsCancellationRequested)
                return ValueTask.FromCanceled(token);

            foreach (var kvp in chunk)
            {
                string lower = text.ToLowerInvariant();
                if (lower.Contains(kvp.Key.ToLowerInvariant())
                 || lower.Contains(kvp.Value.ToLowerInvariant()))
                {
                    lock (matches)
                        matches.Add(kvp.Value);
                }
            }

            return ValueTask.CompletedTask;
        });

        return matches;
    }
}
