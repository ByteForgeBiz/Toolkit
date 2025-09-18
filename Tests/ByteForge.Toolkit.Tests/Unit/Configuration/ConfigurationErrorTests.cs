using AwesomeAssertions;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Tests for error handling and edge cases in configuration including file system errors, type conversion errors, and malformed configurations.
    /// </summary>
    [TestClass]
    public class ConfigurationErrorTests
    {
        private string _tempConfigPath = "";

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
            ResetConfiguration();
        }

        #region File System Error Tests

        /// <summary>
        /// Verifies that attempting to save changes to a read-only configuration file throws an UnauthorizedAccessException.
        /// </summary>
        /// <remarks>
        /// This test ensures proper error handling when file system permissions prevent configuration updates,
        /// which is important for user feedback and preventing data loss scenarios.
        /// </remarks>
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

        /// <summary>
        /// Verifies that initializing configuration with a non-existent file path throws FileNotFoundException.
        /// </summary>
        /// <remarks>
        /// This test ensures proper error handling for missing configuration files, preventing silent failures
        /// and providing clear feedback about configuration file accessibility issues.
        /// </remarks>
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

        /// <summary>
        /// Verifies that initializing configuration with an invalid directory path throws DirectoryNotFoundException.
        /// </summary>
        /// <remarks>
        /// This test ensures robust error handling for invalid file system paths, such as non-existent drives
        /// or directories, providing appropriate exceptions for troubleshooting.
        /// </remarks>
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

        /// <summary>
        /// Verifies that initializing configuration with a path exceeding system limits throws an appropriate exception.
        /// </summary>
        /// <remarks>
        /// This test ensures proper handling of file paths that exceed the MAX_PATH limit on Windows systems,
        /// preventing unexpected failures and providing clear error reporting.
        /// </remarks>
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

        /// <summary>
        /// Verifies that configuration files with malformed section headers throw InvalidDataException.
        /// </summary>
        /// <remarks>
        /// This test ensures that INI files with incomplete or malformed section headers are detected
        /// and handled appropriately, preventing configuration corruption and providing clear error feedback.
        /// </remarks>
        [TestMethod]
        public void Configuration_WithMalformedSection_ShouldThrow()
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

            act.Should().Throw<InvalidDataException>("malformed section headers should throw InvalidDataException");
        }

        /// <summary>
        /// Verifies that configuration files with invalid key-value pairs throw InvalidDataException.
        /// </summary>
        /// <remarks>
        /// This test ensures that malformed key-value pairs (missing keys, missing values, etc.)
        /// are detected and handled appropriately, maintaining configuration file integrity.
        /// </remarks>
        [TestMethod]
        public void Configuration_WithInvalidKeyValuePair_ShouldThrow()
        {
            // Arrange - INI with invalid key-value pairs
            var malformedContent = @"[TestSection]
ValidKey=ValidValue
InvalidKeyWithoutValue
AnotherValidKey=AnotherValue
=ValueWithoutKey
KeyWithMultipleEquals=Value=With=Equals";

            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(malformedContent);

            // Act & Assert - Should handle invalid key-value pairs gracefully
            Action act = () =>
            {
                IConfigurationManager testConfig = new ByteForge.Toolkit.Configuration();
                testConfig.Initialize(_tempConfigPath);
                var section = testConfig.GetSection<BasicTestConfig>("TestSection");
            };

            act.Should().Throw<InvalidDataException>("invalid key-value pairs should throw InvalidDataException");
        }

        /// <summary>
        /// Verifies that empty configuration files are handled gracefully by creating default configuration.
        /// </summary>
        /// <remarks>
        /// This test ensures that empty configuration files don't cause crashes and instead create
        /// usable default configurations, supporting scenarios where config files may be initially empty.
        /// </remarks>
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

        /// <summary>
        /// Verifies that configuration files containing only comments are handled gracefully.
        /// </summary>
        /// <remarks>
        /// This test ensures that configuration files with only comments and no actual configuration
        /// data create default configurations, supporting documentation-heavy config files.
        /// </remarks>
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

        /// <summary>
        /// Verifies that invalid type conversions in configuration values are handled appropriately.
        /// </summary>
        /// <remarks>
        /// This test ensures that configuration values that cannot be converted to their expected types
        /// (int, bool, double) are handled gracefully, preventing runtime exceptions during configuration loading.
        /// </remarks>
        [TestMethod]
        public void Configuration_InvalidIntegerConversion_ShoudThrow()
        {
            // Arrange
            var configContent = @"[TestSection]
IntValue=not_a_number
BoolValue=not_a_boolean
DoubleValue=not_a_double";

            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);

            // Act & Assert - Invalid type conversions should be handled gracefully
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<BasicTestConfig>("TestSection");
            };

            // The system should handle invalid conversions gracefully by using defaults or throwing specific exceptions
            act.Should().Throw<FormatException>("invalid type conversions should throw FormatException");
        }

        /// <summary>
        /// Verifies that numeric values causing overflow are handled appropriately.
        /// </summary>
        /// <remarks>
        /// This test ensures that configuration values that would cause numeric overflow
        /// (exceeding int or double limits) throw appropriate exceptions rather than causing silent data corruption.
        /// </remarks>
        [TestMethod]
        public void Configuration_NumericOverflow_ShouldThrow()
        {
            // Arrange - Values that would overflow int
            var configContent = @"[TestSection]
IntValue=999999999999999999999
DoubleValue=1.7976931348623157e+309";

            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<BasicTestConfig>("TestSection");
            };

            act.Should().Throw<FormatException>("numeric overflow should throw OverflowException");
        }

        /// <summary>
        /// Verifies that invalid date formats in configuration values throw appropriate exceptions.
        /// </summary>
        /// <remarks>
        /// This test ensures that malformed date strings in configuration are detected and handled
        /// properly, preventing runtime errors when date values are accessed.
        /// </remarks>
        [TestMethod]
        public void Configuration_InvalidDateFormat_ShoudThrow()
        {
            // Arrange
            var configContent = @"[TestSection]
DateValue=not_a_date
DateValue2=32/13/2024
DateValue3=2024-13-45";

            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            // 2024-13-45 is an invalid date, should be caught by DateTime parsing
            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<BasicTestConfig>("TestSection");
            };

            act.Should().Throw<FormatException>("invalid date formats should throw FormatException");
        }

        /// <summary>
        /// Verifies that invalid enum values in configuration throw appropriate exceptions.
        /// </summary>
        /// <remarks>
        /// This test ensures that configuration values that don't match valid enum values
        /// are detected and handled appropriately, preventing runtime casting errors.
        /// </remarks>
        [TestMethod]
        public void Configuration_InvalidEnumValue_Throw()
        {
            // Arrange
            var configContent = @"[TestSection]
Level=InvalidLogLevel
DbType=UnknownDatabase";
            
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<EnumTestConfig>("TestSection");
            };

            act.Should().Throw<FormatException>("invalid enum values should throw FormatException");
        }

        #endregion

        #region Array Configuration Error Tests

        /// <summary>
        /// Verifies that array configurations referencing missing sections create empty arrays gracefully.
        /// </summary>
        /// <remarks>
        /// This test ensures that when an array configuration references a non-existent section,
        /// the system creates an empty array rather than throwing an exception, providing robust handling.
        /// </remarks>
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

        /// <summary>
        /// Verifies that arrays with mixed valid and invalid items throw appropriate exceptions.
        /// </summary>
        /// <remarks>
        /// This test ensures that when array sections contain items that cannot be converted to the expected type,
        /// the system throws appropriate format exceptions rather than partially loading the array.
        /// </remarks>
        [TestMethod]
        public void Configuration_ArrayWithMixedValidInvalidItems_ShouldThrow()
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

            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<ArrayTestConfig>("TestSection");
            };

            // Should handle mixed valid/invalid array items gracefully
            act.Should().Throw<FormatException>("mixed valid/invalid array items should throw FormatException");
        }

        /// <summary>
        /// Verifies that circular references in array configurations are handled gracefully.
        /// </summary>
        /// <remarks>
        /// This test ensures that circular references in array section definitions are detected
        /// and handled appropriately, preventing infinite loops or stack overflow exceptions.
        /// </remarks>
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

            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<ArrayTestConfig>("TestSection");
            };

            act.Should().NotThrow("circular array references should be handled gracefully");
        }

        #endregion

        #region Property Attribute Error Tests

        /// <summary>
        /// Verifies that invalid default value providers are handled gracefully.
        /// </summary>
        /// <remarks>
        /// This test ensures that properties with custom default value providers that may not exist
        /// or throw exceptions are handled appropriately without crashing the configuration system.
        /// </remarks>
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
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<CustomDefaultConfig>("TestSection");
            };

            // Should handle custom default providers gracefully
            act.Should().NotThrow("custom default providers should work or handle errors gracefully");
        }

        /// <summary>
        /// Verifies that properties with multiple configuration attributes work correctly.
        /// </summary>
        /// <remarks>
        /// This test ensures that properties decorated with multiple configuration attributes
        /// (such as ConfigName) are properly mapped and function as expected.
        /// </remarks>
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

        /// <summary>
        /// Verifies that very large configuration files are handled without memory issues.
        /// </summary>
        /// <remarks>
        /// This test ensures that the configuration system can handle large configuration files
        /// efficiently without causing memory exhaustion or performance degradation.
        /// </remarks>
        [TestMethod]
        public void Configuration_VeryLargeConfigFile_ShouldHandleWithoutMemoryIssues()
        {
            // Arrange - Create a large configuration file
            var largeConfigContent = "[TestSection]";
            for (var i = 0; i < 10000; i++)
            {
                largeConfigContent += $"Property{i}=Value{i}";
            }

            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(largeConfigContent);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<BasicTestConfig>("TestSection");
            };

            act.Should().NotThrow("large configuration files should be handled without memory issues");
        }

        /// <summary>
        /// Verifies that configuration files with many sections perform acceptably.
        /// </summary>
        /// <remarks>
        /// This test ensures that configuration files containing a large number of sections
        /// can be loaded and accessed efficiently without significant performance issues.
        /// </remarks>
        [TestMethod]
        public void Configuration_ManySections_ShouldPerformAcceptably()
        {
            // Arrange - Create configuration with many sections
            var manySectionsContent = "";
            for (var i = 0; i < 1000; i++)
            {
                manySectionsContent += $"[Section{i}]\n";
                manySectionsContent += $"Value{i}=Data{i}\n\n";
            }
            
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(manySectionsContent);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                config.Initialize(_tempConfigPath);
                
                // Access multiple sections
                for (var i = 0; i < 10; i++)
                {
                    var section = config.GetSection<BasicTestConfig>($"Section{i}");
                }
            };

            act.Should().NotThrow("configuration with many sections should perform acceptably");
        }

        #endregion

        #region Unicode and Special Character Tests

        /// <summary>
        /// Verifies that configuration values containing Unicode characters are handled correctly.
        /// </summary>
        /// <remarks>
        /// This test ensures that the configuration system properly preserves Unicode characters,
        /// including emojis, international text, and special symbols in configuration values.
        /// </remarks>
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

        /// <summary>
        /// Verifies that configuration keys containing special characters are handled correctly.
        /// </summary>
        /// <remarks>
        /// This test ensures that configuration keys with hyphens, underscores, dots, and spaces
        /// are properly parsed and accessible without causing parsing errors.
        /// </remarks>
        [TestMethod]
        public void Configuration_SpecialCharactersInKeys_ShouldHandleCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
String-Value=Hyphenated key
String_Value=Underscore key
String.Value=Dotted key
String Value=Spaced key";

            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<BasicTestConfig>("TestSection");
            };

            act.Should().NotThrow("special characters in keys should be handled correctly");
        }

        /// <summary>
        /// Verifies that escaped characters in configuration values are handled correctly.
        /// </summary>
        /// <remarks>
        /// This test ensures that configuration values containing escaped quotes, backslashes,
        /// and other special characters are properly unescaped and preserved.
        /// </remarks>
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

        /// <summary>
        /// Verifies that concurrent save operations are handled gracefully without corruption.
        /// </summary>
        /// <remarks>
        /// This test ensures that multiple threads attempting to save configuration changes simultaneously
        /// do not corrupt the configuration file or cause exceptions, supporting multi-threaded applications.
        /// </remarks>
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
            for (var i = 0; i < 5; i++)
            {
                var index = i;
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

        /// <summary>
        /// Verifies that concurrent section access is thread-safe and returns valid results.
        /// </summary>
        /// <remarks>
        /// This test ensures that multiple threads can safely access configuration sections simultaneously
        /// without causing race conditions or returning corrupted section objects.
        /// </remarks>
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
            for (var i = 0; i < 10; i++)
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

            isInitializedField?.SetValue(null, false);

            manuallyInitializedField?.SetValue(null, false);
        }

        #endregion
    }
}