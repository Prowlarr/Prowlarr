using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Parser.Model;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerFlagModule : ProwlarrRestModule<IndexerFlagResource>
    {
        public IndexerFlagModule()
        {
            GetResourceAll = GetAll;
        }

        private List<IndexerFlagResource> GetAll()
        {
            return Enum.GetValues(typeof(IndexerFlags)).Cast<IndexerFlags>().Select(f => new IndexerFlagResource
            {
                Id = (int)f,
                Name = f.ToString()
            }).ToList();
        }
    }
}
