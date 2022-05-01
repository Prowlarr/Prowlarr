using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.TorrentPotato
{
    public class TorrentPotatoSettings : NoAuthTorrentBaseSettings
    {
        public TorrentPotatoSettings()
        {
        }

        [FieldDefinition(2, Label = "Username", HelpText = "Indexer Username", Privacy = PrivacyLevel.UserName)]
        public string User { get; set; }

        [FieldDefinition(3, Label = "Passkey", HelpText = "Indexer Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Passkey { get; set; }
    }
}
