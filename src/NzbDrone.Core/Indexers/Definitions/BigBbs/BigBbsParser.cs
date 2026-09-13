using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions.BigBbs;

public class BigBbsParser : IParseIndexerResponse
{
    private static readonly Dictionary<string, int> Days = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Poniedziałek", 1 },
        { "Wtorek", 2 },
        { "Środa", 3 },
        { "Czwartek", 4 },
        { "Piątek", 5 },
        { "Sobota", 6 },
        { "Niedziela", 0 },
        { "Monday", 1 },
        { "Tuesday", 2 },
        { "Wednesday", 3 },
        { "Thursday", 4 },
        { "Friday", 5 },
        { "Saturday", 6 },
        { "Sunday", 0 }
    };

    private readonly BigBbsSettings _settings;
    private readonly IndexerCapabilitiesCategories _categories;

    public BigBbsParser(BigBbsSettings settings, IndexerCapabilitiesCategories categories)
    {
        _settings = settings;
        _categories = categories;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releases = new List<ReleaseInfo>();

        if (!indexerResponse.Content.Contains("bigbbs.eu/?p=logout"))
        {
            throw new IndexerAuthException(
                "The user is not logged in. It is possible that the cookie has expired or you made a mistake when copying it. Please check the settings.");
        }

        var parser = new HtmlParser();
        using var dom = parser.ParseDocument(indexerResponse.Content);

        var selector = _settings.FreeleechOnly
            ? "table#torrents_table_classic > tbody > tr:has(a[href*=\"?p=torrents&pid=10&action=download&tid=\"]):has(img[src$=\"/torrent_free.png\"])"
            : "table#torrents_table_classic > tbody > tr:has(a[href*=\"?p=torrents&pid=10&action=download&tid=\"])";

        var rows = dom.QuerySelectorAll(selector);

        foreach (var row in rows)
        {
            var categoryLink = row.QuerySelector("a[href*=\"?p=torrents&pid=10&cid=\"]");
            var titleLink = row.QuerySelector("a[href*=\"?p=torrents&pid=10&action=details&tid=\"]");
            var downloadLink = row.QuerySelector("a[href*=\"?p=torrents&pid=10&action=download&tid=\"]");
            var sizeLink = row.QuerySelector("a[rel=\"torrent_size\"]");
            var grabsLink = row.QuerySelector("a[rel=\"times_completed\"]");
            var seedersLink = row.QuerySelector("a[rel=\"torrent_seeders\"]");
            var leechersLink = row.QuerySelector("a[rel=\"torrent_leechers\"]");
            var imdbLink = row.QuerySelector("a[href*=\"imdb.com/title/tt\"]");

            if (titleLink == null || downloadLink == null)
            {
                continue;
            }

            var title = titleLink.TextContent.Trim();

            var categoryStr = categoryLink?.GetAttribute("href")?.Split(["cid="], StringSplitOptions.None)
                .LastOrDefault() ?? "1";
            var category = _categories.MapTrackerCatToNewznab(categoryStr);

            var downloadUrl = downloadLink.GetAttribute("href");
            var detailsUrl = titleLink.GetAttribute("href");

            var size = ParseUtil.GetBytes(sizeLink?.TextContent ?? "0");
            var grabs = ParseUtil.CoerceInt(grabsLink?.TextContent ?? "0");
            var seeders = ParseUtil.CoerceInt(seedersLink?.TextContent ?? "0");
            var leechers = ParseUtil.CoerceInt(leechersLink?.TextContent ?? "0");

            var imdbId = ParseUtil.GetImdbId(imdbLink?.GetAttribute("href")) ?? 0;

            var dateElem = row.QuerySelector("td.torrent_name");
            var dateStr = NormalizeDateString(dateElem?.TextContent ?? string.Empty);
            var publishDate = ParsePublishDate(dateStr);

            var isFreeleech = row.QuerySelector("img[src$=\"/torrent_free.png\"]") != null;

            var release = new TorrentInfo
            {
                Title = title,
                DownloadUrl = downloadUrl,
                InfoUrl = detailsUrl,
                Guid = detailsUrl,
                PublishDate = publishDate,
                Categories = category,
                Size = size,
                Grabs = grabs,
                Seeders = seeders,
                Peers = seeders + leechers,
                ImdbId = imdbId,
                DownloadVolumeFactor = isFreeleech ? 0 : 1,
                UploadVolumeFactor = 1,
                MinimumRatio = 1.0,
                MinimumSeedTime = 172800
            };

            releases.Add(release);
        }

        return releases;
    }

    private static string NormalizeDateString(string dateStr)
    {
        if (dateStr.IsNullOrWhiteSpace())
        {
            return dateStr;
        }

        dateStr = Regex.Replace(dateStr, @"Wstawione", "Uploaded", RegexOptions.IgnoreCase);
        dateStr = Regex.Replace(dateStr, @"przez", "by", RegexOptions.IgnoreCase);

        var todayMatch = Regex.Match(dateStr, @"Uploaded (Today|Dzisiaj)\s+(?:at|o)\s+(\d{2}:\d{2}:\d{2})", RegexOptions.IgnoreCase);
        if (todayMatch.Success)
        {
            var time = todayMatch.Groups[2].Value;
            return $"Uploaded {DateTime.Now:dd-MM-yyyy} {time}";
        }

        var yesterdayMatch = Regex.Match(dateStr, @"Uploaded (Yesterday|Wczoraj)\s+(?:at|o)\s+(\d{2}:\d{2}:\d{2})", RegexOptions.IgnoreCase);
        if (yesterdayMatch.Success)
        {
            var time = yesterdayMatch.Groups[2].Value;
            return $"Uploaded {DateTime.Now.AddDays(-1):dd-MM-yyyy} {time}";
        }

        var momentAgoMatch = Regex.Match(dateStr, @"Uploaded (a moment ago|minutę temu)", RegexOptions.IgnoreCase);
        if (momentAgoMatch.Success)
        {
            var now = DateTime.Now;
            return $"Uploaded {now:dd-MM-yyyy} {now:HH:mm:ss}";
        }

        var hoursAgoMatch = Regex.Match(dateStr, @"Uploaded (an hour ago|godzinę temu)", RegexOptions.IgnoreCase);
        if (hoursAgoMatch.Success)
        {
            var now = DateTime.Now;
            var hoursAgo = now.AddHours(-1);
            return $"Uploaded {hoursAgo:dd-MM-yyyy} {hoursAgo:HH:mm:ss}";
        }

        var minutesAgoMatch = Regex.Match(dateStr, @"Uploaded (\d+)\s+(?:minut\(?y?\)?|minutes)\s+(?:temu|ago)", RegexOptions.IgnoreCase);
        if (minutesAgoMatch.Success)
        {
            var minutes = int.Parse(minutesAgoMatch.Groups[1].Value);
            var minutesAgo = DateTime.Now.AddMinutes(-minutes);
            return $"Uploaded {minutesAgo:dd-MM-yyyy} {minutesAgo:HH:mm:ss}";
        }

        var weekdayMatch = Regex.Match(dateStr, @"Uploaded (\w+)\s+(?:o|at)\s+(\d{2}:\d{2}:\d{2})", RegexOptions.IgnoreCase);
        if (weekdayMatch.Success)
        {
            var weekdayName = weekdayMatch.Groups[1].Value;
            var time = weekdayMatch.Groups[2].Value;

            foreach (var day in Days)
            {
                if (string.Equals(day.Key, weekdayName, StringComparison.OrdinalIgnoreCase))
                {
                    var daysToSubtract = ((int)DateTime.Now.DayOfWeek - day.Value + 7) % 7;
                    if (daysToSubtract == 0)
                    {
                        daysToSubtract = 7;
                    }

                    var date = DateTime.Now.AddDays(-daysToSubtract);
                    return $"Uploaded {date:dd-MM-yyyy} {time}";
                }
            }
        }

        return dateStr;
    }

    private static DateTime ParsePublishDate(string dateStr)
    {
        if (dateStr.IsNullOrWhiteSpace())
        {
            return DateTime.Now;
        }

        var dateMatch = Regex.Match(dateStr, @"Uploaded (\d{1,2}-\d{1,2}-\d{4}) (\d{2}:\d{2}:\d{2})");

        if (dateMatch.Success)
        {
            var date = dateMatch.Groups[1].Value;
            var time = dateMatch.Groups[2].Value;
            return DateTime.ParseExact($"{date} {time}", "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        }

        return DateTime.Now;
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}
