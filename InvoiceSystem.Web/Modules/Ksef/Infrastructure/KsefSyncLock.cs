using System.Threading;
using System.Threading.Tasks;

namespace InvoiceSystem.Web.Modules.Ksef.Infrastructure
{
    public interface IKsefSyncLock
    {
        Task<bool> TryAcquireAsync(CancellationToken cancellationToken);
        void Release();
    }

    public sealed class KsefSyncLock : IKsefSyncLock
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public Task<bool> TryAcquireAsync(CancellationToken cancellationToken)
        {
            return _semaphore.WaitAsync(0, cancellationToken);
        }

        public void Release()
        {
            try
            {
                _semaphore.Release();
            }
            catch (System.ObjectDisposedException) {}
            catch (System.Threading.SemaphoreFullException) {}
        }
    }
}
