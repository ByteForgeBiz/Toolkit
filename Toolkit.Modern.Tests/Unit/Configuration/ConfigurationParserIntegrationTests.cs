using AwesomeAssertions;
using ByteForge.Toolkit.Configuration;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;
using ByteForge.Toolkit.Utilities;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Tests for Parser integration with the configuration system, including custom type registration and built-in type support.
    /// </summary>
    [TestClass]
    public class ConfigurationParserIntegrationTests
    {
        private string _tempConfigPath = null!;

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
            ResetConfiguration();
        }

        #region Built-in Type Support Tests

        /// <summary>
        /// Verifies that Parser correctly handles all built-in types in configuration loading.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserBuiltInTypes_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[ParserTest]
StringValue=Test String
IntValue=42
BoolValue=true
DoubleValue=3.14159
DateValue=2024-01-15T10:30:00
TimeoutValue=45";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("ParserTest");

            // Assert
            section.StringValue.Should().Be("Test String", "Parser should handle string values");
            section.IntValue.Should().Be(42, "Parser should handle int values");
            section.BoolValue.Should().Be(true, "Parser should handle bool values");
            section.DoubleValue.Should().Be(3.14159, "Parser should handle double values");
            section.DateValue.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0), "Parser should handle DateTime values");
            section.TimeoutValue.Should().Be(45, "Parser should handle default value attributes");
        }

        /// <summary>
        /// Verifies that Parser correctly handles enum types in configuration.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserEnumTypes_ShouldParseCorrectly()
        {
            // Arrange
            var configContent = @"[EnumTest]
Level=Warning
DbType=PostgreSQL
Mode=Batch";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<EnumTestConfig>("EnumTest");

            // Assert
            section.Level.Should().Be(LogLevel.Warning, "Parser should handle enum parsing");
            section.DbType.Should().Be(DatabaseType.PostgreSQL, "Parser should handle different enum types");
            section.Mode.Should().Be(ProcessingMode.Batch, "Parser should handle enum with default values");
        }

        /// <summary>
        /// Verifies that Parser handles nullable types correctly in configuration.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserNullableTypes_ShouldHandleCorrectly()
        {
            // Arrange
            var configContent = @"[NullableTest]
NullableInt=42
NullableBool=true
NullableDateTime=2024-01-15
NullableDouble=3.14";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<NullableTestConfig>("NullableTest");

            // Assert
            section.NullableInt.Should().Be(42, "Parser should handle nullable int");
            section.NullableBool.Should().Be(true, "Parser should handle nullable bool");
            section.NullableDateTime.Should().Be(new DateTime(2024, 1, 15), "Parser should handle nullable DateTime");
            section.NullableDouble.Should().Be(3.14, "Parser should handle nullable double");
        }

        /// <summary>
        /// Verifies that Parser handles empty/null values for nullable types correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserNullableTypesWithNullValues_ShouldReturnNull()
        {
            // Arrange
            var configContent = @"[NullableTest]
OptionalString=";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<NullableTestConfig>("NullableTest");

            // Assert
            section.NullableInt.Should().BeNull("Parser should return null for missing nullable int");
            section.NullableBool.Should().BeNull("Parser should return null for missing nullable bool");
            section.OptionalString.Should().BeNullOrEmpty("Parser should handle empty string values");
        }

        #endregion

        #region Array Type Parsing Tests

        /// <summary>
        /// Verifies that Parser correctly handles different element types in arrays.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserArrayElementTypes_ShouldParseCorrectly()
        {
            // Arrange
            var configContent = @"[ArrayParserTest]
NumberList=NumberArray
StringArray=StringArray

[NumberArray]
0=100
1=200
2=300

[StringArray]
0=First
1=Second
2=Third";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<ArrayTestConfig>("ArrayParserTest");

            // Assert
            section.NumberList.Should().NotBeNull("number array should load");
            section.NumberList.Should().HaveCount(3, "number array should have correct count");
            section.NumberList[0].Should().Be(100, "Parser should convert string to int correctly");
            section.NumberList[2].Should().Be(300, "Parser should handle all array elements");

            section.StringArray.Should().NotBeNull("string array should load");
            section.StringArray.Should().HaveCount(3, "string array should have correct count");
            section.StringArray[0].Should().Be("First", "Parser should handle string array elements");
        }

        /// <summary>
        /// Verifies that Parser throws appropriate exceptions for invalid array element types.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserInvalidArrayElements_ShouldThrowFormatException()
        {
            // Arrange
            var configContent = @"[ArrayParserTest]
NumberList=InvalidNumberArray

[InvalidNumberArray]
0=100
1=not_a_number
2=300";
            
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<ArrayTestConfig>("ArrayParserTest");
            };

            act.Should().Throw<FormatException>("Parser should throw FormatException for invalid array elements");
        }

        #endregion

        #region Custom Type Registration Tests

        /// <summary>
        /// Verifies that custom types can be registered with Parser and used in configuration.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserCustomTypeRegistration_ShouldWorkCorrectly()
        {
            // Arrange - Register TimeSpan with custom format
            Parser.RegisterType(
                typeof(TimeSpan),
                value => TimeSpan.ParseExact(value, @"hh\:mm\:ss", null),
                value => ((TimeSpan)value).ToString(@"hh\:mm\:ss")
            );

            var configContent = @"[CustomTimeSpanTest]
StringValue=Test
TimeSpanValue=02:30:45";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<CustomTimeSpanTestConfig>("CustomTimeSpanTest");

            // Assert
            section.TimeSpanValue.Should().Be(new TimeSpan(2, 30, 45), "custom registered type should parse correctly");
            section.StringValue.Should().Be("Test", "regular properties should still work");
        }

        /// <summary>
        /// Verifies that custom types can be saved correctly using the registered stringifier.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserCustomTypeSaving_ShouldUseStringifier()
        {
            // Arrange - Register TimeSpan with custom format
            Parser.RegisterType(
                typeof(TimeSpan),
                value => TimeSpan.ParseExact(value, @"hh\:mm\:ss", null),
                value => ((TimeSpan)value).ToString(@"hh\:mm\:ss")
            );

            var configContent = @"[CustomTimeSpanTest]
StringValue=Initial";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<CustomTimeSpanTestConfig>("CustomTimeSpanTest");
            section.TimeSpanValue = new TimeSpan(5, 15, 30);
            section.StringValue = "Modified";

            // Act
            config.Save();

            // Assert
            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain("TimeSpanValue=05:15:30", "custom stringifier should format the value correctly");
            savedContent.Should().Contain("StringValue=Modified", "regular properties should still save");
        }

        #endregion

        #region Parser Error Handling Tests

        /// <summary>
        /// Verifies that Parser handles invalid enum values appropriately.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserInvalidEnumValue_ShouldThrowArgumentException()
        {
            // Arrange
            var configContent = @"[EnumTest]
Level=InvalidLogLevel";

            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<EnumTestConfig>("EnumTest");
            };

            act.Should().Throw<FormatException>("Parser should throw FormatException for invalid enum values");
        }

        /// <summary>
        /// Verifies that Parser handles malformed DateTime values appropriately.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserMalformedDateTime_ShouldThrowFormatException()
        {
            // Arrange
            var configContent = @"[ParserTest]
DateValue=not-a-date";

            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);

            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration.Configuration();
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<BasicTestConfig>("ParserTest");
            };

            act.Should().Throw<FormatException>("Parser should throw FormatException for malformed DateTime values");
        }

        /// <summary>
        /// Verifies that Parser.IsKnownType works correctly for various types.
        /// </summary>
        [TestMethod]
        public void Parser_IsKnownType_ShouldReturnCorrectResults()
        {
            // Act & Assert
            Parser.IsKnownType(typeof(string)).Should().BeTrue("string should be a known type");
            Parser.IsKnownType(typeof(int)).Should().BeTrue("int should be a known type");
            Parser.IsKnownType(typeof(bool)).Should().BeTrue("bool should be a known type");
            Parser.IsKnownType(typeof(DateTime)).Should().BeTrue("DateTime should be a known type");
            Parser.IsKnownType(typeof(Guid)).Should().BeTrue("Guid should be a known type");
            Parser.IsKnownType(typeof(LogLevel)).Should().BeFalse("enum should not be considered known (handled separately)");
            
            // Register TimeSpan with custom format and verify it becomes known
            Parser.RegisterType(typeof(TimeSpan), 
                value => TimeSpan.Parse(value), 
                value => value.ToString());
            Parser.IsKnownType(typeof(TimeSpan)).Should().BeTrue("registered custom type should be known");
        }

        #endregion

        #region Helper Methods

        private void ResetConfiguration()
        {
            var instanceField = typeof(ByteForge.Toolkit.Configuration.Configuration).GetField("_defaultInstance", BindingFlags.NonPublic | BindingFlags.Static);
            
            if (instanceField != null)
            {
                instanceField.SetValue(null, null);
            }
        }

        #endregion
    }
}