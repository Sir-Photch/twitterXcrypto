using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using twitterXcrypto.util;

namespace twitterXcrypto_tests.util;

[TestClass]
public class EnumerableExtensionsTests
{
    private class IntRef
    {
        internal int Int { get; set; } = 0;
        internal IntRef(int @int) => Int = @int;
    }

    [TestMethod]
    public void ForEachTest()
    {
        IntRef[] intRef = { new(0), new(1), new(2), new(3), new(4), new(5), new(6), new(7) };

        intRef.ForEach(@ref => @ref.Int += 1);

        for (int i = 0; i < intRef.Length; i++)
            Assert.IsTrue(intRef[i].Int == i + 1);
    }

    [TestMethod]
    public void EmptyTest()
    {
        string[] empty = new string[0];

        Assert.IsTrue(empty.Empty());
    }

    [TestMethod]
    public void ContainsEveryTest()
    {
        string[] values = {"foo", "bar", "baz"};
        string[] matches = {"foo", "boo", "baz"};

        Assert.IsFalse(values.ContainsEvery(matches));
        Assert.IsTrue(values.ContainsEvery(values));
    }

    [TestMethod]
    public void ContainsEveryKeyTest()
    {
        Dictionary<int, string> dict = new()
        {
            { 1, "foo" },
            { 2, "bar" },
            { 3, "baz" }
        };
        int[] keys = { 0, 1, 2, 3 };

        Assert.IsFalse(dict.ContainsEveryKey(keys));
        Assert.IsTrue(dict.ContainsEveryKey(keys[1..]));
    }
}