using AwesomeAssertions;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;
using ByteForge.Toolkit.Configuration;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Tests configuration reload behavior for raw values and cached strongly typed sections.
    /// </summary>
    [TestClass]
[TestCategory("Unit")]
    public class ConfigurationReloadTests
    {
        private string _tempConfigPath = null!;

        /// <summary>
        /// Cleans up temporary configuration files created by each test.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
        }

        /// <summary>
        /// Verifies that raw string reads observe values from the INI file after an explicit reload.
        /// </summary>
        [TestMethod]
        public void Reload_WhenFileChanges_ShouldRefreshRawValues()
        {
            // Arrange
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(
                @"[TestSection]
StringValue=Before");

            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            config.Initialize(_tempConfigPath);

            // Act
            File.WriteAllText(
                _tempConfigPath,
                @"[TestSection]
StringValue=After");
            config.Reload();

            // Assert
            config.GetString("TestSection", "StringValue").Should().Be("After");
        }

        /// <summary>
        /// Verifies that cached strongly typed sections are updated in place after an explicit reload.
        /// </summary>
        [TestMethod]
        public void Reload_WhenFileChanges_ShouldUpdateCachedTypedSectionInPlace()
        {
            // Arrange
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(
                @"[TestSection]
StringValue=Before
IntValue=1");

            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            config.Initialize(_tempConfigPath);
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Act
            File.WriteAllText(
                _tempConfigPath,
                @"[TestSection]
StringValue=After
IntValue=2");
            config.Reload();
            var sectionAfterReload = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            sectionAfterReload.Should().BeSameAs(section);
            section.StringValue.Should().Be("After");
            section.IntValue.Should().Be(2);
        }

        /// <summary>
        /// Verifies that array and dictionary backing sections are refreshed after an explicit reload.
        /// </summary>
        [TestMethod]
        public void Reload_WhenCollectionSectionsChange_ShouldRefreshCollections()
        {
            // Arrange
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(
                @"[TestSection]
StringArray=Items
FileFormats=Formats

[Items]
0=BeforeOne
1=BeforeTwo

[Formats]
Pdf=Before");

            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            config.Initialize(_tempConfigPath);
            var arrays = config.GetSection<ArrayTestConfig>("TestSection");
            var dictionaries = config.GetSection<DictionaryTestConfig>("TestSection");

            // Act
            File.WriteAllText(
                _tempConfigPath,
                @"[TestSection]
StringArray=Items
FileFormats=Formats

[Items]
0=AfterOne
1=AfterTwo
2=AfterThree

[Formats]
Pdf=After
Csv=New");
            config.Reload();

            // Assert
            arrays.StringArray.Should().Equal("AfterOne", "AfterTwo", "AfterThree");
            dictionaries.FileFormats.Should().ContainKey("Pdf").WhoseValue.Should().Be("After");
            dictionaries.FileFormats.Should().ContainKey("Csv").WhoseValue.Should().Be("New");
        }
    }
}
