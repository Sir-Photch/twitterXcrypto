using Emgu.CV;
using Emgu.CV.CvEnum;

namespace twitterXcrypto.imaging
{
    public class Image
    {
        internal Mat Mat { get; } = new();

        internal Image(Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(buffer);

            CvInvoke.Imdecode(buffer, ImreadModes.Color, Mat);
        }

        public void Save(string path)
        {
            if (Path.GetExtension(path) != ".png")
                throw new ArgumentException("only .png-extensions supported");

            Mat.Save(path);
        }
    }
}
