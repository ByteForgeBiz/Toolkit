using AwesomeAssertions;
using ByteForge.Toolkit.Configuration;
using ByteForge.Toolkit.Security;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Tests for features migrated from the legacy client toolkit configuration system.
    /// </summary>
    [TestClass]
    public class ConfigurationMigrationTests
    {
        private string _tempConfigPath = null!;

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
            ResetConfiguration();
        }

        [TestMethod]
        public void Configuration_GetSetString_WithRegisteredEncryptedKey_ShouldRoundTripDecryptedValue()
        {
            var configContent = @"[Secrets]
Token=initial";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            config.RegisterEncrypted("Secrets", "Token");

            config.SetString("Secrets", "Token", "super-secret");

            config.GetString("Secrets", "Token").Should().Be("super-secret");

            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().NotContain("super-secret");
            savedContent.Should().Contain("Token=");
        }

        [TestMethod]
        public void ConfigSection_EncryptedProperty_ShouldLoadAndSaveEncryptedValue()
        {
            var encryptedValue = Encryptor.Default.Encrypt("api-key-123");
            var configContent = $@"[Secure]
ApiKey={encryptedValue}";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<EncryptedConfig>("Secure");
            section.ApiKey.Should().Be("api-key-123");

            section.ApiKey = "api-key-updated";
            config.Save();

            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().NotContain("api-key-updated");
            savedContent.Should().Contain("ApiKey=");
        }

        [TestMethod]
        public void ConfigSection_EncryptedDictionary_ShouldLoadAndSaveEncryptedEntries()
        {
            var encryptedValue = Encryptor.Default.Encrypt("secret-value");
            var configContent = $@"[Secure]
SecretMap=Secrets

[Secrets]
Api={encryptedValue}";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<EncryptedConfig>("Secure");
            section.SecretMap["Api"].Should().Be("secret-value");

            section.SecretMap["Api"] = "updated-secret";
            config.Save();

            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().NotContain("updated-secret");
            savedContent.Should().Contain("Api=");
        }

        private void ResetConfiguration()
        {
            var instanceField = typeof(ByteForge.Toolkit.Configuration.Configuration).GetField("_defaultInstance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);
        }
    }
}
