using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using NzbDrone.Api.V1.Indexers;

namespace Prowlarr.Api.V1.Test.Indexers
{
    public class NewznabControllerFixture
    {
        [TestCase("/api/v1/indexer/12/newznab", true)]
        [TestCase("/12/api", false)]
        [TestCase("/api/v1/indexer/12/download", false)]
        public void should_only_allow_extended_search_parameters_on_prowlarr_newznab_route(string path, bool expected)
        {
            NewznabController.SupportsExtendedSearchParameters(new PathString(path)).Should().Be(expected);
        }
    }
}
