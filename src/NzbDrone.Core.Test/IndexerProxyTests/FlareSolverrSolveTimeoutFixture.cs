using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerProxies;
using NzbDrone.Core.IndexerProxies.FlareSolverr;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerProxyTests
{
    [TestFixture]
    public class FlareSolverrSolveTimeoutFixture : CoreTest<FlareSolverr>
    {
        private FlareSolverrSettings _settings;

        [SetUp]
        public void Setup()
        {
            _settings = new FlareSolverrSettings { Host = "http://flaresolverr.local:8191/", RequestTimeout = 60 };

            Subject.Definition = new IndexerProxyDefinition
            {
                Name = "FlareSolverr",
                Settings = _settings
            };
        }

        [Test]
        public void should_use_proxy_timeout_when_request_has_no_cap()
        {
            var request = new HttpRequest("http://indexer.local/");

            Subject.GetSolveTimeoutSeconds(request).Should().Be(60);
        }

        [TestCase(10, 10)]
        [TestCase(45, 45)]
        [TestCase(300, 60)]
        public void should_cap_solve_time_by_request_cap_but_never_exceed_proxy_timeout(int capSeconds, int expected)
        {
            var request = new HttpRequest("http://indexer.local/") { MaxProxySolveTime = TimeSpan.FromSeconds(capSeconds) };

            Subject.GetSolveTimeoutSeconds(request).Should().Be(expected);
        }

        [Test]
        public void should_round_sub_second_cap_up_to_one_second()
        {
            var request = new HttpRequest("http://indexer.local/") { MaxProxySolveTime = TimeSpan.FromMilliseconds(200) };

            Subject.GetSolveTimeoutSeconds(request).Should().Be(1);
        }
    }
}
