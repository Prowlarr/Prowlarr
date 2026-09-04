using System.Data;

namespace NzbDrone.Core.Cache
{
    public interface ISqliteCacheDatabase
    {
        IDbConnection OpenConnection();
        void Initialize();
    }
}
