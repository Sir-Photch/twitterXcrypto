using Emgu.CV;
using Emgu.CV.CvEnum;

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

    internal void Save(Stream stream)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        stream.Position = 0L;
        byte[] bytes = CvInvoke.Imencode(".png", Mat);

        stream.Write(bytes, 0, bytes.Length);
    }
}

