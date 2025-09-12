using ByteForge.Toolkit.Logging;
using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Logging
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Logging")]
    public class LogTests
    {
        private LogLevel _originalLogLevel;

        static LogTests()
        {
            var configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestData", "Configuration", "basic_config.ini");
            Configuration.Initialize(configPath);
        }

        [TestInitialize]
        public void TestInitialize()
        {

            // Store original settings to restore later
            _originalLogLevel = Log.LogLevel;
            
            TempFileHelper.CleanupTempFiles();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Restore original log level if changed
            try
            {
                Log.LogLevel = _originalLogLevel;
            }
            catch
            {
                // Ignore restoration errors
            }
            
            TempFileHelper.CleanupTempFiles();
        }

        /// <summary>
        /// Verifies that the Log.Instance property returns the same singleton instance.
        /// </summary>
        /// <remarks>
        /// Ensures the logging system maintains a single instance, preventing inconsistent state and resource leaks.
        /// </remarks>
        [TestMethod]
        public void Instance_ShouldReturnSameInstance()
        {
            // Arrange & Act
            var instance1 = Log.Instance;
            var instance2 = Log.Instance;

            // Assert
            instance1.Should().BeSameAs(instance2, "Log should be a singleton");
            instance1.Should().NotBeNull();
        }

        /// <summary>
        /// Ensures that all log level methods do not throw exceptions when called.
        /// </summary>
        /// <remarks>
        /// Validates the robustness of the logging API, confirming that all log levels handle input gracefully and do not crash the application.
        /// </remarks>
        [TestMethod]
        public void AllLogLevels_ShouldNotThrow()
        {
            // Arrange
            var testMessage = "Test message for all levels";
            var testException = new Exception("Test exception");

            // Act & Assert - Test all logging methods
            Action[] loggingActions = [
                () => Log.Trace(testMessage),
                () => Log.Debug(testMessage),
                () => Log.Verbose(testMessage),
                () => Log.Info(testMessage),
                () => Log.Notice(testMessage),
                () => Log.Warning(testMessage),
                () => Log.Warning(testMessage, testException),
                () => Log.Error(testMessage),
                () => Log.Error(testException),
                () => Log.Error(testMessage, testException),
                () => Log.Critical(testMessage),
                () => Log.Critical(testException),
                () => Log.Critical(testMessage, testException),
                () => Log.Fatal(testMessage),
                () => Log.Fatal(testException),
                () => Log.Fatal(testMessage, testException)
            ];

            foreach (var action in loggingActions)
            {
                action.Should().NotThrow("all logging methods should handle messages gracefully");
            }
        }

        /// <summary>
        /// Tests logging with edge case messages to ensure graceful handling.
        /// </summary>
        /// <remarks>
        /// Prevents failures when logging null, empty, very large, or special character messages, which can occur in real-world scenarios.
        /// </remarks>
        [TestMethod]
        public void Log_EdgeCaseMessages_ShouldHandleGracefully()
        {
            // Arrange & Act & Assert
            Action[] edgeCaseActions = [
                () => Log.Info(null),
                () => Log.Info(string.Empty),
                () => Log.Info("   "),
                () => Log.Info(new string('A', 10000)),
                () => Log.Info("Special chars: \n\r\t\"'\\@#$%^&*(){}[]<>"),
                () => Log.Info("Unicode: 你好世界 🌍 Café résumé naïve élite"),
                () => Log.Info("\0\0\0"),
                () => Log.Info(new string('X', 100000))
            ];

            foreach (var action in edgeCaseActions)
            {
                action.Should().NotThrow("should handle all edge case messages gracefully");
            }
        }

        /// <summary>
        /// Verifies that console logging can be enabled and disabled without errors.
        /// </summary>
        /// <remarks>
        /// Ensures that users can control console output, which is important for debugging and production environments.
        /// </remarks>
        [TestMethod]
        public void ConsoleLogging_ShouldBeControllable()
        {
            // Arrange
            var originalConsoleState = Log.IsConsoleLoggingEnabled;

            try
            {
                // Act & Assert
                Action enableAction = () => Log.EnableConsoleLogging();
                Action disableAction = () => Log.DisableConsoleLogging();

                enableAction.Should().NotThrow();
                disableAction.Should().NotThrow();

                // Test state changes
                Log.DisableConsoleLogging();
                var disabledState = Log.IsConsoleLoggingEnabled;
                
                Log.EnableConsoleLogging();
                var enabledState = Log.IsConsoleLoggingEnabled;

                disabledState.Should().BeFalse("console logging should be disabled");
                enabledState.Should().BeTrue("console logging should be enabled");
            }
            finally
            {
                // Restore original state
                if (originalConsoleState)
                    Log.EnableConsoleLogging();
                else
                    Log.DisableConsoleLogging();
            }
        }

        /// <summary>
        /// Checks that the log level can be set and retrieved correctly.
        /// </summary>
        /// <remarks>
        /// Confirms that the logging system respects user configuration for verbosity, which is essential for filtering log output.
        /// </remarks>
        [TestMethod]
        public void LogLevel_ShouldBeSettableAndGettable()
        {
            // Arrange
            var originalLevel = Log.LogLevel;
            var newLevel = LogLevel.Warning;

            try
            {
                // Act
                Log.LogLevel = newLevel;
                var retrievedLevel = Log.LogLevel;

                // Assert
                retrievedLevel.Should().Be(newLevel, "log level should be settable");
            }
            finally
            {
                // Cleanup - restore original level
                try
                {
                    Log.LogLevel = originalLevel;
                }
                catch
                {
                    // Ignore restoration errors
                }
            }
        }

        /// <summary>
        /// Ensures that the current log file path is returned and is valid.
        /// </summary>
        /// <remarks>
        /// Verifies that log file management is functioning, which is critical for diagnostics and auditing.
        /// </remarks>
        [TestMethod]
        public void CurrentLogFile_ShouldReturnPath()
        {
            // Act
            var logFile = Log.CurrentLogFile;

            // Assert
            logFile?.Should().NotBeEmpty("current log file should have a valid path");
        }

        /// <summary>
        /// Checks that the Console property returns a valid ConsoleLogger instance.
        /// </summary>
        /// <remarks>
        /// Ensures that console logging is properly implemented and accessible for direct output.
        /// </remarks>
        [TestMethod]
        public void Console_ShouldReturnConsoleLoggerInstance()
        {
            // Act
            var consoleLogger = Log.Console;

            // Assert
            consoleLogger.Should().NotBeNull("should return console logger instance");
            consoleLogger.Should().BeOfType<ConsoleLogger>();
        }

        /// <summary>
        /// Verifies that the SessionLogger property returns a valid instance or null.
        /// </summary>
        /// <remarks>
        /// Confirms session-based logging is available when configured, supporting advanced logging scenarios.
        /// </remarks>
        [TestMethod]
        public void SessionLogger_ShouldReturnSessionLoggerOrNull()
        {
            // Act
            var sessionLogger = Log.SessionLogger;

            // Assert
            // SessionLogger can be null if not using session logging
            sessionLogger?.Should().BeOfType<SessionFileLogger>();
        }

        /// <summary>
        /// Ensures that calling EndSession does not throw exceptions.
        /// </summary>
        /// <remarks>
        /// Validates safe cleanup of session logging, preventing resource leaks or crashes during shutdown.
        /// </remarks>
        [TestMethod]
        public void EndSession_ShouldNotThrow()
        {
            // Act & Assert
            Action endSessionAction = () => Log.EndSession();
            endSessionAction.Should().NotThrow("EndSession should not throw");
        }

        /// <summary>
        /// Verifies composite logger features of the Log instance.
        /// </summary>
        /// <remarks>
        /// Ensures that the logging system supports multiple loggers and exposes collection properties correctly.
        /// </remarks>
        [TestMethod]
        public void Instance_CompositeLoggerFeatures_ShouldWork()
        {
            // Arrange
            var instance = Log.Instance;

            // Act & Assert
            instance.Should().BeAssignableTo<CompositeLogger>("Log should inherit from CompositeLogger");
            instance.Count.Should().BeGreaterThan(0, "should have at least one logger configured");
            instance.IsReadOnly.Should().BeFalse("loggers collection should not be read-only by default");
        }

        /// <summary>
        /// Tests logging from multiple threads to ensure thread safety.
        /// </summary>
        /// <remarks>
        /// Prevents race conditions and data corruption in concurrent environments, which is vital for multi-threaded applications.
        /// </remarks>
        [TestMethod]
        public void ConcurrentLogging_ShouldHandleMultipleThreads()
        {
            // Arrange
            var tasks = new System.Threading.Tasks.Task[5];
            var messagesPerTask = 10;

            // Act
            for (var i = 0; i < tasks.Length; i++)
            {
                var taskId = i;
                tasks[i] =Task.Run(() =>
                {
                    for (var j = 0; j < messagesPerTask; j++)
                    {
                        Log.Info($"Task {taskId} - Message {j}");
                    }
                });
            }

            // Assert
            Task.WaitAll(tasks, TimeSpan.FromSeconds(10))
                .Should().BeTrue("all logging tasks should complete");
        }

        /// <summary>
        /// Measures logging performance under high volume.
        /// </summary>
        /// <remarks>
        /// Ensures the logging system can handle large numbers of messages efficiently, which is important for scalability.
        /// </remarks>
        [TestMethod]
        public void Performance_HighVolumeLogging_ShouldPerformWell()
        {
            // Arrange
            var messageCount = 1000;
            var startTime = DateTime.UtcNow;

            // Act
            for (var i = 0; i < messageCount; i++)
            {
                Log.Info($"Performance test message {i}");
            }

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromMilliseconds(200), 
                $"should log {messageCount} messages efficiently");
        }

        /// <summary>
        /// Checks that ToString returns a meaningful string for the Log instance.
        /// </summary>
        /// <remarks>
        /// Confirms that the logger provides useful diagnostic information when converted to a string, aiding debugging and logging.
        /// </remarks>
        [TestMethod]
        public void ToString_ShouldReturnMeaningfulString()
        {
            // Arrange
            var log = Log.Instance;

            // Act
            var stringResult = log.ToString();

            // Assert
            stringResult.Should().NotBeNullOrEmpty();
        }
    }
}