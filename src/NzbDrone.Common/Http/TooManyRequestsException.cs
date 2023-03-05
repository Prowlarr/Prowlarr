using System;

namespace NzbDrone.Common.Http
{
    public class TooManyRequestsException : HttpException
    {
        public TimeSpan RetryAfter { get; private set; }

        public TooManyRequestsException(HttpRequest request, HttpResponse response)
            : base(request, response)
        {
            if (response.Headers.ContainsKey("Retry-After"))
            {
                var retryAfter = response.Headers["Retry-After"];

                if (int.TryParse(retryAfter, out var seconds))
                {
                    RetryAfter = TimeSpan.FromSeconds(seconds);
                }
                else if (DateTime.TryParse(retryAfter, out var date))
                {
                    RetryAfter = date.ToUniversalTime() - DateTime.UtcNow;
                }
            }
        }

        public TooManyRequestsException(HttpRequest request, HttpResponse response, TimeSpan retryWait)
            : base(request, response)
        {
            RetryAfter = retryWait;
        }
    }
}
