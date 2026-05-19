using AwesomeAssertions;
using ByteForge.Toolkit.Configuration;
using ByteForge.Toolkit.Logging;
using ByteForge.Toolkit.Tests.Helpers;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Tests reflected configuration schema metadata used by config editor hosts.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Configuration")]
    public class ConfigurationSchemaEditorTests
    {
        /// <summary>
        /// Cleans up temporary configuration files after each test.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
        }

        /// <summary>
        /// Verifies that non-configurable delegate properties are excluded from reflected schema.
        /// </summary>
        [TestMethod]
        public void GetRegisteredSectionSchemas_WithFileLoggerOptions_ShouldExcludeDelegateProperties()
        {
            var path = TestConfigurationHelper.CreateTempConfigFile(string.Empty);
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();

            config.Initialize(path);
            config.AddSection<FileLoggerOptions>("FileLogger");

            var schema = GetSchema(config, "FileLogger");

            schema.Items.Select(item => item.PropertyName).Should().NotContain(nameof(FileLoggerOptions.CustomFileNameProvider));
        }

        /// <summary>
        /// Verifies that externally enumerable option-backed properties surface as selectable options.
        /// </summary>
        [TestMethod]
        public void GetRegisteredSectionSchemas_WithOptionBackedTypes_ShouldExposeOptions()
        {
            var path = TestConfigurationHelper.CreateTempConfigFile(string.Empty);
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();

            config.Initialize(path);
            config.AddSection<FileLoggerOptions>("FileLogger");

            var fileEncoding = GetItem(config, "FileLogger", nameof(FileLoggerOptions.FileEncoding));
            var cultureInfo = GetItem(config, "Globalization", nameof(GlobalizationInfo.CultureInfo));

            fileEncoding.HasOptions.Should().BeTrue();
            fileEncoding.Options.Select(option => option.Value).Should().Contain("utf-8");
            cultureInfo.HasOptions.Should().BeTrue();
            cultureInfo.Options.Select(option => option.Value).Should().Contain("en-US");
        }

        /// <summary>
        /// Verifies that reflected configuration documentation uses generated XML comments as fallback descriptions.
        /// </summary>
        [TestMethod]
        public void GetItemDescription_WithTypedSection_ShouldUseXmlDocumentationSummary()
        {
            var path = TestConfigurationHelper.CreateTempConfigFile(string.Empty);
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();

            config.Initialize(path);
            config.AddSection<FileLoggerOptions>("FileLogger");

            var description = config.GetItemDescription("FileLogger", nameof(FileLoggerOptions.FileEncoding));

            description.Should().Be("Gets or sets the encoding used for writing to the log file.");
        }

        /// <summary>
        /// Gets a registered section schema by section name.
        /// </summary>
        /// <param name="config">The configuration manager under test.</param>
        /// <param name="sectionName">The section name to locate.</param>
        /// <returns>The matching section schema.</returns>
        private static ConfigSectionSchema GetSchema(IConfigurationManager config, string sectionName)
        {
            return config.GetRegisteredSectionSchemas()
                .Single(schema => string.Equals(schema.Name, sectionName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a registered item schema by section and property name.
        /// </summary>
        /// <param name="config">The configuration manager under test.</param>
        /// <param name="sectionName">The section name to locate.</param>
        /// <param name="propertyName">The property name to locate.</param>
        /// <returns>The matching item schema.</returns>
        private static ConfigItemSchema GetItem(IConfigurationManager config, string sectionName, string propertyName)
        {
            return GetSchema(config, sectionName)
                .Items
                .Single(item => string.Equals(item.PropertyName, propertyName, StringComparison.Ordinal));
        }
    }
}
