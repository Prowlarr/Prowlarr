using System.Text.RegularExpressions;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Parser
{
    public static class TvCategoryFromQualityParser
    {
        private static readonly Regex SourceRegex = new (@"\b(?:
                                                                (?<bluray>BluRay|Blu-Ray|HDDVD|BD)|
                                                                (?<webdl>WEB[-_. ]DL|WEBDL|WebRip|iTunesHD|WebHD)|
                                                                (?<hdtv>HDTV)|
                                                                (?<bdrip>BDRip)|
                                                                (?<brrip>BRRip)|
                                                                (?<dvd>DVD|DVDRip|NTSC|PAL|xvidvd)|
                                                                (?<dsr>WS[-_. ]DSR|DSR)|
                                                                (?<pdtv>PDTV)|
                                                                (?<sdtv>SDTV)|
                                                                (?<tvrip>TVRip)
                                                                )\b",
                                                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex RawHdRegex = new (@"\b(?<rawhd>TrollHD|RawHD|1080i[-_. ]HDTV|Raw[-_. ]HD|MPEG[-_. ]?2)\b",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ResolutionRegex = new (@"\b(?:(?<q480p>480p|640x480|848x480)|(?<q576p>576p)|(?<q720p>720p|1280x720)|(?<q1080p>1080p|1920x1080)|(?<q2160p>2160p))\b",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex CodecRegex = new (@"\b(?:(?<x264>x264)|(?<h264>h264)|(?<xvidhd>XvidHD)|(?<xvid>Xvid)|(?<divx>divx))\b",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex HighDefPdtvRegex = new (@"hr[-_. ]ws", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IndexerCategory ParseTvShowQuality(string tvShowFileName)
        {
            var normalizedName = tvShowFileName.Trim().Replace('_', ' ').Trim().ToLower();

            var sourceMatch = SourceRegex.Match(normalizedName);
            var resolutionMatch = ResolutionRegex.Match(normalizedName);
            var codecMatch = CodecRegex.Match(normalizedName);

            if (sourceMatch.Groups["webdl"].Success)
            {
                if (resolutionMatch.Groups["q2160p"].Success)
                {
                    return NewznabStandardCategory.TVUHD;
                }

                if (resolutionMatch.Groups["q1080p"].Success || resolutionMatch.Groups["q720p"].Success)
                {
                    return NewznabStandardCategory.TVHD;
                }

                if (resolutionMatch.Groups["q480p"].Success)
                {
                    return NewznabStandardCategory.TVSD;
                }
            }

            if (sourceMatch.Groups["hdtv"].Success)
            {
                if (resolutionMatch.Groups["q2160p"].Success)
                {
                    return NewznabStandardCategory.TVUHD;
                }

                if (resolutionMatch.Groups["q1080p"].Success || resolutionMatch.Groups["q720p"].Success)
                {
                    return NewznabStandardCategory.TVHD;
                }
                else
                {
                    return NewznabStandardCategory.TVSD;
                }
            }

            if (sourceMatch.Groups["bluray"].Success || sourceMatch.Groups["bdrip"].Success || sourceMatch.Groups["brrip"].Success)
            {
                if (codecMatch.Groups["xvid"].Success || codecMatch.Groups["divx"].Success)
                {
                    return NewznabStandardCategory.TVSD;
                }

                if (resolutionMatch.Groups["q2160p"].Success)
                {
                    return NewznabStandardCategory.TVUHD;
                }

                if (resolutionMatch.Groups["q1080p"].Success || resolutionMatch.Groups["q720p"].Success)
                {
                    return NewznabStandardCategory.TVHD;
                }

                if (resolutionMatch.Groups["q480p"].Success || resolutionMatch.Groups["q576p"].Success)
                {
                    return NewznabStandardCategory.TVSD;
                }
            }

            if (sourceMatch.Groups["dvd"].Success)
            {
                return NewznabStandardCategory.TVSD;
            }

            if (sourceMatch.Groups["pdtv"].Success || sourceMatch.Groups["sdtv"].Success || sourceMatch.Groups["dsr"].Success || sourceMatch.Groups["tvrip"].Success)
            {
                if (HighDefPdtvRegex.IsMatch(normalizedName))
                {
                    return NewznabStandardCategory.TVHD;
                }
                else
                {
                    return NewznabStandardCategory.TVSD;
                }
            }

            if (RawHdRegex.IsMatch(normalizedName))
            {
                return NewznabStandardCategory.TVHD;
            }

            return NewznabStandardCategory.TV;
        }
    }
}
