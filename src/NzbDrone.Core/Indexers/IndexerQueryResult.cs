using System;
using System.Collections.Generic;
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
        public long ElapsedTime { get; set; }
        public int StatusCode { get; set; }
    }
}
