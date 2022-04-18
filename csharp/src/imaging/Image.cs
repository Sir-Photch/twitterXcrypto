using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("twitterXcrypto_tests")]

namespace twitterXcrypto.imaging;

internal class Image : IDisposable
{
    internal Mat Mat { get; } = new();

    internal string Name { get; }

    internal Image(Stream stream, string name)
    {
        Name = name + ".png";
        byte[] buffer = new byte[stream.Length];
        stream.Position = 0;
        stream.Read(buffer);

        CvInvoke.Imdecode(buffer, ImreadModes.Color, Mat);
    }

    internal string Save(DirectoryInfo directory)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Mat));

        if (!directory.Exists)
            throw new ArgumentException("Bad directory");

        string path = Path.Combine(directory.FullName, Name);
        Mat.Save(path);
        return path;
    }

    internal Task<string> SaveAsync(DirectoryInfo directory, CancellationToken token = default)
        => Task.Run(() => Save(directory), token);

    internal void Save(Stream stream)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Mat));

        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        stream.Position = 0L;
        ReadOnlySpan<byte> bytes = CvInvoke.Imencode(".png", Mat);

        stream.Write(bytes);
    }

    internal async Task SaveAsync(Stream stream, CancellationToken token = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Mat));

        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        stream.Position = 0L;
        ReadOnlyMemory<byte> bytes = CvInvoke.Imencode(".png", Mat);

        await stream.WriteAsync(bytes, token);
    }

    #region IDisposable
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            Mat.Dispose();
        }

        _disposed = true;
    }

    ~Image()
    {
        Dispose(false);
    }
    #endregion
}

