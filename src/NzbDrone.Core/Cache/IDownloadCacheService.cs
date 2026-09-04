using System.Threading.Tasks;

namespace NzbDrone.Core.Cache
{
    public interface IDownloadCacheService
    {
        bool IsEnabled { get; }

        Task<byte[]> Get(string key);

        Task Store(string key, byte[] value, string fileName);

        void Cleanup();
    }
}
