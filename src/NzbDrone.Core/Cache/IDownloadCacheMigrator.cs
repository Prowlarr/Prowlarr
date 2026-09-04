namespace NzbDrone.Core.Cache
{
    public interface IDownloadCacheMigrator
    {
        void Migrate();
    }
}
