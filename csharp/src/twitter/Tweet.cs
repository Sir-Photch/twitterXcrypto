using Tweetinvi.Models;
using twitterXcrypto.imaging;

namespace twitterXcrypto.twitter
{
    public struct Tweet
    {
        public DateTimeOffset Timestamp { get; init; }
        public string Text { get; init; }
        public User User { get; init; }
        public bool ContainsImages { get; init; }
        private Uri[]? PictureUris { get; init; }

        public async IAsyncEnumerable<Image> GetImages()
        {
            if (!ContainsImages)
                throw new InvalidOperationException("Tweet contains no pictures");

            using HttpClient client = new();

            foreach (var uri in PictureUris)
            {
                using var response = await client.GetAsync(uri);
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch { continue; }

                using Stream s = await response.Content.ReadAsStreamAsync();
                yield return new Image(s);
            }
        }

        public override string ToString()
        {
            return $"[{User}]: \"{Text.ReplaceLineEndings().Replace(Environment.NewLine, "[-nl-] ")}\"";
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
    }
}
