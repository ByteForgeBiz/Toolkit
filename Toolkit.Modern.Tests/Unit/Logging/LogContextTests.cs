using AwesomeAssertions;
using ByteForge.Toolkit.Logging;

namespace ByteForge.Toolkit.Tests.Unit.Logging
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Logging")]
    public class LogContextTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            LogContext.Clear();
        }

        /// <summary>
        /// Verifies that page identifier scopes restore the previous value.
        /// </summary>
        [TestMethod]
        public void SetPageIdentifier_WithNestedScopes_ShouldRestorePreviousValues()
        {
            LogContext.PageIdentifier = "outer";

            using (LogContext.SetPageIdentifier("inner"))
            {
                LogContext.PageIdentifier.Should().Be("inner");
            }

            LogContext.PageIdentifier.Should().Be("outer");
        }

        /// <summary>
        /// Verifies that routing scopes merge and restore logging context.
        /// </summary>
        [TestMethod]
        public void BeginRoutingScope_WithNestedScopes_ShouldMergeAndRestoreContext()
        {
            var loggerA = new TestLogger("A");
            var loggerB = new TestLogger("B");

            using (LogContext.BeginRoutingScope(new[] { loggerA }, new[] { "File" }))
            {
                LogContext.RoutingContext.AdditionalLoggers.Should().ContainSingle().Which.Should().BeSameAs(loggerA);
                LogContext.RoutingContext.SuppressedLoggerNames.Should().Contain("File");

                using (LogContext.BeginRoutingScope(new[] { loggerB }, new[] { "Console" }))
                {
                    LogContext.RoutingContext.AdditionalLoggers.Should().Contain(new[] { loggerA, loggerB });
                    LogContext.RoutingContext.SuppressedLoggerNames.Should().Contain(new[] { "File", "Console" });
                }

                LogContext.RoutingContext.AdditionalLoggers.Should().ContainSingle().Which.Should().BeSameAs(loggerA);
                LogContext.RoutingContext.SuppressedLoggerNames.Should().Contain("File");
                LogContext.RoutingContext.SuppressedLoggerNames.Should().NotContain("Console");
            }

            LogContext.RoutingContext.Should().BeNull();
        }

        /// <summary>
        /// Verifies logger suppression is case-insensitive and null-safe.
        /// </summary>
        [TestMethod]
        public void IsSuppressed_ShouldUseCaseInsensitiveLoggerNames()
        {
            var context = new LogRoutingContext(suppressedLoggerNames: new[] { "File" });

            context.IsSuppressed(new TestLogger("file")).Should().BeTrue();
            context.IsSuppressed(new TestLogger("Console")).Should().BeFalse();
            context.IsSuppressed(null).Should().BeFalse();
        }

        private sealed class TestLogger : ILogger
        {
            public TestLogger(string name)
            {
                Name = name;
            }

            public string Name { get; set; }

            public LogLevel MinLogLevel { get; set; }

            public void Log(LogLevel level, string message, Exception ex = null) { }

            public void LogTrace(string message) { }

            public void LogDebug(string message) { }

            public void LogVerbose(string message) { }

            public void LogInfo(string message) { }

            public void LogNotice(string message) { }

            public void LogWarning(string message) { }

            public void LogError(string message, Exception ex = null) { }

            public void LogCritical(string message, Exception ex = null) { }

            public void LogFatal(string message, Exception ex = null) { }
        }
    }
}
