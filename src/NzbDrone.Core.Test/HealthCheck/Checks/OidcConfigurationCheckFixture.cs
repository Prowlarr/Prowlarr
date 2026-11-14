using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class OidcConfigurationCheckFixture : CoreTest<OidcConfigurationCheck>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>()))
                  .Returns("Some Error Message");

            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                  .Returns("Some Error Message");

            GivenAuthenticationMethod(AuthenticationType.Oidc);
            GivenOidcSettings("https://id.example.com", "prowlarr", "secret", "user", "openid profile email");
            GivenDiscoverable(true);
        }

        private void GivenAuthenticationMethod(AuthenticationType authenticationType)
        {
            Mocker.GetMock<IConfigFileProvider>()
                  .SetupGet(s => s.AuthenticationMethod)
                  .Returns(authenticationType);
        }

        private void GivenOidcSettings(string authority, string clientId, string clientSecret, string userIdentifier, string scopes)
        {
            var configFileProvider = Mocker.GetMock<IConfigFileProvider>();

            configFileProvider.SetupGet(s => s.OidcAuthority).Returns(authority);
            configFileProvider.SetupGet(s => s.OidcClientId).Returns(clientId);
            configFileProvider.SetupGet(s => s.OidcClientSecret).Returns(clientSecret);
            configFileProvider.SetupGet(s => s.OidcUserIdentifier).Returns(userIdentifier);
            configFileProvider.SetupGet(s => s.OidcScopes).Returns(scopes);
        }

        private void GivenDiscoverable(bool discoverable)
        {
            Mocker.GetMock<IOidcDiscoveryService>()
                  .Setup(s => s.IsDiscoverable(It.IsAny<string>()))
                  .Returns(discoverable);
        }

        [Test]
        public void should_return_ok_when_authentication_method_is_not_oidc()
        {
            GivenAuthenticationMethod(AuthenticationType.Forms);
            GivenOidcSettings(null, null, null, null, null);

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_ok_when_oidc_is_configured_and_discoverable()
        {
            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_error_when_client_id_is_missing()
        {
            GivenOidcSettings("https://id.example.com", "", "secret", "user", "openid");

            Subject.Check().ShouldBeError(wikiFragment: "oidc-not-configured");
        }

        [Test]
        public void should_return_error_when_scopes_do_not_include_openid()
        {
            GivenOidcSettings("https://id.example.com", "prowlarr", "secret", "user", "profile email");

            Subject.Check().ShouldBeError(wikiFragment: "oidc-not-configured");
        }

        [Test]
        public void should_return_error_when_authority_is_not_https()
        {
            GivenOidcSettings("http://id.example.com", "prowlarr", "secret", "user", "openid");

            Subject.Check().ShouldBeError(wikiFragment: "oidc-authority-not-secure");
        }

        [Test]
        public void should_return_error_when_authority_is_not_discoverable()
        {
            GivenDiscoverable(false);

            Subject.Check().ShouldBeError(wikiFragment: "oidc-configuration-unavailable");
        }
    }
}
