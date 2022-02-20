using Microsoft.VisualStudio.TestTools.UnitTesting;
using twitterXcrypto.imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;

namespace twitterXcrypto_tests.imaging;

[TestClass]

#pragma warning disable CS8604, CS8602 // nullreference
public class ImagingTests
{
    private static FileStream? _sourceStream;
    private static readonly Size _size = new(50, 50);
    private static TestContext? _context;
    private static DirectoryInfo? _TestDir => new(_context.TestDir);

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _context = context;
        using Image<Rgb, byte> source = new(_size);
        string sourceFilePath = Path.Combine(context.TestDir, "foo.bmp");
        source.Save(sourceFilePath);
        _sourceStream = File.OpenRead(sourceFilePath);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        string? filePath = _sourceStream?.Name;
        _sourceStream?.Dispose();
        if (filePath is not null)
            File.Delete(filePath);
    }

    [TestInitialize]
    public void TestInit()
    {
        if (_sourceStream is null)
            Assert.Inconclusive("Could not create fileStream");
    }

    [TestMethod]
    public void CtorTest()
    {
        using Image image = new(_sourceStream, "bar");

        Assert.AreNotEqual(IntPtr.Zero, image.Mat.Ptr);
        Assert.AreEqual(_size, image.Mat.Size);
    }

    [TestMethod]
    public void SaveToFileTest()
    {
        using Image image = new(_sourceStream, "bar");

        string path = image.Save(_TestDir);
        FileInfo fi = new(path);

        Assert.IsTrue(fi.Exists);
        Assert.AreEqual(".png", fi.Extension);
        Assert.IsTrue(fi.Length > 0);

        fi.Delete();
    }

    [TestMethod]
    public async Task SaveToFileTestAsync()
    {
        using Image image = new(_sourceStream, "bar");

        string path = await image.SaveAsync(_TestDir, _context.CancellationTokenSource.Token);
        FileInfo fi = new(path);

        Assert.IsTrue(fi.Exists);
        Assert.AreEqual(".png", fi.Extension);
        Assert.IsTrue(fi.Length > 0);

        fi.Delete();
    }

    [TestMethod]
    public void SaveToStreamTest()
    {
        using Image image = new(_sourceStream, "bar");
        using MemoryStream ms = new();

        image.Save(ms);

        Assert.IsTrue(ms.Length > 0);
    }

    [TestMethod]
    public async Task SaveToStreamTestAsync()
    {
        using Image image = new(_sourceStream, "bar");
        using MemoryStream ms = new();

        await image.SaveAsync(ms, _context.CancellationTokenSource.Token);

        Assert.IsTrue(ms.Length > 0);
    }
}