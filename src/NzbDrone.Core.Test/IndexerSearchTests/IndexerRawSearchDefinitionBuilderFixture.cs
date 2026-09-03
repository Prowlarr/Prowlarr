using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.Indexers.Settings;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    public class IndexerRawSearchDefinitionBuilderFixture
    {
        [Test]
        public void should_reset_known_search_constraints_on_cloned_settings()
        {
            var original = new IndexerDefinition
            {
                Id = 1,
                Name = "test",
                Implementation = "TestIndexer",
                Settings = new TestIndexerSettings
                {
                    BaseUrl = "https://tracker.example",
                    FreeleechOnly = true,
                    UseFreeleechToken = 2,
                    LimitedOnly = true,
                    SearchTypes = new[] { 1, 2 },
                    BaseSettings = new IndexerBaseSettings { QueryLimit = 42 }
                }
            };

            var clone = IndexerRawSearchDefinitionBuilder.Build(original);
            var cloneSettings = (TestIndexerSettings)clone.Settings;
            var originalSettings = (TestIndexerSettings)original.Settings;

            cloneSettings.FreeleechOnly.Should().BeFalse();
            cloneSettings.UseFreeleechToken.Should().Be(0);
            cloneSettings.LimitedOnly.Should().BeFalse();
            cloneSettings.SearchTypes.Should().BeEmpty();
            cloneSettings.BaseSettings.QueryLimit.Should().Be(42);

            originalSettings.FreeleechOnly.Should().BeTrue();
            originalSettings.UseFreeleechToken.Should().Be(2);
            originalSettings.LimitedOnly.Should().BeTrue();
            originalSettings.SearchTypes.Should().Equal(1, 2);
        }

        [Test]
        public void should_reset_known_cardigann_search_constraint_fields()
        {
            var original = new IndexerDefinition
            {
                Id = 2,
                Name = "cardigann-test",
                Implementation = "Cardigann",
                Settings = new CardigannSettings
                {
                    DefinitionFile = "test.yml",
                    BaseUrl = "https://tracker.example",
                    ExtraFieldData = new Dictionary<string, object>
                    {
                        ["freeleech"] = true,
                        ["useFreeleechToken"] = 2,
                        ["someOtherField"] = "keep"
                    }
                },
                ExtraFields = new List<SettingsField>
                {
                    new() { Name = "freeleech", Type = "checkbox", Label = "Freeleech only", Default = "false" },
                    new() { Name = "useFreeleechToken", Type = "select", Label = "Use freeleech token", Default = "0" },
                    new() { Name = "someOtherField", Type = "text", Label = "Other field" }
                }
            };

            var clone = IndexerRawSearchDefinitionBuilder.Build(original);
            var cloneSettings = (CardigannSettings)clone.Settings;
            var originalSettings = (CardigannSettings)original.Settings;

            cloneSettings.ExtraFieldData["freeleech"].Should().Be(false);
            cloneSettings.ExtraFieldData["useFreeleechToken"].Should().Be(0);
            cloneSettings.ExtraFieldData["someOtherField"].Should().Be("keep");

            originalSettings.ExtraFieldData["freeleech"].Should().Be(true);
            originalSettings.ExtraFieldData["useFreeleechToken"].Should().Be(2);
        }

        private class TestIndexerSettings : NoAuthTorrentBaseSettings
        {
            public bool FreeleechOnly { get; set; }

            public int UseFreeleechToken { get; set; }

            [SearchConstraint(SearchConstraintResetBehavior.False)]
            public bool LimitedOnly { get; set; }

            [SearchConstraint]
            public IEnumerable<int> SearchTypes { get; set; } = Array.Empty<int>();
        }
    }
}
