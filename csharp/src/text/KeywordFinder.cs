using System.Collections.Concurrent;

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

    internal string[] Match(string text)
    {
        ConcurrentBag<string> result = new();

        Parallel.ForEach(_keywordMap.Keys.Chunk(Environment.ProcessorCount), 
        keywordChunk =>
        {
            for (int i = 0; i < keywordChunk.Length; i++)
            {
                if (text.ToLower().Contains(keywordChunk[i]))
                {
                    result.Add(_keywordMap[keywordChunk[i]]);
                }
            }
        });

        return result.ToArray();
    }
}
