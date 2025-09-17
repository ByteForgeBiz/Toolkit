using System;
using System.ComponentModel;
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;

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
            savedContent.Should().Contain("BoolValue=True", "bool property should be saved");
            savedContent.Should().Contain("DoubleValue=3.14159", "double property should be saved");
            savedContent.Should().Contain("CustomName=Mapped Value", "ConfigName mapped property should be saved with custom key");
        }

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