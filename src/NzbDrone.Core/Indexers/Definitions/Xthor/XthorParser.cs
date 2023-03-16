using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions.Xthor
{
    public class XthorParser : IParseIndexerResponse
    {
        private readonly XthorSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private string _torrentDetailsUrl;

        public XthorParser(XthorSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
            _torrentDetailsUrl = _settings.BaseUrl.Replace("api.", "").TrimEnd('/') + "/details.php?id={id}";
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();
            var contentString = indexerResponse.Content;
            var xthorResponse = JsonConvert.DeserializeObject<XthorResponse>(contentString);

            if (xthorResponse != null)
            {
                CheckApiState(xthorResponse.Error);

                // If contains torrents
                if (xthorResponse.Torrents != null)
                {
                    // Adding each torrent row to releases
                    // Exclude hidden torrents (category 106, example => search 'yoda' in the API) #10407
                    torrentInfos.AddRange(xthorResponse.Torrents
                        .Where(torrent => torrent.Category != 106).Select(torrent =>
                        {
                            if (_settings.NeedMultiReplacement)
                            {
                                var regex = new Regex("(?i)([\\.\\- ])MULTI([\\.\\- ])");
                                torrent.Name = regex.Replace(torrent.Name,
                                    "$1" + _settings.MultiReplacement + "$2");
                            }

                            // issue #8759 replace vostfr and subfrench with English
                            if (!string.IsNullOrEmpty(_settings.SubReplacement))
                            {
                                torrent.Name = torrent.Name.Replace("VOSTFR", _settings.SubReplacement)
                                    .Replace("SUBFRENCH", _settings.SubReplacement);
                            }

                            var publishDate = DateTimeUtil.UnixTimestampToDateTime(torrent.Added);

                            var guid = new string(_torrentDetailsUrl.Replace("{id}", torrent.Id.ToString()));
                            var details = new string(_torrentDetailsUrl.Replace("{id}", torrent.Id.ToString()));
                            var link = new string(torrent.Download_link);
                            var release = new TorrentInfo
                            {
                                // Mapping data
                                Categories = _categories.MapTrackerCatToNewznab(torrent.Category.ToString()),
                                Title = torrent.Name,
                                Seeders = torrent.Seeders,
                                Peers = torrent.Seeders + torrent.Leechers,
                                MinimumRatio = 1,
                                MinimumSeedTime = 345600,
                                PublishDate = publishDate,
                                Size = torrent.Size,
                                Grabs = torrent.Times_completed,
                                Files = torrent.Numfiles,
                                UploadVolumeFactor = 1,
                                DownloadVolumeFactor = torrent.Freeleech == 1 ? 0 : 1,
                                Guid = guid,
                                InfoUrl = details,
                                DownloadUrl = link,
                                TmdbId = torrent.Tmdb_id
                            };

                            return release;
                        }));
                }
            }

            return torrentInfos.ToArray();
        }

        private void CheckApiState(XthorError state)
        {
            // Switch on state
            switch (state.Code)
            {
                case 0:
                    // Everything OK
                    break;
                case 1:
                    // Passkey not found
                    throw new Exception("Passkey not found in tracker's database");
                case 2:
                    // No results
                    break;
                case 3:
                    // Power Saver
                    break;
                case 4:
                    // DDOS Attack, API disabled
                    throw new Exception("Tracker is under DDOS attack, API disabled");
                case 8:
                    // AntiSpam Protection
                    throw new Exception("Triggered AntiSpam Protection, please delay your requests!");
                default:
                    // Unknown state
                    throw new Exception("Unknown state, aborting querying");
            }
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
