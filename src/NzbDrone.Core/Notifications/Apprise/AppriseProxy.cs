using System;
using System.Linq;
using System.Net;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Notifications.Apprise
{
    public interface IAppriseProxy
    {
        void SendNotification(AppriseSettings settings, string title, string message);

        ValidationFailure Test(AppriseSettings settings);
    }

    public class AppriseProxy : IAppriseProxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public AppriseProxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public void SendNotification(AppriseSettings settings, string title, string body)
        {
            var requestBuilder = new HttpRequestBuilder(settings.BaseUrl.TrimEnd('/', ' ')).Post()
                .AddFormParameter("title", title)
                .AddFormParameter("body", body);

            if (settings.ConfigurationKey.IsNotNullOrWhiteSpace())
            {
                requestBuilder
                    .Resource("/notify/{configurationKey}")
                    .SetSegment("configurationKey", settings.ConfigurationKey);
            }
            else if (settings.StatelessUrls.IsNotNullOrWhiteSpace())
            {
                requestBuilder
                    .Resource("/notify")
                    .AddFormParameter("urls", settings.StatelessUrls);
            }

            if (settings.Tags.Any())
            {
                requestBuilder.AddFormParameter("tag", settings.Tags.Join(","));
            }

            if (settings.AuthUsername.IsNotNullOrWhiteSpace() || settings.AuthPassword.IsNotNullOrWhiteSpace())
            {
                requestBuilder.NetworkCredential = new BasicNetworkCredential(settings.AuthUsername, settings.AuthPassword);
            }

            _httpClient.Execute(requestBuilder.Build());
        }

        public ValidationFailure Test(AppriseSettings settings)
        {
            const string title = "Prowlarr - Test Notification";
            const string body = "Success! You have properly configured your apprise notification settings.";

            try
            {
                SendNotification(settings, title, body);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.Error(ex, $"HTTP Auth credentials are invalid: {ex.Message}");
                    return new ValidationFailure("AuthUsername", $"HTTP Auth credentials are invalid: {ex.Message}");
                }

                _logger.Error(ex, "Unable to send test message. Server connection failed. Status code: {0}", ex.Message);
                return new ValidationFailure("Url", $"Unable to connect to Apprise API. Please try again later. Status code: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to send test message. Status code: {0}", ex.Message);
                return new ValidationFailure("Url", $"Unable to send test message. Status code: {ex.Message}");
            }

            return null;
        }
    }
}
