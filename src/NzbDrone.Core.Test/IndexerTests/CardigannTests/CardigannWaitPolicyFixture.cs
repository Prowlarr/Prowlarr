using System.Net;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.CardigannTests
{
    [TestFixture]
    public class CardigannWaitPolicyFixture : CoreTest<Cardigann>
    {
        [SetUp]
        public void SetUp()
        {
            Subject.Definition = new IndexerDefinition
            {
                Id = 1,
                Name = "test",
                Settings = new CardigannSettings { DefinitionFile = "test" }
            };
        }

        [Test]
        public void should_return_no_policy_when_definition_has_no_wait_block()
        {
            SetCachedDefinition(new CardigannDefinition());

            GetPolicy().Should().BeNull();
        }

        [Test]
        public void should_return_policy_for_configured_wait_block()
        {
            SetCachedDefinition(new CardigannDefinition
            {
                Download = new DownloadBlock
                {
                    Wait = new DownloadWaitBlock { Title = "Wait page", DelayVariable = "download_after" }
                }
            });

            var policy = GetPolicy();

            policy.Should().NotBeNull();
            policy.Title.Should().Be("Wait page");
            policy.DelayVariable.Should().Be("download_after");
        }

        [TestCase(null, "download_after")]
        [TestCase("   ", "download_after")]
        [TestCase("Wait page", null)]
        [TestCase("Wait page", "")]
        [TestCase("Wait page", "download after")]
        [TestCase("Wait page", "1download")]
        [TestCase("Wait page", "download;evil")]
        public void should_reject_incomplete_or_invalid_wait_configuration(string title, string delayVariable)
        {
            SetCachedDefinition(new CardigannDefinition
            {
                Download = new DownloadBlock
                {
                    Wait = new DownloadWaitBlock { Title = title, DelayVariable = delayVariable }
                }
            });

            var exception = Assert.Throws<TargetInvocationException>(() => GetPolicy());

            exception.InnerException.Should().BeOfType<WebException>();
        }

        private void SetCachedDefinition(CardigannDefinition definition)
        {
            Mocker.GetMock<IIndexerDefinitionUpdateService>()
                .Setup(service => service.GetCachedDefinition("test"))
                .Returns(definition);
        }

        private DownloadWaitPolicy GetPolicy()
        {
            // GetDownloadWaitPolicy is a protected override invoked by the download pipeline.
            var method = typeof(Cardigann).GetMethod("GetDownloadWaitPolicy", BindingFlags.Instance | BindingFlags.NonPublic);
            return (DownloadWaitPolicy)method.Invoke(Subject, null);
        }
    }
}
