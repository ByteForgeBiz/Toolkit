using AwesomeAssertions;
using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Utilities;

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class BooleanParserTests
    {
        /// <summary>
        /// Verifies that the default instance is properly initialized.
        /// </summary>
        /// <remarks>
        /// Ensures singleton pattern works correctly for default instance.
        /// </remarks>
        [TestMethod]
        public void Default_ShouldReturnSameInstance()
        {
            // Act
            var instance1 = BooleanParser.Default;
            var instance2 = BooleanParser.Default;

            // Assert
            instance1.Should().NotBeNull();
            instance2.Should().NotBeNull();
            instance1.Should().BeSameAs(instance2);
        }

        /// <summary>
        /// Verifies that ParseValue correctly parses standard true values.
        /// </summary>
        /// <remarks>
        /// Ensures all built-in true representations are recognized.
        /// </remarks>
        [TestMethod]
        public void ParseValue_StandardTrueValues_ShouldReturnTrue()
        {
            // Arrange
            var parser = new BooleanParser();
            var trueValues = new[] { "true", "t", ".T.", "yes", "y", "1", "on", "enabled" };

            // Act & Assert
            foreach (var value in trueValues)
            {
                parser.ParseValue(value).Should().BeTrue($"'{value}' should parse as true");
                parser.ParseValue(value.ToUpper()).Should().BeTrue($"'{value.ToUpper()}' should parse as true (case insensitive)");
                parser.ParseValue($" {value} ").Should().BeTrue($"' {value} ' should parse as true (with whitespace)");
            }
        }

        /// <summary>
        /// Verifies that ParseValue correctly parses standard false values.
        /// </summary>
        /// <remarks>
        /// Ensures all built-in false representations are recognized.
        /// </remarks>
        [TestMethod]
        public void ParseValue_StandardFalseValues_ShouldReturnFalse()
        {
            // Arrange
            var parser = new BooleanParser();
            var falseValues = new[] { "false", "f", ".F.", "no", "n", "0", "off", "disabled" };

            // Act & Assert
            foreach (var value in falseValues)
            {
                parser.ParseValue(value).Should().BeFalse($"'{value}' should parse as false");
                parser.ParseValue(value.ToUpper()).Should().BeFalse($"'{value.ToUpper()}' should parse as false (case insensitive)");
                parser.ParseValue($" {value} ").Should().BeFalse($"' {value} ' should parse as false (with whitespace)");
            }
        }

        /// <summary>
        /// Verifies that ParseValue correctly handles numeric values.
        /// </summary>
        /// <remarks>
        /// Ensures numeric parsing treats non-zero as true and zero as false.
        /// </remarks>
        [TestMethod]
        public void ParseValue_NumericValues_ShouldParseCorrectly()
        {
            // Arrange
            var parser = new BooleanParser();
            var testCases = new[]
            {
                ("0", false),
                ("1", true),
                ("-1", true),
                ("42", true),
                ("-42", true),
                ("100", true),
                ("999999", true),
                ("-999999", true)
            };

            // Act & Assert
            foreach (var (input, expected) in testCases)
            {
                parser.ParseValue(input).Should().Be(expected, $"'{input}' should parse as {expected}");
            }
        }

        /// <summary>
        /// Verifies that ParseValue handles null input gracefully.
        /// </summary>
        /// <remarks>
        /// Ensures null input returns false rather than throwing exception.
        /// </remarks>
        [TestMethod]
        public void ParseValue_NullInput_ShouldReturnFalse()
        {
            // Arrange
            var parser = new BooleanParser();

            // Act
            var result = parser.ParseValue(null);

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that ParseValue throws FormatException for invalid input.
        /// </summary>
        /// <remarks>
        /// Ensures invalid input is properly rejected with meaningful error.
        /// </remarks>
        [TestMethod]
        public void ParseValue_InvalidInput_ShouldThrowFormatException()
        {
            // Arrange
            var parser = new BooleanParser();
            var invalidInputs = new[] { "invalid", "maybe", "abc", "2.5", "true1", "false0" };

            // Act & Assert
            foreach (var input in invalidInputs)
            {
                AssertionHelpers.AssertThrows<FormatException>(() => parser.ParseValue(input), input);
            }
        }

        /// <summary>
        /// Verifies that TryParseValue returns true for valid input.
        /// </summary>
        /// <remarks>
        /// Ensures TryParseValue succeeds for all valid boolean representations.
        /// </remarks>
        [TestMethod]
        public void TryParseValue_ValidInput_ShouldReturnTrueWithCorrectResult()
        {
            // Arrange
            var parser = new BooleanParser();
            var testCases = new[]
            {
                ("true", true),
                ("false", false),
                ("yes", true),
                ("no", false),
                ("1", true),
                ("0", false),
                ("42", true),
                ("enabled", true),
                ("disabled", false)
            };

            // Act & Assert
            foreach (var (input, expected) in testCases)
            {
                var success = parser.TryParseValue(input, out var result);
                success.Should().BeTrue($"TryParseValue should succeed for '{input}'");
                result.Should().Be(expected, $"'{input}' should parse as {expected}");
            }
        }

        /// <summary>
        /// Verifies that TryParseValue returns false for invalid input.
        /// </summary>
        /// <remarks>
        /// Ensures TryParseValue fails gracefully for invalid input.
        /// </remarks>
        [TestMethod]
        public void TryParseValue_InvalidInput_ShouldReturnFalseWithDefaultResult()
        {
            // Arrange
            var parser = new BooleanParser();
            var invalidInputs = new[] { "invalid", "maybe", "abc", "2.5" };

            // Act & Assert
            foreach (var input in invalidInputs)
            {
                var success = parser.TryParseValue(input, out var result);
                success.Should().BeFalse($"TryParseValue should fail for '{input}'");
                result.Should().BeFalse($"result should be false for invalid input '{input}'");
            }
        }

        /// <summary>
        /// Verifies that AddTrueValue correctly adds new true values.
        /// </summary>
        /// <remarks>
        /// Ensures custom true values can be registered and used.
        /// </remarks>
        [TestMethod]
        public void AddTrueValue_ValidValue_ShouldAddAndParseCorrectly()
        {
            // Arrange
            var parser = new BooleanParser();
            var customValue = "oui"; // French for "yes"

            // Act
            parser.AddTrueValue(customValue);
            var result = parser.ParseValue(customValue);

            // Assert
            result.Should().BeTrue($"'{customValue}' should parse as true after being added");
            parser.ParseValue("OUI").Should().BeTrue("case insensitive matching should work");
            parser.ParseValue(" oui ").Should().BeTrue("whitespace trimming should work");
        }

        /// <summary>
        /// Verifies that AddFalseValue correctly adds new false values.
        /// </summary>
        /// <remarks>
        /// Ensures custom false values can be registered and used.
        /// </remarks>
        [TestMethod]
        public void AddFalseValue_ValidValue_ShouldAddAndParseCorrectly()
        {
            // Arrange
            var parser = new BooleanParser();
            var customValue = "nein"; // German for "no"

            // Act
            parser.AddFalseValue(customValue);
            var result = parser.ParseValue(customValue);

            // Assert
            result.Should().BeFalse($"'{customValue}' should parse as false after being added");
            parser.ParseValue("NEIN").Should().BeFalse("case insensitive matching should work");
            parser.ParseValue(" nein ").Should().BeFalse("whitespace trimming should work");
        }

        /// <summary>
        /// Verifies that AddTrueValue throws ArgumentNullException for null input.
        /// </summary>
        /// <remarks>
        /// Ensures proper null validation for AddTrueValue method.
        /// </remarks>
        [TestMethod]
        public void AddTrueValue_NullValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            var parser = new BooleanParser();

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => parser.AddTrueValue(null));
        }

        /// <summary>
        /// Verifies that AddFalseValue throws ArgumentNullException for null input.
        /// </summary>
        /// <remarks>
        /// Ensures proper null validation for AddFalseValue method.
        /// </remarks>
        [TestMethod]
        public void AddFalseValue_NullValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            var parser = new BooleanParser();

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => parser.AddFalseValue(null));
        }

        /// <summary>
        /// Verifies that AddTrueValue throws ArgumentException when value is already false.
        /// </summary>
        /// <remarks>
        /// Ensures ambiguity is prevented when registering conflicting values.
        /// </remarks>
        [TestMethod]
        public void AddTrueValue_ValueAlreadyFalse_ShouldThrowArgumentException()
        {
            // Arrange
            var parser = new BooleanParser();

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentException>(() => parser.AddTrueValue("false"));
        }

        /// <summary>
        /// Verifies that AddFalseValue throws ArgumentException when value is already true.
        /// </summary>
        /// <remarks>
        /// Ensures ambiguity is prevented when registering conflicting values.
        /// </remarks>
        [TestMethod]
        public void AddFalseValue_ValueAlreadyTrue_ShouldThrowArgumentException()
        {
            // Arrange
            var parser = new BooleanParser();

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentException>(() => parser.AddFalseValue("true"));
        }

        /// <summary>
        /// Verifies that RemoveTrueValue correctly removes true values.
        /// </summary>
        /// <remarks>
        /// Ensures true values can be removed and are no longer recognized.
        /// </remarks>
        [TestMethod]
        public void RemoveTrueValue_ExistingValue_ShouldRemoveAndReturnTrue()
        {
            // Arrange
            var parser = new BooleanParser();

            // Act
            var removed = parser.RemoveTrueValue("yes");

            // Assert
            removed.Should().BeTrue("RemoveTrueValue should return true for existing value");
            AssertionHelpers.AssertThrows<FormatException>(() => parser.ParseValue("yes"));
        }

        /// <summary>
        /// Verifies that RemoveFalseValue correctly removes false values.
        /// </summary>
        /// <remarks>
        /// Ensures false values can be removed and are no longer recognized.
        /// </remarks>
        [TestMethod]
        public void RemoveFalseValue_ExistingValue_ShouldRemoveAndReturnTrue()
        {
            // Arrange
            var parser = new BooleanParser();

            // Act
            var removed = parser.RemoveFalseValue("no");

            // Assert
            removed.Should().BeTrue("RemoveFalseValue should return true for existing value");
            AssertionHelpers.AssertThrows<FormatException>(() => parser.ParseValue("no"));
        }

        /// <summary>
        /// Verifies that RemoveTrueValue returns false for non-existing values.
        /// </summary>
        /// <remarks>
        /// Ensures RemoveTrueValue handles non-existing values gracefully.
        /// </remarks>
        [TestMethod]
        public void RemoveTrueValue_NonExistingValue_ShouldReturnFalse()
        {
            // Arrange
            var parser = new BooleanParser();

            // Act
            var removed = parser.RemoveTrueValue("nonexistent");

            // Assert
            removed.Should().BeFalse("RemoveTrueValue should return false for non-existing value");
        }

        /// <summary>
        /// Verifies that RemoveFalseValue returns false for non-existing values.
        /// </summary>
        /// <remarks>
        /// Ensures RemoveFalseValue handles non-existing values gracefully.
        /// </remarks>
        [TestMethod]
        public void RemoveFalseValue_NonExistingValue_ShouldReturnFalse()
        {
            // Arrange
            var parser = new BooleanParser();

            // Act
            var removed = parser.RemoveFalseValue("nonexistent");

            // Assert
            removed.Should().BeFalse("RemoveFalseValue should return false for non-existing value");
        }

        /// <summary>
        /// Verifies static Parse method works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures static methods delegate to default instance properly.
        /// </remarks>
        [TestMethod]
        public void Parse_StaticMethod_ShouldWorkCorrectly()
        {
            // Arrange & Act
            var trueResult = BooleanParser.Parse("true");
            var falseResult = BooleanParser.Parse("false");

            // Assert
            trueResult.Should().BeTrue();
            falseResult.Should().BeFalse();
        }

        /// <summary>
        /// Verifies static TryParse method works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures static TryParse delegates to default instance properly.
        /// </remarks>
        [TestMethod]
        public void TryParse_StaticMethod_ShouldWorkCorrectly()
        {
            // Act
            var success1 = BooleanParser.TryParse("true", out var result1);
            var success2 = BooleanParser.TryParse("invalid", out var result2);

            // Assert
            success1.Should().BeTrue();
            result1.Should().BeTrue();
            success2.Should().BeFalse();
            result2.Should().BeFalse();
        }

        /// <summary>
        /// Verifies static RegisterTrueValue method works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures static registration methods affect the default instance.
        /// </remarks>
        [TestMethod]
        public void RegisterTrueValue_StaticMethod_ShouldWorkCorrectly()
        {
            // Arrange
            var customValue = "affirmative";

            try
            {
                // Act
                BooleanParser.RegisterTrueValue(customValue);
                var result = BooleanParser.Parse(customValue);

                // Assert
                result.Should().BeTrue();
            }
            finally
            {
                // Cleanup
                BooleanParser.UnegisterTrueValue(customValue);
            }
        }

        /// <summary>
        /// Verifies static RegisterFalseValue method works correctly.
        /// </summary>
        /// <remarks>
        /// Ensures static registration methods affect the default instance.
        /// </remarks>
        [TestMethod]
        public void RegisterFalseValue_StaticMethod_ShouldWorkCorrectly()
        {
            // Arrange
            var customValue = "negative";

            try
            {
                // Act
                BooleanParser.RegisterFalseValue(customValue);
                var result = BooleanParser.Parse(customValue);

                // Assert
                result.Should().BeFalse();
            }
            finally
            {
                // Cleanup
                BooleanParser.UnegisterFalseValue(customValue);
            }
        }

        /// <summary>
        /// Verifies that parsing is case insensitive for all scenarios.
        /// </summary>
        /// <remarks>
        /// Ensures case insensitivity works across all value types.
        /// </remarks>
        [TestMethod]
        public void ParseValue_CaseInsensitive_ShouldWorkForAllValues()
        {
            // Arrange
            var parser = new BooleanParser();
            var testCases = new[]
            {
                ("TRUE", true), ("True", true), ("tRuE", true),
                ("FALSE", false), ("False", false), ("fAlSe", false),
                ("YES", true), ("Yes", true), ("yEs", true),
                ("NO", false), ("No", false), ("nO", false),
                ("ON", true), ("On", true), ("oN", true),
                ("OFF", false), ("Off", false), ("oFf", false)
            };

            // Act & Assert
            foreach (var (input, expected) in testCases)
            {
                parser.ParseValue(input).Should().Be(expected, $"'{input}' should parse as {expected}");
            }
        }

        /// <summary>
        /// Verifies that whitespace is properly trimmed from input.
        /// </summary>
        /// <remarks>
        /// Ensures robust handling of whitespace in various forms.
        /// </remarks>
        [TestMethod]
        public void ParseValue_WithWhitespace_ShouldTrimAndParseCorrectly()
        {
            // Arrange
            var parser = new BooleanParser();
            var testCases = new[]
            {
                (" true ", true),
                ("\ttrue\t", true),
                ("\nfalse\n", false),
                ("\r\nyes\r\n", true),
                ("  no  ", false),
                (" 1 ", true),
                (" 0 ", false)
            };

            // Act & Assert
            foreach (var (input, expected) in testCases)
            {
                parser.ParseValue(input).Should().Be(expected, $"'{input}' should parse as {expected} after trimming");
            }
        }

        /// <summary>
        /// Verifies that Clipper-style boolean values work correctly.
        /// </summary>
        /// <remarks>
        /// Ensures backward compatibility with Clipper database boolean format.
        /// </remarks>
        [TestMethod]
        public void ParseValue_ClipperStyle_ShouldParseCorrectly()
        {
            // Arrange
            var parser = new BooleanParser();

            // Act & Assert
            parser.ParseValue(".T.").Should().BeTrue("Clipper-style true should be recognized");
            parser.ParseValue(".F.").Should().BeFalse("Clipper-style false should be recognized");
            parser.ParseValue(".t.").Should().BeTrue("Clipper-style true should be case insensitive");
            parser.ParseValue(".f.").Should().BeFalse("Clipper-style false should be case insensitive");
        }

        /// <summary>
        /// Performance test for multiple boolean parses.
        /// </summary>
        /// <remarks>
        /// Ensures boolean parsing is efficient for repeated use.
        /// </remarks>
        [TestMethod]
        public void ParseValue_Performance_ShouldHandleMultipleParsesQuickly()
        {
            // Arrange
            var parser = new BooleanParser();
            var inputs = new[] { "true", "false", "yes", "no", "1", "0", "on", "off" };
            var iterations = 10000;
            var startTime = DateTime.UtcNow;

            // Act
            for (var i = 0; i < iterations; i++)
                foreach (var input in inputs)
                    parser.Invoking(p => p.ParseValue(input)).Should().NotThrow();

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(2), 
                $"should parse {iterations * inputs.Length} boolean values quickly");
        }

        /// <summary>
        /// Verifies thread safety for concurrent boolean parsing.
        /// </summary>
        /// <remarks>
        /// Ensures the parser can be used safely in multi-threaded environments.
        /// </remarks>
        [TestMethod]
        public void ParseValue_ThreadSafety_ShouldHandleConcurrentAccess()
        {
            // Arrange
            var parser = new BooleanParser();
            var tasks = new System.Threading.Tasks.Task[10];

            // Act
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    for (var j = 0; j < 100; j++)
                    {
                        var trueResult = parser.ParseValue("true");
                        var falseResult = parser.ParseValue("false");
                        trueResult.Should().BeTrue("concurrent parsing should produce consistent results");
                        falseResult.Should().BeFalse("concurrent parsing should produce consistent results");
                    }
                });
            }

            // Assert
            System.Threading.Tasks.Task.WaitAll(tasks, TimeSpan.FromSeconds(5))
                .Should().BeTrue("all concurrent parsing tasks should complete");
        }
    }
}