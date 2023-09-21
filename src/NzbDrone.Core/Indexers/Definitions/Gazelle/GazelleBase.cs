using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions.Gazelle;

public abstract class GazelleBase<TSettings> : TorrentIndexerBase<TSettings>
        where TSettings : GazelleSettings, new()
{
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
        return new IndexerCapabilities();
    }

    protected override async Task DoLogin()
    {
        var cookies = Cookies;
        Cookies = null;

        var authLoginRequestBuilder = AuthLoginRequestBuilder();

        var response = await ExecuteAuth(authLoginRequestBuilder.Build());

        CheckForLoginError(response);

        cookies = response.GetCookies();
        UpdateCookies(cookies, DateTime.Now.AddDays(30));

        _logger.Debug("Gazelle authentication succeeded.");
    }

    protected virtual HttpRequestBuilder AuthLoginRequestBuilder()
    {
        var requestBuilder = new HttpRequestBuilder(LoginUrl)
        {
            LogResponseContent = true,
            Method = HttpMethod.Post
        };

        var authLoginRequestBuilder = requestBuilder
            .AddFormParameter("username", Settings.Username)
            .AddFormParameter("password", Settings.Password)
            .AddFormParameter("keeplogged", "1")
            .SetHeader("Content-Type", "application/x-www-form-urlencoded")
            .SetHeader("Referer", LoginUrl)
            .Accept(HttpAccept.Json);

        return authLoginRequestBuilder;
    }

    protected virtual bool CheckForLoginError(HttpResponse response) => true;

    public override async Task<IndexerDownloadResponse> Download(Uri link)
    {
        var downloadResponse = await base.Download(link);

        var fileData = downloadResponse.Data;

        if (Settings.UseFreeleechToken == (int)GazelleFreeleechTokenAction.Preferred
            && fileData.Length >= 1
            && fileData[0] != 'd' // simple test for torrent vs HTML content
            && link.Query.Contains("usetoken=1"))
        {
            var html = Encoding.GetString(fileData);

            if (html.Contains("You do not have any freeleech tokens left.")
                || html.Contains("You do not have enough freeleech tokens")
                || html.Contains("This torrent is too large.")
                || html.Contains("You cannot use tokens here"))
            {
                // Try to download again without usetoken
                downloadResponse = await base.Download(link.RemoveQueryParam("usetoken"));
            }
        }

        return downloadResponse;
    }

    protected override IDictionary<string, string> GetCookies()
    {
        if (Settings is GazelleUserPassOrCookieSettings cookieSettings && !string.IsNullOrWhiteSpace(cookieSettings.Cookie))
        {
            return CookieUtil.CookieHeaderToDictionary(cookieSettings.Cookie);
        }

        return base.GetCookies();
    }

    protected override bool CheckIfLoginNeeded(HttpResponse response)
    {
        if (Settings is GazelleUserPassOrCookieSettings cookieSettings && !string.IsNullOrWhiteSpace(cookieSettings.Cookie))
        {
            return false;
        }

        var invalidResponses = new[] { "\"bad credentials\"", "\"groupName\":\"wrong-creds\"" };

        return response.HasHttpRedirect || (response.Content != null && invalidResponses.Any(response.Content.Contains));
    }
}
