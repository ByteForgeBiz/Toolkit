using System;
using System.Threading;
using System.Threading.Tasks;
using ByteForge.Toolkit;
using ByteForge.Toolkit.Tests.Helpers;
using AwesomeAssertions;

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class UtilsTests
    {
        /// <summary>
        /// Verifies that arrCRLF contains the correct carriage return and line feed characters.
        /// </summary>
        /// <remarks>
        /// Ensures the static field is properly initialized with CR and LF characters.
        /// </remarks>
        [TestMethod]
        public void ArrCRLF_ShouldContainCorrectCharacters()
        {
            // Assert
            ByteForge.Toolkit.Utils.arrCRLF.Should().NotBeNull();
            ByteForge.Toolkit.Utils.arrCRLF.Length.Should().Be(2);
            ByteForge.Toolkit.Utils.arrCRLF[0].Should().Be('\r');
            ByteForge.Toolkit.Utils.arrCRLF[1].Should().Be('\n');
        }

        /// <summary>
        /// Verifies that FormatUSPhoneNumber correctly formats valid 10-digit phone numbers.
        /// </summary>
        /// <remarks>
        /// Ensures standard 10-digit phone numbers are formatted as (XXX) XXX-XXXX.
        /// </remarks>
        [TestMethod]
        public void FormatUSPhoneNumber_ValidTenDigits_ShouldFormatCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                ("1234567890", "(123) 456-7890"),
                ("5551234567", "(555) 123-4567"),
                ("9876543210", "(987) 654-3210")
            };

            // Act & Assert
            foreach (var (input, expected) in testCases)
            {
                var result = ByteForge.Toolkit.Utils.FormatUSPhoneNumber(input);
                result.Should().Be(expected, $"'{input}' should format to '{expected}'");
            }
        }

        /// <summary>
        /// Verifies that FormatUSPhoneNumber correctly formats 11-digit phone numbers starting with 1.
        /// </summary>
        /// <remarks>
        /// Ensures phone numbers with country code 1 are properly stripped and formatted.
        /// </remarks>
        [TestMethod]
        public void FormatUSPhoneNumber_ElevenDigitsStartingWithOne_ShouldStripCountryCode()
        {
            // Arrange
            var testCases = new[]
            {
                ("11234567890", "(123) 456-7890"),
                ("15551234567", "(555) 123-4567"),
                ("19876543210", "(987) 654-3210")
            };

            // Act & Assert
            foreach (var (input, expected) in testCases)
            {
                var result = ByteForge.Toolkit.Utils.FormatUSPhoneNumber(input);
                result.Should().Be(expected, $"'{input}' should format to '{expected}'");
            }
        }

        /// <summary>
        /// Verifies that FormatUSPhoneNumber removes non-numeric characters before formatting.
        /// </summary>
        /// <remarks>
        /// Ensures phone numbers with various formatting are cleaned and formatted consistently.
        /// </remarks>
        [TestMethod]
        public void FormatUSPhoneNumber_WithNonNumericCharacters_ShouldCleanAndFormat()
        {
            // Arrange
            var testCases = new[]
            {
                ("(123) 456-7890", "(123) 456-7890"),
                ("123-456-7890", "(123) 456-7890"),
                ("123.456.7890", "(123) 456-7890"),
                ("123 456 7890", "(123) 456-7890"),
                ("+1 (555) 123-4567", "(555) 123-4567")
            };

            // Act & Assert
            foreach (var (input, expected) in testCases)
            {
                var result = ByteForge.Toolkit.Utils.FormatUSPhoneNumber(input);
                result.Should().Be(expected, $"'{input}' should format to '{expected}'");
            }
        }

        /// <summary>
        /// Verifies that FormatUSPhoneNumber converts letters to numbers based on phone keypad mapping.
        /// </summary>
        /// <remarks>
        /// Ensures letters are properly converted: A/B/C=2, D/E/F=3, G/H/I=4, etc.
        /// </remarks>
        [TestMethod]
        public void FormatUSPhoneNumber_WithLetters_ShouldConvertToNumbers()
        {
            // Arrange
            var testCases = new[]
            {
                ("1-800-FLOWERS", "(800) 356-9377"), // FLOWERS = 3569377 (7 digits) -> 1-800-3569377 = 18003569377 (11 digits) -> 8003569377 (10 digits)
                ("abc-def-ghij", "(222) 333-4445"), // ABC=222, DEF=333, GHIJ=4445 -> 2223334445 (10 digits)
                ("HELLO-WORLD", "(435) 569-6753"), // HELLO=43556 (5 digits), WORLD=96753 (5 digits) -> 4355696753 (10 digits)
                ("TESTPHONE1", "(837) 874-6631") // TEST=8378, PHONE=74663, 1=1 -> 837874663 + 1 = 8378746631 (10 digits)
            };

            // Act & Assert
            foreach (var (input, expected) in testCases)
            {
                var result = ByteForge.Toolkit.Utils.FormatUSPhoneNumber(input);
                result.Should().Be(expected, $"'{input}' should format to '{expected}'");
            }
        }

        /// <summary>
        /// Verifies that FormatUSPhoneNumber handles mixed case letters correctly.
        /// </summary>
        /// <remarks>
        /// Ensures case-insensitive letter conversion works properly.
        /// </remarks>
        [TestMethod]
        public void FormatUSPhoneNumber_WithMixedCaseLetters_ShouldConvertCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                ("hello-world", "(435) 569-6753"),
                ("HELLO-WORLD", "(435) 569-6753"),
                ("Hello-World", "(435) 569-6753"),
                ("HeLLo-WoRLd", "(435) 569-6753"),
                ("1-800-FlOwErS", "(800) 356-9377")
            };

            // Act & Assert
            foreach (var (input, expected) in testCases)
            {
                var result = ByteForge.Toolkit.Utils.FormatUSPhoneNumber(input);
                result.Should().Be(expected, $"'{input}' should format to '{expected}'");
            }
        }

        /// <summary>
        /// Verifies that FormatUSPhoneNumber handles all phone keypad letter mappings correctly.
        /// </summary>
        /// <remarks>
        /// Tests each letter group on the phone keypad systematically.
        /// </remarks>
        [TestMethod]
        public void FormatUSPhoneNumber_AllLetterMappings_ShouldConvertCorrectly()
        {
            // Arrange - Test each keypad letter group
            var testCases = new[]
            {
                // 2: ABC
                ("555-123-ABCD", "(555) 123-2223"),
                // 3: DEF  
                ("555-123-DEFG", "(555) 123-3334"),
                // 4: GHI
                ("555-123-GHIJ", "(555) 123-4445"),
                // 5: JKL
                ("555-123-JKLM", "(555) 123-5556"),
                // 6: MNO
                ("555-123-MNOP", "(555) 123-6667"),
                // 7: PQRS
                ("555-123-PQRS", "(555) 123-7777"),
                // 8: TUV
                ("555-123-TUVW", "(555) 123-8889"),
                // 9: WXYZ
                ("555-123-WXYZ", "(555) 123-9999")
            };

            // Act & Assert
            foreach (var (input, expected) in testCases)
            {
                var result = ByteForge.Toolkit.Utils.FormatUSPhoneNumber(input);
                result.Should().Be(expected, $"'{input}' should format to '{expected}'");
            }
        }

        /// <summary>
        /// Verifies that FormatUSPhoneNumber handles common vanity phone number patterns.
        /// </summary>
        /// <remarks>
        /// Tests realistic vanity numbers that businesses commonly use.
        /// </remarks>
        [TestMethod]
        public void FormatUSPhoneNumber_VanityNumbers_ShouldFormatCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                ("1-800-FLOWERS", "(800) 356-9377"), // FLOWERS=3569377 -> 18003569377 -> 8003569377
                ("1-800-COLLECT", "(800) 265-5328"), // COLLECT=2655328 -> 18002655328 -> 8002655328  
                ("1-800-TAXICAB", "(800) 829-4222"), // TAXICAB=8294222 -> 18008294222 -> 8008294222
                ("TESTPHONE1", "(837) 874-6631") // TEST=8378, PHONE=74663, 1=1 -> 8378746631 (10 digits)
            };

            // Act & Assert
            foreach (var (input, expected) in testCases)
            {
                var result = ByteForge.Toolkit.Utils.FormatUSPhoneNumber(input);
                result.Should().Be(expected, $"'{input}' should format to '{expected}'");
            }
        }

        /// <summary>
        /// Verifies that FormatUSPhoneNumber returns empty string for null, empty, or whitespace input.
        /// </summary>
        /// <remarks>
        /// Ensures proper handling of invalid input without throwing exceptions.
        /// </remarks>
        [TestMethod]
        public void FormatUSPhoneNumber_NullOrEmpty_ShouldReturnEmptyString()
        {
            // Arrange
            var invalidInputs = new[] { null, "", " ", "\t", "\n", "   " };

            // Act & Assert
            foreach (var input in invalidInputs)
            {
                var result = ByteForge.Toolkit.Utils.FormatUSPhoneNumber(input);
                result.Should().Be(string.Empty, $"'{input}' should return empty string");
            }
        }

        /// <summary>
        /// Verifies that FormatUSPhoneNumber returns original input for invalid phone numbers.
        /// </summary>
        /// <remarks>
        /// Ensures phone numbers that can't be formatted are returned unchanged.
        /// </remarks>
        [TestMethod]
        public void FormatUSPhoneNumber_InvalidLength_ShouldReturnOriginalInput()
        {
            // Arrange
            var invalidInputs = new[]
            {
                "123",           // Too short after conversion
                "12345678",      // Too short after conversion
                "123456789",     // Too short after conversion
                "123456789012",  // Too long after conversion
                "21234567890",   // 11 digits not starting with 1
                "ab",            // Too short after conversion (ab -> 22)
                "a1b2c"          // Too short after conversion (a1b2c -> 21222)
            };

            // Act & Assert
            foreach (var input in invalidInputs)
            {
                var result = ByteForge.Toolkit.Utils.FormatUSPhoneNumber(input);
                result.Should().Be(input, $"'{input}' should be returned unchanged");
            }
        }

        /// <summary>
        /// Verifies that NullIfEmpty returns null for null, empty, or whitespace strings.
        /// </summary>
        /// <remarks>
        /// Ensures proper detection of null, empty, and whitespace-only strings.
        /// </remarks>
        [TestMethod]
        public void NullIfEmpty_NullEmptyOrWhitespace_ShouldReturnNull()
        {
            // Arrange
            var invalidInputs = new[] { null, "", " ", "\t", "\n", "\r", "   ", "\t\n\r " };

            // Act & Assert
            foreach (var input in invalidInputs)
            {
                var result = ByteForge.Toolkit.Utils.NullIfEmpty(input);
                result.Should().BeNull($"'{input}' should return null");
            }
        }

        /// <summary>
        /// Verifies that NullIfEmpty returns the original string for valid input.
        /// </summary>
        /// <remarks>
        /// Ensures non-empty strings are returned unchanged.
        /// </remarks>
        [TestMethod]
        public void NullIfEmpty_ValidString_ShouldReturnOriginalString()
        {
            // Arrange
            var validInputs = new[] { "a", "test", "  text  ", "\tvalue\n", "0", "false" };

            // Act & Assert
            foreach (var input in validInputs)
            {
                var result = ByteForge.Toolkit.Utils.NullIfEmpty(input);
                result.Should().Be(input, $"'{input}' should be returned unchanged");
            }
        }

        /// <summary>
        /// Verifies that RunSync executes an async function synchronously and returns the result.
        /// </summary>
        /// <remarks>
        /// Ensures async functions can be executed synchronously with proper result handling.
        /// </remarks>
        [TestMethod]
        public void RunSync_AsyncFunction_ShouldExecuteSynchronouslyAndReturnResult()
        {
            // Arrange
            var expectedResult = "test result";
            async Task<string> asyncFunc(CancellationToken ct)
            {
                await Task.Delay(10, ct);
                return expectedResult;
            }

            // Act
            var result = ByteForge.Toolkit.Utils.RunSync(asyncFunc);

            // Assert
            result.Should().Be(expectedResult);
        }

        /// <summary>
        /// Verifies that RunSync handles async functions that return different types correctly.
        /// </summary>
        /// <remarks>
        /// Ensures generic type handling works for various return types.
        /// </remarks>
        [TestMethod]
        public void RunSync_DifferentReturnTypes_ShouldHandleCorrectly()
        {
            // Arrange & Act & Assert
            var intResult = ByteForge.Toolkit.Utils.RunSync<int>(async (ct) =>
            {
                await Task.Delay(1, ct);
                return 42;
            });
            intResult.Should().Be(42);

            var boolResult = ByteForge.Toolkit.Utils.RunSync<bool>(async (ct) =>
            {
                await Task.Delay(1, ct);
                return true;
            });
            boolResult.Should().BeTrue();

            var dateResult = ByteForge.Toolkit.Utils.RunSync<DateTime>(async (ct) =>
            {
                await Task.Delay(1, ct);
                return new DateTime(2024, 1, 1);
            });
            dateResult.Should().Be(new DateTime(2024, 1, 1));
        }

        /// <summary>
        /// Verifies that RunSync properly propagates exceptions from async functions.
        /// </summary>
        /// <remarks>
        /// Ensures exceptions in async functions are properly handled and thrown synchronously.
        /// </remarks>
        [TestMethod]
        public void RunSync_AsyncFunctionThrowsException_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");
            async Task<string> asyncFunc(CancellationToken ct)
            {
                await Task.Delay(1, ct);
                throw expectedException;
            }

            // Act & Assert
            AssertionHelpers.AssertThrows<AggregateException>(() => ByteForge.Toolkit.Utils.RunSync(asyncFunc));
        }

        /// <summary>
        /// Verifies that RunSync handles async functions that complete immediately.
        /// </summary>
        /// <remarks>
        /// Ensures synchronous completion of async functions is handled correctly.
        /// </remarks>
        [TestMethod]
        public void RunSync_ImmediateCompletion_ShouldReturnResult()
        {
            // Arrange
            var expectedResult = "immediate result";
            Task<string> asyncFunc(CancellationToken ct) => Task.FromResult(expectedResult);

            // Act
            var result = ByteForge.Toolkit.Utils.RunSync(asyncFunc);

            // Assert
            result.Should().Be(expectedResult);
        }

        /// <summary>
        /// Verifies that RunSync handles null return values correctly.
        /// </summary>
        /// <remarks>
        /// Ensures null values are properly handled as valid return values.
        /// </remarks>
        [TestMethod]
        public void RunSync_NullReturnValue_ShouldReturnNull()
        {
            // Arrange
            static async Task<string?> asyncFunc(CancellationToken ct)
            {
                await Task.Delay(1, ct);
                return null;
            }

            // Act
            var result = ByteForge.Toolkit.Utils.RunSync(asyncFunc);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Performance test for RunSync with multiple executions.
        /// </summary>
        /// <remarks>
        /// Ensures RunSync performs adequately for repeated use.
        /// </remarks>
        [TestMethod]
        public void RunSync_Performance_ShouldHandleMultipleExecutionsQuickly()
        {
            // Arrange
            var iterations = 100;
            var startTime = DateTime.UtcNow;

            // Act
            for (var i = 0; i < iterations; i++)
            {
                var result = ByteForge.Toolkit.Utils.RunSync<int>(async (ct) =>
                {
                    await Task.Delay(1, ct);
                    return i;
                });
                result.Should().Be(i);
            }

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(5),
                $"should execute {iterations} RunSync calls quickly");
        }
    }
}