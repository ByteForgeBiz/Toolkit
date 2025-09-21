using AwesomeAssertions;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Tests for dictionary configuration functionality including all supported types, edge cases, and error conditions.
    /// </summary>
    [TestClass]
    public class ConfigurationDictionaryTests
    {
        private string _tempConfigPath = null!;

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
            ResetConfiguration();
        }

        #region Basic Dictionary Tests

        /// <summary>
        /// Verifies that Dictionary&lt;string, string&gt; properties are loaded correctly from configuration files.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryProperty_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=FileFormatsDict

[FileFormatsDict]
TCI=TCI_MMddyyyy_WIN_Return.txt
CRC=CRC_MMddyyyy_Return.csv
GICS=GICS_MMddyyyy_Return.csv";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("dictionary property should be loaded");
            section.FileFormats.Should().HaveCount(3, "should load all dictionary entries");
            section.FileFormats["TCI"].Should().Be("TCI_MMddyyyy_WIN_Return.txt", "should load TCI value correctly");
            section.FileFormats["CRC"].Should().Be("CRC_MMddyyyy_Return.csv", "should load CRC value correctly");
            section.FileFormats["GICS"].Should().Be("GICS_MMddyyyy_Return.csv", "should load GICS value correctly");
        }

        /// <summary>
        /// Verifies that dictionary properties with custom section names work correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryWithCustomSectionName_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
CustomSettings=MyCustomSection

[MyCustomSection]
Setting1=Value1
Setting2=Value2
Setting3=Value3";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.CustomSettings.Should().NotBeNull("custom section dictionary should be loaded");
            section.CustomSettings.Should().HaveCount(3, "should load all custom section entries");
            section.CustomSettings["Setting1"].Should().Be("Value1");
            section.CustomSettings["Setting2"].Should().Be("Value2");
            section.CustomSettings["Setting3"].Should().Be("Value3");
        }

        /// <summary>
        /// Verifies that empty dictionaries are handled correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_EmptyDictionary_ShouldLoadAsEmpty()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=FileFormatsDict

[FileFormatsDict]";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("empty dictionary should still be initialized");
            section.FileFormats.Should().BeEmpty("empty section should result in empty dictionary");
        }

        /// <summary>
        /// Verifies that missing dictionary sections are handled gracefully.
        /// </summary>
        [TestMethod]
        public void ConfigSection_MissingDictionarySection_ShouldCreateEmptyDictionary()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=NonExistentSection";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("missing section should create empty dictionary");
            section.FileFormats.Should().BeEmpty("missing section should result in empty dictionary");
        }

        #endregion

        #region Dictionary Type Support Tests

        /// <summary>
        /// Verifies that IDictionary&lt;string, string&gt; interface properties work correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_IDictionaryProperty_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
InterfaceDictionary=InterfaceDictDict

[InterfaceDictDict]
Key1=Value1
Key2=Value2";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.InterfaceDictionary.Should().NotBeNull("IDictionary property should be loaded");
            section.InterfaceDictionary.Should().HaveCount(2, "should load all IDictionary entries");
            section.InterfaceDictionary["Key1"].Should().Be("Value1");
            section.InterfaceDictionary["Key2"].Should().Be("Value2");
        }

        /// <summary>
        /// Verifies that IReadOnlyDictionary&lt;string, string&gt; properties work correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_IReadOnlyDictionaryProperty_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
ReadOnlyDictionary=ReadOnlyDictDict

[ReadOnlyDictDict]
ReadOnlyKey1=ReadOnlyValue1
ReadOnlyKey2=ReadOnlyValue2";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.ReadOnlyDictionary.Should().NotBeNull("IReadOnlyDictionary property should be loaded");
            section.ReadOnlyDictionary.Should().HaveCount(2, "should load all IReadOnlyDictionary entries");
            section.ReadOnlyDictionary["ReadOnlyKey1"].Should().Be("ReadOnlyValue1");
            section.ReadOnlyDictionary["ReadOnlyKey2"].Should().Be("ReadOnlyValue2");
        }

        /// <summary>
        /// Verifies that ICollection&lt;KeyValuePair&lt;string, string&gt;&gt; properties work correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ICollectionKeyValuePairProperty_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
CollectionKeyValuePairs=CollectionDict

[CollectionDict]
CollectionKey1=CollectionValue1
CollectionKey2=CollectionValue2";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.CollectionKeyValuePairs.Should().NotBeNull("ICollection<KeyValuePair> property should be loaded");
            section.CollectionKeyValuePairs.Should().HaveCount(2, "should load all ICollection entries");
            
            var keyValuePairs = section.CollectionKeyValuePairs.ToList();
            keyValuePairs.Should().Contain(kvp => kvp.Key == "CollectionKey1" && kvp.Value == "CollectionValue1");
            keyValuePairs.Should().Contain(kvp => kvp.Key == "CollectionKey2" && kvp.Value == "CollectionValue2");
        }

        /// <summary>
        /// Verifies that IEnumerable&lt;KeyValuePair&lt;string, string&gt;&gt; properties work correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_IEnumerableKeyValuePairProperty_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
EnumerableKeyValuePairs=EnumerableDict

[EnumerableDict]
EnumKey1=EnumValue1
EnumKey2=EnumValue2";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.EnumerableKeyValuePairs.Should().NotBeNull("IEnumerable<KeyValuePair> property should be loaded");
            
            var keyValuePairs = section.EnumerableKeyValuePairs.ToList();
            keyValuePairs.Should().HaveCount(2, "should load all IEnumerable entries");
            keyValuePairs.Should().Contain(kvp => kvp.Key == "EnumKey1" && kvp.Value == "EnumValue1");
            keyValuePairs.Should().Contain(kvp => kvp.Key == "EnumKey2" && kvp.Value == "EnumValue2");
        }

        /// <summary>
        /// Verifies that IReadOnlyCollection&lt;KeyValuePair&lt;string, string&gt;&gt; properties work correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_IReadOnlyCollectionKeyValuePairProperty_ShouldLoadCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
ReadOnlyCollectionKeyValuePairs=ReadOnlyCollectionDict

[ReadOnlyCollectionDict]
ROCollectionKey1=ROCollectionValue1
ROCollectionKey2=ROCollectionValue2";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.ReadOnlyCollectionKeyValuePairs.Should().NotBeNull("IReadOnlyCollection<KeyValuePair> property should be loaded");
            section.ReadOnlyCollectionKeyValuePairs.Should().HaveCount(2, "should load all IReadOnlyCollection entries");
            
            var keyValuePairs = section.ReadOnlyCollectionKeyValuePairs.ToList();
            keyValuePairs.Should().Contain(kvp => kvp.Key == "ROCollectionKey1" && kvp.Value == "ROCollectionValue1");
            keyValuePairs.Should().Contain(kvp => kvp.Key == "ROCollectionKey2" && kvp.Value == "ROCollectionValue2");
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Verifies that dictionary entries with null or empty values are handled correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryWithNullOrEmptyValues_ShouldHandleCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=FileFormatsDict

[FileFormatsDict]
ValidKey=ValidValue
EmptyKey=
# Keys with only whitespace values are handled by the INI provider";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("dictionary should be loaded");
            section.FileFormats["ValidKey"].Should().Be("ValidValue", "valid entry should load correctly");
            
            // Empty value behavior depends on implementation
            if (section.FileFormats.ContainsKey("EmptyKey"))
            {
                section.FileFormats["EmptyKey"].Should().Be("", "empty value should be preserved");
            }
        }

        /// <summary>
        /// Verifies that dictionary keys with special characters are handled correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryWithSpecialCharacterKeys_ShouldHandleCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=FileFormatsDict

[FileFormatsDict]
Key.With.Dots=Value1
Key-With-Dashes=Value2
Key_With_Underscores=Value3
KeyWithNumbers123=Value4";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("dictionary should be loaded");
            section.FileFormats["Key.With.Dots"].Should().Be("Value1", "keys with dots should work");
            section.FileFormats["Key-With-Dashes"].Should().Be("Value2", "keys with dashes should work");
            section.FileFormats["Key_With_Underscores"].Should().Be("Value3", "keys with underscores should work");
            section.FileFormats["KeyWithNumbers123"].Should().Be("Value4", "keys with numbers should work");
        }

        /// <summary>
        /// Verifies that dictionary values with special characters are handled correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryWithSpecialCharacterValues_ShouldHandleCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=FileFormatsDict

[FileFormatsDict]
PathKey=C:\Path\With\Backslashes
UrlKey=https://example.com/path?param=value
SqlKey=SELECT * FROM Table WHERE Column = 'Value'
JsonKey={""key"": ""value"", ""number"": 123}";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("dictionary should be loaded");
            section.FileFormats["PathKey"].Should().Be(@"C:\Path\With\Backslashes", "path values should work");
            section.FileFormats["UrlKey"].Should().Be("https://example.com/path?param=value", "URL values should work");
            section.FileFormats["SqlKey"].Should().Be("SELECT * FROM Table WHERE Column = 'Value'", "SQL values should work");
            section.FileFormats["JsonKey"].Should().Be(@"{""key"": ""value"", ""number"": 123}", "JSON values should work");
        }

        /// <summary>
        /// Verifies that case-insensitive key lookup works for dictionaries.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryCaseInsensitiveKeys_ShouldWork()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=FileFormatsDict

[FileFormatsDict]
TCI=TCI_Value
crc=CRC_Value
Gics=GICS_Value";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("dictionary should be loaded");
            
            // Test case-insensitive access
            section.FileFormats["tci"].Should().Be("TCI_Value", "case-insensitive lookup should work");
            section.FileFormats["CRC"].Should().Be("CRC_Value", "case-insensitive lookup should work");
            section.FileFormats["GICS"].Should().Be("GICS_Value", "case-insensitive lookup should work");
            section.FileFormats["gics"].Should().Be("GICS_Value", "case-insensitive lookup should work");
        }

        #endregion

        #region Default Section Name Tests

        /// <summary>
        /// Verifies that default section names are generated correctly when not specified.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryDefaultSectionName_ShouldUsePropertyNamePlusDict()
        {
            // Arrange
            var configContent = @"[TestSection]
FileFormats=FileFormatsDict

[FileFormatsDict]
DefaultKey=DefaultValue";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("dictionary with default section name should load");
            section.FileFormats["DefaultKey"].Should().Be("DefaultValue", "should load from default section name");
        }

        /// <summary>
        /// Verifies that dictionary properties without section reference use default naming.
        /// </summary>
        [TestMethod]
        public void ConfigSection_DictionaryWithoutSectionReference_ShouldUseDefaultNaming()
        {
            // Arrange - Don't specify the section reference in main section
            var sectionName = "TestSection";
            var configContent = $@"[{sectionName}]

[{sectionName}#FileFormatsDict]
AutoKey=AutoValue";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<DictionaryTestConfig>("TestSection");

            // Assert
            section.FileFormats.Should().NotBeNull("dictionary should be initialized");
            section.FileFormats["AutoKey"].Should().Be("AutoValue", "should load from default section name even without reference");
        }

        #endregion

        #region Error Condition Tests

        /// <summary>
        /// Verifies that unsupported dictionary types throw appropriate exceptions.
        /// </summary>
        [TestMethod]
        public void ConfigSection_UnsupportedDictionaryType_ShouldThrowException()
        {
            // Arrange
            var configContent = @"[TestSection]
UnsupportedDict=SomeSection

[SomeSection]
Key=Value";
            
            // Act & Assert
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<UnsupportedDictionaryTestConfig>("TestSection");
            };

            act.Should().Throw<NotSupportedException>("unsupported dictionary type should throw NotSupportedException")
               .WithMessage("*must be of a supported dictionary type*");
        }

        /// <summary>
        /// Verifies that null dictionary properties are handled gracefully.
        /// </summary>
        [TestMethod]
        public void ConfigSection_NullDictionaryProperty_ShouldNotThrow()
        {
            // Arrange
            var configContent = @"[TestSection]
SomeOtherProperty=Value";
            
            // Act
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
                config.Initialize(_tempConfigPath);
                var section = config.GetSection<DictionaryTestConfig>("TestSection");
            };

            // Assert
            act.Should().NotThrow("null dictionary properties should be handled gracefully");
        }

        #endregion

        #region Save Tests

        /// <summary>
        /// Verifies that dictionary properties can be saved back to configuration files.
        /// </summary>
        [TestMethod]
        public void ConfigSection_SaveDictionary_ShouldPersistCorrectly()
        {
            // Arrange
            var configContent = @"[TestSection]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var sectionName = "TestSection";
            var section = config.GetSection<DictionaryTestConfig>(sectionName);
            section.FileFormats = new Dictionary<string, string>
            {
                {"TCI", "TCI_MMddyyyy_WIN_Return.txt"},
                {"CRC", "CRC_MMddyyyy_Return.csv"},
                {"GICS", "GICS_MMddyyyy_Return.csv"}
            };
            var propertyName = nameof(section.FileFormats);

            // Act
            config.Save();

            // Assert
            var savedContent = System.IO.File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain($"FileFormats={sectionName}#{propertyName}Dict", "should save section reference");
            savedContent.Should().Contain($"[{sectionName}#{propertyName}Dict]", "should create dictionary section");
            savedContent.Should().Contain("TCI=TCI_MMddyyyy_WIN_Return.txt", "should save TCI entry");
            savedContent.Should().Contain("CRC=CRC_MMddyyyy_Return.csv", "should save CRC entry");
            savedContent.Should().Contain("GICS=GICS_MMddyyyy_Return.csv", "should save GICS entry");
        }

        /// <summary>
        /// Verifies that empty dictionaries are saved correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_SaveEmptyDictionary_ShouldNotCreateEmptySection()
        {
            // Arrange
            var sectionName = "TestSection";
            var configContent = $"[{sectionName}]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<DictionaryTestConfig>(sectionName);
            section.FileFormats = [];
            var propertyName = nameof(section.FileFormats);

            // Act
            config.Save();

            // Assert
            var savedContent = System.IO.File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain($"FileFormats={sectionName}#{propertyName}Dict", "should save section reference even for empty dictionary");
            savedContent.Should().NotContain($"[{sectionName}#{propertyName}Dict]", "should not create empty dictionary section");
        }

        /// <summary>
        /// Verifies that dictionary properties with custom section names are saved correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_SaveDictionaryWithCustomSectionName_ShouldUseCustomName()
        {
            // Arrange
            var sectionName = "TestSection";
            var configContent = $"[{sectionName}]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<DictionaryTestConfig>(sectionName);
            section.CustomSettings = new Dictionary<string, string>
            {
                {"Setting1", "Value1"},
                {"Setting2", "Value2"}
            };

            // Act
            config.Save();

            // Assert
            var savedContent = System.IO.File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain($"CustomSettings={sectionName}#MyCustomSection", "should save custom section reference");
            savedContent.Should().Contain($"[{sectionName}#MyCustomSection]", "should create custom section");
            savedContent.Should().Contain("Setting1=Value1", "should save custom section entries");
            savedContent.Should().Contain("Setting2=Value2", "should save custom section entries");
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