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

        /// <summary>
        /// Verifies that configuration can be initialized with a file path and loads successfully.
        /// </summary>
        /// <remarks>
        /// This test ensures the basic initialization pattern works correctly, loading configuration
        /// data from a specified file path and making the root configuration available.
        /// This is the most common initialization method used in applications.
        /// </remarks>
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

        /// <summary>
        /// Verifies that configuration can be initialized with separate directory and filename parameters.
        /// </summary>
        /// <remarks>
        /// This test validates the alternative initialization method that accepts directory and filename
        /// as separate parameters, providing flexibility for applications that need to construct
        /// file paths dynamically.
        /// </remarks>
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

        /// <summary>
        /// Verifies that initializing with a null directory parameter throws ArgumentNullException.
        /// </summary>
        /// <remarks>
        /// This test ensures proper input validation by confirming that null directory parameters
        /// are rejected with appropriate exceptions, preventing runtime errors and providing
        /// clear error messaging for invalid usage.
        /// </remarks>
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

        /// <summary>
        /// Verifies that initializing with a null filename parameter throws ArgumentNullException.
        /// </summary>
        /// <remarks>
        /// This test ensures comprehensive input validation by confirming that null filename parameters
        /// are properly rejected, maintaining the robustness of the initialization process.
        /// </remarks>
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

        /// <summary>
        /// Verifies that initializing with an empty directory string throws ArgumentNullException.
        /// </summary>
        /// <remarks>
        /// This test ensures that empty strings are treated as invalid input for directory parameters,
        /// maintaining consistent validation behavior alongside null checks.
        /// </remarks>
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

        /// <summary>
        /// Verifies that initializing with an empty filename string throws ArgumentNullException.
        /// </summary>
        /// <remarks>
        /// This test completes the input validation coverage by ensuring empty filename strings
        /// are rejected, providing comprehensive protection against invalid initialization parameters.
        /// </remarks>
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

        /// <summary>
        /// Verifies that attempting to initialize an already initialized configuration throws InvalidOperationException.
        /// </summary>
        /// <remarks>
        /// This test ensures that the configuration system prevents double initialization,
        /// which could lead to unexpected behavior or resource conflicts. This enforces
        /// the singleton pattern and initialization lifecycle.
        /// </remarks>
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

        /// <summary>
        /// Verifies that initializing with a non-existent directory throws DirectoryNotFoundException.
        /// </summary>
        /// <remarks>
        /// This test ensures that the system properly validates directory existence before
        /// attempting to access configuration files, providing clear error messaging
        /// for file system issues.
        /// </remarks>
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

        /// <summary>
        /// Verifies that initializing with a non-existent configuration file throws FileNotFoundException.
        /// </summary>
        /// <remarks>
        /// This test ensures that missing configuration files are detected during initialization
        /// and reported with appropriate exceptions, preventing silent failures and
        /// providing clear guidance for troubleshooting.
        /// </remarks>
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

        /// <summary>
        /// Verifies that adding a configuration section with a valid type creates the section successfully.
        /// </summary>
        /// <remarks>
        /// This test validates the basic section creation functionality, ensuring that strongly-typed
        /// configuration sections can be added to the configuration system and are properly instantiated.
        /// </remarks>
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

        /// <summary>
        /// Verifies that adding a configuration section with a custom name creates a properly named section.
        /// </summary>
        /// <remarks>
        /// This test ensures that the configuration system supports custom section naming,
        /// allowing multiple instances of the same configuration type with different names.
        /// This is essential for scenarios with multiple database connections or environments.
        /// </remarks>
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

        /// <summary>
        /// Verifies that attempting to add a duplicate section throws InvalidOperationException.
        /// </summary>
        /// <remarks>
        /// This test ensures that the configuration system prevents duplicate section registration,
        /// which could lead to ambiguous behavior or data corruption. This enforces unique
        /// section identity within the configuration system.
        /// </remarks>
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

        /// <summary>
        /// Verifies that retrieving an existing configuration section returns the correct section instance.
        /// </summary>
        /// <remarks>
        /// This test validates the section retrieval mechanism, ensuring that previously added
        /// sections can be retrieved and that the same instance is returned for consistency.
        /// This is critical for maintaining state across configuration operations.
        /// </remarks>
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

        /// <summary>
        /// Verifies that retrieving a non-existing configuration section automatically creates it.
        /// </summary>
        /// <remarks>
        /// This test ensures the convenient auto-creation behavior where sections are created
        /// on-demand when first accessed, reducing boilerplate code and improving developer
        /// experience while maintaining type safety.
        /// </remarks>
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

        /// <summary>
        /// Verifies that retrieving a configuration section by custom name returns the correct named section.
        /// </summary>
        /// <remarks>
        /// This test validates the named section retrieval mechanism, ensuring that custom-named
        /// sections can be accessed correctly and that the proper instance is returned.
        /// This supports complex configuration scenarios with multiple section instances.
        /// </remarks>
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

        /// <summary>
        /// Verifies that saving configuration with modified section data persists changes to the file.
        /// </summary>
        /// <remarks>
        /// This test ensures that the save mechanism correctly writes modified configuration
        /// data back to the configuration file, maintaining data persistence and allowing
        /// configuration changes to survive application restarts.
        /// </remarks>
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

        /// <summary>
        /// Verifies that saving configuration with new sections adds them to the configuration file.
        /// </summary>
        /// <remarks>
        /// This test ensures that newly added configuration sections are properly written
        /// to the configuration file during save operations, expanding the configuration
        /// structure as needed without losing existing data.
        /// </remarks>
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

        /// <summary>
        /// Verifies that configuration data is preserved through save and reload operations.
        /// </summary>
        /// <remarks>
        /// This test validates the complete persistence cycle by saving modified configuration
        /// data and then reloading it in a fresh configuration instance, ensuring data
        /// integrity and proper serialization/deserialization behavior.
        /// </remarks>
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

        /// <summary>
        /// Verifies that concurrent access to configuration sections is thread-safe.
        /// </summary>
        /// <remarks>
        /// This test ensures that the configuration system can handle multiple threads
        /// accessing and creating sections simultaneously without race conditions or
        /// corruption, which is essential for multi-threaded applications.
        /// </remarks>
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