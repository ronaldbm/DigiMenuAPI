using System.Collections.Concurrent;

namespace DigiMenuAPI.Application.Services
{
    /// <summary>
    /// Singleton que garantiza que solo una importación masiva se ejecute
    /// a la vez por tenant (CompanyId). Previene race conditions, duplicados
    /// y corrupción de DisplayOrder.
    /// </summary>
    public class ImportLockService
    {
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> Locks = new();

        public SemaphoreSlim GetLock(int companyId)
            => Locks.GetOrAdd(companyId, _ => new SemaphoreSlim(1, 1));
    }
}
