using Emgu.CV.OCR;

namespace twitterXcrypto.imaging
{
    internal class OCR : IDisposable
    {
        internal static OCR Instance => _instance ??= new();

        internal static string? Whitelist { get; set; }

        internal async Task<string> GetText(Image image) => await Task.Run(() =>
        {
            _ocr.SetImage(image.Mat);
            return _ocr.Recognize() is 0 ? _ocr.GetUTF8Text() : string.Empty;
        });

        public void Dispose() => _ocr.Dispose();

        #region private

        private static OCR? _instance;
        private readonly Tesseract _ocr;
        private static readonly string DATA_PATH = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, nameof(OCR))).FullName;

        private OCR()
        {
            if (string.IsNullOrEmpty(Whitelist))
                throw new InvalidOperationException("No whitelist provided");

            _ocr = new Tesseract(DATA_PATH, "eng", OcrEngineMode.TesseractLstmCombined, Whitelist);
        }

        #endregion
    }
}
