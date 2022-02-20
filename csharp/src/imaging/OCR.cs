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

            await CreateWorkingDirectoryAsync();

            return _instance = new();
        }

        internal static string? Whitelist { get; set; }

        internal static string Locale { get; set; } = "eng";

        internal Task<string> GetTextAsync(Image image) => Task.Run(() =>
        {
            _ocr.SetImage(image.Mat);
            return _ocr.Recognize() is 0 ? _ocr.GetUTF8Text() : string.Empty;
        });

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _ocr.Dispose();
                _instance = null;
            }

            _disposed = true;
        }

        ~OCR() => Dispose(false);

        #endregion

        #region private

        private static OCR? _instance;
        private readonly Tesseract _ocr;
        private static string? _tesseractDir;

        private OCR() 
        { 
            _ocr = new Tesseract(_tesseractDir, Locale, OcrEngineMode.TesseractLstmCombined, Whitelist);
            Log.Write($"Initialized Tesseract OCR for locale {Locale}");
        }

        private static async Task CreateWorkingDirectoryAsync()
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
                        await Log.WriteAsync($"Downloading ocr-data for locale {locale} from {url} ...");
                        using HttpClient client = new();
                        await using Stream download = await client.GetStreamAsync(url);

                        await using FileStream fs = new(Path.Combine(_tesseractDir, trainedDataFileName), FileMode.Create);

                        await download.CopyToAsync(fs);

                        await Log.WriteAsync($"Download of locale {locale} finished");
                    }
                    catch (Exception e)
                    {
                        await Log.WriteAsync($"Could not download tesseract-data for locale {locale}", e, Log.Level.FTL);
                        throw;
                    }
                }
            }));

            await Task.WhenAll(downloaders);
        }

        #endregion
    }
}
