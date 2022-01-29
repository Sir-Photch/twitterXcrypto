using System.Xml;
using System.Text;
using System.Xml.Linq;
using System.Globalization;
using System.Runtime.Serialization.Json;
using twitterXcrypto.util;


namespace twitterXcrypto.crypto;

internal class CoinmarketcapClient
{
    private const string URL = "https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest";
    private readonly string _apiKey;
    private readonly HashSet<Asset> _assets = new();

    internal readonly struct Asset : IEquatable<Asset>
    {
        internal string Name { get; init; }
        internal string Symbol { get; init; }
        internal double Price { get; init; }
        internal double PercentChange1h { get; init; }

        public override string ToString() => ToString(false);

        public string ToString(bool withLineBreaks)
        {
            StringBuilder sb = new();

            sb.Append($"[ {Symbol} | {Name} ]: ");
            if (withLineBreaks)
                sb.Append($"{Environment.NewLine}\t");
            sb.Append($"Price: {Price}USD, ");
            if (withLineBreaks)
                sb.Append($"{Environment.NewLine}\t");
            sb.Append($"1h change: {PercentChange1h}%");

            return sb.ToString();
        }

        #region ops
        public override bool Equals(object? obj)
        {
            return obj is Asset asset && Equals(asset);
        }

        public bool Equals(Asset other)
        {
            return Name == other.Name &&
                   Symbol == other.Symbol;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Symbol);
        }

        public static bool operator ==(Asset left, Asset right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Asset left, Asset right)
        {
            return !(left == right);
        }
        #endregion
    }

    internal uint NumAssetsToGet { get; set; } = 10;

    internal IEnumerable<Asset> Assets
    {
        get
        {
            lock (_assets)
                return _assets;
        }
    }

    internal CoinmarketcapClient(string coinmarketcapApiKey)
    {
        _apiKey = coinmarketcapApiKey;
    }

    // may throws
    internal async Task RefreshAssets()
    {
        XDocument doc = await GetCryptos();

        if (doc?.Root?.Element("data") is null)
            return;

#pragma warning disable CS8602
        IEnumerable<XElement> items = doc.Root.Element("data").Elements("item");

        if (items.Empty())
            return;

        await Parallel.ForEachAsync(items.Chunk(Environment.ProcessorCount), (chunk, token) =>
        {
            if (token.IsCancellationRequested)
                return ValueTask.FromCanceled(token);

            foreach (var item in chunk)
            {
                string name, symbol;
                double price, percentChange1h;
                try
                {
                    name = item.Element("name").Value;
                    symbol = item.Element("symbol").Value;

                    NumberStyles parseStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

                    XElement? quote = item.Element("quote").Element("USD");

                    string priceString = quote.Element("price").Value;
                    string percentString = quote.Element("percent_change_1h").Value;

                    price = double.Parse(priceString, parseStyle, CultureInfo.InvariantCulture);
                    percentChange1h = double.Parse(percentString, parseStyle, CultureInfo.InvariantCulture);
#pragma warning restore CS8602
                }
                catch (Exception ex)
                {
                    return ValueTask.FromException(ex);
                }

                lock (_assets)
                    _assets.AddReplace(new Asset { Name = name, Symbol = symbol, PercentChange1h = percentChange1h, Price = price });
            }

            return ValueTask.CompletedTask;
        });
    }

    private async Task<XDocument> GetCryptos()
    {
        UriBuilder uriBuilder = new(URL);
        uriBuilder.Query = $"start=1&limit={NumAssetsToGet}&convert=USD";

        using HttpClient client = new();
        using HttpRequestMessage request = new(new HttpMethod("GET"), uriBuilder.Uri);
        var succ = request.Headers.TryAddWithoutValidation("Accepts", "application/json");
        var zucc = request.Headers.TryAddWithoutValidation("X-CMC_PRO_API_KEY", _apiKey);

        HttpResponseMessage response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode(); // may throw

        Stream responseStream = await response.Content.ReadAsStreamAsync();

        return XDocument.Load(
            JsonReaderWriterFactory.CreateJsonReader(
                responseStream, new XmlDictionaryReaderQuotas()));
    }
}
