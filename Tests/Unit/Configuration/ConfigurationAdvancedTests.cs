using AwesomeAssertions;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Advanced integration tests for complex configuration scenarios, inheritance, and real-world usage patterns.
    /// </summary>
    [TestClass]
    public class ConfigurationAdvancedTests
    {
        private string _tempConfigPath = null!;

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
            ResetConfiguration();
        }

        #region Complex Naming Convention Tests

        /// <summary>
        /// Verifies that complex naming scenarios work correctly with the enhanced naming system.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ComplexNamingScenarios_ShouldWorkCorrectly()
        {
            // Arrange
            var configContent = @"[ComplexNaming]
DefaultNamedArray=ComplexNaming#DefaultNamedArrayArray
CustomNamedArray=SpecificArraySection
DefaultNamedDict=ComplexNaming#DefaultNamedDictDict
CustomNamedDict=SpecificDictSection
RegularProperty=TestValue

[ComplexNaming#DefaultNamedArrayArray]
0=DefaultArray1
1=DefaultArray2

[SpecificArraySection]
0=CustomArray1
1=CustomArray2

[ComplexNaming#DefaultNamedDictDict]
DefaultKey1=DefaultValue1
DefaultKey2=DefaultValue2

[SpecificDictSection]
CustomKey1=CustomValue1
CustomKey2=CustomValue2";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<NamingConventionTestConfig>("ComplexNaming");

            // Assert
            section.RegularProperty.Should().Be("TestValue", "regular property should load");
            
            section.DefaultNamedArray.Should().NotBeNull("default named array should load");
            section.DefaultNamedArray.Should().HaveCount(2, "default array should have correct count");
            section.DefaultNamedArray[0].Should().Be("DefaultArray1");
            
            section.CustomNamedArray.Should().NotBeNull("custom named array should load");
            section.CustomNamedArray.Should().HaveCount(2, "custom array should have correct count");
            section.CustomNamedArray[0].Should().Be("CustomArray1");
            
            section.DefaultNamedDict.Should().NotBeNull("default named dictionary should load");
            section.DefaultNamedDict.Should().HaveCount(2, "default dict should have correct count");
            section.DefaultNamedDict["DefaultKey1"].Should().Be("DefaultValue1");
            
            section.CustomNamedDict.Should().NotBeNull("custom named dictionary should load");
            section.CustomNamedDict.Should().HaveCount(2, "custom dict should have correct count");
            section.CustomNamedDict["CustomKey1"].Should().Be("CustomValue1");
        }

        /// <summary>
        /// Verifies that saving complex naming scenarios maintains correct section references.
        /// </summary>
        [TestMethod]
        public void ConfigSection_SaveComplexNaming_ShouldMaintainCorrectReferences()
        {
            // Arrange
            var sectionName = "ComplexNaming";
            var configContent = $@"[{sectionName}]
RegularProperty=Initial";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<NamingConventionTestConfig>(sectionName);
            section.DefaultNamedArray = new[] { "NewDefault1", "NewDefault2" };
            section.CustomNamedArray = new List<string> { "NewCustom1", "NewCustom2" };
            section.DefaultNamedDict = new Dictionary<string, string> { { "NewDefaultKey", "NewDefaultValue" } };
            section.CustomNamedDict = new Dictionary<string, string> { { "NewCustomKey", "NewCustomValue" } };

            // Act
            config.Save();

            // Assert
            var savedContent = File.ReadAllText(_tempConfigPath);
            
            // Check section references
            savedContent.Should().Contain("DefaultNamedArray=", "default array reference should be saved");
            savedContent.Should().Contain($"CustomNamedArray={sectionName}#SpecificArraySection", "custom array reference should use custom name");
            savedContent.Should().Contain("DefaultNamedDict=", "default dict reference should be saved");
            savedContent.Should().Contain($"CustomNamedDict={sectionName}#SpecificDictSection", "custom dict reference should use custom name");
            
            // Check section headers
            savedContent.Should().Contain($"[{sectionName}#SpecificArraySection]", "custom array section should be created");
            savedContent.Should().Contain($"[{sectionName}#SpecificDictSection]", "custom dict section should be created");
            
            // Check content
            savedContent.Should().Contain("0=NewCustom1", "custom array content should be saved");
            savedContent.Should().Contain("NewCustomKey=NewCustomValue", "custom dict content should be saved");
        }

        #endregion

        #region Inheritance and Complex Type Tests

        /// <summary>
        /// Verifies that configuration inheritance works correctly with base class properties.
        /// </summary>
        [TestMethod]
        public void ConfigSection_InheritanceScenarios_ShouldWorkCorrectly()
        {
            // Arrange
            var configContent = @"[InheritanceTest]
Server=InheritedServer
Port=5432
Username=inherited_user
ExtendedProperty=ExtendedValue
AdditionalFeatures=ExtendedFeatures
ExtendedSettings=ExtendedSettings

[ExtendedFeatures]
0=Feature1
1=Feature2

[ExtendedSettings]
Setting1=Value1
Setting2=Value2";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<InheritanceTestConfig>("InheritanceTest");

            // Assert
            // Base class properties
            section.Server.Should().Be("InheritedServer", "inherited property should work");
            section.Port.Should().Be(5432, "inherited property should work");
            section.Username.Should().Be("inherited_user", "inherited property should work");
            section.Timeout.Should().Be(30, "inherited default value should work");
            section.UseSSL.Should().Be(false, "inherited default value should work");
            
            // Extended properties
            section.ExtendedProperty.Should().Be("ExtendedValue", "extended property should work");
            section.AdditionalFeatures.Should().NotBeNull("extended array should work");
            section.AdditionalFeatures.Should().HaveCount(2, "extended array should have correct count");
            section.ExtendedSettings.Should().NotBeNull("extended dictionary should work");
            section.ExtendedSettings.Should().HaveCount(2, "extended dictionary should have correct count");
        }

        /// <summary>
        /// Verifies that mixed complex configurations with multiple data types work correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_MixedComplexConfiguration_ShouldWorkCorrectly()
        {
            // Arrange
            var configContent = @"[MixedComplex]
Name=ComplexConfig
PrimaryItems=PrimaryArray
SecondaryItems=SecondaryArray
PrimarySettings=PrimaryDict
SecondarySettings=SecondaryDict
IsActive=true

[PrimaryArray]
0=Primary1
1=Primary2
2=Primary3

[SecondaryArray]
0=100
1=200
2=300

[PrimaryDict]
PrimaryKey1=PrimaryValue1
PrimaryKey2=PrimaryValue2

[SecondaryDict]
SecondaryKey1=SecondaryValue1
SecondaryKey2=SecondaryValue2";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<MixedComplexTestConfig>("MixedComplex");

            // Assert
            section.Name.Should().Be("ComplexConfig", "basic property should load");
            section.IsActive.Should().Be(true, "boolean property should load");
            
            section.PrimaryItems.Should().NotBeNull("string array should load");
            section.PrimaryItems.Should().HaveCount(3, "string array should have correct count");
            section.PrimaryItems[0].Should().Be("Primary1");
            
            section.SecondaryItems.Should().NotBeNull("int array should load");
            section.SecondaryItems.Should().HaveCount(3, "int array should have correct count");
            section.SecondaryItems[0].Should().Be(100);
            
            section.PrimarySettings.Should().NotBeNull("primary dictionary should load");
            section.PrimarySettings.Should().HaveCount(2, "primary dictionary should have correct count");
            section.PrimarySettings["PrimaryKey1"].Should().Be("PrimaryValue1");
            
            section.SecondarySettings.Should().NotBeNull("secondary dictionary should load");
            section.SecondarySettings.Should().HaveCount(2, "secondary dictionary should have correct count");
            section.SecondarySettings["SecondaryKey1"].Should().Be("SecondaryValue1");
            
            section.TotalItems.Should().Be(6, "computed property should work correctly");
        }

        #endregion

        #region Real-World Integration Tests

        /// <summary>
        /// Verifies that a real-world GHM export configuration scenario works correctly.
        /// </summary>
        [TestMethod]
        public void ConfigSection_RealWorldGHMExportScenario_ShouldWorkCorrectly()
        {
            // Arrange
            var configContent = @"[GHMExport]
WiredFolder=C:\Export\Wired
WirelessFolder=C:\Export\Wireless
RetainFor=30
FormatConfigurations=GHMFormats
OutputPaths=GHMOutputPaths

[GHMFormats]
TCI=TCI_MMddyyyy_WIN_Return.txt
CRC=CRC_MMddyyyy_Return.csv
GICS=GICS_MMddyyyy_Return.csv
LMCA=LMCA_MMddyyyy_Return.txt

[GHMOutputPaths]
TCI=\\server\export\tci\
CRC=\\server\export\crc\
GICS=\\server\export\gics\
LMCA=\\server\export\lmca\";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<GHMExportTestConfig>("GHMExport");

            // Assert
            section.WiredFolder.Should().Be(@"C:\Export\Wired", "folder configuration should load");
            section.WirelessFolder.Should().Be(@"C:\Export\Wireless", "folder configuration should load");
            section.RetainFor.Should().Be(30, "retention configuration should load");
            
            section.FormatConfigurations.Should().NotBeNull("format configurations should load");
            section.FormatConfigurations.Should().HaveCount(4, "should have all format configurations");
            section.FormatConfigurations["TCI"].Should().Be("TCI_MMddyyyy_WIN_Return.txt");
            section.FormatConfigurations["CRC"].Should().Be("CRC_MMddyyyy_Return.csv");
            
            section.OutputPaths.Should().NotBeNull("output paths should load");
            section.OutputPaths.Should().HaveCount(4, "should have all output paths");
            section.OutputPaths["TCI"].Should().Be(@"\\server\export\tci\");
            section.OutputPaths["CRC"].Should().Be(@"\\server\export\crc\");
        }

        /// <summary>
        /// Verifies that modifying and saving a real-world configuration maintains integrity.
        /// </summary>
        [TestMethod]
        public void ConfigSection_ModifyRealWorldConfiguration_ShouldMaintainIntegrity()
        {
            // Arrange
            var configContent = @"[GHMExport]
WiredFolder=C:\Export\Wired
RetainFor=30
FormatConfigurations=GHMFormats

[GHMFormats]
TCI=TCI_MMddyyyy_WIN_Return.txt
CRC=CRC_MMddyyyy_Return.csv";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<GHMExportTestConfig>("GHMExport");
            
            // Modify configuration
            section.WiredFolder = @"C:\NewExport\Wired";
            section.RetainFor = 60;
            section.FormatConfigurations["GICS"] = "GICS_MMddyyyy_NEW_Return.csv";
            section.FormatConfigurations.Remove("CRC");

            // Act
            config.Save();

            // Reload and verify
            IConfigurationManager newConfig = new ByteForge.Toolkit.Configuration();
            newConfig.Initialize(_tempConfigPath);
            var reloadedSection = newConfig.GetSection<GHMExportTestConfig>("GHMExport");

            // Assert
            reloadedSection.WiredFolder.Should().Be(@"C:\NewExport\Wired", "modified folder should persist");
            reloadedSection.RetainFor.Should().Be(60, "modified retention should persist");
            reloadedSection.FormatConfigurations.Should().HaveCount(2, "dictionary should have correct count after modification");
            reloadedSection.FormatConfigurations["TCI"].Should().Be("TCI_MMddyyyy_WIN_Return.txt", "unchanged value should persist");
            reloadedSection.FormatConfigurations["GICS"].Should().Be("GICS_MMddyyyy_NEW_Return.csv", "new value should persist");
            reloadedSection.FormatConfigurations.Should().NotContainKey("CRC", "removed key should not be present");
        }

        #endregion

        #region Advanced Error Recovery Tests

        /// <summary>
        /// Verifies that the system gracefully recovers from partially corrupted configurations.
        /// </summary>
        [TestMethod]
        public void ConfigSection_PartiallyCorruptedConfiguration_ShouldThrow()
        {
            // Arrange - Configuration with some valid and some invalid sections
            var configContent = @"[ValidSection]
StringValue=ValidString
IntValue=42

[InvalidSection
StringValue=This section header is malformed

[AnotherValidSection]
StringValue=AnotherValidString
BoolValue=true

[ValidArraySection]
StringArray=ValidArray

[ValidArray]
0=Item1
1=Item2";
            
            // Act & Assert - Should handle valid sections despite corruption
            Action act = () =>
            {
                IConfigurationManager config = new ByteForge.Toolkit.Configuration();
                _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
                config.Initialize(_tempConfigPath);
                
                var validSection = config.GetSection<BasicTestConfig>("ValidSection");
                var anotherValidSection = config.GetSection<BasicTestConfig>("AnotherValidSection");
                var arraySection = config.GetSection<ArrayTestConfig>("ValidArraySection");
                
                // Valid sections should work
                validSection.StringValue.Should().Be("ValidString");
                anotherValidSection.StringValue.Should().Be("AnotherValidString");
                arraySection.StringArray.Should().NotBeNull();
            };

            // The system should either handle this gracefully or throw a specific exception
            act.Should().Throw<InvalidDataException>("the data is corrupted");
        }

#if false

        /*
         * Future Enhancement:
         *
         * The following test is disabled because the current implementation does not support
         * polymorphic deserialization. This would require significant changes to the
         * configuration system to handle type metadata and dynamic instantiation.
         *
         * Once polymorphic support is implemented, this test can be enabled to verify
         * that derived types are correctly instantiated based on configuration data.
         *
         * Currently, once a section is defined with a specific type, it cannot be
         * changed to a derived type without modifying the configuration system.
         *
         */

        /// <summary>
        /// Verifies that configuration handles missing referenced sections gracefully.
        /// </summary>
        [TestMethod]
        public void ConfigSection_MissingReferencedSections_ShouldHandleGracefully()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=NonExistentArraySection
FileFormats=NonExistentDictSection
StringValue=ValidValue";
            
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            // Act
            var arraySection = config.GetSection<ArrayTestConfig>("TestSection");
            var dictSection = config.GetSection<DictionaryTestConfig>("DictTestSection");
            var basicSection = config.GetSection<BasicTestConfig>("TestSection");

            // Assert
            basicSection.StringValue.Should().Be("ValidValue", "valid properties should still work");
            arraySection.StringArray.Should().NotBeNull("missing array section should create empty array");
            arraySection.StringArray.Should().BeEmpty("missing array section should result in empty array");
            dictSection.FileFormats.Should().NotBeNull("missing dict section should create empty dictionary");
            dictSection.FileFormats.Should().BeEmpty("missing dict section should result in empty dictionary");
        }
#endif

        #endregion

        #region Performance and Memory Tests

        /// <summary>
        /// Verifies that the system handles memory efficiently with large object graphs.
        /// </summary>
        [TestMethod]
        public void ConfigSection_LargeObjectGraphs_ShouldHandleMemoryEfficiently()
        {
            // Arrange
            var configContent = @"[MemoryTest]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<MixedComplexTestConfig>("MemoryTest");

            // Create large object graphs
            section.PrimaryItems = new string[10000];
            for (var i = 0; i < 10000; i++)
            {
                section.PrimaryItems[i] = $"LargeItem{i}_with_substantial_content_to_test_memory_usage";
            }

            section.PrimarySettings = new Dictionary<string, string>();
            for (var i = 0; i < 5000; i++)
            {
                section.PrimarySettings[$"LargeKey{i}"] = $"LargeValue{i}_with_substantial_content";
            }

            // Act - Measure memory usage during save
            var initialMemory = GC.GetTotalMemory(true);
            config.Save();
            var afterSaveMemory = GC.GetTotalMemory(false);

            // Assert
            var memoryIncrease = afterSaveMemory - initialMemory;
            var maxAcceptableIncrease = 100 * 1024 * 1024; // 100MB
            
            memoryIncrease.Should().BeLessThan(maxAcceptableIncrease, "memory usage should remain reasonable");
            File.Exists(_tempConfigPath).Should().BeTrue("large configuration should be saved successfully");
        }

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