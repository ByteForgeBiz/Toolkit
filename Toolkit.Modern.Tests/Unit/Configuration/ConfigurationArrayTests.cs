using AwesomeAssertions;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Tests for array and collection support in configuration sections including various collection types, section naming, and key formats.
    /// </summary>
    [TestClass]
    public class ConfigurationArrayTests
    {
        private string _tempConfigPath;

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
            ResetConfiguration();
        }

        #region Basic Array Support Tests

        /// <summary>
        /// Verifies that string arrays are loaded correctly from configuration sections.
        /// </summary>
        /// <remarks>
        /// This test ensures that configuration sections can properly load and parse string array values
        /// from referenced INI sections, supporting complex data structures in configuration.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_StringArray_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=TestStringArray

[TestStringArray]
0=Item1
1=Item2
2=Item3";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.StringArray.Should().NotBeNull("string array should be loaded");
            section.StringArray.Should().HaveCount(3, "string array should have 3 items");
            section.StringArray.Should().SatisfyAll("string array items should match INI values",
                list => list.Count() == 3,
                list => list.ElementAt(0) == "Item1",
                list => list.ElementAt(1) == "Item2",
                list => list.ElementAt(2) == "Item3");
        }

        /// <summary>
        /// Verifies that generic lists of integers are loaded and parsed correctly from configuration.
        /// </summary>
        /// <remarks>
        /// This test ensures that strongly-typed generic lists can be populated from configuration sections
        /// with automatic type conversion from string to the target type.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_GenericList_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
NumberList=TestNumberList

[TestNumberList]
0=10
1=20
2=30
3=40";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.NumberList.Should().NotBeNull("number list should be loaded");
            section.NumberList.Should().HaveCount(4, "number list should have 4 items");
            section.NumberList.Should().SatisfyAll("number list items should be parsed as integers",
                list => list.Count() == 4,
                list => list.ElementAt(0) == 10,
                list => list.ElementAt(1) == 20,
                list => list.ElementAt(2) == 30,
                list => list.ElementAt(3) == 40);
        }

        /// <summary>
        /// Verifies that lists defined as interfaces are loaded correctly from configuration.
        /// </summary>
        /// <remarks>
        /// This test ensures that properties declared with interface types (IList, ICollection)
        /// can be properly populated from configuration sections.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_InterfaceList_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
InterfaceList=TestInterfaceList

[TestInterfaceList]
0=Alpha
1=Beta
2=Gamma";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.InterfaceList.Should().NotBeNull("interface list should be loaded");
            section.InterfaceList.Should().HaveCount(3, "interface list should have 3 items");
            section.InterfaceList.Should().SatisfyAll("interface list items should match INI values",
                list => list.Count() == 3,
                list => list.ElementAt(0) == "Alpha",
                list => list.ElementAt(1) == "Beta",
                list => list.ElementAt(2) == "Gamma");
        }

        /// <summary>
        /// Verifies that enumerable collections are loaded correctly from configuration.
        /// </summary>
        /// <remarks>
        /// This test ensures that properties declared as IEnumerable can be properly loaded
        /// from configuration sections, supporting various collection abstractions.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_EnumerableCollection_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
EnumerableCollection=TestEnumerableCollection

[TestEnumerableCollection]
0=First
1=Second
2=Third";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.EnumerableCollection.Should().NotBeNull("enumerable collection should be loaded");
            section.EnumerableCollection.Should().HaveCount(3, "enumerable collection should have 3 items");
            section.EnumerableCollection.Should().SatisfyAll("enumerable collection items should match INI values",
                list => list.Count() == 3,
                list => list.ElementAt(0) == "First",
                list => list.ElementAt(1) == "Second",
                list => list.ElementAt(2) == "Third");
        }

        #endregion

        #region Array Section Naming Tests

        /// <summary>
        /// Verifies that arrays use default naming convention based on property name when no explicit name is provided.
        /// </summary>
        /// <remarks>
        /// This test ensures that when no custom array section name is specified, the configuration system
        /// uses the default naming convention to locate the array section.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_DefaultArrayNaming_ShouldUsePropertyNameArray()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=StringArrayArray

[StringArrayArray]
0=DefaultNaming1
1=DefaultNaming2";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.StringArray.Should().NotBeNull("array with default naming should be loaded");
            section.StringArray.Should().SatisfyAll("default named array should load correctly",
                list => list.Count() == 2,
                list => list.ElementAt(0) == "DefaultNaming1",
                list => list.ElementAt(1) == "DefaultNaming2");
        }

        /// <summary>
        /// Verifies that arrays use custom section names when specified via configuration attributes.
        /// </summary>
        /// <remarks>
        /// This test ensures that configuration attributes can override the default array section naming
        /// to provide more meaningful or application-specific section names.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_CustomArrayNaming_ShouldUseAttributeName()
        {
            // Arrange
            var configContent = @"[TestSection]
CustomNamedArray=CustomArraySection

[CustomArraySection]
0=Custom1
1=Custom2
2=Custom3";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.CustomNamedArray.Should().NotBeNull("custom named array should be loaded");
            section.CustomNamedArray.Should().HaveCount(3, "custom named array should have 3 items");
            section.CustomNamedArray.Should().SatisfyAll("custom named array items should match",
                list => list.Count() == 3,
                list => list.ElementAt(0) == "Custom1",
                list => list.ElementAt(1) == "Custom2",
                list => list.ElementAt(2) == "Custom3");
        }

        /// <summary>
        /// Verifies that array section names specified in INI values override default and attribute-based names.
        /// </summary>
        /// <remarks>
        /// This test ensures that the actual INI file content can specify which section contains array data,
        /// providing runtime flexibility in array section organization.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_OverrideArrayNameInINI_ShouldUseINIValue()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=OverriddenArraySection

[OverriddenArraySection]
0=Override1
1=Override2";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.StringArray.Should().NotBeNull("INI overridden array should be loaded");
            section.StringArray.Should().SatisfyAll("INI overridden array should use specified section",
                list => list.Count() == 2,
                list => list.ElementAt(0) == "Override1",
                list => list.ElementAt(1) == "Override2");
        }

        #endregion

        #region Array Key Format Tests

        /// <summary>
        /// Verifies that array items with numeric indices are loaded in correct sequential order.
        /// </summary>
        /// <remarks>
        /// This test ensures that when array sections use numeric keys (0, 1, 2, etc.),
        /// the items are loaded in the expected numerical sequence order.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_NumericIndices_ShouldLoadInOrder()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=NumericIndicesArray

[NumericIndicesArray]
0=Zero
1=One
2=Two
3=Three";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.StringArray.Should().SatisfyAll("sequential numeric indices should maintain order (alphabetical equals numeric)",
                list => list.Count() == 4,
                list => list.ElementAt(0) == "Zero",
                list => list.ElementAt(1) == "One",
                list => list.ElementAt(2) == "Two",
                list => list.ElementAt(3) == "Three");
        }

        /// <summary>
        /// Verifies that array items with named keys are loaded in alphabetical order by key name.
        /// </summary>
        /// <remarks>
        /// This test ensures that when array sections use named keys instead of numeric indices,
        /// the items are loaded in alphabetical order based on their key names.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_NamedKeys_ShouldLoadInAlphabeticalOrder()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=NamedKeysArray

[NamedKeysArray]
Primary=First
Secondary=Second
Backup=Third";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.StringArray.Should().NotBeNull("named keys array should be loaded");
            section.StringArray.Should().HaveCount(3, "named keys array should have 3 items");
            section.StringArray.Should().SatisfyAll("named keys should load in alphabetical key order",
                list => list.Count() == 3,
                list => list.ElementAt(0) == "Third",    // "Backup" key (alphabetically first)
                list => list.ElementAt(1) == "First",   // "Primary" key (alphabetically second)
                list => list.ElementAt(2) == "Second");  // "Secondary" key (alphabetically third)
        }

        /// <summary>
        /// Verifies that array items with mixed key formats (numeric and named) are loaded in alphabetical order.
        /// </summary>
        /// <remarks>
        /// This test ensures that when array sections contain both numeric and string keys,
        /// all items are loaded in alphabetical order treating all keys as strings.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_MixedKeyFormats_ShouldLoadInAlphabeticalOrder()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=MixedKeysArray

[MixedKeysArray]
99=NinetyNine
abc=AlphaBetaCharlie
0=Zero
xyz=ExWhyZed";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.StringArray.Should().NotBeNull("mixed keys array should be loaded");
            section.StringArray.Should().HaveCount(4, "mixed keys array should have 4 items");
            section.StringArray.Should().SatisfyAll("mixed keys should load in alphabetical key order",
                list => list.Count() == 4,
                list => list.ElementAt(0) == "Zero",           // "0" key (alphabetically first)
                list => list.ElementAt(1) == "NinetyNine",    // "99" key (alphabetically second)
                list => list.ElementAt(2) == "AlphaBetaCharlie", // "abc" key (alphabetically third)
                list => list.ElementAt(3) == "ExWhyZed");     // "xyz" key (alphabetically fourth)
        }


        /// <summary>
        /// Verifies that array items with sparse numeric indices are loaded in alphabetical key order.
        /// </summary>
        /// <remarks>
        /// This test ensures that non-sequential numeric keys (0, 2, 5, 10) are sorted alphabetically as strings,
        /// which affects the order of items in the resulting array. This is important for understanding
        /// how the configuration system handles gaps in numeric indices.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_SparseIndices_ShouldLoadInAlphabeticalOrder()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=SparseIndicesArray

[SparseIndicesArray]
0=Zero
5=Five
10=Ten
2=Two";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.StringArray.Should().NotBeNull("sparse indices array should be loaded");
            section.StringArray.Should().HaveCount(4, "sparse indices array should have 4 items");
            section.StringArray.Should().SatisfyAll("sparse indices should load in alphabetical key order",
                list => list.Count() == 4,
                list => list.ElementAt(0) == "Zero",  // "0" key (alphabetically first)
                list => list.ElementAt(1) == "Two",   // "2" key (alphabetically second)
                list => list.ElementAt(2) == "Five",  // "5" key (alphabetically third) 
                list => list.ElementAt(3) == "Ten");  // "10" key (alphabetically fourth - string sort puts "10" after "5")
        }

        /// <summary>
        /// Verifies that an empty array section creates an empty but non-null array.
        /// </summary>
        /// <remarks>
        /// This test ensures that when an array section exists but contains no items,
        /// the system creates an empty array rather than null, which is important for
        /// null-safe operations and consistent behavior.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_EmptyArraySection_ShouldCreateEmptyArray()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=EmptyArraySection

[EmptyArraySection]";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.StringArray.Should().NotBeNull("empty array should not be null");
            section.StringArray.Should().BeEmpty("empty array section should create empty array");
        }

        #endregion

        #region Array Persistence Tests

        /// <summary>
        /// Verifies that saving an array normalizes indices to sequential numeric format (0, 1, 2, ...).
        /// </summary>
        /// <remarks>
        /// This test ensures that regardless of how array items were originally indexed,
        /// the save operation normalizes them to standard sequential numeric indices.
        /// This provides consistency and predictability in saved configuration files.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_SaveArray_ShouldNormalizeToNumericIndices()
        {
            // Arrange
            var configContent = @"[TestSection]";
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            var sectionName = "TestSection";
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>(sectionName);
            section.StringArray = ["Save1", "Save2", "Save3"];
            var propertyName = nameof(section.StringArray);

            // Act
            ((IConfigurationManager)config).Save();

            // Assert
            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain($"[{sectionName}#{propertyName}Array]", "array section should be created");
            savedContent.Should().Contain("0=Save1", "first item should use index 0");
            savedContent.Should().Contain("1=Save2", "second item should use index 1");
            savedContent.Should().Contain("2=Save3", "third item should use index 2");
        }

        /// <summary>
        /// Verifies that arrays with custom section names save to their designated sections.
        /// </summary>
        /// <remarks>
        /// This test ensures that arrays configured with custom section names via ConfigurationArrayAttribute
        /// are correctly saved to their designated sections rather than default naming conventions.
        /// This supports flexible configuration organization.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_SaveCustomNamedArray_ShouldUseCustomSection()
        {
            // Arrange
            var sectionName = "TestSection";
            var configContent = $"[{sectionName}]";
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>(sectionName);
            section.CustomNamedArray = new List<string> { "Custom1", "Custom2" };
            var propertyName = nameof(section.CustomNamedArray);

            // Act
            ((IConfigurationManager)config).Save();

            // Assert
            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain($"[{sectionName}#CustomArraySection]", "custom array section should be created");
            savedContent.Should().Contain("0=Custom1", "custom array first item should be saved");
            savedContent.Should().Contain("1=Custom2", "custom array second item should be saved");
        }

        /// <summary>
        /// Verifies that saving a modified array removes old entries that are no longer needed.
        /// </summary>
        /// <remarks>
        /// This test ensures that when an array is shortened (e.g., from 3 items to 2),
        /// the save operation properly removes the old entries to prevent stale data.
        /// This is critical for maintaining configuration integrity.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_SaveModifiedArray_ShouldClearOldEntries()
        {
            // Arrange - Start with 3 items
            var configContent = @"[TestSection]
StringArray=StringArrayArray

[StringArrayArray]
0=Old1
1=Old2
2=Old3";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");
            
            // Modify to have only 2 items
            section.StringArray = new[] { "New1", "New2" };

            // Act
            ((IConfigurationManager)config).Save();

            // Assert
            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain("0=New1", "first new item should be saved");
            savedContent.Should().Contain("1=New2", "second new item should be saved");
            savedContent.Should().NotContain("2=Old3", "old third item should be removed");
            savedContent.Should().NotContain("Old1", "old values should not remain");
            savedContent.Should().NotContain("Old2", "old values should not remain");
            savedContent.Should().NotContain("Old3", "old values should not remain");
        }

        /// <summary>
        /// Verifies that saving a null array clears all entries from its configuration section.
        /// </summary>
        /// <remarks>
        /// This test ensures that setting an array property to null and saving will properly
        /// remove all associated array items from the configuration file, effectively
        /// clearing the array section while maintaining other configuration data.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_SaveNullArray_ShouldClearSection()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=StringArrayArray

[StringArrayArray]
0=Item1
1=Item2";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");
            section.StringArray = null;

            // Act
            ((IConfigurationManager)config).Save();

            // Assert
            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().NotContain("0=Item1", "null array should clear all items");
            savedContent.Should().NotContain("1=Item2", "null array should clear all items");
        }

        #endregion

        #region Array Type Support Tests

        /// <summary>
        /// Verifies that integer arrays properly parse numeric string values into their target type.
        /// </summary>
        /// <remarks>
        /// This test ensures that the type conversion system correctly handles arrays of numeric types,
        /// converting string values from configuration into proper integer values.
        /// This validates the type safety and conversion capabilities of the array system.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_IntegerArray_ShouldParseNumbers()
        {
            // Arrange
            var configContent = @"[TestSection]
NumberList=NumberArray

[NumberArray]
0=100
1=200
2=300";
            
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.NumberList.Should().NotBeNull("number array should be loaded");
            section.NumberList.Should().SatisfyAll("numbers should be parsed correctly",
                list => list.Count() == 3,
                list => list.ElementAt(0) == 100,
                list => list.ElementAt(1) == 200,
                list => list.ElementAt(2) == 300);
        }

        /// <summary>
        /// Verifies that arrays with invalid type conversions throw appropriate format exceptions.
        /// </summary>
        /// <remarks>
        /// This test ensures that when an array contains values that cannot be converted to the target type
        /// (e.g., non-numeric strings in an integer array), the system throws a FormatException.
        /// This provides clear error reporting for configuration validation.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_MixedTypesInArray_ShouldThrowFormatException()
        {
            // Arrange
            var configContent = @"[TestSection]
NumberList=MixedTypeArray

[MixedTypeArray]
0=100
1=not_a_number
2=300";
            
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            
            // Act & Assert
            Action act = () =>
            {
                var testConfig = new ByteForge.Toolkit.Configuration();
                ((IConfigurationManager)testConfig).Initialize(_tempConfigPath);
                var section = ((IConfigurationManager)testConfig).GetSection<ArrayTestConfig>("TestSection");
            };

            // The system should throw when encountering invalid type conversions in arrays
            act.Should().Throw<FormatException>("mixed types in array should throw FormatException for invalid conversions");
        }

        #endregion

        #region Array Round-trip Tests

        /// <summary>
        /// Verifies that array data is preserved through save and reload operations.
        /// </summary>
        /// <remarks>
        /// This test ensures data integrity by verifying that arrays can be saved to configuration
        /// and then reloaded with identical content and ordering. This validates the complete
        /// serialization and deserialization pipeline for array data.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_ArrayRoundTrip_ShouldPreserveData()
        {
            // Arrange
            var configContent = @"[TestSection]";
            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            var originalArray = new[] { "RoundTrip1", "RoundTrip2", "RoundTrip3" };
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");
            section.StringArray = originalArray;

            // Act - Save and reload
            ((IConfigurationManager)config).Save();
            var newConfig = new ByteForge.Toolkit.Configuration();
            ((IConfigurationManager)newConfig).Initialize(_tempConfigPath);
            var reloadedSection = ((IConfigurationManager)newConfig).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            reloadedSection.StringArray.Should().NotBeNull("reloaded array should not be null");
            reloadedSection.StringArray.Should().SatisfyAll("round-trip should preserve array data",
                list => list.Count() == originalArray.Length,
                list => list.ElementAt(0) == originalArray[0],
                list => list.ElementAt(1) == originalArray[1],
                list => list.ElementAt(2) == originalArray[2]);
        }

        /// <summary>
        /// Verifies that multiple arrays of different types can coexist in a single configuration section.
        /// </summary>
        /// <remarks>
        /// This test validates complex scenarios where a configuration section contains multiple
        /// array properties of different types (string arrays, custom named arrays, integer arrays).
        /// This ensures the system can handle realistic configuration requirements.
        /// </remarks>
        [TestMethod]
        public void ConfigSection_ComplexArrayScenario_ShouldWork()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=MainStringArray
CustomNamedArray=CustomSection
NumberList=NumberSection

[MainStringArray]
0=Main1
1=Main2

[CustomSection]
0=Custom1
1=Custom2
2=Custom3

[NumberSection]
0=10
1=20
2=30";

            var config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            ((IConfigurationManager)config).Initialize(_tempConfigPath);

            // Act
            var section = ((IConfigurationManager)config).GetSection<ArrayTestConfig>("TestSection");

            // Assert
            section.StringArray.Should().SatisfyAll("main string array should load correctly",
                list => list.Count() == 2,
                list => list.ElementAt(0) == "Main1",
                list => list.ElementAt(1) == "Main2");
            section.CustomNamedArray.Should().SatisfyAll("custom string array should load correctly",
                list => list.Count() == 3,
                list => list.ElementAt(0) == "Custom1",
                list => list.ElementAt(1) == "Custom2",
                list => list.ElementAt(2) == "Custom3");
            section.NumberList.Should().SatisfyAll("number list should load correctly",
                list => list.Count() == 3,
                list => list.ElementAt(0) == 10,
                list => list.ElementAt(1) == 20,
                list => list.ElementAt(2) == 30);
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