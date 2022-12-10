using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NzbDrone.Core.Download
{
    public class DownloadClientCategory
    {
        public string ClientCategory { get; set; }
        public List<int> Categories { get; set; }
    }
}
