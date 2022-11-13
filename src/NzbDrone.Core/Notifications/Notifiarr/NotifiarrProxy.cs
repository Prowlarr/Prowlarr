using System.Net.Http;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Notifications.Webhook;

namespace NzbDrone.Core.Notifications.Notifiarr
{
    public interface INotifiarrProxy
    {
        void SendNotification(WebhookPayload payload, NotifiarrSettings settings);
    }

    public class NotifiarrProxy : INotifiarrProxy
    {
        private const string URL = "https://notifiarr.com";
        private readonly IHttpClient _httpClient;
        private readonly IConfigFileProvider _configFileProvider;

        public NotifiarrProxy(IHttpClient httpClient, IConfigFileProvider configFileProvider)
        {
            _httpClient = httpClient;
            _configFileProvider = configFileProvider;
        }

        public void SendNotification(WebhookPayload payload, NotifiarrSettings settings)
        {
            ProcessNotification(payload, settings);
        }

        private void ProcessNotification(WebhookPayload payload, NotifiarrSettings settings)
        {
            try
            {
                var request = new HttpRequestBuilder(URL + "/api/v1/notification/prowlarr")
                    .Accept(HttpAccept.Json)
                    .SetHeader("X-API-Key", settings.APIKey)
                    .Build();

                request.Method = HttpMethod.Post;

                request.Headers.ContentType = "application/json";
                request.SetContent(payload.ToJson());

                var instanceName = _configFileProvider.InstanceName;

                _httpClient.Post(request);
            }
            catch (HttpException ex)
            {
                var responseCode = ex.Response.StatusCode;
                switch ((int)responseCode)
                {
                    case 401:
                        throw new NotifiarrException("API key is invalid");
                    case 400:
                        throw new NotifiarrException("Unable to send notification. Ensure Prowlarr Integration is enabled & assigned a channel on Notifiarr");
                    case 502:
                    case 503:
                    case 504:
                        throw new NotifiarrException("Unable to send notification. Service Unavailable", ex);
                    case 520:
                    case 521:
                    case 522:
                    case 523:
                    case 524:
                        throw new NotifiarrException("Cloudflare Related HTTP Error - Unable to send notification", ex);
                    default:
                        throw new NotifiarrException("Unknown HTTP Error - Unable to send notification", ex);
                }
            }
        }
    }
}
