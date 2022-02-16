using Emgu.CV.OCR;
using twitterXcrypto.util;

namespace twitterXcrypto.imaging
{
    internal class OCR : IDisposable
    {
        internal static async Task<OCR> InitializeAsync()
        {
            if (_instance is not null)
                return _instance;

            if (string.IsNullOrEmpty(Whitelist))
                throw new InvalidOperationException("No whitelist provided");

            if (string.IsNullOrWhiteSpace(Locale))
                throw new InvalidOperationException("No locale provided");

            await CreateWorkingDirectory();

            return _instance = new();
        }

        internal static string? Whitelist { get; set; }

        internal static string Locale { get; set; } = "eng";

        internal Task<string> GetText(Image image) => Task.Run(() =>
        {
            _ocr.SetImage(image.Mat);
            return _ocr.Recognize() is 0 ? _ocr.GetUTF8Text() : string.Empty;
        });

        public void Dispose()
        {
            _ocr.Dispose();
            _instance = null;
            GC.SuppressFinalize(this);
        }

        #region private

        private static OCR? _instance;
        private readonly Tesseract _ocr;
        private static string? _tesseractDir;

        private OCR() => _ocr = new Tesseract(_tesseractDir, Locale, OcrEngineMode.TesseractLstmCombined, Whitelist);

        private static async Task CreateWorkingDirectory()
        {
            DirectoryInfo tessdir = Directory.CreateDirectory(Tesseract.DefaultTesseractDirectory);
            _tesseractDir = tessdir.FullName;

            var downloaders = Locale.Split('+').Where(locale => !locale.StartsWith('~')).Select(locale =>
            Task.Run(async () =>
            {
                string url = Tesseract.GetLangFileUrl(locale);
                string trainedDataFileName = url.Split('/').Last().Split('?').First();

                if (tessdir.IsEmpty() || !tessdir.ContainsFile(trainedDataFileName))
                {
                    try
                    {
                        using HttpClient client = new();
                        await using Stream download = await client.GetStreamAsync(url);

                        await using FileStream fs = new(Path.Combine(_tesseractDir, trainedDataFileName), FileMode.Create);

                        await download.CopyToAsync(fs);

                        Log.Write($"Downloaded tesseract-data for {locale} from {url}");
                    }
                    catch (Exception e)
                    {
                        Log.Write($"Could not download tesseract-data for {locale} from {url}", e, Log.Level.FTL);
                        throw;
                    }
                }
            }));

            await Task.WhenAll(downloaders);
        }

        #endregion
    }
}
