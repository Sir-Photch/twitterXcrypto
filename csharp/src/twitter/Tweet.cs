using System.Text;
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
        public bool ContainsImages { get; init; }
        private Uri[]? PictureUris { get; init; }

        public async IAsyncEnumerable<Image> GetImages()
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

        public string ToString(
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
    }
}
