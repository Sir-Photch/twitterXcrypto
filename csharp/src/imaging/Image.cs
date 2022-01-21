using Emgu.CV;
using Emgu.CV.CvEnum;

namespace twitterXcrypto.imaging
{
    public class Image
    {
        internal Mat Mat { get; } = new();

        internal string Name { get; }

        internal Image(Stream stream, string name)
        {
            Name = name;
            byte[] buffer = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(buffer);

            CvInvoke.Imdecode(buffer, ImreadModes.Color, Mat);
        }

        public string Save(DirectoryInfo directory)
        {
            if (!directory.Exists)
                throw new ArgumentException("Bad directory");

            string path = Path.Combine(directory.FullName, Name + ".png");
            Mat.Save(path);
            return path;
        }
    }
}
