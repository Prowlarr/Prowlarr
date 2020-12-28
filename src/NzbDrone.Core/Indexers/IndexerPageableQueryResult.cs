using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers
{
    public class IndexerPageableQueryResult
    {
        public IndexerPageableQueryResult()
        {
            Releases = new List<ReleaseInfo>();
            Queries = new List<IndexerQueryResult>();
        }

        public IList<ReleaseInfo> Releases { get; set; }
        public IList<IndexerQueryResult> Queries { get; set; }
    }
}
