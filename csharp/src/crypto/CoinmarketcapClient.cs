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
        internal class Change : IEquatable<Change>
        {
            #region private dict
            private readonly Dictionary<string, double?> _changes = new()
            {
                { Intervals.ElementAt(0), null },
                { Intervals.ElementAt(1), null },
                { Intervals.ElementAt(2), null },
                { Intervals.ElementAt(3), null },
                { Intervals.ElementAt(4), null },
                { Intervals.ElementAt(5), null }
            };
            #endregion

            internal static IReadOnlyCollection<string> Intervals { get; } = new string[] { "1h", "24h", "7d", "30d", "60d", "90d" };

            internal double? this[string interval]
            {
                get
                {
                    try { return _changes[interval]; }
                    catch (KeyNotFoundException) { return null; }
                }
                private set => _changes[interval] = value;
            }

            internal Change(double?[] changes)
            {
                if (changes.Length != Intervals.Count)
                    throw new ArgumentOutOfRangeException(nameof(changes));

                for (int i = 0; i < changes.Length; i++)
                    this[Intervals.ElementAt(i)] = changes[i];
            }

            #region ops

            public override string ToString() => ToString(false, Intervals.Count);

            public string ToString(bool lineBreaks, int first) => string.Join(lineBreaks ? $"{Environment.NewLine}\t" : " | ", _changes.Take(first).Where(kvp => kvp.Value is not null).Select(kvp => $"{kvp.Key}: {kvp.Value} %"));

            public bool Equals(Change? other) => other is Change change && Enumerable.SequenceEqual(_changes.Values, change._changes.Values);

            public override bool Equals(object? obj) => Equals(obj as Change);

            public override int GetHashCode() => HashCode.Combine(_changes.Values);

            #endregion
        }

        internal string Name { get; init; }
        internal string Symbol { get; init; }
        internal double Price { get; init; }
        internal Change PercentChange { get; init; }

        public override string ToString() => ToString(false, 2);

        public string ToString(bool withLineBreaks, int changes)
        {
            if (changes > Change.Intervals.Count)
                changes = Change.Intervals.Count;

            StringBuilder sb = new();

            sb.Append($"[ {Symbol} | {Name} ]: ");
            if (withLineBreaks)
                sb.Append($"{Environment.NewLine}\t");
            sb.Append($"Price: {Price} USD, ");
            if (withLineBreaks)
                sb.Append($"{Environment.NewLine}\t");
            sb.Append(PercentChange.ToString(withLineBreaks, changes));

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

    // may throw
    internal async Task RefreshAssetsAsync()
    {
        XDocument doc = await GetCryptosAsync();

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
                double price;
                double?[] changes = new double?[Asset.Change.Intervals.Count];
                try
                {
                    name = item.Element("name").Value;
                    symbol = item.Element("symbol").Value;

                    XElement? quote = item.Element("quote").Element("USD");

                    string priceString = quote.Element("price").Value;

                    for (int i = 0; i < Asset.Change.Intervals.Count; i++)
                    {
                        string percentString = quote.Element($"percent_change_{Asset.Change.Intervals.ElementAt(i)}").Value;
                        if (double.TryParse(percentString, NumberStyles.Float, CultureInfo.InvariantCulture, out price))
                            changes[i] = price;
                    }

                    price = double.Parse(priceString, NumberStyles.Float, CultureInfo.InvariantCulture);
#pragma warning restore CS8602
                }
                catch (Exception ex)
                {
                    return ValueTask.FromException(ex);
                }

                lock (_assets)
                    _assets.AddReplace(new Asset { Name = name, Symbol = symbol, Price = price, PercentChange = new(changes) });
            }

            return ValueTask.CompletedTask;
        });
    }

    private async Task<XDocument> GetCryptosAsync()
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
