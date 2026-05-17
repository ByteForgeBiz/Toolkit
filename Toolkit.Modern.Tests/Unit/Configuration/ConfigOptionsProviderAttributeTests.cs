using AwesomeAssertions;
using ByteForge.Toolkit.Configuration;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Configuration")]
    public class ConfigOptionsProviderAttributeTests
    {
        /// <summary>
        /// Verifies that provider attributes create the configured provider instance.
        /// </summary>
        [TestMethod]
        public void CreateProvider_WithValidProviderType_ShouldCreateProvider()
        {
            var attribute = new ConfigOptionsProviderAttribute(typeof(TestOptionsProvider));

            var provider = attribute.CreateProvider();

            provider.Should().BeOfType<TestOptionsProvider>();
        }

        /// <summary>
        /// Verifies that provider attributes reject non-provider types.
        /// </summary>
        [TestMethod]
        public void Constructor_WithInvalidProviderType_ShouldThrowArgumentException()
        {
            Action action = () => new ConfigOptionsProviderAttribute(typeof(string));

            action.Should().Throw<ArgumentException>()
                .WithParameterName("providerType");
        }

        /// <summary>
        /// Verifies that provider attributes reject null provider types.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullProviderType_ShouldThrowArgumentNullException()
        {
            Action action = () => new ConfigOptionsProviderAttribute(null);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("providerType");
        }

        private sealed class TestOptionsProvider : IConfigOptionsProvider
        {
            public IReadOnlyCollection<ConfigItemOption> GetOptions(PropertyInfo property)
            {
                return Array.Empty<ConfigItemOption>();
            }
        }
    }
}
