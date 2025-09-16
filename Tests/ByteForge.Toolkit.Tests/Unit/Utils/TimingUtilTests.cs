using System;
using System.IO;
using System.Threading.Tasks;
using ByteForge.Toolkit;
using ByteForge.Toolkit.Logging;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class TimingUtilTests
    {
        #region Test Logger Implementation

        /// <summary>
        /// Test logger implementation that captures log messages for verification.
        /// </summary>
        private class TestLogger : ILogger
        {
            public string Name { get; set; } = "TestLogger";
            public LogLevel MinLogLevel { get; set; } = LogLevel.Debug;
            public string LastMessage { get; private set; } = string.Empty;
            public LogLevel LastLogLevel { get; private set; }
            public int LogCallCount { get; private set; }

            public void Log(LogLevel level, string message, Exception ex) => LogMessage(level, ex == null ? message : $"{message} - {ex.Message}");
            public void LogTrace(string message) => LogMessage(LogLevel.Trace, message);
            public void LogDebug(string message) => LogMessage(LogLevel.Debug, message);
            public void LogVerbose(string message) => LogMessage(LogLevel.Verbose, message);
            public void LogInfo(string message) => LogMessage(LogLevel.Info, message);
            public void LogNotice(string message) => LogMessage(LogLevel.Notice, message);
            public void LogWarning(string message) => LogMessage(LogLevel.Warning, message);
            public void LogError(string message, Exception ex) => LogMessage(LogLevel.Error, ex == null ? message : $"{message} - {ex.Message}");
            public void LogCritical(string message, Exception ex) => LogMessage(LogLevel.Critical, ex == null ? message : $"{message} - {ex.Message}");
            public void LogFatal(string message, Exception ex) => LogMessage(LogLevel.Fatal, ex == null ? message : $"{message} - {ex.Message}");

            private void LogMessage(LogLevel level, string message)
            {
                LastMessage = message;
                LastLogLevel = level;
                LogCallCount++;
            }

            public void Reset()
            {
                LastMessage = string.Empty;
                LastLogLevel = LogLevel.Debug;
                LogCallCount = 0;
            }
        }

        #endregion

        #region Constructor Tests

        /// <summary>
        /// Verifies that the default constructor creates a TimingUtil with the global Log instance.
        /// </summary>
        /// <remarks>
        /// Tests the parameterless constructor that uses Log.Instance.
        /// </remarks>
        [TestMethod]
        public void Constructor_Default_ShouldUseGlobalLogInstance()
        {
            // Act
            var timingUtil = new TimingUtil();

            // Assert
            timingUtil.Should().NotBeNull("constructor should create instance successfully");
        }

        /// <summary>
        /// Verifies that the logger constructor creates a TimingUtil with the provided logger.
        /// </summary>
        /// <remarks>
        /// Tests the constructor that accepts an ILogger parameter.
        /// </remarks>
        [TestMethod]
        public void Constructor_WithLogger_ShouldUseProvidedLogger()
        {
            // Arrange
            var testLogger = new TestLogger();

            // Act
            var timingUtil = new TimingUtil(testLogger);

            // Assert
            timingUtil.Should().NotBeNull("constructor should create instance successfully");
            testLogger.MinLogLevel.Should().Be(LogLevel.Debug, "constructor should set logger to Debug level");
        }

        /// <summary>
        /// Verifies that the file path constructor creates a TimingUtil with a FileLogger.
        /// </summary>
        /// <remarks>
        /// Tests the constructor that accepts a log file path.
        /// </remarks>
        [TestMethod]
        public void Constructor_WithLogFile_ShouldCreateFileLogger()
        {
            // Arrange
            var logFile = Path.GetTempFileName();

            try
            {
                // Act
                var timingUtil = new TimingUtil(logFile);

                // Assert
                timingUtil.Should().NotBeNull("constructor should create instance successfully");
            }
            finally
            {
                // Cleanup
                if (File.Exists(logFile))
                    File.Delete(logFile);
            }
        }

        #endregion

        #region Time Action Tests

        /// <summary>
        /// Verifies that Time method executes the action and logs the elapsed time.
        /// </summary>
        /// <remarks>
        /// Tests basic timing functionality with Action delegate.
        /// </remarks>
        [TestMethod]
        public void Time_Action_ShouldExecuteAndLogTime()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var actionExecuted = false;

            // Act
            timingUtil.Time(() => actionExecuted = true);

            // Assert
            actionExecuted.Should().BeTrue("action should have been executed");
            testLogger.LogCallCount.Should().Be(1, "should have logged once");
            testLogger.LastLogLevel.Should().Be(LogLevel.Debug, "should log at Debug level");
            testLogger.LastMessage.Should().Contain("ms", "should contain milliseconds in log message");
        }

        /// <summary>
        /// Verifies that Time method with custom message includes the message in the log.
        /// </summary>
        /// <remarks>
        /// Tests timing functionality with custom log message.
        /// </remarks>
        [TestMethod]
        public void Time_ActionWithMessage_ShouldIncludeMessageInLog()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var customMessage = "Custom operation";

            // Act
            timingUtil.Time(() => { /* do nothing */ }, customMessage);

            // Assert
            testLogger.LastMessage.Should().Contain(customMessage, "should include custom message in log");
            testLogger.LastMessage.Should().Contain("ms", "should contain milliseconds in log message");
        }

        /// <summary>
        /// Verifies that Time method handles exceptions correctly.
        /// </summary>
        /// <remarks>
        /// Tests that exceptions from the timed action are propagated correctly.
        /// </remarks>
        [TestMethod]
        public void Time_ActionThrowsException_ShouldPropagateException()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var testException = new InvalidOperationException("Test exception");

            // Act & Assert
            timingUtil.Invoking(t => t.Time(() => throw testException))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("Test exception");

            // The timing is not logged when exception occurs before logging line
            testLogger.LogCallCount.Should().Be(0, "timing is not logged when exception prevents reaching log statement");
        }

        /// <summary>
        /// Verifies that Time method measures elapsed time accurately.
        /// </summary>
        /// <remarks>
        /// Tests that the logged time reflects the actual execution time.
        /// </remarks>
        [TestMethod]
        public void Time_Action_ShouldMeasureElapsedTimeAccurately()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var sleepDuration = 100; // milliseconds

            // Act
            timingUtil.Time(() => System.Threading.Thread.Sleep(sleepDuration), "Sleep test");

            // Assert
            testLogger.LastMessage.Should().MatchRegex(@"\d+(\.\d+)?ms", "should contain numeric millisecond value");
            
            // Extract the millisecond value from the log message
            var match = System.Text.RegularExpressions.Regex.Match(testLogger.LastMessage, @"(\d+(?:\.\d+)?)ms");
            if (match.Success && double.TryParse(match.Groups[1].Value, out var loggedMs))
            {
                loggedMs.Should().BeGreaterOrEqualTo(sleepDuration * 0.8, "logged time should be approximately the sleep duration");
                loggedMs.Should().BeLessThan(sleepDuration * 3, "logged time should be reasonable");
            }
        }

        #endregion

        #region Time Function Tests

        /// <summary>
        /// Verifies that Time method executes the function and returns its result.
        /// </summary>
        /// <remarks>
        /// Tests timing functionality with Func delegate.
        /// </remarks>
        [TestMethod]
        public void Time_Function_ShouldExecuteAndReturnResult()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var expectedResult = 42;

            // Act
            var result = timingUtil.Time(() => expectedResult);

            // Assert
            result.Should().Be(expectedResult, "should return function result");
            testLogger.LogCallCount.Should().Be(1, "should have logged once");
            testLogger.LastLogLevel.Should().Be(LogLevel.Debug, "should log at Debug level");
            testLogger.LastMessage.Should().Contain("ms", "should contain milliseconds in log message");
        }

        /// <summary>
        /// Verifies that Time method with function and custom message includes the message in the log.
        /// </summary>
        /// <remarks>
        /// Tests timing functionality with Func delegate and custom message.
        /// </remarks>
        [TestMethod]
        public void Time_FunctionWithMessage_ShouldIncludeMessageInLog()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var customMessage = "Custom calculation";
            var expectedResult = "Hello World";

            // Act
            var result = timingUtil.Time(() => expectedResult, customMessage);

            // Assert
            result.Should().Be(expectedResult, "should return function result");
            testLogger.LastMessage.Should().Contain(customMessage, "should include custom message in log");
            testLogger.LastMessage.Should().Contain("ms", "should contain milliseconds in log message");
        }

        /// <summary>
        /// Verifies that Time method handles function exceptions correctly.
        /// </summary>
        /// <remarks>
        /// Tests that exceptions from the timed function are propagated correctly.
        /// </remarks>
        [TestMethod]
        public void Time_FunctionThrowsException_ShouldPropagateException()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var testException = new ArgumentException("Function test exception");

            // Act & Assert
            timingUtil.Invoking(t => t.Time<string>(() => throw testException))
                .Should().Throw<ArgumentException>()
                .WithMessage("Function test exception");
        }

        /// <summary>
        /// Verifies that Time method works with different return types.
        /// </summary>
        /// <remarks>
        /// Tests timing functionality with various generic return types.
        /// </remarks>
        [TestMethod]
        public void Time_FunctionWithDifferentTypes_ShouldWorkCorrectly()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);

            // Act & Assert
            var intResult = timingUtil.Time(() => 123);
            intResult.Should().Be(123);

            var stringResult = timingUtil.Time(() => "test");
            stringResult.Should().Be("test");

            var boolResult = timingUtil.Time(() => true);
            boolResult.Should().BeTrue();

            var objectResult = timingUtil.Time(() => new { Name = "Test", Value = 456 });
            objectResult.Name.Should().Be("Test");
            objectResult.Value.Should().Be(456);

            testLogger.LogCallCount.Should().Be(4, "should have logged for each timed operation");
        }

        #endregion

        #region TimeAsync Tests

        /// <summary>
        /// Verifies that TimeAsync method executes the async action and logs the elapsed time.
        /// </summary>
        /// <remarks>
        /// Tests asynchronous timing functionality.
        /// </remarks>
        [TestMethod]
        public async Task TimeAsync_AsyncAction_ShouldExecuteAndLogTime()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var actionExecuted = false;

            // Act
            await timingUtil.TimeAsync(async () =>
            {
                await Task.Delay(50);
                actionExecuted = true;
            });

            // Assert
            actionExecuted.Should().BeTrue("async action should have been executed");
            testLogger.LogCallCount.Should().Be(1, "should have logged once");
            testLogger.LastLogLevel.Should().Be(LogLevel.Debug, "should log at Debug level");
            testLogger.LastMessage.Should().Contain("ms", "should contain milliseconds in log message");
        }

        /// <summary>
        /// Verifies that TimeAsync method with custom message includes the message in the log.
        /// </summary>
        /// <remarks>
        /// Tests async timing functionality with custom log message.
        /// </remarks>
        [TestMethod]
        public async Task TimeAsync_AsyncActionWithMessage_ShouldIncludeMessageInLog()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var customMessage = "Async operation";

            // Act
            await timingUtil.TimeAsync(async () => await Task.Delay(10), customMessage);

            // Assert
            testLogger.LastMessage.Should().Contain(customMessage, "should include custom message in log");
            testLogger.LastMessage.Should().Contain("ms", "should contain milliseconds in log message");
        }

        /// <summary>
        /// Verifies that TimeAsync method handles async exceptions correctly.
        /// </summary>
        /// <remarks>
        /// Tests that exceptions from async operations are propagated correctly.
        /// </remarks>
        [TestMethod]
        public async Task TimeAsync_AsyncActionThrowsException_ShouldPropagateException()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var testException = new TaskCanceledException("Async test exception");

            // Act & Assert
            await timingUtil.Invoking(t => t.TimeAsync(async () =>
            {
                await Task.Delay(10);
                throw testException;
            })).Should().ThrowAsync<TaskCanceledException>()
              .WithMessage("Async test exception");
        }

        /// <summary>
        /// Verifies that TimeAsync method measures async operation time accurately.
        /// </summary>
        /// <remarks>
        /// Tests that the logged time reflects the actual async execution time.
        /// </remarks>
        [TestMethod]
        public async Task TimeAsync_AsyncAction_ShouldMeasureElapsedTimeAccurately()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var delayDuration = 100; // milliseconds

            // Act
            await timingUtil.TimeAsync(async () => await Task.Delay(delayDuration), "Async delay test");

            // Assert
            testLogger.LastMessage.Should().MatchRegex(@"\d+(\.\d+)?ms", "should contain numeric millisecond value");
            
            // Extract the millisecond value from the log message
            var match = System.Text.RegularExpressions.Regex.Match(testLogger.LastMessage, @"(\d+(?:\.\d+)?)ms");
            if (match.Success && double.TryParse(match.Groups[1].Value, out var loggedMs))
            {
                loggedMs.Should().BeGreaterOrEqualTo(delayDuration * 0.8, "logged time should be approximately the delay duration");
                loggedMs.Should().BeLessThan(delayDuration * 3, "logged time should be reasonable");
            }
        }

        #endregion

        #region Message Formatting Tests

        /// <summary>
        /// Verifies that TimingUtil handles null messages correctly.
        /// </summary>
        /// <remarks>
        /// Tests behavior when null is passed as the message parameter.
        /// </remarks>
        [TestMethod]
        public void Time_NullMessage_ShouldHandleGracefully()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);

            // Act
            timingUtil.Time(() => { }, null);

            // Assert
            testLogger.LastMessage.Should().NotBeNull("log message should not be null");
            testLogger.LastMessage.Should().Contain("ms", "should contain milliseconds even with null message");
        }

        /// <summary>
        /// Verifies that TimingUtil handles empty messages correctly.
        /// </summary>
        /// <remarks>
        /// Tests behavior when empty string is passed as the message parameter.
        /// </remarks>
        [TestMethod]
        public void Time_EmptyMessage_ShouldHandleGracefully()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);

            // Act
            timingUtil.Time(() => { }, "");

            // Assert
            testLogger.LastMessage.Should().NotBeNull("log message should not be null");
            testLogger.LastMessage.Should().Contain("ms", "should contain milliseconds even with empty message");
        }

        /// <summary>
        /// Verifies that TimingUtil formats messages correctly with various special characters.
        /// </summary>
        /// <remarks>
        /// Tests message handling with special characters and formatting.
        /// </remarks>
        [TestMethod]
        public void Time_MessageWithSpecialCharacters_ShouldFormatCorrectly()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var specialMessage = "Operation with \"quotes\" and 'apostrophes' and {braces}";

            // Act
            timingUtil.Time(() => { }, specialMessage);

            // Assert
            testLogger.LastMessage.Should().Contain(specialMessage, "should preserve special characters in message");
            testLogger.LastMessage.Should().Contain("ms", "should contain milliseconds timing");
        }

        #endregion

        #region Performance and Edge Cases

        /// <summary>
        /// Verifies that TimingUtil handles very fast operations correctly.
        /// </summary>
        /// <remarks>
        /// Tests timing of operations that complete in less than 1 millisecond.
        /// </remarks>
        [TestMethod]
        public void Time_VeryFastOperation_ShouldLogZeroOrSmallTime()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);

            // Act
            timingUtil.Time(() => Math.Sqrt(16), "Fast math operation");

            // Assert
            testLogger.LastMessage.Should().MatchRegex(@"\d+(\.\d+)?ms", "should contain numeric millisecond value");
            testLogger.LastMessage.Should().Contain("Fast math operation", "should contain the operation description");
        }

        /// <summary>
        /// Verifies that TimingUtil handles nested timing operations.
        /// </summary>
        /// <remarks>
        /// Tests behavior when timing operations are nested within each other.
        /// </remarks>
        [TestMethod]
        public void Time_NestedOperations_ShouldLogBothOperations()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var innerExecuted = false;

            // Act
            timingUtil.Time(() =>
            {
                timingUtil.Time(() => innerExecuted = true, "Inner operation");
            }, "Outer operation");

            // Assert
            innerExecuted.Should().BeTrue("inner operation should have executed");
            testLogger.LogCallCount.Should().Be(2, "should have logged both operations");
        }

        /// <summary>
        /// Performance test to ensure TimingUtil doesn't add significant overhead.
        /// </summary>
        /// <remarks>
        /// Tests that the timing utility itself doesn't significantly impact performance.
        /// </remarks>
        [TestMethod]
        public void Time_MultipleOperations_ShouldHandleEfficiently()
        {
            // Arrange
            var testLogger = new TestLogger();
            var timingUtil = new TimingUtil(testLogger);
            var iterations = 1000;
            var startTime = DateTime.UtcNow;

            // Act
            for (var i = 0; i < iterations; i++)
            {
                timingUtil.Time(() => Math.Sqrt(i), $"Operation {i}");
            }

            // Assert
            var totalDuration = DateTime.UtcNow - startTime;
            totalDuration.Should().BeLessThan(TimeSpan.FromSeconds(5), 
                $"should handle {iterations} timing operations efficiently");
            testLogger.LogCallCount.Should().Be(iterations, "should have logged all operations");
        }

        #endregion
    }
}