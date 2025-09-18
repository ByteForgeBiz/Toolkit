using System;
using ByteForge.Toolkit;
using AwesomeAssertions;
using Ignore = Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute;

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class ConsoleUtilTests
    {
        /// <summary>
        /// Tests that the <see cref="ConsoleUtil.HasConsole"/> method returns <see langword="false"/>  when no console is present.
        /// </summary>
        /// <remarks>
        /// This test verifies the behavior of the <see cref="ConsoleUtil.HasConsole"/> method in scenarios  
        /// where a console is not available, ensuring that the method correctly identifies the absence of a console.
        /// </remarks>
        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("Utils")]
        public void ConsoleUtil_HasConsole_ShouldReturnFalse_WhenNoConsoleIsPresent()
        {
            // Act & Assert
            ConsoleUtil.IsConsoleAvailable.Should().BeFalse();
        }

        [TestMethod]
        [TestCategory("Unit")]
        [TestCategory("Utils")]
        [@Ignore("DrawProgressBar cannot be effectively tested in an automated unit test environment.")]
#pragma warning disable MSTEST0015
        public void ConsoleUtil_Should_DrawProgressBar()
        {
            // Arrange
            float[] testPercentages = { 0f, 25f, 50f, 75f, 100f };
            string message = "Loading";
            // Act
            foreach (var pct in testPercentages)
            {
                ConsoleUtil.DrawProgressBar(pct, message);
                Thread.Sleep(500); // Pause to visualize the progress bar update
            }
            // Finalize the progress bar
            Console.WriteLine(); // Move to the next line after the progress bar is complete
        }
#pragma warning restore MSTEST0015
    }
}