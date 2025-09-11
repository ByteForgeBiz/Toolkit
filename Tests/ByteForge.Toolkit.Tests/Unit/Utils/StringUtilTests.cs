using System;
using ByteForge.Toolkit;
using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class StringUtilTests
    {
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