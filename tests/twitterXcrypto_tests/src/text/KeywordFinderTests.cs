using Microsoft.VisualStudio.TestTools.UnitTesting;
using twitterXcrypto.text;

namespace twitterXcrypto_tests.text;

[TestClass]
public class KeywordFinderTests
{
    private static readonly (string Key, string Val)[] pairs =
    {
        ("foo", "foobar"),
        ("bar", "foobar"),
        ("baz", "foobaz")
    };

    [TestMethod]
    public void ReadKeywordsMatchTest()
    {
        KeywordFinder finder = new();
        finder.ReadKeywords(pairs);

        var foobar = finder.Match("foo");
        Assert.IsTrue(foobar.Contains("foobar"));

        foobar = finder.Match("bar");
        Assert.IsTrue(foobar.Contains("foobar"));

        foobar = finder.Match("foobar");
        Assert.IsTrue(foobar.Contains("foobar"));
        Assert.IsTrue(foobar.Count is 1);

        foobar = finder.Match("foobarbaz");
        Assert.IsTrue(foobar.Contains("foobar") &&
                      foobar.Contains("foobaz"));
        Assert.IsTrue(foobar.Count is 2);
    }
}