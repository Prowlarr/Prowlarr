using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class BrazilianPublicTorrentRequestGenerator : IIndexerRequestGenerator
    {
        private readonly NoAuthTorrentBaseSettings _settings;
        private readonly BrazilianPublicTorrentSite _site;

        public BrazilianPublicTorrentRequestGenerator(NoAuthTorrentBaseSettings settings, BrazilianPublicTorrentSite site)
        {
            _settings = settings;
            _site = site;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var chain = new IndexerPageableRequestChain();
            AddRequests(chain, searchCriteria.SanitizedSearchTerm, BrazilianPublicTorrentSearchKind.Movie, null, null, searchCriteria.Year);
            return chain;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var chain = new IndexerPageableRequestChain();
            AddRequests(chain, searchCriteria.SanitizedSearchTerm, BrazilianPublicTorrentSearchKind.Tv, searchCriteria.Season, searchCriteria.Episode, null);
            return chain;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var chain = new IndexerPageableRequestChain();
            AddRequests(chain, searchCriteria.SanitizedSearchTerm, BrazilianPublicTorrentSearchKind.Basic, null, null, null);
            return chain;
        }

        private void AddRequests(IndexerPageableRequestChain chain, string term, BrazilianPublicTorrentSearchKind kind, int? season, string episode, int? year)
        {
            foreach (var request in GetRequests(term, kind, season, episode, year))
            {
                chain.Add(new[] { request });
            }
        }

        private IEnumerable<IndexerRequest> GetRequests(string term, BrazilianPublicTorrentSearchKind kind, int? season, string episode, int? year)
        {
            if (term.IsNullOrWhiteSpace())
            {
                yield break;
            }

            if (_site.UsesHdrRoutes)
            {
                if (kind == BrazilianPublicTorrentSearchKind.Basic)
                {
                    yield return InternalRequest(term, kind, BrazilianPublicTorrentRoute.SearchPage, season, episode, year);
                    yield break;
                }

                var routes = kind switch
                {
                    BrazilianPublicTorrentSearchKind.Movie => new[] { BrazilianPublicTorrentRoute.Movies },
                    BrazilianPublicTorrentSearchKind.Tv => new[] { BrazilianPublicTorrentRoute.Series },
                    _ => new[] { BrazilianPublicTorrentRoute.SearchPage }
                };

                foreach (var route in routes)
                {
                    yield return InternalRequest(term, kind, route, season, episode, year);
                }

                yield break;
            }

            yield return InternalRequest(term, kind, BrazilianPublicTorrentRoute.SearchPage, season, episode, year);
        }

        private IndexerRequest InternalRequest(string term, BrazilianPublicTorrentSearchKind kind, BrazilianPublicTorrentRoute route, int? season, string episode, int? year)
        {
            var query = new NameValueCollection
            {
                { "query", term },
                { "kind", kind.ToString() },
                { "route", route.ToString() }
            };

            if (season.HasValue)
            {
                query.Add("season", season.Value.ToString());
            }

            if (episode.IsNotNullOrWhiteSpace())
            {
                query.Add("episode", episode);
            }

            if (year.HasValue)
            {
                query.Add("year", year.Value.ToString());
            }

            return new IndexerRequest($"{_settings.BaseUrl.TrimEnd('/')}/__prowlarr_brazilian_public_search?{query.GetQueryString()}", HttpAccept.Html);
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class BrazilianPublicTorrentNoopParser : IParseIndexerResponse
    {
        public IList<NzbDrone.Core.Parser.Model.ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            return Array.Empty<NzbDrone.Core.Parser.Model.ReleaseInfo>();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
