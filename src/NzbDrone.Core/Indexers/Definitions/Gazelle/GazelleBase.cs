using System;
using System.Net.Http;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions.Gazelle;

public abstract class GazelleBase<TSettings> : TorrentIndexerBase<TSettings>
        where TSettings : GazelleSettings, new()
{
    public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
    public override string[] IndexerUrls => new[] { "" };
    protected virtual string LoginUrl => Settings.BaseUrl + "login.php";
    public override bool SupportsRss => true;
    public override bool SupportsSearch => true;
    public override int PageSize => 50;
    public override IndexerCapabilities Capabilities => SetCapabilities();

    public GazelleBase(IIndexerHttpClient httpClient,
                   IEventAggregator eventAggregator,
                   IIndexerStatusService indexerStatusService,
                   IConfigService configService,
                   Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new GazelleRequestGenerator(Settings, Capabilities, _httpClient, _logger);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new GazelleParser(Settings, Capabilities);
    }

    protected virtual IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities();

        return caps;
    }

    protected override async Task DoLogin()
    {
        var requestBuilder = new HttpRequestBuilder(LoginUrl)
        {
            LogResponseContent = true,
            Method = HttpMethod.Post
        };
        requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);

        var cookies = Cookies;

        Cookies = null;
        var authLoginRequest = requestBuilder
            .AddFormParameter("username", Settings.Username)
            .AddFormParameter("password", Settings.Password)
            .AddFormParameter("keeplogged", "1")
            .SetHeader("Content-Type", "application/x-www-form-urlencoded")
            .Accept(HttpAccept.Json)
            .Build();

        var response = await ExecuteAuth(authLoginRequest);

        cookies = response.GetCookies();

        UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

        _logger.Debug("Gazelle authentication succeeded.");
    }

    public override async Task<byte[]> Download(Uri link)
    {
        var response = await base.Download(link);

        if (response.Length >= 1
            && response[0] != 'd' // simple test for torrent vs HTML content
            && link.Query.Contains("usetoken=1"))
        {
            var html = Encoding.GetString(response);
            if (html.Contains("You do not have any freeleech tokens left.")
                || html.Contains("You do not have enough freeleech tokens")
                || html.Contains("This torrent is too large.")
                || html.Contains("You cannot use tokens here"))
            {
                // download again with usetoken=0
                var requestLinkNew = link.ToString().Replace("usetoken=1", "usetoken=0");

                response = await base.Download(new Uri(requestLinkNew));
            }
        }

        return response;
    }

    protected override bool CheckIfLoginNeeded(HttpResponse response)
    {
        return response.HasHttpRedirect || (response.Content != null && response.Content.Contains("\"bad credentials\""));
    }
}
