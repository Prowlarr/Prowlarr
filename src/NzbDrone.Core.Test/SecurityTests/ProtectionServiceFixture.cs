using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Security;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.SecurityTests
{
    [TestFixture]
    public class ProtectionServiceFixture : CoreTest<ProtectionService>
    {
        private string _protectionKey;

        [SetUp]
        public void Setup()
        {
            _protectionKey = Guid.NewGuid().ToString().Replace("-", "");

            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.DownloadProtectionKey)
                  .Returns(_protectionKey);
        }

        [Test]
        public void should_encrypt_and_decrypt_string()
        {
            const string plainText = "https://prowlarr.com";

            var encrypted = Subject.Protect(plainText);
            var decrypted = Subject.UnProtect(encrypted);

            decrypted.Should().Be(plainText);
        }
    }
}
