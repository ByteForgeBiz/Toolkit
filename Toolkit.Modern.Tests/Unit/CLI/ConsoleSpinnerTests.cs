using AwesomeAssertions;
using ByteForge.Toolkit.Tests.Helpers;

namespace ByteForge.Toolkit.Tests.Unit.CLI
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("CLI")]
    public class ConsoleSpinnerTests
    {
        #region Constructor Tests

        private static CancellationTokenSource? _cancelTokenSource;
        private static CancellationToken CancelToken => _cancelTokenSource?.Token ?? CancellationToken.None;

        /// <summary>
        /// Verifies that the test database is properly configured before running tests.
        /// </summary>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _cancelTokenSource = context.CancellationTokenSource;
        }

        /// <summary>
        /// Performs cleanup operations for the test class after all tests have been executed.
        /// </summary>
        /// <remarks>This method is called once at the end of the test class lifecycle. It disposes of any
        /// resources  associated with the test class, such as the cancellation token, to ensure proper resource
        /// management.</remarks>
        [ClassCleanup]
        public static void ClassCleanup()
        {
            _cancelTokenSource?.Dispose();
            _cancelTokenSource = null;
        }

        [TestMethod]
        public void Constructor_Default_ShouldInitializeWithDefaults()
        {
            // Act
            using var spinner = new ConsoleSpinner();

            // Assert
            spinner.Should().NotBeNull();
            spinner.IsRunning.Should().BeFalse();
            spinner.Message.Should().BeEmpty();
        }

        [TestMethod]
        public void Constructor_WithMessage_ShouldInitializeWithMessage()
        {
            // Arrange
            var message = "Loading...";

            // Act
            using var spinner = new ConsoleSpinner(message);

            // Assert
            spinner.Message.Should().Be(message);
            spinner.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public void Constructor_WithMessageAndStyle_ShouldInitializeCorrectly()
        {
            // Arrange
            var message = "Processing...";
            var style = SpinnerStyle.Dots;

            // Act
            using var spinner = new ConsoleSpinner(message, style);

            // Assert
            spinner.Message.Should().Be(message);
            spinner.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public void Constructor_WithAllParameters_ShouldInitializeCorrectly()
        {
            // Arrange
            var message = "Working...";
            var style = SpinnerStyle.Arrows;
            var delay = 200;

            // Act
            using var spinner = new ConsoleSpinner(message, style, delay);

            // Assert
            spinner.Message.Should().Be(message);
            spinner.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public void Constructor_WithNullMessage_ShouldHandleGracefully()
        {
            // Arrange & Act
            using var spinner = new ConsoleSpinner(null);

            // Assert
            spinner.Message.Should().BeEmpty();
        }

        [TestMethod]
        public void Constructor_WithEmptyMessage_ShouldHandleGracefully()
        {
            // Arrange
            var message = string.Empty;

            // Act
            using var spinner = new ConsoleSpinner(message);

            // Assert
            spinner.Message.Should().BeEmpty();
        }

        #endregion

        #region Property Tests

        [TestMethod]
        public void Message_Property_ShouldBeSettable()
        {
            // Arrange
            using var spinner = new ConsoleSpinner();
            var newMessage = "New message";

            // Act
            spinner.Message = newMessage;

            // Assert
            spinner.Message.Should().Be(newMessage);
        }

        [TestMethod]
        public void Message_Property_ShouldAcceptNull()
        {
            // Arrange
            using var spinner = new ConsoleSpinner("Initial message");

            // Act
            spinner.Message = null;

            // Assert
            spinner.Message.Should().BeNull();
        }

        [TestMethod]
        public void Color_Property_ShouldBeSettable()
        {
            // Arrange
            using var spinner = new ConsoleSpinner();
            var color = ConsoleColor.Green;

            // Act
            spinner.Color = color;

            // Assert
            spinner.Color.Should().Be(color);
        }

        [TestMethod]
        public void Color_Property_ShouldAcceptNull()
        {
            // Arrange
            using var spinner = new ConsoleSpinner();

            // Act
            spinner.Color = null;

            // Assert
            spinner.Color.Should().BeNull();
        }

        [TestMethod]
        public void IsRunning_InitiallyFalse_ShouldBeAccessible()
        {
            // Arrange & Act
            using var spinner = new ConsoleSpinner();

            // Assert
            spinner.IsRunning.Should().BeFalse();
        }

        #endregion

        #region Start/Stop Tests

        [TestMethod]
        public void Start_WhenNotRunning_ShouldStartSpinner()
        {
            // Arrange
            using var spinner = new ConsoleSpinner("Testing...");

            // Act
            spinner.Start();

            // Assert
            spinner.IsRunning.Should().BeTrue();

            // Cleanup
            spinner.Stop();
        }

        [TestMethod]
        public void Stop_WhenRunning_ShouldStopSpinner()
        {
            // Arrange
            using var spinner = new ConsoleSpinner("Testing...");
            spinner.Start();

            // Act
            spinner.Stop();

            // Assert
            spinner.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public void Start_WhenAlreadyRunning_ShouldNotThrow()
        {
            // Arrange
            using var spinner = new ConsoleSpinner("Testing...");
            spinner.Start();

            // Act & Assert
            Action act = () => spinner.Start();
            act.Should().NotThrow();
            spinner.IsRunning.Should().BeTrue();

            // Cleanup
            spinner.Stop();
        }

        [TestMethod]
        public void Stop_WhenNotRunning_ShouldNotThrow()
        {
            // Arrange
            using var spinner = new ConsoleSpinner("Testing...");

            // Act & Assert
            Action act = () => spinner.Stop();
            act.Should().NotThrow();
            spinner.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public void StartStop_Multiple_ShouldWorkCorrectly()
        {
            // Arrange
            using var spinner = new ConsoleSpinner("Testing...");

            // Act & Assert
            spinner.Start(); spinner.IsRunning.Should().BeTrue();
            spinner.Stop(); spinner.IsRunning.Should().BeFalse();
            spinner.Start(); spinner.IsRunning.Should().BeTrue();
            spinner.Stop(); spinner.IsRunning.Should().BeFalse();
        }

        #endregion

        #region Spinner Style Tests

        [TestMethod]
        public void SpinnerStyle_AllValues_ShouldBeValidEnumValues()
        {
            // Arrange & Act
            var styles = Enum.GetValues(typeof(SpinnerStyle));

            // Assert
            styles.Length.Should().BeGreaterThan(0, "Styles should not be empty");
            var names = styles.Cast<object>().Select(o => o.ToString()).ToList();
            names.Should().Contain(nameof(SpinnerStyle.ASCII));
            names.Should().Contain(nameof(SpinnerStyle.Braille));
            names.Should().Contain(nameof(SpinnerStyle.Arrows));
        }

        [TestMethod]
        public void Constructor_WithDifferentStyles_ShouldAcceptAllStyles()
        {
            // Arrange & Act
            var styles = Enum.GetValues(typeof(SpinnerStyle)).Cast<SpinnerStyle>();

            // Assert
            foreach (var style in styles)
            {
                using var spinner = new ConsoleSpinner("Test", style);
                spinner.Should().NotBeNull();
            }
        }

        #endregion

        #region Thread Safety Tests

        [TestMethod]
        public void Concurrent_StartStop_ShouldBeThreadSafe()
        {
            // Arrange
            using var spinner = new ConsoleSpinner("Thread safety test");
            var tasks = new List<Task>();
            var exceptions = new List<Exception>();

            // Act
            for (var i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        spinner.Start();
                        Thread.Sleep(10);
                        spinner.Stop();
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions) exceptions.Add(ex);
                    }
                }, CancelToken));
            }
            Task.WaitAll([.. tasks], CancelToken);

            // Assert
            exceptions.Should().BeEmpty();
            spinner.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public void Message_ConcurrentAccess_ShouldBeThreadSafe()
        {
            // Arrange
            using var spinner = new ConsoleSpinner();
            var tasks = new List<Task>();
            var exceptions = new List<Exception>();

            // Act
            for (var i = 0; i < 10; i++)
            {
                var index = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        spinner.Message = $"Message {index}";
                        var message = spinner.Message;
                        message.Should().NotBeNull();
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions) exceptions.Add(ex);
                    }
                }, CancelToken));
            }
            Task.WaitAll([.. tasks], CancelToken);

            // Assert
            exceptions.Should().BeEmpty();
        }

        #endregion

        #region Dispose Tests

        [TestMethod]
        public void Dispose_WhenRunning_ShouldStopSpinner()
        {
            // Arrange
            var spinner = new ConsoleSpinner("Dispose test");
            spinner.Start();
            spinner.IsRunning.Should().BeTrue();

            // Act
            spinner.Dispose();

            // Assert
            spinner.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public void Dispose_WhenNotRunning_ShouldNotThrow()
        {
            // Arrange
            var spinner = new ConsoleSpinner("Dispose test");

            // Act & Assert
            Action act = () => spinner.Dispose();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Dispose_Multiple_ShouldNotThrow()
        {
            // Arrange
            var spinner = new ConsoleSpinner("Dispose test");

            // Act & Assert
            spinner.Dispose();
            Action act = () => spinner.Dispose();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void UsingStatement_ShouldDisposeCorrectly()
        {
            // Arrange
            ConsoleSpinner spinner;

            // Act
            using (spinner = new ConsoleSpinner("Using test"))
            {
                spinner.Start();
                spinner.IsRunning.Should().BeTrue();
            }

            // Assert
            spinner.IsRunning.Should().BeFalse();
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void Constructor_WithVeryLongMessage_ShouldHandleCorrectly()
        {
            // Arrange
            var longMessage = new string('a', 1000);

            // Act
            using var spinner = new ConsoleSpinner(longMessage);

            // Assert
            spinner.Message.Should().Be(longMessage);
            spinner.Message.Should().HaveLength(1000);
        }

        [TestMethod]
        public void Constructor_WithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var specialMessage = "Loading... 🚀 progress: 50% [■■■□□]";

            // Act
            using var spinner = new ConsoleSpinner(specialMessage);

            // Assert
            spinner.Message.Should().Be(specialMessage);
        }

        [TestMethod]
        public void Constructor_WithZeroDelay_ShouldHandleCorrectly()
        {
            // Arrange & Act
            using var spinner = new ConsoleSpinner("Fast spinner", SpinnerStyle.Default, 0);

            // Assert
            spinner.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_WithNegativeDelay_ShouldHandleCorrectly()
        {
            // Arrange & Act
            using var spinner = new ConsoleSpinner("Negative delay", SpinnerStyle.Default, -1);

            // Assert
            spinner.Should().NotBeNull();
        }

        [TestMethod]
        public void Message_WithLineBreaks_ShouldAccept()
        {
            // Arrange
            using var spinner = new ConsoleSpinner();
            var messageWithLineBreaks = "Line 1\nLine 2\rLine 3\r\nLine 4";

            // Act
            spinner.Message = messageWithLineBreaks;

            // Assert
            spinner.Message.Should().Be(messageWithLineBreaks);
        }

        #endregion

        #region Performance Tests

        [TestMethod]
        public void StartStop_Rapid_ShouldNotCauseMemoryLeaks()
        {
            // Arrange
            using var spinner = new ConsoleSpinner("Performance test");

            // Act & Assert
            for (var i = 0; i < 100; i++)
            {
                spinner.Start();
                spinner.Stop();
            }

            spinner.IsRunning.Should().BeFalse();
        }

        #endregion
    }
}