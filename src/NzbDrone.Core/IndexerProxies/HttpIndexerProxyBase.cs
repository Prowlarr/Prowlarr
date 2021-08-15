using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.IndexerProxies
{
    public abstract class HttpIndexerProxyBase<TSettings> : IndexerProxyBase<TSettings>
        where TSettings : IIndexerProxySettings, new()
    {
        protected readonly IHttpClient _httpClient;
        protected readonly IHttpRequestBuilderFactory _cloudRequestBuilder;
        protected readonly Logger _logger;

        public HttpIndexerProxyBase(IProwlarrCloudRequestBuilder cloudRequestBuilder, IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cloudRequestBuilder = cloudRequestBuilder.Services;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            var addresses = Dns.GetHostAddresses(Settings.Host);
            if (!addresses.Any())
            {
                failures.Add(new NzbDroneValidationFailure("Host", "ProxyCheckResolveIpMessage"));
            }

            var request = PreRequest(_cloudRequestBuilder.Create()
                                              .Resource("/ping")
                                              .Build());

            try
            {
                var response = PostResponse(_httpClient.Execute(request));

                // We only care about 400 responses, other error codes can be ignored
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    _logger.Error("Proxy Health Check failed: {0}", response.StatusCode);
                    failures.Add(new NzbDroneValidationFailure("Host", string.Format("Failed to test proxy. StatusCode: {0}", response.StatusCode)));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Proxy Health Check failed");
                failures.Add(new NzbDroneValidationFailure("Host", string.Format("Failed to test proxy: {0}", request.Url)));
            }

            return new ValidationResult(failures);
        }
    }
}
