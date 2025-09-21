using AwesomeAssertions;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace ByteForge.Toolkit.Tests.Unit.Configuration
{
    /// <summary>
    /// Comprehensive thread safety and performance tests for the configuration system.
    /// </summary>
    [TestClass]
    public class ConfigurationThreadSafetyTests
    {
        private string _tempConfigPath = null!;

        [TestCleanup]
        public void TestCleanup()
        {
            TestConfigurationHelper.CleanupTempFiles();
            ResetConfiguration();
        }

        #region Concurrent Save Operations Tests

        /// <summary>
        /// Verifies that multiple threads can safely save different configuration sections simultaneously.
        /// </summary>
        [TestMethod]
        public void Configuration_ConcurrentSaveDifferentSections_ShouldBeThreadSafe()
        {
            // Arrange
            var configContent = new StringBuilder();
            for (var i = 0; i < 10; i++)
            {
                configContent.AppendLine($"[Section{i}]");
                configContent.AppendLine($"StringValue=Initial{i}");
                configContent.AppendLine($"IntValue={i + 1}");
                configContent.AppendLine();
            }

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent.ToString());
            config.Initialize(_tempConfigPath);

            var tasks = new Task[10];
            var exceptions = new ConcurrentBag<Exception>();

            // Act - Multiple threads saving different sections
            for (var i = 0; i < 10; i++)
            {
                var index = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        var section = config.GetSection<BasicTestConfig>($"Section{index}");
                        section.StringValue = $"Updated{index}";
                        section.IntValue = (index + 1) * 10;
                        config.Save();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            var completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(30));

            // Assert
            completed.Should().BeTrue("all concurrent save tasks should complete");
            exceptions.Should().BeEmpty("no exceptions should occur during concurrent saves");

            // Verify all changes were saved
            var savedContent = File.ReadAllText(_tempConfigPath);
            for (var i = 0; i < 10; i++)
            {
                savedContent.Should().Contain($"StringValue=Updated{i}", $"section {i} should be updated");
                savedContent.Should().Contain($"IntValue={(i + 1) * 10}", $"section {i} int value should be updated");
            }
        }

        /// <summary>
        /// Verifies that concurrent section creation is thread-safe.
        /// </summary>
        [TestMethod]
        public void Configuration_ConcurrentSectionCreation_ShouldBeThreadSafe()
        {
            // Arrange
            var configContent = @"[ExistingSection]
ExistingValue=Test";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var tasks = new Task<BasicTestConfig>[20];
            var exceptions = new ConcurrentBag<Exception>();

            // Act - Multiple threads creating new sections concurrently
            for (var i = 0; i < 20; i++)
            {
                var index = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        return config.GetSection<BasicTestConfig>($"ConcurrentSection{index}");
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                        return null;
                    }
                });
            }

            Task.WaitAll(tasks);

            // Assert
            exceptions.Should().BeEmpty("no exceptions should occur during concurrent section creation");
            
            for (var i = 0; i < 20; i++)
            {
                tasks[i].Result.Should().NotBeNull($"concurrent section {i} should be created successfully");
            }
        }

        /// <summary>
        /// Verifies that concurrent access to the same section is thread-safe.
        /// </summary>
        [TestMethod]
        public void Configuration_ConcurrentSameSectionAccess_ShouldReturnSameInstance()
        {
            // Arrange
            var configContent = @"[SharedSection]
StringValue=Shared
IntValue=100";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var tasks = new Task<BasicTestConfig>[50];
            var exceptions = new ConcurrentBag<Exception>();

            // Act - Multiple threads accessing the same section
            for (var i = 0; i < 50; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        return config.GetSection<BasicTestConfig>("SharedSection");
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                        return null;
                    }
                });
            }

            Task.WaitAll(tasks);

            // Assert
            exceptions.Should().BeEmpty("no exceptions should occur during concurrent same section access");
            
            var firstSection = tasks[0].Result;
            firstSection.Should().NotBeNull("first section should be valid");

            for (var i = 1; i < 50; i++)
            {
                tasks[i].Result.Should().BeSameAs(firstSection, $"all concurrent accesses should return the same section instance");
            }
        }

        #endregion

        #region Concurrent Array and Dictionary Operations Tests

        /// <summary>
        /// Verifies that concurrent array modifications are handled safely.
        /// </summary>
        [TestMethod]
        public void Configuration_ConcurrentArrayModifications_ShouldBeThreadSafe()
        {
            // Arrange
            var configContent = @"[TestSection]
StringArray=TestStringArray

[TestStringArray]
0=Initial1
1=Initial2";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var tasks = new Task[10];
            var exceptions = new ConcurrentBag<Exception>();

            // Act - Multiple threads modifying arrays
            for (var i = 0; i < 10; i++)
            {
                var index = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        var section = config.GetSection<ArrayTestConfig>("TestSection");
                        section.StringArray = new[] { $"Concurrent{index}_1", $"Concurrent{index}_2", $"Concurrent{index}_3" };
                        config.Save();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            var completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(30));

            // Assert
            completed.Should().BeTrue("all concurrent array modification tasks should complete");
            exceptions.Should().BeEmpty("no exceptions should occur during concurrent array modifications");

            // File should exist and contain valid data
            File.Exists(_tempConfigPath).Should().BeTrue("configuration file should exist after concurrent operations");
            var content = File.ReadAllText(_tempConfigPath);
            content.Should().NotBeEmpty("configuration file should not be empty");
        }

        /// <summary>
        /// Verifies that concurrent dictionary modifications are handled safely.
        /// </summary>
        [TestMethod]
        public void Configuration_ConcurrentDictionaryModifications_ShouldBeThreadSafe()
        {
            // Arrange
            var configContent = @"[TestSection]";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var tasks = new Task[10];
            var exceptions = new ConcurrentBag<Exception>();

            // Act - Multiple threads modifying dictionaries
            for (var i = 0; i < 10; i++)
            {
                var index = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        var section = config.GetSection<DictionaryTestConfig>("TestSection");
                        section.FileFormats = new Dictionary<string, string>
                        {
                            { $"Key{index}_1", $"Value{index}_1" },
                            { $"Key{index}_2", $"Value{index}_2" }
                        };
                        config.Save();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
            }

            var completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(30));

            // Assert
            completed.Should().BeTrue("all concurrent dictionary modification tasks should complete");
            exceptions.Should().BeEmpty("no exceptions should occur during concurrent dictionary modifications");

            // File should exist and contain valid data
            File.Exists(_tempConfigPath).Should().BeTrue("configuration file should exist after concurrent operations");
            var content = File.ReadAllText(_tempConfigPath);
            content.Should().NotBeEmpty("configuration file should not be empty");
        }

        #endregion

#region Performance Tests

#if false
        /*
         * This test was commented out because it requires polymorphic section handling,
         * which is not currently supported in the configuration system.
         */

        /// <summary>
        /// Verifies that loading large configurations performs acceptably.
        /// </summary>
        [TestMethod]
        public void Configuration_LoadLargeConfiguration_ShouldPerformAcceptably()
        {
            // Arrange - Create large configuration
            var configBuilder = new StringBuilder();
            configBuilder.AppendLine("[MainSection]");
            configBuilder.AppendLine("LargeArray=LargeArraySection");
            configBuilder.AppendLine("LargeDict=LargeDictSection");
            configBuilder.AppendLine();

            // Large array section
            configBuilder.AppendLine("[LargeArraySection]");
            for (var i = 0; i < 1000; i++)
            {
                configBuilder.AppendLine($"{i:D4}=ArrayItem{i}");
            }
            configBuilder.AppendLine();

            // Large dictionary section
            configBuilder.AppendLine("[LargeDictSection]");
            for (var i = 0; i < 1000; i++)
            {
                configBuilder.AppendLine($"DictKey{i:D4}=DictValue{i}");
            }

            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configBuilder.ToString());

            // Act - Measure load time
            var startTime = DateTime.Now;
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            config.Initialize(_tempConfigPath);

            var arraySection = config.GetSection<ArrayTestConfig>("MainSection");
            var dictSection = config.GetSection<DictionaryTestConfig>("MainSection");
            
            var loadTime = DateTime.Now - startTime;

            // Assert
            loadTime.TotalSeconds.Should().BeLessThan(5, "large configuration should load within acceptable time");
            arraySection.StringArray.Should().HaveCount(1000, "large array should load completely");
            dictSection.FileFormats.Should().HaveCount(1000, "large dictionary should load completely");
        }

#endif

#if false

        /*
         * This test was commented out because it requires polymorphic section handling,
         * which is not currently supported in the configuration system.
         */

        /// <summary>
        /// Verifies that saving large configurations performs acceptably.
        /// </summary>
        [TestMethod]
        public void Configuration_SaveLargeConfiguration_ShouldPerformAcceptably()
        {
            // Arrange
            var configContent = @"[MainSection]";
            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var arraySection = config.GetSection<ArrayTestConfig>("MainSection");
            var dictSection = config.GetSection<DictionaryTestConfig>("MainSection");

            // Create large data structures
            var largeArray = new string[1000];
            for (var i = 0; i < 1000; i++)
            {
                largeArray[i] = $"LargeArrayItem{i}";
            }

            var largeDict = new Dictionary<string, string>();
            for (var i = 0; i < 1000; i++)
            {
                largeDict[$"LargeKey{i:D4}"] = $"LargeValue{i}";
            }

            arraySection.StringArray = largeArray;
            dictSection.FileFormats = largeDict;

            // Act - Measure save time
            var startTime = DateTime.Now;
            config.Save();
            var saveTime = DateTime.Now - startTime;

            // Assert
            saveTime.TotalSeconds.Should().BeLessThan(10, "large configuration should save within acceptable time");
            
            // Verify data integrity
            var savedSize = new FileInfo(_tempConfigPath).Length;
            savedSize.Should().BeGreaterThan(50000, "saved file should contain substantial data");
        }

        /// <summary>
        /// Verifies that multiple rapid save operations don't cause corruption.
        /// </summary>
        [TestMethod]
        public void Configuration_RapidSaveOperations_ShouldMaintainIntegrity()
        {
            // Arrange
            var configContent = @"[TestSection]
StringValue=Initial
IntValue=0";

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configContent);
            config.Initialize(_tempConfigPath);

            var section = config.GetSection<BasicTestConfig>("TestSection");

            // Act - Rapid save operations
            for (var i = 0; i < 100; i++)
            {
                section.StringValue = $"Rapid{i}";
                section.IntValue = i;
                config.Save();
            }

            // Assert - File should exist and contain final values
            File.Exists(_tempConfigPath).Should().BeTrue("configuration file should exist after rapid saves");
            
            var finalContent = File.ReadAllText(_tempConfigPath);
            finalContent.Should().Contain("StringValue=Rapid99", "final string value should be saved");
            finalContent.Should().Contain("IntValue=99", "final int value should be saved");
            
            // Verify structure is maintained
            finalContent.Should().Contain("[TestSection]", "section structure should be maintained");
        }

        #endif

#endregion

        #region Stress Tests

#if false

        /*
         * This test was commented out because it requires polymorphic section handling,
         * which is not currently supported in the configuration system.
         */

        /// <summary>
        /// Comprehensive stress test combining multiple concurrent operations.
        /// </summary>
        [TestMethod]
        public void Configuration_ComprehensiveStressTest_ShouldHandleLoad()
        {
            // Arrange
            var configBuilder = new StringBuilder();
            for (var i = 0; i < 20; i++)
            {
                configBuilder.AppendLine($"[StressSection{i}]");
                configBuilder.AppendLine($"StringValue=Stress{i}");
                configBuilder.AppendLine($"IntValue={i}");
                configBuilder.AppendLine($"StringArray=StressArray{i}");
                configBuilder.AppendLine($"FileFormats=StressDict{i}");
                configBuilder.AppendLine();

                configBuilder.AppendLine($"[StressArray{i}]");
                for (var j = 0; j < 10; j++)
                {
                    configBuilder.AppendLine($"{j}=Array{i}Item{j}");
                }
                configBuilder.AppendLine();

                configBuilder.AppendLine($"[StressDict{i}]");
                for (var j = 0; j < 10; j++)
                {
                    configBuilder.AppendLine($"DictKey{j}=Dict{i}Value{j}");
                }
                configBuilder.AppendLine();
            }

            IConfigurationManager config = new ByteForge.Toolkit.Configuration();
            _tempConfigPath = TestConfigurationHelper.CreateTempConfigFile(configBuilder.ToString());
            config.Initialize(_tempConfigPath);

            var tasks = new Task[60]; // 3 tasks per section
            var exceptions = new ConcurrentBag<Exception>();

            // Act - Multiple types of concurrent operations
            for (var i = 0; i < 20; i++)
            {
                var sectionIndex = i;
                
                // Task 1: Read basic properties
                tasks[i * 3] = Task.Run(() =>
                {
                    try
                    {
                        var section = config.GetSection<BasicTestConfig>($"StressSection{sectionIndex}");
                        _ = section.StringValue;
                        _ = section.IntValue;
                    }
                    catch (Exception ex) { exceptions.Add(ex); }
                });

                // Task 2: Read arrays
                tasks[i * 3 + 1] = Task.Run(() =>
                {
                    try
                    {
                        var section = config.GetSection<ArrayTestConfig>($"StressSection{sectionIndex}");
                        _ = section.StringArray?.Length;
                    }
                    catch (Exception ex) { exceptions.Add(ex); }
                });

                // Task 3: Read dictionaries and save
                tasks[i * 3 + 2] = Task.Run(() =>
                {
                    try
                    {
                        var section = config.GetSection<DictionaryTestConfig>($"StressSection{sectionIndex}");
                        _ = section.FileFormats?.Count;
                        config.Save();
                    }
                    catch (Exception ex) { exceptions.Add(ex); }
                });
            }

            var completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(60));

            // Assert
            completed.Should().BeTrue("all stress test tasks should complete within timeout");
            exceptions.Should().BeEmpty("no exceptions should occur during stress test");
            
            File.Exists(_tempConfigPath).Should().BeTrue("configuration file should exist after stress test");
            var content = File.ReadAllText(_tempConfigPath);
            content.Should().NotBeEmpty("configuration file should not be empty after stress test");
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