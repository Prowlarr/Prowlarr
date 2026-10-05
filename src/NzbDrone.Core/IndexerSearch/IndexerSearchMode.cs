using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.IndexerSearch
{
    public enum IndexerSearchMode
    {
        Default = 0,
        Raw = 1
    }

    public static class IndexerSearchModeParser
    {
        public static IndexerSearchMode Parse(string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return IndexerSearchMode.Default;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "raw" => IndexerSearchMode.Raw,
                "normal" => IndexerSearchMode.Default,
                "default" => IndexerSearchMode.Default,
                _ => IndexerSearchMode.Default
            };
        }
    }
}
