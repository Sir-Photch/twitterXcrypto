using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using twitterXcrypto.crypto;
using twitterXcrypto.text;

namespace twitterXcrypto_tests.text;

[TestClass]
public class KeywordFinderTests
{
    [TestMethod]
    public void ReadKeywordsMatchTest()
    {
        (string Key, string Val)[] pairs =
        {
            ("foo", "foobar"),
            ("bar", "foobar"),
            ("baz", "foobaz")
        };

        KeywordFinder finder = new(pairs);

        var foobar = finder.Match("foo");
        Assert.IsTrue(foobar.Contains("foobar"));

        foobar = finder.Match("bar");
        Assert.IsTrue(foobar.Contains("foobar"));

        foobar = finder.Match("foo bar");
        Assert.IsTrue(foobar.Contains("foobar"));
        Assert.IsTrue(foobar.Count is 1);

        foobar = finder.Match("foo bar baz");
        Assert.IsTrue(foobar.Contains("foobar") &&
                      foobar.Contains("foobaz"));
        Assert.IsTrue(foobar.Count is 2);
    }

    [TestMethod]
    public async Task ReadKeywordsFromCoinmarketcapMatchTestAsync()
    {
        string? apiKey = Environment.GetEnvironmentVariable("XCMC_PRO_API_KEY");

        if (apiKey is null)
            Assert.Inconclusive("Coinmarketcap-Key not defined in environment");

        CoinmarketcapClient client = new(apiKey);
        client.NumAssetsToGet = 100u;
        await client.RefreshAssetsAsync();

        KeywordFinder finder = new(client.Assets.Select(ass => (ass.Symbol, ass.Name)));

        var matches = finder.Match("#Tether EthbtcBitcoinbtcdoge USDT +BNB-");

        Assert.IsTrue(matches.Contains("Tether"));
        Assert.IsTrue(matches.Contains("BNB"));
        Assert.IsFalse(matches.Contains("Ethereum"));
        Assert.IsFalse(matches.Contains("Bitcoin"));
        Assert.IsFalse(matches.Contains("Dogecoin"));
    }
}