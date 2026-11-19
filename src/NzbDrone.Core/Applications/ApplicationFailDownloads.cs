using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Applications
{
    public enum ApplicationFailDownloads
    {
        [FieldOption(Label = "Executables")]
        Executables = 0,

        [FieldOption(Label = "Potentially Dangerous")]
        PotentiallyDangerous = 1
    }
}
