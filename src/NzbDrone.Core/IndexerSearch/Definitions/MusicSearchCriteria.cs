using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class MusicSearchCriteria : SearchCriteriaBase
    {
        public string Album { get; set; }
        public string Artist { get; set; }
        public string Label { get; set; }
    }
}
