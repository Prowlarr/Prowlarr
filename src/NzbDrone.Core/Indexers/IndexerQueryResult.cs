using System.Collections.Generic;
using NzbDrone.Common.Http;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers
{
    public class IndexerQueryResult
    {
        public IndexerQueryResult()
        {
            Releases = new List<ReleaseInfo>();
        }

        public IList<ReleaseInfo> Releases { get; set; }
        public HttpResponse Response { get; set; }
        public bool Cached { get; set; }
    }
}
