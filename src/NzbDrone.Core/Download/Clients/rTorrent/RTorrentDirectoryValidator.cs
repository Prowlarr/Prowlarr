using FluentValidation;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download.Clients.RTorrent;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Download.Clients.rTorrent
{
    public interface IRTorrentDirectoryValidator
    {
        ValidationResult Validate(RTorrentSettings instance);
    }

    public class RTorrentDirectoryValidator : AbstractValidator<RTorrentSettings>, IRTorrentDirectoryValidator
    {
        public RTorrentDirectoryValidator(PathExistsValidator pathExistsValidator,
                                          MappedNetworkDriveValidator mappedNetworkDriveValidator)
        {
            RuleFor(c => c.Directory).Cascade(CascadeMode.Stop)
                .IsValidPath()
                .SetValidator(mappedNetworkDriveValidator)
                .SetValidator(pathExistsValidator)
                .When(c => c.Directory.IsNotNullOrWhiteSpace())
                .When(c => c.Host == "localhost" || c.Host == "127.0.0.1");
        }
    }
}
