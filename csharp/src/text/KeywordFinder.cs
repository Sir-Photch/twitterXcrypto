using System.Collections.Concurrent;

namespace twitterXcrypto.text;

/*
 *  Expected string-input:
 *  [ 'keyword1', 'value1', 'keyword2', 'value2'... ]
 *  
 */
internal class KeywordFinder
{
    private readonly ConcurrentDictionary<string, string> _keywordMap = new();

    public async Task ReadKeywordsFromFileAsync(FileInfo file)
    {
        if (!file.Exists)
            throw new FileNotFoundException("File does not exist", file.FullName);

        using StreamReader sr = file.OpenText();

        string fileString = await sr.ReadToEndAsync();

        try
        {
            // TODO come up with regex
            string[] tokens = fileString.Replace(" ", string.Empty)
                                        .Replace("[", string.Empty)
                                        .Replace("]", string.Empty)
                                        .Split('\'')
                                        .Where(str => !string.IsNullOrWhiteSpace(str) && str != ",")
                                        .ToArray();

            for (int i = 0; i < tokens.Length - 1; i += 2)
            {
                _keywordMap[tokens[i].ToLower()] = tokens[i + 1];
            }
        }
        catch (Exception e)
        {
            throw new ArgumentException("Invalid Format in File.", e);
        }
    }

    public string[] Match(string text)
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
