using AwesomeAssertions;
using ByteForge.Toolkit.Utilities;

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class TemplateProcessorTests
    {
        #region Constructor and Basic Properties Tests

        /// <summary>
        /// Verifies that TemplateProcessor initializes correctly with default values.
        /// </summary>
        /// <remarks>
        /// Tests the initial state of a newly created TemplateProcessor.
        /// </remarks>
        [TestMethod]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Act
            var processor = new TemplateProcessor();

            // Assert
            processor.Count.Should().Be(0, "should start with empty dictionary");
            processor.UseEscapeSequences.Should().BeFalse("should default to false for escape sequences");
        }

        /// <summary>
        /// Verifies that UseEscapeSequences property can be set and retrieved correctly.
        /// </summary>
        /// <remarks>
        /// Tests the UseEscapeSequences property getter and setter.
        /// </remarks>
        [TestMethod]
        public void UseEscapeSequences_ShouldGetAndSetCorrectly()
        {
            // Arrange
            var processor = new TemplateProcessor();

            // Act & Assert
            processor.UseEscapeSequences = true;
            processor.UseEscapeSequences.Should().BeTrue("should be able to set to true");

            processor.UseEscapeSequences = false;
            processor.UseEscapeSequences.Should().BeFalse("should be able to set to false");
        }

        #endregion

        #region Add Method Tests

        /// <summary>
        /// Verifies that Add method correctly adds key-value pairs to the dictionary.
        /// </summary>
        /// <remarks>
        /// Tests basic key-value pair addition functionality.
        /// </remarks>
        [TestMethod]
        public void Add_ValidKeyValue_ShouldAddToDictionary()
        {
            // Arrange
            var processor = new TemplateProcessor();
            var key = "name";
            var value = "John Doe";

            // Act
            processor.Add(key, value);

            // Assert
            processor.Count.Should().Be(1, "should add one key-value pair");
        }

        /// <summary>
        /// Verifies that Add method handles null values correctly.
        /// </summary>
        /// <remarks>
        /// Tests that null values are converted to empty strings.
        /// </remarks>
        [TestMethod]
        public void Add_NullValue_ShouldConvertToEmptyString()
        {
            // Arrange
            var processor = new TemplateProcessor();
            var key = "empty";

            // Act
            processor.Add(key, null);

            // Assert
            processor.Count.Should().Be(1, "should add the key even with null value");
        }

        /// <summary>
        /// Verifies that Add method throws exception for null keys.
        /// </summary>
        /// <remarks>
        /// Tests validation of null key parameter.
        /// </remarks>
        [TestMethod]
        public void Add_NullKey_ShouldThrowArgumentNullException()
        {
            // Arrange
            var processor = new TemplateProcessor();

            // Act & Assert
            processor.Invoking(p => p.Add(null, "value"))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("key");
        }

        /// <summary>
        /// Verifies that Add method throws exception for empty keys.
        /// </summary>
        /// <remarks>
        /// Tests validation of empty key parameter.
        /// </remarks>
        [TestMethod]
        public void Add_EmptyKey_ShouldThrowArgumentNullException()
        {
            // Arrange
            var processor = new TemplateProcessor();

            // Act & Assert
            processor.Invoking(p => p.Add(string.Empty, "value"))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("key");
        }

        /// <summary>
        /// Verifies that Add method throws exception for keys containing angle brackets.
        /// </summary>
        /// <remarks>
        /// Tests validation of keys with invalid characters.
        /// </remarks>
        [TestMethod]
        public void Add_KeyWithAngleBrackets_ShouldThrowArgumentException()
        {
            // Arrange
            var processor = new TemplateProcessor();
            var invalidKeys = new[] { "key<", "key>", "<key", ">key", "key<>", "<key>" };

            // Act & Assert
            foreach (var invalidKey in invalidKeys)
            {
                processor.Invoking(p => p.Add(invalidKey, "value"))
                    .Should().Throw<ArgumentException>()
                    .WithMessage($"Key cannot contain < or >: '{invalidKey}'");
            }
        }

        /// <summary>
        /// Verifies that Add method overwrites existing keys (case-insensitive).
        /// </summary>
        /// <remarks>
        /// Tests that the dictionary uses case-insensitive key comparison.
        /// </remarks>
        [TestMethod]
        public void Add_DuplicateKeyCaseInsensitive_ShouldOverwrite()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("Name", "John");

            // Act
            processor.Add("name", "Jane");  // Different case
            processor.Add("NAME", "Bob");   // Different case

            // Assert
            processor.Count.Should().Be(1, "should overwrite existing key regardless of case");
        }

        #endregion

        #region Remove Method Tests

        /// <summary>
        /// Verifies that Remove method correctly removes existing keys.
        /// </summary>
        /// <remarks>
        /// Tests basic key removal functionality.
        /// </remarks>
        [TestMethod]
        public void Remove_ExistingKey_ShouldRemoveFromDictionary()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("name", "John");
            processor.Add("age", "30");

            // Act
            processor.Remove("name");

            // Assert
            processor.Count.Should().Be(1, "should remove one key-value pair");
        }

        /// <summary>
        /// Verifies that Remove method handles non-existing keys gracefully.
        /// </summary>
        /// <remarks>
        /// Tests that removing non-existing keys doesn't cause errors.
        /// </remarks>
        [TestMethod]
        public void Remove_NonExistingKey_ShouldNotThrow()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("name", "John");

            // Act & Assert
            processor.Invoking(p => p.Remove("nonexistent"))
                .Should().NotThrow("should handle non-existing keys gracefully");
            
            processor.Count.Should().Be(1, "should not affect existing keys");
        }

        /// <summary>
        /// Verifies that Remove method throws exception for null keys.
        /// </summary>
        /// <remarks>
        /// Tests validation of null key parameter in Remove method.
        /// </remarks>
        [TestMethod]
        public void Remove_NullKey_ShouldThrowArgumentNullException()
        {
            // Arrange
            var processor = new TemplateProcessor();

            // Act & Assert
            processor.Invoking(p => p.Remove(null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("key");
        }

        /// <summary>
        /// Verifies that Remove method throws exception for empty keys.
        /// </summary>
        /// <remarks>
        /// Tests validation of empty key parameter in Remove method.
        /// </remarks>
        [TestMethod]
        public void Remove_EmptyKey_ShouldThrowArgumentNullException()
        {
            // Arrange
            var processor = new TemplateProcessor();

            // Act & Assert
            processor.Invoking(p => p.Remove(string.Empty))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("key");
        }

        #endregion

        #region Clear Method Tests

        /// <summary>
        /// Verifies that Clear method removes all key-value pairs.
        /// </summary>
        /// <remarks>
        /// Tests the Clear method functionality.
        /// </remarks>
        [TestMethod]
        public void Clear_ShouldRemoveAllKeyValuePairs()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("name", "John");
            processor.Add("age", "30");
            processor.Add("city", "New York");

            // Act
            processor.Clear();

            // Assert
            processor.Count.Should().Be(0, "should remove all key-value pairs");
        }

        /// <summary>
        /// Verifies that Clear method works when dictionary is already empty.
        /// </summary>
        /// <remarks>
        /// Tests Clear method on empty dictionary.
        /// </remarks>
        [TestMethod]
        public void Clear_EmptyDictionary_ShouldNotThrow()
        {
            // Arrange
            var processor = new TemplateProcessor();

            // Act & Assert
            processor.Invoking(p => p.Clear())
                .Should().NotThrow("should handle empty dictionary gracefully");
            
            processor.Count.Should().Be(0, "should remain empty");
        }

        #endregion

        #region Process Method Tests

        /// <summary>
        /// Verifies that Process method correctly replaces single placeholders.
        /// </summary>
        /// <remarks>
        /// Tests basic placeholder replacement functionality.
        /// </remarks>
        [TestMethod]
        public void Process_SinglePlaceholder_ShouldReplaceCorrectly()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("name", "John Doe");
            var template = "Hello <name>!";

            // Act
            var result = processor.Process(template);

            // Assert
            result.Should().Be("Hello John Doe!", "should replace placeholder with value");
        }

        /// <summary>
        /// Verifies that Process method correctly replaces multiple placeholders.
        /// </summary>
        /// <remarks>
        /// Tests replacement of multiple different placeholders.
        /// </remarks>
        [TestMethod]
        public void Process_MultiplePlaceholders_ShouldReplaceAll()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("firstName", "John");
            processor.Add("lastName", "Doe");
            processor.Add("age", "30");
            var template = "Name: <firstName> <lastName>, Age: <age>";

            // Act
            var result = processor.Process(template);

            // Assert
            result.Should().Be("Name: John Doe, Age: 30", "should replace all placeholders with their values");
        }

        /// <summary>
        /// Verifies that Process method handles repeated placeholders correctly.
        /// </summary>
        /// <remarks>
        /// Tests replacement of the same placeholder appearing multiple times.
        /// </remarks>
        [TestMethod]
        public void Process_RepeatedPlaceholders_ShouldReplaceAll()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("name", "John");
            var template = "<name> likes <name>'s job. <name> is happy.";

            // Act
            var result = processor.Process(template);

            // Assert
            result.Should().Be("John likes John's job. John is happy.", 
                "should replace all instances of the same placeholder");
        }

        /// <summary>
        /// Verifies that Process method leaves unknown placeholders unchanged.
        /// </summary>
        /// <remarks>
        /// Tests behavior when placeholders don't have corresponding values.
        /// </remarks>
        [TestMethod]
        public void Process_UnknownPlaceholder_ShouldLeaveUnchanged()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("name", "John");
            var template = "Hello <name>, your <unknown> is not found.";

            // Act
            var result = processor.Process(template);

            // Assert
            result.Should().Be("Hello John, your <unknown> is not found.", 
                "should replace known placeholders but leave unknown ones unchanged");
        }

        /// <summary>
        /// Verifies that Process method handles case-insensitive key matching.
        /// </summary>
        /// <remarks>
        /// Tests that placeholder matching is case-insensitive.
        /// </remarks>
        [TestMethod]
        public void Process_CaseInsensitivePlaceholders_ShouldReplaceCorrectly()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("Name", "John Doe");
            var template = "Hello <name>! Welcome <NAME>. How are you <Name>?";

            // Act
            var result = processor.Process(template);

            // Assert
            result.Should().Be("Hello John Doe! Welcome John Doe. How are you John Doe?", 
                "should match placeholders case-insensitively");
        }

        /// <summary>
        /// Verifies that Process method handles null input correctly.
        /// </summary>
        /// <remarks>
        /// Tests Process method with null input.
        /// </remarks>
        [TestMethod]
        public void Process_NullInput_ShouldReturnNull()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("name", "John");

            // Act
            var result = processor.Process(null);

            // Assert
            result.Should().BeNull("should return null for null input");
        }

        /// <summary>
        /// Verifies that Process method handles empty input correctly.
        /// </summary>
        /// <remarks>
        /// Tests Process method with empty string input.
        /// </remarks>
        [TestMethod]
        public void Process_EmptyInput_ShouldReturnEmpty()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("name", "John");

            // Act
            var result = processor.Process(string.Empty);

            // Assert
            result.Should().Be(string.Empty, "should return empty string for empty input");
        }

        /// <summary>
        /// Verifies that Process method handles whitespace-only input correctly.
        /// </summary>
        /// <remarks>
        /// Tests Process method with whitespace input.
        /// </remarks>
        [TestMethod]
        public void Process_WhitespaceOnlyInput_ShouldReturnUnchanged()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("name", "John");
            var whitespaceInput = "   \t  \n  ";

            // Act
            var result = processor.Process(whitespaceInput);

            // Assert
            result.Should().Be(whitespaceInput, "should return whitespace input unchanged");
        }

        /// <summary>
        /// Verifies that Process method handles text without placeholders correctly.
        /// </summary>
        /// <remarks>
        /// Tests Process method with text containing no placeholders.
        /// </remarks>
        [TestMethod]
        public void Process_NoPlaceholders_ShouldReturnUnchanged()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("name", "John");
            var textWithoutPlaceholders = "This is plain text without any placeholders.";

            // Act
            var result = processor.Process(textWithoutPlaceholders);

            // Assert
            result.Should().Be(textWithoutPlaceholders, "should return text unchanged when no placeholders present");
        }

        #endregion

        #region Escape Sequences Tests

        /// <summary>
        /// Verifies that Process method handles escape sequences when UseEscapeSequences is true.
        /// </summary>
        /// <remarks>
        /// Tests escape sequence processing functionality.
        /// </remarks>
        [TestMethod]
        public void Process_WithEscapeSequences_ShouldUnescapeResult()
        {
            // Arrange
            var processor = new TemplateProcessor
            {
                UseEscapeSequences = true
            };
            processor.Add("newline", "\\n");
            processor.Add("tab", "\\t");
            var template = "Line 1<newline>Line 2<tab>Tabbed";

            // Act
            var result = processor.Process(template);

            // Assert
            result.Should().Be("Line 1\nLine 2\tTabbed", "should unescape sequences when UseEscapeSequences is true");
        }

        /// <summary>
        /// Verifies that Process method doesn't process escape sequences when UseEscapeSequences is false.
        /// </summary>
        /// <remarks>
        /// Tests that escape sequences are not processed by default.
        /// </remarks>
        [TestMethod]
        public void Process_WithoutEscapeSequences_ShouldNotUnescape()
        {
            // Arrange
            var processor = new TemplateProcessor
            {
                UseEscapeSequences = false // Default, but explicit for clarity
            };
            processor.Add("newline", "\\n");
            processor.Add("tab", "\\t");
            var template = "Line 1<newline>Line 2<tab>Tabbed";

            // Act
            var result = processor.Process(template);

            // Assert
            result.Should().Be("Line 1\\nLine 2\\tTabbed", "should not unescape sequences when UseEscapeSequences is false");
        }

        /// <summary>
        /// Verifies that Process method handles various escape sequences correctly.
        /// </summary>
        /// <remarks>
        /// Tests different types of escape sequences.
        /// </remarks>
        [TestMethod]
        public void Process_VariousEscapeSequences_ShouldUnescapeCorrectly()
        {
            // Arrange
            var processor = new TemplateProcessor
            {
                UseEscapeSequences = true
            };
            processor.Add("escape", "\\\"quoted\\\" and \\\\backslash\\\\ and \\r\\n");
            var template = "Text: <escape>";

            // Act
            var result = processor.Process(template);

            // Assert
            result.Should().Be("Text: \"quoted\" and \\backslash\\ and \r\n", 
                "should correctly unescape various escape sequences");
        }

        #endregion

        #region Edge Cases and Complex Scenarios

        /// <summary>
        /// Verifies that Process method handles nested angle brackets correctly.
        /// </summary>
        /// <remarks>
        /// Tests behavior with complex bracket patterns.
        /// </remarks>
        [TestMethod]
        public void Process_NestedAngleBrackets_ShouldHandleCorrectly()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("inner", "value");
            var template = "<outer<inner>>";

            // Act
            var result = processor.Process(template);

            // Assert
            result.Should().Be("<outervalue>", "should only replace valid placeholder patterns");
        }

        /// <summary>
        /// Verifies that Process method handles empty placeholders.
        /// </summary>
        /// <remarks>
        /// Tests behavior with empty placeholder brackets.
        /// </remarks>
        [TestMethod]
        public void Process_EmptyPlaceholder_ShouldLeaveUnchanged()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("name", "John");
            var template = "Hello <> and <name>";

            // Act
            var result = processor.Process(template);

            // Assert
            result.Should().Be("Hello <> and John", "should leave empty placeholders unchanged");
        }

        /// <summary>
        /// Verifies that Process method handles placeholders with special characters.
        /// </summary>
        /// <remarks>
        /// Tests placeholders containing various special characters.
        /// </remarks>
        [TestMethod]
        public void Process_PlaceholdersWithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("key-with-dash", "dash value");
            processor.Add("key_with_underscore", "underscore value");
            processor.Add("key.with.dots", "dots value");
            processor.Add("key123", "numeric value");
            var template = "<key-with-dash> <key_with_underscore> <key.with.dots> <key123>";

            // Act
            var result = processor.Process(template);

            // Assert
            result.Should().Be("dash value underscore value dots value numeric value", 
                "should handle placeholders with special characters");
        }

        /// <summary>
        /// Verifies that Process method handles very long templates efficiently.
        /// </summary>
        /// <remarks>
        /// Performance test for large template processing.
        /// </remarks>
        [TestMethod]
        public void Process_LargeTemplate_ShouldHandleEfficiently()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("name", "John");
            processor.Add("value", "test");
            
            // Create a large template with many placeholders
            var largeTemplate = string.Empty;
            for (var i = 0; i < 1000; i++)
            {
                largeTemplate += $"<name> has <value> number {i}. ";
            }
            
            var startTime = DateTime.UtcNow;

            // Act
            var result = processor.Process(largeTemplate);

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(2), "should handle large templates efficiently");
            result.Should().Contain("John has test number 0", "should correctly process large templates");
            result.Should().Contain("John has test number 999", "should process all placeholders");
        }

        /// <summary>
        /// Verifies that Process method handles templates with only angle brackets.
        /// </summary>
        /// <remarks>
        /// Tests edge case with malformed or incomplete placeholders.
        /// </remarks>
        [TestMethod]
        public void Process_OnlyAngleBrackets_ShouldLeaveUnchanged()
        {
            // Arrange
            var processor = new TemplateProcessor();
            processor.Add("name", "John");
            var template = "< > << >> <<< >>> < name > <name < >name>";

            // Act
            var result = processor.Process(template);

            // Assert
            result.Should().Be("< > << >> <<< >>> < name > <name < >name>", 
                "should leave malformed placeholders unchanged");
        }

        /// <summary>
        /// Verifies that Process method handles empty dictionary correctly.
        /// </summary>
        /// <remarks>
        /// Tests processing with no key-value pairs defined.
        /// </remarks>
        [TestMethod]
        public void Process_EmptyDictionary_ShouldLeaveAllPlaceholdersUnchanged()
        {
            // Arrange
            var processor = new TemplateProcessor();
            var template = "Hello <name>, your age is <age> and city is <city>.";

            // Act
            var result = processor.Process(template);

            // Assert
            result.Should().Be(template, "should leave all placeholders unchanged when dictionary is empty");
        }

        /// <summary>
        /// Performance test to ensure the TemplateProcessor scales well.
        /// </summary>
        /// <remarks>
        /// Tests performance with many key-value pairs and multiple processing calls.
        /// </remarks>
        [TestMethod]
        public void Process_MultipleOperations_ShouldHandleEfficiently()
        {
            // Arrange
            var processor = new TemplateProcessor();
            
            // Add many key-value pairs
            for (var i = 0; i < 100; i++)
            {
                processor.Add($"key{i}", $"value{i}");
            }
            
            var template = "Processing <key0> and <key50> and <key99>";
            var iterations = 1000;
            var startTime = DateTime.UtcNow;

            // Act
            for (var i = 0; i < iterations; i++)
            {
                var result = processor.Process(template);
                result.Should().Contain("value0");
                result.Should().Contain("value50");
                result.Should().Contain("value99");
            }

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(3), 
                $"should handle {iterations} processing operations efficiently");
        }

        #endregion
    }
}