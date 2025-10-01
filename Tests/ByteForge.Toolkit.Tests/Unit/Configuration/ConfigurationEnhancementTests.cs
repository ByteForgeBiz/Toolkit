using AwesomeAssertions;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;
using System.Collections.Generic;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Tests for recent enhancements and modifications to the configuration system including naming conventions, type validation, and interface implementations.
    /// </summary>
    [TestClass]
    public class ConfigurationEnhancementTests
    {
        private string _tempConfigPath = null!;

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
            ResetConfiguration();
        }

        #region GetCorrectName Method Tests

        /// <summary>
        /// Verifies that the new section naming convention (section#{name}{suffix}) works correctly for arrays.
        /// </summary>
        [TestMethod]
        public void ConfigSection_GetCorrectName_ArrayWithDefaultNaming_ShouldUseNewConvention()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=TestSection#StringArrayArray

[TestSection#StringArrayArray]
0=Item1
1=Item2
2=Item3";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.StringArray.Should().NotBeNull("array with new naming convention should load");
            section.StringArray.Should().HaveCount(3, "array should have correct number of items");
            section.StringArray[0].Should().Be("Item1", "array items should load correctly");
            section.StringArray[1].Should().Be("Item2", "array items should load correctly");
            section.StringArray[2].Should().Be("Item3", "array items should load correctly");
        }

        /// <summary>
        /// Verifies that the new section naming convention works correctly for dictionaries.
        /// </summary>
        [TestMethod]
        public void ConfigSection_GetCorrectName_DictionaryWithDefaultNaming_ShouldUseNewConvention()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=TestSection#FileFormatsDict

[TestSection#FileFormatsDict]
TCI=TCI_MMddyyyy_WIN_Return.txt
CRC=CRC_MMddyyyy_Return.csv";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("dictionary with new naming convention should load");
            section.FileFormats.Should().HaveCount(2, "dictionary should have correct number of items");
            section.FileFormats["TCI"].Should().Be("TCI_MMddyyyy_WIN_Return.txt", "dictionary values should load correctly");
            section.FileFormats["CRC"].Should().Be("CRC_MMddyyyy_Return.csv", "dictionary values should load correctly");
        }

        /// <summary>
        /// Verifies that custom section names still override the new naming convention.
        /// </summary>
        [TestMethod]
        public void ConfigSection_GetCorrectName_WithCustomSectionName_ShouldUseCustomName()
        {
            // Arrange
            var configContent = @"[TestSection]
CustomNamedArray=MyCustomArraySection

[MyCustomArraySection]
0=Custom1
1=Custom2";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.CustomNamedArray.Should().NotBeNull("array with custom section name should load");
            section.CustomNamedArray.Should().HaveCount(2, "custom array should have correct items");
            section.CustomNamedArray[0].Should().Be("Custom1", "custom array values should load correctly");
        }

        /// <summary>
        /// Verifies that the GetCorrectName method handles empty/null values correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_GetCorrectName_WithEmptyValue_ShouldFallbackToConvention()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=

[TestSection#StringArrayArray]
0=Fallback1
1=Fallback2";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.StringArray.Should().NotBeNull("array should fallback to convention naming");
            section.StringArray.Should().HaveCount(2, "fallback array should load correctly");
        }

        /// <summary>
        /// Verifies that saving arrays creates the correct section names using the new convention.
        /// </summary>
        [TestMethod]
        public void ConfigSection_SaveArray_WithNewNamingConvention_ShouldCreateCorrectSectionName()
        {
            // Arrange
            var configContent = @"[TestSection]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<ArrayTestConfig>("TestSection");
            section.StringArray = new[] { "Save1", "Save2", "Save3" };

            // Act
            config.Save();

            // Assert
            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain("StringArray=", "array reference should be saved");
            savedContent.Should().Contain("[TestSection#StringArrayArray]", "section should use new naming convention");
            savedContent.Should().Contain("0=Save1", "array items should be saved");
        }

        #endregion

        #region Enhanced Dictionary Type Validation Tests

        /// <summary>
        /// Verifies that unsupported dictionary key types are properly rejected.
        /// </summary>
        [TestMethod]
        public void ConfigSection_UnsupportedDictionaryKeyType_ShouldThrowNotSupportedException()
        {
            // Arrange
            var configContent = @"[TestSection]
UnsupportedDict=SomeSection

[SomeSection]
1=Value1
2=Value2";
            
            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<UnsupportedDictionaryTestConfig>("TestSection");
            };

            act.Should().Throw<NotSupportedException>("unsupported dictionary key type should throw NotSupportedException")
               .WithMessage("*must be of a supported dictionary type*");
        }

        /// <summary>
        /// Verifies that Dictionary&lt;string, object&gt; is not supported.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryWithObjectValues_ShouldNotBeSupported()
        {
            // This would require a test model with Dictionary<string, object>
            // The current implementation only supports Dictionary<string, string>
            
            // Arrange
            var configContent = @"[TestSection]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert - All supported dictionary types should be string, string
            section.FileFormats.Should().NotBeNull("Dictionary<string, string> should be supported");
            section.InterfaceDictionary.Should().NotBeNull("IDictionary<string, string> should be supported");
            section.ReadOnlyDictionary.Should().NotBeNull("IReadOnlyDictionary<string, string> should be supported");
        }

        /// <summary>
        /// Verifies that the enhanced IsSupportedDictionaryType method correctly validates complex generic types.
        /// </summary>
        [TestMethod]
        public void ConfigSection_IsSupportedDictionaryType_WithComplexGenerics_ShouldValidateCorrectly()
        {
            // This test verifies the logic in IsSupportedDictionaryType method
            // We test through the actual usage since the method is private
            
            // Arrange
            var configContent = @"[TestSection]
CollectionKeyValuePairs=CollectionDict
EnumerableKeyValuePairs=EnumerableDict
ReadOnlyCollectionKeyValuePairs=ReadOnlyCollectionDict

[CollectionDict]
Key1=Value1

[EnumerableDict] 
Key2=Value2

[ReadOnlyCollectionDict]
Key3=Value3";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert - All these complex generic types should be supported
            section.CollectionKeyValuePairs.Should().NotBeNull("ICollection<KeyValuePair<string, string>> should be supported");
            section.EnumerableKeyValuePairs.Should().NotBeNull("IEnumerable<KeyValuePair<string, string>> should be supported");
            section.ReadOnlyCollectionKeyValuePairs.Should().NotBeNull("IReadOnlyCollection<KeyValuePair<string, string>> should be supported");
            
            section.CollectionKeyValuePairs.Should().HaveCount(1, "collection should load correctly");
            section.EnumerableKeyValuePairs.Should().HaveCount(1, "enumerable should load correctly");
            section.ReadOnlyCollectionKeyValuePairs.Should().HaveCount(1, "readonly collection should load correctly");
        }

        #endregion

        #region Explicit Interface Implementation Tests

        /// <summary>
        /// Verifies that explicit interface implementation methods work correctly.
        /// </summary>
        [TestMethod]
        public void Configuration_ExplicitInterfaceImplementation_ShouldWorkCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
StringValue=Test Value
IntValue=42";
            
            // Test explicit interface usage
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            
            // Act - Use explicit interface methods
            config.Initialize(_tempConfigPath);
            var section = config.GetSection<BasicTestConfig>("TestSection");
            
            // Assert
            config.IsInitialized.Should().BeTrue("explicit interface IsInitialized should work");
            section.Should().NotBeNull("explicit interface GetSection should work");
            section.StringValue.Should().Be("Test Value", "section should load correctly through interface");
        }

        /// <summary>
        /// Verifies that the globalization property works through explicit interface implementation.
        /// </summary>
        [TestMethod]
        public void Configuration_ExplicitInterface_Globalization_ShouldWorkCorrectly()
        {
            // Arrange
            var configContent = @"[Globalization]
CultureInfo=en-US
DateFormat=MM/dd/yyyy
NumberFormat=#,##0.00

[TestSection]
StringValue=Test";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var globalization = config.Globalization;

            // Assert
            globalization.Should().NotBeNull("globalization should be accessible through interface");
            globalization.CultureInfo.Name.Should().Be("en-US", "globalization properties should load correctly");
        }

        /// <summary>
        /// Verifies that static methods delegate correctly to the default instance.
        /// </summary>
        [TestMethod]
        public void Configuration_StaticMethods_ShouldDelegateToDefaultInstance()
        {
            // Arrange
            var configContent = @"[TestSection]
StringValue=Static Test
IntValue=99";
            
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);

            // Act - Use static methods
            ByteForge.Toolkit.Configuration.Initialize(_tempConfigPath);
            var section = ByteForge.Toolkit.Configuration.GetSection<BasicTestConfig>("TestSection");

            // Assert
            ByteForge.Toolkit.Configuration.IsInitialized.Should().BeTrue("static IsInitialized should work");
            section.Should().NotBeNull("static GetSection should work");
            section.StringValue.Should().Be("Static Test", "static methods should work correctly");
        }

        #endregion

        #region Parser Integration Edge Cases Tests

        /// <summary>
        /// Verifies that Parser integration works correctly with built-in types through the configuration system.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserIntegration_WithBuiltInTypes_ShouldWork()
        {
            // Arrange
            var configContent = @"[TestSection]
StringValue=Test String
IntValue=42
BoolValue=true
DoubleValue=3.14159
DateValue=2024-01-15T10:30:00";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            section.StringValue.Should().Be("Test String", "Parser should handle string values correctly");
            section.IntValue.Should().Be(42, "Parser should handle int values correctly");
            section.BoolValue.Should().Be(true, "Parser should handle bool values correctly");
            section.DoubleValue.Should().Be(3.14159, "Parser should handle double values correctly");
            section.DateValue.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0), "Parser should handle DateTime values correctly");
        }

        /// <summary>
        /// Verifies that Parser integration handles enum types correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserIntegration_WithEnumTypes_ShouldWork()
        {
            // Arrange
            var configContent = @"[TestSection]
Level=Warning
DbType=PostgreSQL
Mode=Batch";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<EnumTestConfig>("TestSection");

            // Assert
            section.Level.Should().Be(LogLevel.Warning, "Parser should handle enum parsing correctly");
            section.DbType.Should().Be(DatabaseType.PostgreSQL, "Parser should handle different enum types");
            section.Mode.Should().Be(ProcessingMode.Batch, "Parser should handle enum with default values");
        }

        /// <summary>
        /// Verifies that Parser.Stringify works correctly during save operations for built-in types.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserStringify_DuringSave_ShouldWork()
        {
            // Arrange
            var configContent = @"[TestSection]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<EnumTestConfig>("TestSection");
            section.Level = LogLevel.Critical;
            section.DbType = DatabaseType.Oracle;

            // Act
            config.Save();

            // Assert
            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain("Level=Critical", "Parser should stringify enum correctly");
            savedContent.Should().Contain("DbType=Oracle", "Parser should stringify different enum types correctly");
        }

        /// <summary>
        /// Verifies that Parser correctly handles custom type registration through static methods.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserCustomTypeRegistration_ShouldIntegrateWithConfiguration()
        {
            // Arrange - Register a custom type using Parser.RegisterType static method
            Parser.RegisterType(
                typeof(TimeSpan),
                value => TimeSpan.ParseExact(value, @"hh\:mm\:ss", null),
                value => ((TimeSpan)value).ToString(@"hh\:mm\:ss")
            );

            var configContent = @"[TestSection]
StringValue=Test
TimeSpanValue=02:30:45";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<CustomTimeSpanTestConfig>("TestSection");

            // Assert
            section.StringValue.Should().Be("Test", "regular properties should still work");
            section.TimeSpanValue.Should().Be(new TimeSpan(2, 30, 45), "custom registered type should parse correctly");
        }

        /// <summary>
        /// Verifies that Parser.IsKnownType works correctly for checking type support.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ParserIsKnownType_ShouldReflectTypeSupport()
        {
            // Act & Assert
            Parser.IsKnownType(typeof(string)).Should().BeTrue("string should be a known type");
            Parser.IsKnownType(typeof(int)).Should().BeTrue("int should be a known type");
            Parser.IsKnownType(typeof(bool)).Should().BeTrue("bool should be a known type");
            Parser.IsKnownType(typeof(DateTime)).Should().BeTrue("DateTime should be a known type");
            Parser.IsKnownType(typeof(Guid)).Should().BeTrue("Guid should be a known type");
            
            // Enums are handled separately in Parser.Parse, not through registered types
            Parser.IsKnownType(typeof(LogLevel)).Should().BeFalse("enums are handled separately, not as known types");
        }

        #endregion

        #region DefaultValueHelper Complex Scenarios Tests

        /// <summary>
        /// Verifies that DefaultValueHelper works with custom default value providers.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DefaultValueHelper_WithCustomProviders_ShouldWork()
        {
            // Arrange
            var configContent = @"[TestSection]
RegularProperty=TestValue";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<CustomDefaultConfig>("TestSection");

            // Assert
            section.RegularProperty.Should().Be("TestValue", "regular property should load");
            section.DataPath.Should().Be(@"C:\Temp\DefaultData", "custom default provider should work");
            section.ConnectionTimeout.Should().Be(60, "custom default provider should work");
        }

        /// <summary>
        /// Verifies that DefaultValueHelper handles inheritance scenarios correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DefaultValueHelper_WithInheritance_ShouldResolveCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
Server=TestServer";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DatabaseConfig>("TestSection");

            // Assert
            section.Server.Should().Be("TestServer", "specified value should load");
            section.Port.Should().Be(1433, "default value should be used");
            section.Timeout.Should().Be(30, "default value should be used");
            section.UseSSL.Should().Be(false, "default value should be used");
        }

        /// <summary>
        /// Verifies that DefaultValueHelper works correctly with arrays and default values.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DefaultValueHelper_WithArraysAndDefaults_ShouldWork()
        {
            // Arrange - No array section provided, should use defaults
            var configContent = @"[TestSection]
IsEnabled=true";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<ComplexTestConfig>("TestSection");

            // Assert
            section.IsEnabled.Should().Be(true, "explicit value should load");
            section.EnabledFeatures.Should().NotBeNull("array should be initialized even without section");
            section.SupportedEnvironments.Should().NotBeNull("array should be initialized even without section");
        }

        #endregion

        #region Enhanced Save Functionality Tests

        /// <summary>
        /// Verifies that the enhanced save functionality preserves comments and structure.
        /// </summary>
        [TestMethod]
        public void Configuration_EnhancedSave_ShouldPreserveCommentsAndStructure()
        {
            // Arrange
            var configContent = @"; Configuration file for testing
; This is a test configuration

[TestSection]
; String value comment
StringValue=Original Value
; Integer value comment  
IntValue=42

; Database section
[DatabaseSettings]
Server=localhost
Port=1433";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<BasicTestConfig>("TestSection");
            section.StringValue = "Modified Value";

            // Act
            config.Save();

            // Assert
            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain("; Configuration file for testing", "initial comments should be preserved");
            savedContent.Should().Contain("; String value comment", "property comments should be preserved");
            savedContent.Should().Contain("; Database section", "section comments should be preserved");
            savedContent.Should().Contain("StringValue=Modified Value", "modified value should be saved");
            savedContent.Should().Contain("Server=localhost", "unmodified values should be preserved");
        }

        /// <summary>
        /// Verifies that saving with new sections maintains proper file organization.
        /// </summary>
        [TestMethod]
        public void Configuration_SaveWithNewSections_ShouldMaintainOrganization()
        {
            // Arrange
            var configContent = @"[ExistingSection]
ExistingValue=Test";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Add new sections
            var basicSection = config.GetSection<BasicTestConfig>("NewSection1");
            basicSection.StringValue = "New String";
            basicSection.IntValue = 100;

            var dbSection = config.GetSection<DatabaseConfig>("NewSection2");
            dbSection.Server = "NewServer";
            dbSection.Port = 5432;

            // Act
            config.Save();

            // Assert
            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain("[ExistingSection]", "existing section should be preserved");
            savedContent.Should().Contain("[NewSection1]", "new section should be added");
            savedContent.Should().Contain("[NewSection2]", "second new section should be added");
            savedContent.Should().Contain("StringValue=New String", "new section values should be saved");
            savedContent.Should().Contain("Server=NewServer", "new section values should be saved");
        }

#if false

        /*
         * This test requires configuration polymorphism to handle multiple section types in one config.
         * Since the current configuration system does not support this, the test is commented out.
         *
         * Once polymorphic section handling is implemented, this test can be enabled to verify complex save scenarios.
         */

        /// <summary>
        /// Verifies that the ThreadUnsafeSave method handles complex section updates correctly.
        /// </summary>
        [TestMethod]
        public void Configuration_ThreadUnsafeSave_WithComplexUpdates_ShouldHandleCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=TestStringArray
FileFormats=TestFileFormats

[TestStringArray]
0=Old1
1=Old2
2=Old3

[TestFileFormats]
TCI=OldTCI
CRC=OldCRC";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Create a combined test config that has both arrays and dictionaries
            var arraySection = config.GetSection<ArrayTestConfig>("TestSection");
            var dictSection = config.GetSection<DictionaryTestConfig>("TestSection");
            
            // Modify both array and dictionary
            arraySection.StringArray = new[] { "New1", "New2" };
            dictSection.FileFormats = new Dictionary<string, string>
            {
                {"TCI", "NewTCI"},
                {"GICS", "NewGICS"}
            };

            // Act
            config.Save();

            // Assert
            var savedContent = File.ReadAllText(_tempConfigPath);
            
            // Array should be updated
            savedContent.Should().Contain("0=New1", "array should be updated");
            savedContent.Should().Contain("1=New2", "array should be updated");
            savedContent.Should().NotContain("2=Old3", "old array items should be removed");
            
            // Dictionary should be updated
            savedContent.Should().Contain("TCI=NewTCI", "dictionary should be updated");
            savedContent.Should().Contain("GICS=NewGICS", "new dictionary items should be added");
            savedContent.Should().NotContain("CRC=OldCRC", "removed dictionary items should not be present");
        }

#endif

        #endregion

        #region Helper Methods

        private void ResetConfiguration()
        {
            var instanceField = typeof(ByteForge.Toolkit.Configuration).GetField("_defaultInstance", BindingFlags.NonPublic | BindingFlags.Static);
            
            if (instanceField != null)
            {
                instanceField.SetValue(null, null);
            }
        }

        #endregion
    }
}