using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Authentication
{
    [TestFixture]
    public class AuthenticationConfigurationExtensionsFixture : TestBase
    {
        private Mock<IConfigFileProvider> GivenConfig(AuthenticationType authenticationType, string authority)
        {
            var configFileProvider = new Mock<IConfigFileProvider>();

            configFileProvider.SetupGet(s => s.AuthenticationMethod).Returns(authenticationType);
            configFileProvider.SetupGet(s => s.OidcAuthority).Returns(authority);
            configFileProvider.SetupGet(s => s.OidcClientId).Returns("prowlarr");
            configFileProvider.SetupGet(s => s.OidcClientSecret).Returns("secret");
            configFileProvider.SetupGet(s => s.OidcUserIdentifier).Returns("user");
            configFileProvider.SetupGet(s => s.OidcScopes).Returns("openid profile email");

            return configFileProvider;
        }

        [TestCase("openid profile email", new[] { "openid", "profile", "email" })]
        [TestCase("openid,profile, email", new[] { "openid", "profile", "email" })]
        [TestCase("  openid   ", new[] { "openid" })]
        [TestCase("", new string[0])]
        [TestCase(null, new string[0])]
        public void should_parse_oidc_scopes(string scopes, string[] expected)
        {
            AuthenticationConfigurationExtensions.GetOidcScopes(scopes).Should().Equal(expected);
        }

        [Test]
        public void should_be_configured_when_all_settings_are_set_and_authority_is_https()
        {
            GivenConfig(AuthenticationType.Oidc, "https://id.example.com").Object.IsOidcConfigured().Should().BeTrue();
        }

        [Test]
        public void should_not_be_configured_when_authority_is_http()
        {
            GivenConfig(AuthenticationType.Oidc, "http://id.example.com").Object.IsOidcConfigured().Should().BeFalse();
        }

        [Test]
        public void should_fall_back_to_forms_when_oidc_is_not_configured()
        {
            GivenConfig(AuthenticationType.Oidc, "").Object.EffectiveAuthenticationMethod().Should().Be(AuthenticationType.Forms);
        }

        [Test]
        public void should_use_oidc_when_oidc_is_configured()
        {
            GivenConfig(AuthenticationType.Oidc, "https://id.example.com").Object.EffectiveAuthenticationMethod().Should().Be(AuthenticationType.Oidc);
        }

        [TestCase(AuthenticationType.Forms)]
        [TestCase(AuthenticationType.External)]
        [TestCase(AuthenticationType.None)]
        public void should_not_change_other_authentication_methods(AuthenticationType authenticationType)
        {
            GivenConfig(authenticationType, "").Object.EffectiveAuthenticationMethod().Should().Be(authenticationType);
        }
    }
}
