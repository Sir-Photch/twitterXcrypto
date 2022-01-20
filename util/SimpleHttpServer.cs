using System.Net;

namespace twitterXcrypto.util
{
    /*
     * https://linvi.github.io/tweetinvi/dist/account-activity/account-activity-with-http-server
     */
    internal class SimpleHttpServer : IDisposable
    {
        #region base field
        private readonly HttpListener _server;
        #endregion

        #region event OnRequest
        public EventHandler<HttpListenerContext>? OnRequest;
        #endregion

        #region ctor
        public SimpleHttpServer(int port)
        {
            _server = new HttpListener();
            _server.Prefixes.Add("http://*:" + port + "/");
        }
        #endregion

        #region methods
        public void Start()
        {
            _server.Start();
#pragma warning disable CS4014
            RunServerAsync(); // do not await
#pragma warning restore CS4014
        }

        public void Stop()
        {
            _server.Stop();
        }

        private async Task RunServerAsync()
        {
            while (_server.IsListening)
            {
                var context = await _server.GetContextAsync();
                OnRequest?.Invoke(this, context);
            }
        }

        public async Task WaitUntilDisposed()
        {
            while (!_disposed)
            {
                await Task.Delay(200);
            }
        }

        private bool _disposed;
        public void Dispose()
        {
            _disposed = true;
            ((IDisposable)_server)?.Dispose();
        }
        #endregion
    }
}
