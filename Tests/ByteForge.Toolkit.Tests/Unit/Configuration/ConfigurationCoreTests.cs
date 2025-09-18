using System;
using System.IO;
using System.Reflection;
using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Tests for core Configuration management functionality including initialization, section management, and save/load operations.
    /// </summary>
    [TestClass]
    public class ConfigurationCoreTests
    {
        private string _tempConfigPath;
        private string _testConfigContent;

        [TestInitialize]
        public void TestInitialize()
        {
            // Reset Configuration for each test
            ResetConfiguration();
            
            _testConfigContent = @"[TestSection]
StringValue=Test String
IntValue=42
BoolValue=true
DoubleValue=3.14159
DateValue=2024-01-15
TimeoutValue=45
CustomName=Mapped Value

[DatabaseSettings]
Server=localhost
Port=1433
Username=testuser
Password=testpass
Timeout=30
UseSSL=true

[GlobalizationSettings]
Culture=en-US
DateFormat=MM/dd/yyyy
NumberFormat=#,##0.00
CurrencySymbol=$";
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
            ResetConfiguration();
        }

        #region Initialization Tests

        [TestMethod]
        public void Initialize_WithFilePath_ShouldLoadConfiguration()
        {
            // Arrange - Create isolated instance for testing
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(_testConfigContent);

            // Act
            config.Initialize(_tempConfigPath);

            // Assert
            config.IsInitialized.Should().BeTrue("configuration should be initialized");
            config.Root.Should().NotBeNull("root configuration should be available");
        }

        [TestMethod]
        public void Initialize_WithDirectoryAndFileName_ShouldLoadConfiguration()
        {
            // Arrange - Create isolated instance for testing
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(_testConfigContent);
            var directory = Path.GetDirectoryName(_tempConfigPath);
            var fileName = Path.GetFileName(_tempConfigPath);

            // Act
            config.Initialize(directory, fileName);

            // Assert
            config.IsInitialized.Should().BeTrue("configuration should be initialized");
            config.Root.Should().NotBeNull("root configuration should be available");
        }

        [TestMethod]
        public void Initialize_WithNullDirectory_ShouldThrowArgumentNullException()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();

            // Act & Assert
            Action act = () => config.Initialize(null, "test.ini");
            
            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("directory", "null directory should throw ArgumentNullException");
        }

        [TestMethod]
        public void Initialize_WithNullFileName_ShouldThrowArgumentNullException()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();

            // Act & Assert
            Action act = () => config.Initialize(@"C:\Temp", null);
            
            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("fileName", "null fileName should throw ArgumentNullException");
        }

        [TestMethod]
        public void Initialize_WithEmptyDirectory_ShouldThrowArgumentNullException()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();

            // Act & Assert
            Action act = () => config.Initialize("", "test.ini");
            
            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("directory", "empty directory should throw ArgumentNullException");
        }

        [TestMethod]
        public void Initialize_WithEmptyFileName_ShouldThrowArgumentNullException()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();

            // Act & Assert
            Action act = () => config.Initialize(@"C:\Temp", "");
            
            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("fileName", "empty fileName should throw ArgumentNullException");
        }

        [TestMethod]
        public void Initialize_TwiceWithSameFile_ShouldThrowInvalidOperationException()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(_testConfigContent);
            config.Initialize(_tempConfigPath);

            // Act & Assert
            Action act = () => config.Initialize(_tempConfigPath);
            
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*already been initialized*", "double initialization should throw InvalidOperationException");
        }

        [TestMethod]
        public void Initialize_WithNonExistentDirectory_ShouldThrowDirectoryNotFoundException()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();

            // Act & Assert
            Action act = () => config.Initialize(@"C:\NonExistentDirectory\SubDir", "test.ini");
            
            act.Should().Throw<DirectoryNotFoundException>()
                .WithMessage("*directory was not found*", "non-existent directory should throw DirectoryNotFoundException");
        }

        [TestMethod]
        public void Initialize_WithNonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            var tempDir = Path.GetTempPath();

            // Act & Assert
            Action act = () => config.Initialize(tempDir, "NonExistentFile.ini");
            
            act.Should().Throw<FileNotFoundException>()
                .WithMessage("*file was not found*", "non-existent file should throw FileNotFoundException");
        }

        #endregion

        #region Section Management Tests

        [TestMethod]
        public void AddSection_WithValidType_ShouldCreateSection()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(_testConfigContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.AddSection<BasicTestConfig>();

            // Assert
            section.Should().NotBeNull("section should be created");
            section.Should().BeOfType<BasicTestConfig>("section should be of correct type");
        }

        [TestMethod]
        public void AddSection_WithCustomSectionName_ShouldCreateNamedSection()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(_testConfigContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.AddSection<BasicTestConfig>("CustomSection");

            // Assert
            section.Should().NotBeNull("named section should be created");
            section.Should().BeOfType<BasicTestConfig>("section should be of correct type");
        }

        [TestMethod]
        public void AddSection_Duplicate_ShouldThrowInvalidOperationException()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(_testConfigContent);
            config.Initialize(_tempConfigPath);
            config.AddSection<BasicTestConfig>();

            // Act & Assert
            Action act = () => config.AddSection<BasicTestConfig>();
            
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*section*already exists*", "duplicate section should throw InvalidOperationException");
        }

        [TestMethod]
        public void GetSection_ExistingSection_ShouldReturnSection()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(_testConfigContent);
            config.Initialize(_tempConfigPath);
            var originalSection = config.AddSection<BasicTestConfig>();

            // Act
            var retrievedSection = config.GetSection<BasicTestConfig>();

            // Assert
            retrievedSection.Should().NotBeNull("section should be retrieved");
            retrievedSection.Should().BeSameAs(originalSection, "should return the same section instance");
        }

        [TestMethod]
        public void GetSection_NonExistingSection_ShouldAutoCreate()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(_testConfigContent);
            config.Initialize(_tempConfigPath);

            // Act
            var section = config.GetSection<BasicTestConfig>();

            // Assert
            section.Should().NotBeNull("section should be auto-created");
            section.Should().BeOfType<BasicTestConfig>("section should be of correct type");
        }

        [TestMethod]
        public void GetSection_WithCustomName_ShouldReturnNamedSection()
        {
            var _tempConfigContent = @"[CustomSection]" +
                                     "StringValue=Custom String" + Environment.NewLine +
                                     "IntValue=100" + Environment.NewLine +
                                     "BoolValue=false" + Environment.NewLine;
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(_tempConfigContent);
            config.Initialize(_tempConfigPath);
            var originalSection = config.AddSection<BasicTestConfig>("CustomSection");

            // Act
            var retrievedSection = config.GetSection<BasicTestConfig>("CustomSection");

            // Assert
            retrievedSection.Should().NotBeNull("named section should be retrieved");
            retrievedSection.Should().BeSameAs(originalSection, "should return the same named section instance");
        }

        #endregion

        #region Save/Load Tests

        [TestMethod]
        public void Save_WithModifiedSection_ShouldPersistChanges()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(_testConfigContent);
            config.Initialize(_tempConfigPath);
            var section = config.GetSection<BasicTestConfig>("TestSection");
            
            // Modify values
            section.StringValue = "Modified String";
            section.IntValue = 999;
            section.BoolValue = false;

            // Act
            config.Save();

            // Assert - Read file directly to verify persistence
            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain("StringValue=Modified String", "modified string should be saved");
            savedContent.Should().Contain("IntValue=999", "modified int should be saved");
            savedContent.Should().Contain("BoolValue=False", "modified bool should be saved");
        }

        [TestMethod]
        public void Save_WithNewSection_ShouldAddToFile()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(_testConfigContent);
            config.Initialize(_tempConfigPath);
            var newSection = config.AddSection<DatabaseConfig>("NewDatabaseSection");
            
            newSection.Server = "newserver";
            newSection.Port = 5432;
            newSection.UseSSL = true;

            // Act
            config.Save();

            // Assert
            var savedContent = File.ReadAllText(_tempConfigPath);
            savedContent.Should().Contain("[NewDatabaseSection]", "new section header should be added");
            savedContent.Should().Contain("Server=newserver", "new section data should be saved");
            savedContent.Should().Contain("Port=5432", "new section port should be saved");
        }

        [TestMethod]
        public void RoundTrip_SaveAndReload_ShouldPreserveData()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(_testConfigContent);
            config.Initialize(_tempConfigPath);
            var originalSection = config.GetSection<BasicTestConfig>("TestSection");
            
            var originalString = originalSection.StringValue;
            var originalInt = originalSection.IntValue;
            var originalBool = originalSection.BoolValue;

            // Modify and save
            originalSection.StringValue = "Round Trip Test";
            originalSection.IntValue = 12345;
            config.Save();

            // Create new instance and reload
            IConfigurationManager newConfig = new ByteForge.Toolkit.Configuration();
            newConfig.Initialize(_tempConfigPath);
            var reloadedSection = newConfig.GetSection<BasicTestConfig>("TestSection");

            // Assert
            reloadedSection.StringValue.Should().Be("Round Trip Test", "modified string should persist");
            reloadedSection.IntValue.Should().Be(12345, "modified int should persist");
            reloadedSection.BoolValue.Should().Be(originalBool, "unmodified bool should remain unchanged");
        }

        #endregion

        #region Thread Safety Tests

        [TestMethod]
        public void ConcurrentAccess_MultipleSections_ShouldBeThreadSafe()
        {
            // Arrange
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(_testConfigContent);
            config.Initialize(_tempConfigPath);

            var tasks = new System.Threading.Tasks.Task[10];
            var results = new BasicTestConfig[10];

            // Act - Create multiple tasks accessing configuration concurrently
            for (int i = 0; i < 10; i++)
            {
                int index = i;
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    results[index] = config.GetSection<BasicTestConfig>($"Section{index}");
                });
            }

            System.Threading.Tasks.Task.WaitAll(tasks);

            // Assert
            for (int i = 0; i < 10; i++)
            {
                results[i].Should().NotBeNull($"section {i} should be created safely");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Resets the Configuration singleton for testing.
        /// </summary>
        private void ResetConfiguration()
        {
            // Use reflection to reset the singleton instance for testing
            var instanceField = typeof(ByteForge.Toolkit.Configuration).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            var isInitializedField = typeof(ByteForge.Toolkit.Configuration).GetField("IsInitialized", BindingFlags.Public | BindingFlags.Static);
            var manuallyInitializedField = typeof(ByteForge.Toolkit.Configuration).GetField("_manuallyInitialized", BindingFlags.NonPublic | BindingFlags.Static);

            if (instanceField != null)
            {
                // Reset the lazy instance
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