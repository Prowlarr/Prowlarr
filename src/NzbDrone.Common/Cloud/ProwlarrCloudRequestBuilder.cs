using NzbDrone.Common.Http;

namespace NzbDrone.Common.Cloud
{
    public interface IProwlarrCloudRequestBuilder
    {
        IHttpRequestBuilderFactory Services { get; }
        IHttpRequestBuilderFactory Releases { get; }
    }

    public class ProwlarrCloudRequestBuilder : IProwlarrCloudRequestBuilder
    {
        public ProwlarrCloudRequestBuilder()
        {
            Services = new HttpRequestBuilder("https://prowlarr.servarr.com/v1/")
                .CreateFactory();

            Releases = new HttpRequestBuilder("https://releases.servarr.com/v1/")
                .CreateFactory();
        }

        public IHttpRequestBuilderFactory Services { get; private set; }

        public IHttpRequestBuilderFactory Releases { get; private set; }
    }
}
