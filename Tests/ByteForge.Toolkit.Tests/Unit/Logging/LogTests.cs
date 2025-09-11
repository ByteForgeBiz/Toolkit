using ByteForge.Toolkit.Logging;
using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;

namespace ByteForge.Toolkit.Tests.Unit.Logging
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Logging")]
    public class LogTests
    {
        private string _originalLogFile;
        private LogLevel _originalLogLevel;

        [TestInitialize]
        public void TestInitialize()
        {
            // Store original settings to restore later
            _originalLogFile = Log.CurrentLogFile;
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

        [TestMethod]
        public void CurrentLogFile_ShouldReturnPath()
        {
            // Act
            var logFile = Log.CurrentLogFile;

            // Assert
            logFile?.Should().NotBeEmpty("current log file should have a valid path");
        }

        [TestMethod]
        public void Console_ShouldReturnConsoleLoggerInstance()
        {
            // Act
            var consoleLogger = Log.Console;

            // Assert
            consoleLogger.Should().NotBeNull("should return console logger instance");
            consoleLogger.Should().BeOfType<ConsoleLogger>();
        }

        [TestMethod]
        public void SessionLogger_ShouldReturnSessionLoggerOrNull()
        {
            // Act
            var sessionLogger = Log.SessionLogger;

            // Assert
            // SessionLogger can be null if not using session logging
            sessionLogger?.Should().BeOfType<SessionFileLogger>();
        }

        [TestMethod]
        public void EndSession_ShouldNotThrow()
        {
            // Act & Assert
            Action endSessionAction = () => Log.EndSession();
            endSessionAction.Should().NotThrow("EndSession should not throw");
        }

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
            duration.Should().BeLessThan(TimeSpan.FromSeconds(10), 
                $"should log {messageCount} messages efficiently");
        }

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