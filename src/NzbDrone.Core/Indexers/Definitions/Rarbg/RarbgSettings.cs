using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;

namespace NzbDrone.Core.Indexers.Rarbg
{
    public class RarbgSettings : NoAuthTorrentBaseSettings
    {
        public RarbgSettings()
        {
            RankedOnly = false;
        }

        [FieldDefinition(2, Type = FieldType.Checkbox, Label = "Ranked Only", HelpText = "Only include ranked results.")]
        public bool RankedOnly { get; set; }
    }
}
