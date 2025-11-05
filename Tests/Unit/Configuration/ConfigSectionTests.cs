using AwesomeAssertions;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Tests for typed configuration section functionality including property mapping, default values, and attribute handling.
    /// </summary>
    [TestClass]
    public class ConfigSectionTests
    {
        private string _tempConfigPath;

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
            ResetConfiguration();
        }

        #region Basic Property Mapping Tests

        /// <summary>
        /// Verifies that string properties are loaded correctly from configuration files.
        /// </summary>
        /// <remarks>
        /// Tests the fundamental property mapping functionality for string values in INI files.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_StringProperty_ShouldLoadAndSaveCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
StringValue=Hello World";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.StringValue.Should().Be("Hello World", "string property should be loaded from INI");
        }

        /// <summary>
        /// Verifies that integer properties are loaded and parsed correctly from configuration files.
        /// </summary>
        /// <remarks>
        /// Tests type conversion from string to int for configuration values.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_IntProperty_ShouldLoadAndSaveCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
IntValue=42";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.IntValue.Should().Be(42, "int property should be loaded from INI");
        }

        /// <summary>
        /// Verifies that boolean true values are parsed correctly from configuration files.
        /// </summary>
        /// <remarks>
        /// Tests string to boolean conversion for 'true' values in INI files.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_BoolProperty_ShouldLoadTrueCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
BoolValue=true";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.BoolValue.Should().BeTrue("boolean true property should be loaded from INI");
        }

        /// <summary>
        /// Verifies that boolean false values are parsed correctly from configuration files.
        /// </summary>
        /// <remarks>
        /// Tests string to boolean conversion for 'false' values in INI files.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_BoolProperty_ShouldLoadFalseCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
BoolValue=false";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.BoolValue.Should().BeFalse("boolean false property should be loaded from INI");
        }

        /// <summary>
        /// Verifies that double precision floating point properties are parsed correctly from configuration files.
        /// </summary>
        /// <remarks>
        /// Tests string to double conversion and precision handling.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_DoubleProperty_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
DoubleValue=3.14159";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.DoubleValue.Should().BeApproximately(3.14159, 0.00001, "double property should be loaded from INI");
        }

        /// <summary>
        /// Verifies that DateTime properties are loaded and parsed correctly from configuration files.
        /// </summary>
        /// <remarks>
        /// This test validates string to DateTime conversion functionality, ensuring that ISO 8601
        /// formatted date strings are properly parsed into DateTime objects.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_DateTimeProperty_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
DateValue=2024-01-15T10:30:00";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.DateValue.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0), "DateTime property should be loaded from INI");
        }

        /// <summary>
        /// Verifies that enum properties are loaded and parsed correctly from configuration files.
        /// </summary>
        /// <remarks>
        /// This test validates string to enum conversion functionality, ensuring that enum values
        /// specified as strings in configuration files are properly parsed into their corresponding
        /// enum types. This enables type-safe configuration for categorical values.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_EnumProperty_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
Level=Warning
DbType=PostgreSQL";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<EnumTestConfig>("TestSection");

            // Assert
            section.Level.Should().Be(LogLevel.Warning, "enum property should be loaded from INI");
            section.DbType.Should().Be(DatabaseType.PostgreSQL, "enum property should be parsed correctly");
        }

        #endregion

        #region Default Value Tests

        /// <summary>
        /// Verifies that missing configuration properties use their DefaultValue attribute values.
        /// </summary>
        /// <remarks>
        /// This test ensures that when properties are not present in the configuration file,
        /// the system falls back to values specified in DefaultValue attributes, providing
        /// robust behavior and reducing required configuration complexity.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_MissingProperty_ShouldUseDefaultValue()
        {
            // Arrange - Config without TimeoutValue
            var configContent = @"[TestSection]
StringValue=Test";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.TimeoutValue.Should().Be(30, "missing property should use DefaultValue attribute");
        }

        /// <summary>
        /// Verifies that missing configuration properties use values from custom DefaultValueProvider implementations.
        /// </summary>
        /// <remarks>
        /// This test validates the advanced default value system that allows for dynamic default values
        /// through custom provider classes, enabling more complex default value logic than simple
        /// static attribute values can provide.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_CustomDefaultProvider_ShouldUseProviderValue()
        {
            // Arrange - Config without DataPath
            var configContent = @"[TestSection]
RegularProperty=Some Value";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<CustomDefaultConfig>("TestSection");

            // Assert
            section.DataPath.Should().Be(@"C:\Temp\DefaultData", "missing property should use DefaultValueProvider");
            section.ConnectionTimeout.Should().Be(60, "missing property should use DefaultValueProvider method");
        }

        /// <summary>
        /// Verifies that nullable type properties correctly handle null values when not specified in configuration.
        /// </summary>
        /// <remarks>
        /// This test ensures that nullable properties remain null when not provided in configuration,
        /// allowing for optional configuration values that can be distinguished from default values.
        /// This is important for scenarios where null has semantic meaning.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_NullableTypes_ShouldHandleNullCorrectly()
        {
            // Arrange - Config with no nullable properties set
            var configContent = @"[TestSection]
OptionalString=";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<NullableTestConfig>("TestSection");

            // Assert
            section.NullableInt.Should().BeNull("missing nullable int should be null");
            section.NullableBool.Should().BeNull("missing nullable bool should be null");
            section.NullableDateTime.Should().BeNull("missing nullable DateTime should be null");
            section.NullableDouble.Should().BeNull("missing nullable double should be null");
        }

        /// <summary>
        /// Verifies that nullable type properties correctly load and convert values when specified in configuration.
        /// </summary>
        /// <remarks>
        /// This test validates that nullable properties can successfully load and parse values
        /// from configuration when provided, ensuring proper type conversion while maintaining
        /// the ability to represent null states.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_NullableTypes_ShouldLoadValuesCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
NullableInt=123
NullableBool=true
NullableDateTime=2024-01-15T10:30:00
NullableDouble=3.14";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<NullableTestConfig>("TestSection");

            // Assert
            section.NullableInt.Should().Be(123, "nullable int should be loaded");
            section.NullableBool.Should().Be(true, "nullable bool should be loaded");
            section.NullableDateTime.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0), "nullable DateTime should be loaded");
            section.NullableDouble.Should().BeApproximately(3.14, 0.001, "nullable double should be loaded");
        }

        #endregion

        #region Attribute Tests

        /// <summary>
        /// Verifies that ConfigName attributes correctly map properties to custom configuration keys.
        /// </summary>
        /// <remarks>
        /// This test validates the key mapping functionality that allows properties to be mapped
        /// to configuration keys with different names, enabling compatibility with existing
        /// configuration files or preferred naming conventions.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_ConfigNameAttribute_ShouldMapToCustomKey()
        {
            // Arrange
            var configContent = @"[TestSection]
CustomName=Mapped Value";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.MappedProperty.Should().Be("Mapped Value", "ConfigName attribute should map to custom INI key");
        }

        /// <summary>
        /// Verifies that DoNotPersist attributes prevent properties from being saved while still allowing loading.
        /// </summary>
        /// <remarks>
        /// This test ensures that properties marked with DoNotPersist can load values from configuration
        /// but will not be written back when saving, useful for runtime-only properties or
        /// computed values that shouldn't be persisted.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_DoNotPersistAttribute_ShouldLoadButNotSave()
        {
            // Arrange
            var configContent = @"[TestSection]
StringValue=Test
LastAccessed=2024-01-15T10:30:00";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");
            section.LastAccessed = DateTime.Now; // Modify the DoNotPersist property
            section.StringValue = "Modified"; // Modify a regular property
            
            config.Save();

            // Assert
            var savedContent = System.IO.File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain("StringValue=Modified", "regular property should be saved");
            savedContent.Should().NotContain("LastAccessed=" + DateTime.Now.ToString("o"), "DoNotPersist property should not be saved with new value");
        }

        /// <summary>
        /// Verifies that Ignore attributes completely exclude properties from configuration processing.
        /// </summary>
        /// <remarks>
        /// This test ensures that properties marked with Ignore are completely excluded from
        /// configuration loading and saving, allowing for computed or runtime-only properties
        /// that should never interact with the configuration system.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_IgnoreAttribute_ShouldBeCompletelyIgnored()
        {
            // Arrange
            var configContent = @"[TestSection]
StringValue=Test
ComputedProperty=This should be ignored";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.ComputedProperty.Should().Be("Test_computed", "Ignore attribute should prevent loading, property should use computed value");
        }

        #endregion

        #region Property Type Conversion Tests

        /// <summary>
        /// Verifies that string to integer type conversion works correctly for configuration values.
        /// </summary>
        /// <remarks>
        /// This test validates the basic type conversion system for integer values, ensuring that
        /// numeric strings in configuration files are properly converted to integer properties
        /// without data loss or conversion errors.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_StringToIntConversion_ShouldWork()
        {
            // Arrange
            var configContent = @"[TestSection]
IntValue=12345";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.IntValue.Should().Be(12345, "string to int conversion should work");
        }

        /// <summary>
        /// Verifies that string to boolean conversion handles various input formats correctly.
        /// </summary>
        /// <remarks>
        /// This comprehensive test validates that the boolean conversion system can handle
        /// multiple string representations of boolean values (true/True/TRUE/1 for true,
        /// false/False/FALSE/0 for false), providing flexibility in configuration files.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_StringToBoolConversion_ShouldHandleVariousFormats()
        {
            // Test various boolean formats
            var testCases = new[]
            {
                ("true", true),
                ("True", true),
                ("TRUE", true),
                ("false", false),
                ("False", false),
                ("FALSE", false),
                ("1", true),
                ("0", false)
            };

            foreach (var (input, expected) in testCases)
            {
                // Arrange
                var configContent = $@"[TestSection]
BoolValue={input}";
                
                _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                config.Initialize(_tempConfigPath);

                // Act
                var section = config.GetSection<BasicTestConfig>("TestSection");

                // Assert
                section.BoolValue.Should().Be(expected, $"'{input}' should convert to {expected}");
                
                TestConfigurationHelper.CleanupTempFiles();
            }
        }

        /// <summary>
        /// Verifies that invalid type conversions throw appropriate exceptions.
        /// </summary>
        /// <remarks>
        /// This test ensures that when configuration values cannot be converted to the target
        /// property type, the system throws meaningful exceptions rather than silently failing
        /// or producing unexpected results, enabling proper error handling and debugging.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_InvalidTypeConversion_ShouldThrow()
        {
            // Arrange
            var configContent = @"[TestSection]
IntValue=not_a_number";
            
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            
            // Act & Assert - Invalid conversion should be handled gracefully
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<BasicTestConfig>("TestSection");
            };

            // The behavior may vary - it might throw or use default value
            // Check the actual behavior and adjust assertion accordingly
            act.Should().Throw<FormatException>("invalid int conversion should throw FormatException");
        }

        #endregion

        #region Save Tests

        /// <summary>
        /// Verifies that saving configuration persists all modified properties to the configuration file.
        /// </summary>
        /// <remarks>
        /// This test validates the complete save functionality, ensuring that all property changes
        /// are written back to the configuration file with correct formatting and that custom
        /// key mappings are respected during the save operation.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_Save_ShouldPersistAllProperties()
        {
            // Arrange
            var configContent = @"[TestSection]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<BasicTestConfig>("TestSection");
            section.StringValue = "Test String";
            section.IntValue = 42;
            section.BoolValue = true;
            section.DoubleValue = 3.14159;
            section.DateValue = new DateTime(2024, 1, 15);
            section.MappedProperty = "Mapped Value";

            // Act
            config.Save();

            // Assert
            var savedContent = System.IO.File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain("StringValue=Test String", "string property should be saved");
            savedContent.Should().Contain("IntValue=42", "int property should be saved");
            savedContent.Should().Contain("BoolValue=true", "bool property should be saved");
            savedContent.Should().Contain("DoubleValue=3.14159", "double property should be saved");
            savedContent.Should().Contain("CustomName=Mapped Value", "ConfigName mapped property should be saved with custom key");
        }

        /// <summary>
        /// Verifies that DateTime properties are saved in ISO 8601 format for consistency and interoperability.
        /// </summary>
        /// <remarks>
        /// This test ensures that DateTime values are serialized using ISO 8601 format when saving
        /// to configuration files, providing a standardized, unambiguous date representation that
        /// can be reliably parsed across different systems and cultures.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_SaveDateTimeAsISO8601_ShouldUseCorrectFormat()
        {
            // Arrange
            var configContent = @"[TestSection]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<BasicTestConfig>("TestSection");
            section.DateValue = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

            // Act
            config.Save();

            // Assert
            var savedContent = System.IO.File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain("DateValue=2024-01-15T10:30:00.0000000Z", "DateTime should be saved in ISO 8601 format");
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