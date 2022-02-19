using Emgu.CV;
using Emgu.CV.CvEnum;
using System;

namespace twitterXcrypto.imaging;

internal class Image
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
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        stream.Position = 0L;
        ReadOnlySpan<byte> bytes = CvInvoke.Imencode(".png", Mat);

        stream.Write(bytes);
    }

    internal async Task SaveAsync(Stream stream, CancellationToken token = default)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        stream.Position = 0L;
        ReadOnlyMemory<byte> bytes = CvInvoke.Imencode(".png", Mat);

        await stream.WriteAsync(bytes, token);
    }
}

