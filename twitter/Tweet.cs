using Tweetinvi.Models;
using twitterXcrypto.imaging;
using twitterXcrypto.util;

namespace twitterXcrypto.twitter
{
    public struct Tweet
    {
        public DateTimeOffset Timestamp { get; init; }
        public string Text { get; init; }
        public User User { get; init; }
        public bool ContainsPictures { get; init; }
        private Uri[]? PictureUris { get; init; }

        public async IAsyncEnumerable<Image> GetImage()
        {
            if (!ContainsPictures)
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

                yield return new Image(await response.Content.ReadAsStreamAsync());
            }
        }

        public override string ToString()
        {
            return $"{User} at {Timestamp}: \"{Text}\"";
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
                ContainsPictures = containsPics,
#pragma warning disable CS8604
                PictureUris = containsPics ? tweet.Media.Select(entry => new Uri(entry.MediaURLHttps)).ToArray() : null
#pragma warning restore CS8604
            };
        }
    }
}
