using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NzbDrone.Core.Applications.Sonarr;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Applications
{
    [TestFixture]
    public class SonarrIndexerFixture : TestBase
    {
        private static SonarrIndexer BuildIndexer(params int[] failDownloads)
        {
            var fields = new List<SonarrField>
            {
                new() { Name = "baseUrl", Value = "http://localhost:9696/1/" },
                new() { Name = "apiPath", Value = "/api" },
                new() { Name = "apiKey", Value = "key" },
                new() { Name = "categories", Value = JArray.FromObject(new[] { 5000 }) },
                new() { Name = "animeCategories", Value = JArray.FromObject(new[] { 5070 }) }
            };

            if (failDownloads != null)
            {
                fields.Add(new() { Name = "failDownloads", Value = JArray.FromObject(failDownloads) });
            }

            return new SonarrIndexer
            {
                Name = "Test (Prowlarr)",
                Implementation = "Torznab",
                Fields = fields
            };
        }

        [Test]
        public void should_ignore_fail_downloads_when_built_indexer_does_not_have_field()
        {
            var built = BuildIndexer(null);
            var remote = BuildIndexer(0, 1);

            built.Equals(remote).Should().BeTrue();
        }

        [Test]
        public void should_be_equal_when_fail_downloads_match_regardless_of_order()
        {
            var built = BuildIndexer(0, 1);
            var remote = BuildIndexer(1, 0);

            built.Equals(remote).Should().BeTrue();
        }

        [Test]
        public void should_not_be_equal_when_fail_downloads_differ()
        {
            var built = BuildIndexer(0, 1);
            var remote = BuildIndexer(0);

            built.Equals(remote).Should().BeFalse();
        }

        [Test]
        public void should_not_be_equal_when_remote_lacks_fail_downloads_value()
        {
            var built = BuildIndexer(0, 1);
            var remote = BuildIndexer(null);

            built.Equals(remote).Should().BeFalse();
        }
    }
}
