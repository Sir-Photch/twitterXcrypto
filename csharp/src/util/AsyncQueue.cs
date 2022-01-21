using System.Threading.Tasks.Dataflow;

namespace twitterXcrypto.util
{
    /*
     * https://stackoverflow.com/a/55912725
     */
    internal class AsyncQueue<T> : IAsyncEnumerable<T>
    {
        private readonly SemaphoreSlim _semaphore = new(1);
        private readonly BufferBlock<T> _buffer = new();

        public bool CompleteWhenCancelled { get; init; }

        public bool Enqueue(T item) => _buffer.Post(item);

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return await _buffer.ReceiveAsync(cancellationToken);
                }
            }
            finally
            {
                if (CompleteWhenCancelled) _buffer.Complete();
                _semaphore.Release();
            }
        }
    }
}
