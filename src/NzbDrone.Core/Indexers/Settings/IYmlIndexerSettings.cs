using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NzbDrone.Core.Indexers.Settings
{
    public interface IYmlIndexerSettings : IIndexerSettings
    {
        public string DefinitionFile { get; set; }
    }
}
