using AwesomeAssertions;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Edge case tests for dictionary configuration functionality to ensure robustness and proper error handling.
    /// </summary>
    [TestClass]
    public class ConfigurationDictionaryEdgeCaseTests
    {
        private string _tempConfigPath = null!;

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
            ResetConfiguration();
        }

        #region Extreme Edge Cases

        /// <summary>
        /// Verifies that dictionaries with very long keys and values are handled correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryWithVeryLongKeysAndValues_ShouldWork()
        {
            // Arrange
            var longKey = new string('A', 500);
            var longValue = new string('B', 1000);
            var configContent = $@"[TestSection]
FileFormats=FileFormatsDict

[FileFormatsDict]
{longKey}={longValue}
ShortKey=ShortValue";
            
            IConfigurationManager config = new Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("dictionary should be loaded");
            section.FileFormats.Should().HaveCount(2, "should load all entries including long ones");
            section.FileFormats[longKey].Should().Be(longValue, "long key-value pairs should work");
            section.FileFormats["ShortKey"].Should().Be("ShortValue", "normal entries should still work");
        }

        /// <summary>
        /// Verifies that dictionaries with many entries (stress test) are handled correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryWithManyEntries_ShouldHandleCorrectly()
        {
            // Arrange
            var entriesCount = 100;
            var configBuilder = new System.Text.StringBuilder();
            configBuilder.AppendLine("[TestSection]");
            configBuilder.AppendLine("FileFormats=FileFormatsDict");
            configBuilder.AppendLine();
            configBuilder.AppendLine("[FileFormatsDict]");

            for (var i = 0; i < entriesCount; i++)
            {
                configBuilder.AppendLine($"Key{i:D3}=Value{i:D3}");
            }
            
            IConfigurationManager config = new Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configBuilder.ToString());
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("dictionary should be loaded");
            section.FileFormats.Should().HaveCount(entriesCount, $"should load all {entriesCount} entries");
            
            // Spot check a few entries
            section.FileFormats["Key000"].Should().Be("Value000", "first entry should be correct");
            section.FileFormats["Key050"].Should().Be("Value050", "middle entry should be correct");
            section.FileFormats["Key099"].Should().Be("Value099", "last entry should be correct");
        }

        /// <summary>
        /// Verifies that dictionary keys with Unicode characters are handled correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryWithUnicodeKeys_ShouldWork()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=FileFormatsDict

[FileFormatsDict]
Café=CoffeeValue
日本語=JapaneseValue
Москва=RussianValue
🚀=EmojiValue";
            
            IConfigurationManager config = new Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("dictionary should be loaded");
            section.FileFormats["Café"].Should().Be("CoffeeValue", "French characters should work");
            section.FileFormats["日本語"].Should().Be("JapaneseValue", "Japanese characters should work");
            section.FileFormats["Москва"].Should().Be("RussianValue", "Cyrillic characters should work");
            section.FileFormats["🚀"].Should().Be("EmojiValue", "Emoji characters should work");
        }

        /// <summary>
        /// Verifies that dictionary values with line breaks and special INI characters are handled correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryWithSpecialINICharacters_ShouldEscapeCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=FileFormatsDict

[FileFormatsDict]
SemicolonKey=Value;WithSemicolon
HashKey=Value#WithHash
EqualsKey=Value=WithEquals
BracketKey=Value[WithBrackets]
QuoteKey=Value""WithQuotes""";
            
            IConfigurationManager config = new Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("dictionary should be loaded");
            section.FileFormats["SemicolonKey"].Should().Be("Value;WithSemicolon", "semicolons in values should work");
            section.FileFormats["HashKey"].Should().Be("Value#WithHash", "hash characters should work");
            section.FileFormats["EqualsKey"].Should().Be("Value=WithEquals", "equals in values should work");
            section.FileFormats["BracketKey"].Should().Be("Value[WithBrackets]", "brackets in values should work");
            section.FileFormats["QuoteKey"].Should().Be("Value\"WithQuotes\"", "quotes in values should work");
        }

        #endregion

        #region Memory and Performance Edge Cases

        /// <summary>
        /// Verifies that loading and saving large dictionaries doesn't cause memory issues.
        /// </summary>
        [TestMethod]
        public void ConfigSection_LargeDictionaryLoadAndSave_ShouldNotCauseMemoryIssues()
        {
            // Arrange
            var entriesCount = 1000;
            IConfigurationManager config = new Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile("[TestSection]");
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<DictionaryTestConfig>("TestSection");
            section.FileFormats = [];

            // Create large dictionary
            for (var i = 0; i < entriesCount; i++)
            {
                section.FileFormats[$"LargeKey{i:D4}"] = $"LargeValue{i:D4}_with_some_additional_content_to_make_it_larger";
            }

            // Act - Save large dictionary
            var startTime = DateTime.Now;
            config.Save();
            var saveTime = DateTime.Now - startTime;

            // Reload to test loading performance
            config = new Toolkit.Configuration();
            config.Initialize(_tempConfigPath);
            
            startTime = DateTime.Now;
            section = config.GetSection<DictionaryTestConfig>("TestSection");
            var loadTime = DateTime.Now - startTime;

            // Assert
            section.FileFormats.Should().NotBeNull("large dictionary should be loaded");
            section.FileFormats.Should().HaveCount(entriesCount, $"should load all {entriesCount} entries");
            saveTime.TotalSeconds.Should().BeLessThan(10, "save operation should complete in reasonable time");
            loadTime.TotalSeconds.Should().BeLessThan(10, "load operation should complete in reasonable time");
        }

        #endregion

        #region Concurrent Access Edge Cases

        /// <summary>
        /// Verifies that multiple dictionary properties in the same config section work correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_MultipleDictionariesInSameSection_ShouldWorkCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
Connections=DatabaseConnections
Features=FeatureFlags
Name=TestConfig

[DatabaseConnections]
Production=Server=prod;Database=app
Staging=Server=stage;Database=app
Development=Server=dev;Database=app

[FeatureFlags]
Feature1=true
Feature2=false
Feature3=experimental";
            
            IConfigurationManager config = new Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<ComplexDictionaryTestConfig>("TestSection");

            // Assert
            section.Name.Should().Be("TestConfig", "regular property should load");
            
            section.Connections.Should().NotBeNull("first dictionary should load");
            section.Connections.Should().HaveCount(3, "first dictionary should have all entries");
            section.Connections["Production"].Should().Be("Server=prod;Database=app");
            
            section.Features.Should().NotBeNull("second dictionary should load");
            section.Features.Should().HaveCount(3, "second dictionary should have all entries");
            section.Features["Feature1"].Should().Be("true");
            section.Features["Feature2"].Should().Be("false");
        }

        /// <summary>
        /// Verifies that dictionaries work correctly alongside arrays in the same configuration.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionariesAndArraysTogether_ShouldWorkCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
Name=ComplexConfig
Connections=DatabaseConnections
SupportedEnvironments=Environments
IsEnabled=true

[DatabaseConnections]
Prod=prod-connection
Stage=stage-connection

[Environments]
0=Production
1=Staging
2=Development";
            
            IConfigurationManager config = new Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<ComplexDictionaryTestConfig>("TestSection");

            // Assert
            section.Name.Should().Be("ComplexConfig", "regular property should load");
            section.IsEnabled.Should().BeTrue("boolean property should load");
            
            section.Connections.Should().NotBeNull("dictionary should load");
            section.Connections.Should().HaveCount(2, "dictionary should have correct entries");
            section.Connections["Prod"].Should().Be("prod-connection");
            
            section.SupportedEnvironments.Should().NotBeNull("array should load");
            section.SupportedEnvironments.Should().HaveCount(3, "array should have correct entries");
            section.SupportedEnvironments.Should().Contain("Production");
            section.SupportedEnvironments.Should().Contain("Staging");
            section.SupportedEnvironments.Should().Contain("Development");
        }

        #endregion

        #region Malformed Configuration Edge Cases

        /// <summary>
        /// Verifies that circular references between sections don't cause infinite loops.
        /// </summary>
        [TestMethod]
        public void ConfigSection_CircularSectionReferences_ShouldNotCauseInfiniteLoop()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=SectionA

[SectionA]
Key1=Value1
Redirect=SectionB

[SectionB]
Key2=Value2
BackToA=SectionA";
            
            IConfigurationManager config = new Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act & Assert - Should not hang or throw
            Action act = () =>
            {
                var section = config.GetSection<DictionaryTestConfig>("TestSection");
            };

            act.Should().NotThrow("circular references should not cause crashes");
        }

        #endregion

        #region Null and Empty Value Edge Cases

        /// <summary>
        /// Verifies that null dictionary assignments are handled correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_NullDictionaryAssignment_ShouldHandleCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]";
            IConfigurationManager config = new Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<DictionaryTestConfig>("TestSection");
            section.FileFormats = null!;

            // Act & Assert - Should not throw when saving null dictionary
            Action act = () => config.Save();
            act.Should().NotThrow("null dictionary assignment should be handled gracefully");
        }

        /// <summary>
        /// Verifies that dictionaries with all empty values are handled correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryWithAllEmptyValues_ShouldWork()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=FileFormatsDict

[FileFormatsDict]
Key1=
Key2=
Key3=";
            
            IConfigurationManager config = new Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("dictionary should be loaded");
            
            // Check behavior with empty values
            if (section.FileFormats.Count > 0)
            {
                foreach (var kvp in section.FileFormats)
                {
                    kvp.Value.Should().Be("", $"empty value for key {kvp.Key} should be preserved");
                }
            }
        }

        #endregion

        #region Case Sensitivity Edge Cases

        /// <summary>
        /// Verifies that case sensitivity behavior is consistent across operations.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryCaseSensitivityConsistency_ShouldBeConsistent()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=FileFormatsDict

[FileFormatsDict]
LowerCase=lowercase_value
UPPERCASE=uppercase_value
MixedCase=mixedcase_value";
            
            IConfigurationManager config = new Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert - Test various case combinations
            section.FileFormats.Should().NotBeNull("dictionary should be loaded");
            
            // Test case-insensitive behavior (StringComparer.OrdinalIgnoreCase)
            section.FileFormats["lowercase"].Should().Be("lowercase_value", "case-insensitive lookup should work");
            section.FileFormats["LOWERCASE"].Should().Be("lowercase_value", "case-insensitive lookup should work");
            section.FileFormats["uppercase"].Should().Be("uppercase_value", "case-insensitive lookup should work");
            section.FileFormats["MIXEDCASE"].Should().Be("mixedcase_value", "case-insensitive lookup should work");
            section.FileFormats["mixedcase"].Should().Be("mixedcase_value", "case-insensitive lookup should work");
        }

        #endregion

        #region Helper Methods

        private void ResetConfiguration()
        {
            var instanceField = typeof(Toolkit.Configuration).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            var isInitializedField = typeof(Toolkit.Configuration).GetField("IsInitialized", BindingFlags.Public | BindingFlags.Static);
            var manuallyInitializedField = typeof(Toolkit.Configuration).GetField("_manuallyInitialized", BindingFlags.NonPublic | BindingFlags.Static);

            if (instanceField != null)
            {
                var newLazy = new Lazy<Toolkit.Configuration>();
                instanceField.SetValue(null, newLazy);
            }

            isInitializedField?.SetValue(null, false);

            manuallyInitializedField?.SetValue(null, false);
        }

        #endregion
    }
}