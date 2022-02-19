using System.Text;
using Tweetinvi.Models;
using twitterXcrypto.data;
using twitterXcrypto.data.interfaces;
using twitterXcrypto.imaging;
using twitterXcrypto.util;

namespace twitterXcrypto.twitter;

internal struct Tweet : ISqlSerializable
{
    internal DateTimeOffset Timestamp { get; init; }
    internal string Text { get; init; }
    internal User User { get; init; }
    internal bool ContainsImages { get; init; }
    internal Uri[]? PictureUris { get; init; }

    internal async IAsyncEnumerable<Image> GetImagesAsync()
    {
        if (PictureUris is null || PictureUris.Empty())
            throw new InvalidOperationException("Tweet contains no pictures");

        using HttpClient client = new();

        foreach (var uri in PictureUris)
        {
            string imageName = "image";
            if (uri.Segments.Any())
            {
                imageName = Path.GetFileNameWithoutExtension(uri.Segments.Last());
            }

            using var response = await client.GetAsync(uri);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch { continue; }

            using Stream s = await response.Content.ReadAsStreamAsync();

            yield return new Image(s, imageName);
        }
    }

    public override string ToString() => ToString(true, true, @" \n ");

    internal string ToString(
        bool replaceLineEndings = false,
        bool prependUser = false,
        string lineEndingReplacement = " ")
    {
        StringBuilder sb = new();
        if (prependUser)
            sb.Append($"[{User.Name}]:{Environment.NewLine}");

        sb.Append($"\"{(replaceLineEndings ? Text.ReplaceLineEndings(lineEndingReplacement) : Text)}\"");
        return sb.ToString();
    }

    internal static Tweet FromITweet(ITweet tweet)
    {
        bool containsPics = tweet.Media?.Any(ent => ent.MediaType == "photo") ?? false;

        return new Tweet
        {
            User = new User
            {
                Id = tweet.CreatedBy.Id,
                Name = tweet.CreatedBy.Name
            },
            Text = tweet.Text,
            Timestamp = tweet.CreatedAt,
            ContainsImages = containsPics,
#pragma warning disable CS8604
            PictureUris = containsPics ? tweet.Media.Select(entry => new Uri(entry.MediaURLHttps)).ToArray() : null
#pragma warning restore CS8604
        };
    }

    /*
     * insert into tweet (time, text, userid, containsImages)
     */
    public string ToSqlTuple() => $"('{Timestamp.ToSql()}', '{Text.Sanitize()}', {User.Id}, {ContainsImages})";
}
