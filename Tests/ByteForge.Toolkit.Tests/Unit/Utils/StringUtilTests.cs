using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class StringUtilTests
    {
        /// <summary>
        /// Verifies that EscapeForJavaScript returns an empty string for null input.
        /// </summary>
        /// <remarks>
        /// Ensures null input is handled gracefully, preventing exceptions.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_NullInput_ShouldReturnEmptyString()
        {
            // Arrange
            string input = null;

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript returns an empty string for empty input.
        /// </summary>
        /// <remarks>
        /// Ensures empty input is handled gracefully, preventing exceptions.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_EmptyInput_ShouldReturnEmptyString()
        {
            // Arrange
            var input = string.Empty;

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript returns unchanged text for simple input.
        /// </summary>
        /// <remarks>
        /// Ensures normal text is not unnecessarily escaped.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_SimpleText_ShouldReturnUnchanged()
        {
            // Arrange
            var input = "Hello World";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("Hello World");
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes backslashes correctly.
        /// </summary>
        /// <remarks>
        /// Ensures special characters are escaped for safe JavaScript usage.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_Backslashes_ShouldEscapeCorrectly()
        {
            // Arrange
            var input = "Path\\to\\file";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("Path\\\\to\\\\file");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes single quotes correctly.
        /// </summary>
        /// <remarks>
        /// Ensures single quotes are escaped for safe JavaScript usage.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_SingleQuotes_ShouldEscapeCorrectly()
        {
            // Arrange
            var input = "It's a beautiful day";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("It\\'s a beautiful day");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes double quotes correctly.
        /// </summary>
        /// <remarks>
        /// Ensures double quotes are escaped for safe JavaScript usage.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_DoubleQuotes_ShouldEscapeCorrectly()
        {
            // Arrange
            var input = "He said \"Hello\"";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("He said \\\"Hello\\\"");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes newlines correctly.
        /// </summary>
        /// <remarks>
        /// Ensures newlines are escaped for safe JavaScript usage.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_Newlines_ShouldEscapeCorrectly()
        {
            // Arrange
            var input = "Line 1\nLine 2";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("Line 1\\nLine 2");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes carriage returns correctly.
        /// </summary>
        /// <remarks>
        /// Ensures carriage returns are escaped for safe JavaScript usage.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_CarriageReturns_ShouldEscapeCorrectly()
        {
            // Arrange
            var input = "Line 1\rLine 2";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("Line 1\\rLine 2");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes tabs correctly.
        /// </summary>
        /// <remarks>
        /// Ensures tabs are escaped for safe JavaScript usage.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_Tabs_ShouldEscapeCorrectly()
        {
            // Arrange
            var input = "Column1\tColumn2";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("Column1\\tColumn2");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes backspace characters correctly.
        /// </summary>
        /// <remarks>
        /// Ensures backspace is escaped for safe JavaScript usage.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_Backspace_ShouldEscapeCorrectly()
        {
            // Arrange
            var input = "Text\bBackspace";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("Text\\bBackspace");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes form feed characters correctly.
        /// </summary>
        /// <remarks>
        /// Ensures form feed is escaped for safe JavaScript usage.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_FormFeed_ShouldEscapeCorrectly()
        {
            // Arrange
            var input = "Text\fFormFeed";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("Text\\fFormFeed");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes Windows line endings correctly.
        /// </summary>
        /// <remarks>
        /// Ensures Windows line endings are escaped for cross-platform compatibility.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_WindowsLineEndings_ShouldEscapeCorrectly()
        {
            // Arrange
            var input = "Line 1\r\nLine 2";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("Line 1\\r\\nLine 2");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes mixed special characters correctly.
        /// </summary>
        /// <remarks>
        /// Ensures all special characters are escaped for safe JavaScript usage.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_MixedSpecialCharacters_ShouldEscapeAllCorrectly()
        {
            // Arrange
            var input = "Path: \"C:\\temp\\file.txt\"\nContent: 'Hello'\tWorld\r\nEnd";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("Path: \\\"C:\\\\temp\\\\file.txt\\\"\\nContent: \\'Hello\\'\\tWorld\\r\\nEnd");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript does not escape Unicode characters.
        /// </summary>
        /// <remarks>
        /// Ensures Unicode is preserved for internationalization.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_UnicodeCharacters_ShouldNotEscape()
        {
            // Arrange
            var input = "Unicode: 你好世界 🌍";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("Unicode: 你好世界 🌍", "Unicode characters should not be escaped");
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes only special characters.
        /// </summary>
        /// <remarks>
        /// Ensures correct escaping for input containing only special characters.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_OnlySpecialCharacters_ShouldEscapeAll()
        {
            // Arrange
            var input = "\\\"\'\n\r\t\b\f";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("\\\\\\\"\\'\\n\\r\\t\\b\\f");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes repeated characters correctly.
        /// </summary>
        /// <remarks>
        /// Ensures correct escaping for input with repeated special characters.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_RepeatedCharacters_ShouldEscapeAll()
        {
            // Arrange
            var input = "\\\\\\\"\"\"'''";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("\\\\\\\\\\\\\\\"\\\"\\\"\\'\\'\\\'");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes long strings correctly.
        /// </summary>
        /// <remarks>
        /// Ensures performance and correctness for large input.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_LongString_ShouldEscapeCorrectly()
        {
            // Arrange
            var input = new string('\\', 1000) + new string('"', 1000);

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Contain("\\\\", "backslashes should be escaped");
            result.Should().Contain("\\\"", "quotes should be escaped");
            result.Length.Should().Be(4000, "each character should be escaped, doubling the length");
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes JSON-like strings correctly.
        /// </summary>
        /// <remarks>
        /// Ensures correct escaping for structured data formats.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_JsonLikeString_ShouldEscapeCorrectly()
        {
            // Arrange
            var input = "{\"name\": \"John\", \"message\": \"Hello\\nWorld\"}";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("{\\\"name\\\": \\\"John\\\", \\\"message\\\": \\\"Hello\\\\nWorld\\\"}");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes JavaScript code correctly.
        /// </summary>
        /// <remarks>
        /// Ensures code snippets are safely escaped for embedding.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_JavaScriptCode_ShouldEscapeCorrectly()
        {
            // Arrange
            var input = "function test() { alert('Hello\\nWorld'); }";

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("function test() { alert(\\'Hello\\\\nWorld\\'); }");
            AssertionHelpers.AssertJavaScriptEscaping(input, result);
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript handles control characters gracefully.
        /// </summary>
        /// <remarks>
        /// Ensures control characters do not cause exceptions or crashes.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_ControlCharacters_ShouldHandleGracefully()
        {
            // Arrange
            var input = "\x00\x01\x02\x03\x1F"; // null and control characters

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().NotBeNull();
            // The method may or may not handle these specific control characters
            // but it should not throw an exception
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes backslashes before other characters.
        /// </summary>
        /// <remarks>
        /// Ensures correct escape order for safe JavaScript usage.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_EscapeOrder_ShouldEscapeBackslashesFirst()
        {
            // Arrange - This tests that backslashes are escaped before other characters
            var input = "\\n"; // A literal backslash followed by 'n'

            // Act
            var result = StringUtil.EscapeForJavaScript(input);

            // Assert
            result.Should().Be("\\\\n", "backslash should be escaped first, then n remains as is");
        }

        /// <summary>
        /// Performance test for escaping large strings.
        /// </summary>
        /// <remarks>
        /// Ensures escaping logic is efficient for large input.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_Performance_ShouldHandleLargeStrings()
        {
            // Arrange
            var largeInput = new string('a', 10000) + "\"'\\\n\r\t" + new string('b', 10000);
            var startTime = DateTime.UtcNow;

            // Act
            var result = StringUtil.EscapeForJavaScript(largeInput);

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(1), "should handle large strings quickly");
            result.Should().NotBeNull();
            result.Should().Contain("\\\"");
            result.Should().Contain("\\'");
            result.Should().Contain("\\\\");
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript escapes special whitespace characters.
        /// </summary>
        /// <remarks>
        /// Ensures whitespace is handled correctly for JavaScript embedding.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_WhitespaceOnly_ShouldEscapeSpecialWhitespace()
        {
            // Arrange
            var testCases = new[]
            {
                (" ", " "), // Regular space should not be escaped
                ("\t", "\\t"),
                ("\n", "\\n"),
                ("\r", "\\r"),
                ("\b", "\\b"),
                ("\f", "\\f")
            };

            // Act & Assert
            foreach (var (input, expected) in testCases)
            {
                var result = StringUtil.EscapeForJavaScript(input);
                result.Should().Be(expected, $"'{input}' should be escaped to '{expected}'");
            }
        }

        /// <summary>
        /// Verifies that EscapeForJavaScript handles edge case characters correctly.
        /// </summary>
        /// <remarks>
        /// Ensures robustness for unusual or boundary values.
        /// </remarks>
        [TestMethod]
        public void EscapeForJavaScript_EdgeCaseCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var edgeCases = new[]
            {
                ("", ""),
                ("a", "a"),
                ("\\", "\\\\"),
                ("\"", "\\\""),
                ("'", "\\'"),
                ("\n", "\\n"),
                ("\r\n", "\\r\\n"),
                ("\\\"", "\\\\\\\""),
                ("\\'", "\\\\\\'")
            };

            // Act & Assert
            foreach (var (input, expected) in edgeCases)
            {
                var result = StringUtil.EscapeForJavaScript(input);
                result.Should().Be(expected, $"Edge case '{input}' should produce '{expected}'");
            }
        }
    }
}