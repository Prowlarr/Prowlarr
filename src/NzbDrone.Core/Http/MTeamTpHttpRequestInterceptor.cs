using System;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Http
{
    public class MTeamTpHttpRequestInterceptor : IHttpRequestInterceptor
    {
        private string Host => "kp.m-team.cc";

        private string UserAgent => "Mozilla/5.0 (X11; Linux x86_64; rv:94.0) Gecko/20100101 Firefox/94.0";

        public HttpRequest PreRequest(HttpRequest request)
        {
            // if use default user agent,the site will be redirect to google
            if (request.Url.Host == Host && request.Headers.UserAgent == null)
            {
                request.Headers.UserAgent = UserAgent;
            }

            return request;
        }

        public HttpResponse PostResponse(HttpResponse response)
        {
            var request = response.Request;

            // some times,site always redirect to verify page even though has benn already login done
            if (request.Url.Host == Host && response.HasHttpRedirect && response.RedirectUrl.Contains("verify.php", StringComparison.Ordinal))
            {
                request.AllowAutoRedirect = true;
            }

            return response;
        }
    }
}
