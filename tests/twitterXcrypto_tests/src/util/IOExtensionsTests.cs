using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using twitterXcrypto.util;

namespace twitterXcrypto_tests.util;

[TestClass]
public class IOExtensionsTests
{
    private static TestContext? _context;

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _context = context;
    }

    [TestMethod]
    public void IsEmptyTest()
    {
        DirectoryInfo testDir = new(_context.TestDir);
        testDir.Create();

        testDir.CreateSubdirectory("foo");
        Assert.IsFalse(testDir.IsEmpty());
    }

    [TestMethod]
    public void ContainsFileTest()
    {
        DirectoryInfo testDir = new(_context.TestDir);
        testDir.Create();

        FileInfo testFile = new(Path.Combine(testDir.FullName, "foo.bar"));
        using var fs = testFile.Create();

        Assert.IsFalse(testDir.ContainsFile("bar.foo"));
        Assert.IsTrue(testDir.ContainsFile("foo.bar"));
    }
}