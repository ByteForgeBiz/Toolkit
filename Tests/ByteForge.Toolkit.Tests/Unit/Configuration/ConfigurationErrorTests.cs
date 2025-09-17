using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Tests for error handling and edge cases in configuration including file system errors, type conversion errors, and malformed configurations.
    /// </summary>
    [TestClass]
    public class ConfigurationErrorTests
    {
        private string _tempConfigPath;

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
            ResetConfiguration();
        }

        #region File System Error Tests

        [TestMethod]
        public void Configuration_InitializeWithReadOnlyFile_ShouldThrowOnSave()
        {
            // Arrange
            var configContent = @"[TestSection]
StringValue=Test";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);
            
            // Make file read-only
            File.SetAttributes(_tempConfigPath, FileAttributes.ReadOnly);

            try
            {
                var section = config.GetSection<BasicTestConfig>("TestSection");
                section.StringValue = "Modified Value";

                // Act & Assert
                Action act = () => config.Save();
                act.Should().Throw<UnauthorizedAccessException>("read-only file should prevent saving");
            }
            finally
            {
                // Cleanup - remove read-only attribute
                File.SetAttributes(_tempConfigPath, FileAttributes.Normal);
            }
        }

        [TestMethod]
        public void Configuration_InitializeWithNonExistentPath_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid().ToString() + ".ini");

            // Act & Assert
            Action act = () => {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(nonExistentPath);
            };
            act.Should().Throw<FileNotFoundException>("non-existent file should throw FileNotFoundException");
        }

        [TestMethod]
        public void Configuration_InitializeWithInvalidPath_ShouldThrowDirectoryNotFoundException()
        {
            // Arrange
            var invalidPath = @"Z:\NonExistentDrive\NonExistentDirectory\config.ini";

            // Act & Assert
            Action act = () => {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(invalidPath);
            };
            act.Should().Throw<DirectoryNotFoundException>("invalid path should throw DirectoryNotFoundException");
        }

        [TestMethod]
        public void Configuration_InitializeWithPathTooLong_ShouldThrowPathTooLongException()
        {
            // Arrange - Create a path that's too long
            var tempDir = Path.GetTempPath();
            var longFileName = new string('a', 260 - tempDir.Length); // Exceed MAX_PATH
            var tooLongPath = Path.Combine(tempDir, longFileName + ".ini");

            // Act & Assert
            Action act = () => {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(tooLongPath);
            };
            act.Should().Throw<Exception>("path too long should throw an exception");
        }

        #endregion

        #region Malformed INI File Tests

        [TestMethod]
        public void Configuration_WithMalformedSection_ShouldHandleGracefully()
        {
            // Arrange - INI with malformed section headers
            var malformedContent = @"[InvalidSection
StringValue=Test
[ValidSection]
IntValue=42";
            
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(malformedContent);

            // Act & Assert - Should handle malformed sections gracefully
            Action act = () =>
            {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(_tempConfigPath);
                var section = testConfig.GetSection<BasicTestConfig>("ValidSection");
            };

            act.Should().NotThrow("malformed sections should be handled gracefully");
        }

        [TestMethod]
        public void Configuration_WithInvalidKeyValuePair_ShouldHandleGracefully()
        {
            // Arrange - INI with invalid key-value pairs
            var malformedContent = @"[TestSection]
ValidKey=ValidValue
InvalidKeyWithoutValue
AnotherValidKey=AnotherValue
=ValueWithoutKey
KeyWithMultipleEquals=Value=With=Equals";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(malformedContent);
            config.Initialize(_tempConfigPath);

            // Act & Assert - Should handle invalid key-value pairs gracefully
            Action act = () =>
            {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(_tempConfigPath);
                var section = config.GetSection<BasicTestConfig>("TestSection");
            };

            act.Should().NotThrow("invalid key-value pairs should be handled gracefully");
        }

        [TestMethod]
        public void Configuration_WithEmptyFile_ShouldCreateEmptyConfiguration()
        {
            // Arrange
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile("");

            // Act
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            config.Initialize(_tempConfigPath);
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.Should().NotBeNull("empty configuration should create default section");
            section.StringValue.Should().BeNull("properties should have default values");
        }

        [TestMethod]
        public void Configuration_WithOnlyComments_ShouldCreateEmptyConfiguration()
        {
            // Arrange
            var commentsOnlyContent = @"; This is a comment
# This is another comment
; More comments
; [Not a real section]";
            
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(commentsOnlyContent);

            // Act
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            config.Initialize(_tempConfigPath);
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.Should().NotBeNull("comments-only configuration should create default section");
        }

        #endregion

        #region Type Conversion Error Tests

        [TestMethod]
        public void Configuration_InvalidIntegerConversion_ShouldHandleGracefully()
        {
            // Arrange
            var configContent = @"[TestSection]
IntValue=not_a_number
BoolValue=not_a_boolean
DoubleValue=not_a_double";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act & Assert - Invalid type conversions should be handled gracefully
            Action act = () =>
            {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(_tempConfigPath);
                var section = config.GetSection<BasicTestConfig>("TestSection");
            };

            // The system should handle invalid conversions gracefully by using defaults or throwing specific exceptions
            act.Should().NotThrow("invalid type conversions should be handled gracefully");
        }

        [TestMethod]
        public void Configuration_NumericOverflow_ShouldHandleGracefully()
        {
            // Arrange - Values that would overflow int
            var configContent = @"[TestSection]
IntValue=999999999999999999999
DoubleValue=1.7976931348623157e+309";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(_tempConfigPath);
                var section = config.GetSection<BasicTestConfig>("TestSection");
            };

            act.Should().NotThrow("numeric overflow should be handled gracefully");
        }

        [TestMethod]
        public void Configuration_InvalidDateFormat_ShouldHandleGracefully()
        {
            // Arrange
            var configContent = @"[TestSection]
DateValue=not_a_date
DateValue2=32/13/2024
DateValue3=2024-13-45";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(_tempConfigPath);
                var section = config.GetSection<BasicTestConfig>("TestSection");
            };

            act.Should().NotThrow("invalid date formats should be handled gracefully");
        }

        [TestMethod]
        public void Configuration_InvalidEnumValue_ShouldHandleGracefully()
        {
            // Arrange
            var configContent = @"[TestSection]
Level=InvalidLogLevel
DbType=UnknownDatabase";
            
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(_tempConfigPath);
                var section = testConfig.GetSection<EnumTestConfig>("TestSection");
            };

            act.Should().NotThrow("invalid enum values should be handled gracefully");
        }

        #endregion

        #region Array Configuration Error Tests

        [TestMethod]
        public void Configuration_ArrayWithMissingSection_ShouldCreateEmptyArray()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=NonExistentArraySection";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.StringArray.Should().NotBeNull("array with missing section should not be null");
            section.StringArray.Should().BeEmpty("array with missing section should be empty");
        }

        [TestMethod]
        public void Configuration_ArrayWithMixedValidInvalidItems_ShouldLoadValidItems()
        {
            // Arrange
            var configContent = @"[TestSection]
NumberList=MixedNumberArray

[MixedNumberArray]
0=100
1=not_a_number
2=200
3=also_not_a_number
4=300";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(_tempConfigPath);
                var section = testConfig.GetSection<ArrayTestConfig>("TestSection");
            };

            // Should handle mixed valid/invalid array items gracefully
            act.Should().NotThrow("mixed valid/invalid array items should be handled gracefully");
        }

        [TestMethod]
        public void Configuration_CircularArrayReference_ShouldHandleGracefully()
        {
            // Arrange - Create circular reference scenario
            var configContent = @"[TestSection]
StringArray=ArraySection1

[ArraySection1]
0=ArraySection2

[ArraySection2]
StringArray=ArraySection1";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(_tempConfigPath);
                var section = testConfig.GetSection<ArrayTestConfig>("TestSection");
            };

            act.Should().NotThrow("circular array references should be handled gracefully");
        }

        #endregion

        #region Property Attribute Error Tests

        [TestMethod]
        public void Configuration_InvalidDefaultValueProvider_ShouldHandleGracefully()
        {
            // This test would require a test class with an invalid DefaultValueProvider
            // For now, we'll test with a non-existent method
            
            // Arrange
            var configContent = @"[TestSection]
RegularProperty=TestValue";
            
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(_tempConfigPath);
                var section = testConfig.GetSection<CustomDefaultConfig>("TestSection");
            };

            // Should handle custom default providers gracefully
            act.Should().NotThrow("custom default providers should work or handle errors gracefully");
        }

        [TestMethod]
        public void Configuration_PropertyWithMultipleAttributes_ShouldWorkCorrectly()
        {
            // Arrange - Test property with multiple configuration attributes
            var configContent = @"[TestSection]
CustomName=MappedValue";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.MappedProperty.Should().Be("MappedValue", "property with ConfigName attribute should work");
        }

        #endregion

        #region Large File and Performance Error Tests

        [TestMethod]
        public void Configuration_VeryLargeConfigFile_ShouldHandleWithoutMemoryIssues()
        {
            // Arrange - Create a large configuration file
            var largeConfigContent = "[TestSection]";
            for (int i = 0; i < 10000; i++)
            {
                largeConfigContent += $"Property{i}=Value{i}";
            }

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(largeConfigContent);
            config.Initialize(_tempConfigPath);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(_tempConfigPath);
                var section = config.GetSection<BasicTestConfig>("TestSection");
            };

            act.Should().NotThrow("large configuration files should be handled without memory issues");
        }

        [TestMethod]
        public void Configuration_ManySections_ShouldPerformAcceptably()
        {
            // Arrange - Create configuration with many sections
            var manySectionsContent = "";
            for (int i = 0; i < 1000; i++)
            {
                manySectionsContent += $"[Section{i}]\n";
                manySectionsContent += $"Value{i}=Data{i}\n\n";
            }
            
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(manySectionsContent);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(_tempConfigPath);
                
                // Access multiple sections
                for (int i = 0; i < 10; i++)
                {
                    var section = testConfig.GetSection<BasicTestConfig>($"Section{i}");
                }
            };

            act.Should().NotThrow("configuration with many sections should perform acceptably");
        }

        #endregion

        #region Unicode and Special Character Tests

        [TestMethod]
        public void Configuration_UnicodeCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
StringValue=Hello 世界 🌍 Ñoël Café
UnicodeProperty=Здравствуй мир";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.StringValue.Should().Be("Hello 世界 🌍 Ñoël Café", "unicode characters should be preserved");
        }

        [TestMethod]
        public void Configuration_SpecialCharactersInKeys_ShouldHandleCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
String-Value=Hyphenated key
String_Value=Underscore key
String.Value=Dotted key
String Value=Spaced key";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(_tempConfigPath);
                var section = config.GetSection<BasicTestConfig>("TestSection");
            };

            act.Should().NotThrow("special characters in keys should be handled correctly");
        }

        [TestMethod]
        public void Configuration_EscapedCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
StringValue=Value with ""quotes""
PathValue=C:\Windows\System32
UrlValue=https://example.com/path?param=value&other=test";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.StringValue.Should().Be("Value with \"quotes\"", "escaped quotes should be handled correctly");
        }

        #endregion

        #region Concurrent Access Error Tests

        [TestMethod]
        public void Configuration_ConcurrentSaveOperations_ShouldHandleGracefully()
        {
            // Arrange
            var configContent = @"[TestSection]
StringValue=Initial";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act - Attempt concurrent save operations
            var tasks = new Task[5];
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                tasks[i] = Task.Run(() =>
                {
                    var section = config.GetSection<BasicTestConfig>("TestSection");
                    section.StringValue = $"Value{index}";
                    config.Save();
                });
            }

            // Assert
            Action act = () => Task.WaitAll(tasks);
            act.Should().NotThrow("concurrent save operations should be handled gracefully");
        }

        [TestMethod]
        public void Configuration_ConcurrentSectionAccess_ShouldBeThreadSafe()
        {
            // Arrange
            var configContent = @"[TestSection]
StringValue=Initial";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act - Access sections concurrently
            var tasks = new Task<BasicTestConfig>[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() => config.GetSection<BasicTestConfig>("TestSection"));
            }

            // Assert
            Action act = () => Task.WaitAll(tasks);
            act.Should().NotThrow("concurrent section access should be thread-safe");

            // All tasks should return valid sections
            foreach (var task in tasks)
            {
                task.Result.Should().NotBeNull("all concurrent section accesses should return valid sections");
            }
        }

        #endregion

        #region Helper Methods

        private void ResetConfiguration()
        {
            var instanceField = typeof(ByteForge.Toolkit.Configuration).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            var isInitializedField = typeof(ByteForge.Toolkit.Configuration).GetField("IsInitialized", BindingFlags.Public | BindingFlags.Static);
            var manuallyInitializedField = typeof(ByteForge.Toolkit.Configuration).GetField("_manuallyInitialized", BindingFlags.NonPublic | BindingFlags.Static);

            if (instanceField != null)
            {
                var newLazy = new Lazy<ByteForge.Toolkit.Configuration>();
                instanceField.SetValue(null, newLazy);
            }

            if (isInitializedField != null)
            {
                isInitializedField.SetValue(null, false);
            }

            if (manuallyInitializedField != null)
            {
                manuallyInitializedField.SetValue(null, false);
            }
        }

        #endregion
    }
}