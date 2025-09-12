using System;
using System.Globalization;
using ByteForge.Toolkit;
using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class DateTimeParserTests
    {
        /// <summary>
        /// Verifies that Parse parses a valid ISO date-time string correctly.
        /// </summary>
        /// <remarks>
        /// Ensures ISO date-time formats are supported for interoperability.
        /// </remarks>
        [TestMethod]
        public void Parse_ValidIsoDateTime_ShouldParseCorrectly()
        {
            // Arrange
            var input = "2024-01-15T10:30:45";
            var expected = new DateTime(2024, 1, 15, 10, 30, 45);

            // Act
            var result = DateTimeParser.Parse(input);

            // Assert
            result.Should().Be(expected);
        }

        /// <summary>
        /// Verifies that Parse parses a valid date-only string correctly.
        /// </summary>
        /// <remarks>
        /// Ensures date-only formats are supported for date-based logic.
        /// </remarks>
        [TestMethod]
        public void Parse_ValidDateOnly_ShouldParseCorrectly()
        {
            // Arrange
            var input = "2024-01-15";
            var expected = new DateTime(2024, 1, 15);

            // Act
            var result = DateTimeParser.Parse(input);

            // Assert
            result.Should().Be(expected);
        }

        /// <summary>
        /// Verifies that Parse parses a valid US date format correctly.
        /// </summary>
        /// <remarks>
        /// Ensures US date formats are supported for localization.
        /// </remarks>
        [TestMethod]
        public void Parse_ValidUsFormat_ShouldParseCorrectly()
        {
            // Arrange
            var input = "01/15/2024";
            var expected = new DateTime(2024, 1, 15);

            // Act
            var result = DateTimeParser.Parse(input);

            // Assert
            result.Should().Be(expected);
        }

        /// <summary>
        /// Verifies that Parse parses a valid European date format correctly.
        /// </summary>
        /// <remarks>
        /// Ensures European date formats are supported for localization.
        /// </remarks>
        [TestMethod]
        public void Parse_ValidEuropeanFormat_ShouldParseCorrectly()
        {
            // Arrange
            var input = "15/01/2024";

            // Act
            var result = DateTimeParser.Parse(input);

            // Assert
            result.Year.Should().Be(2024);
            result.Month.Should().Be(1);
            result.Day.Should().Be(15);
        }

        /// <summary>
        /// Verifies that Parse parses a valid time with seconds correctly.
        /// </summary>
        /// <remarks>
        /// Ensures time parsing supports seconds for precise time values.
        /// </remarks>
        [TestMethod]
        public void Parse_ValidTimeWithSeconds_ShouldParseCorrectly()
        {
            // Arrange
            var input = "14:30:45";

            // Act
            var result = DateTimeParser.Parse(input);

            // Assert
            result.Hour.Should().Be(14);
            result.Minute.Should().Be(30);
            result.Second.Should().Be(45);
        }

        /// <summary>
        /// Verifies that Parse parses a valid time without seconds correctly.
        /// </summary>
        /// <remarks>
        /// Ensures time parsing supports formats without seconds.
        /// </remarks>
        [TestMethod]
        public void Parse_ValidTimeWithoutSeconds_ShouldParseCorrectly()
        {
            // Arrange
            var input = "14:30";

            // Act
            var result = DateTimeParser.Parse(input);

            // Assert
            result.Hour.Should().Be(14);
            result.Minute.Should().Be(30);
            result.Second.Should().Be(0);
        }

        /// <summary>
        /// Verifies that Parse parses a valid date-time with milliseconds correctly.
        /// </summary>
        /// <remarks>
        /// Ensures millisecond precision is supported for high-resolution timestamps.
        /// </remarks>
        [TestMethod]
        public void Parse_ValidDateTimeWithMilliseconds_ShouldParseCorrectly()
        {
            // Arrange
            var input = "2024-01-15T10:30:45.123";

            // Act
            var result = DateTimeParser.Parse(input);

            // Assert
            result.Year.Should().Be(2024);
            result.Month.Should().Be(1);
            result.Day.Should().Be(15);
            result.Hour.Should().Be(10);
            result.Minute.Should().Be(30);
            result.Second.Should().Be(45);
            result.Millisecond.Should().Be(123);
        }

        /// <summary>
        /// Verifies that Parse parses a valid date-time with time zone correctly.
        /// </summary>
        /// <remarks>
        /// Ensures time zone offsets are supported for global applications.
        /// </remarks>
        [TestMethod]
        public void Parse_ValidDateTimeWithTimeZone_ShouldParseCorrectly()
        {
            // Arrange
            var inputs = new[]
            {
                "2024-01-15T10:30:45Z",
                "2024-01-15T10:30:45+00:00",
                "2024-01-15T10:30:45-05:00"
            };

            // Act & Assert
            foreach (var input in inputs)
            {
                var result = DateTimeParser.Parse(input);
                result.Year.Should().Be(2024);
                result.Month.Should().Be(1);
                result.Day.Should().Be(15);
            }
        }

        /// <summary>
        /// Attempts to parse invalid date-time strings, expecting exceptions.
        /// </summary>
        /// <remarks>
        /// Verifies error handling for malformed or out-of-range input.
        /// </remarks>
        [TestMethod]
        public void Parse_InvalidDateTime_ShouldThrowException()
        {
            // Arrange
            var invalidInputs = new[]
            {
                "invalid_date",
                "2024-13-01", // Invalid month
                "2024-01-32", // Invalid day
                "25:00:00", // Invalid hour
                "12:60:00", // Invalid minute
                "12:30:60", // Invalid second
                "abc-def-ghi"
            };

            // Act & Assert
            foreach (var input in invalidInputs)
            {
                // FormatException is expected for invalid formats and message should include the input
                AssertionHelpers.AssertThrows<FormatException>(() => DateTimeParser.Parse(input), input);
            }
        }

        /// <summary>
        /// Attempts to parse a null input, expecting an exception.
        /// </summary>
        /// <remarks>
        /// Ensures null input is handled with appropriate exceptions.
        /// </remarks>
        [TestMethod]
        public void Parse_NullInput_ShouldThrowException()
        {
            // Arrange
            string? input = null;

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => DateTimeParser.Parse(input));
        }

        /// <summary>
        /// Attempts to parse an empty input, expecting an exception.
        /// </summary>
        /// <remarks>
        /// Ensures empty input is handled with appropriate exceptions.
        /// </remarks>
        [TestMethod]
        public void Parse_EmptyInput_ShouldThrowException()
        {
            // Arrange
            var input = string.Empty;

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => DateTimeParser.Parse(input));
        }

        /// <summary>
        /// Attempts to parse whitespace input, expecting an exception.
        /// </summary>
        /// <remarks>
        /// Ensures whitespace input is handled with appropriate exceptions.
        /// </remarks>
        [TestMethod]
        public void Parse_WhitespaceInput_ShouldThrowException()
        {
            // Arrange
            var inputs = new[] { " ", "\t", "\n", "\r\n", "   " };

            // Act & Assert
            foreach (var input in inputs)
            {
                // ArgumentNullException is expected for whitespace input and message should say it could not parse
                AssertionHelpers.AssertThrows<FormatException>(() => DateTimeParser.Parse(input), "Could not parse");
            }
        }

        /// <summary>
        /// Verifies that Parse respects specific culture settings.
        /// </summary>
        /// <remarks>
        /// Ensures culture-specific parsing for internationalization.
        /// </remarks>
        [TestMethod]
        public void Parse_WithSpecificCulture_ShouldRespectCulture()
        {
            // Arrange
            var input = "15.01.2024"; // German format
            var germanCulture = new CultureInfo("de-DE");

            // Act
            var result = DateTimeParser.Parse(input, germanCulture);

            // Assert
            result.Year.Should().Be(2024);
            result.Month.Should().Be(1);
            result.Day.Should().Be(15);
        }

        /// <summary>
        /// Verifies that Parse parses edge case dates correctly.
        /// </summary>
        /// <remarks>
        /// Ensures leap years, end-of-year, and other boundary dates are supported.
        /// </remarks>
        [TestMethod]
        public void Parse_EdgeCaseDates_ShouldParseCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                ("2024-02-29", new DateTime(2024, 2, 29)), // Leap year
                ("2000-02-29", new DateTime(2000, 2, 29)), // Leap year (divisible by 400)
                ("1900-02-28", new DateTime(1900, 2, 28)), // Not a leap year
                ("2024-12-31", new DateTime(2024, 12, 31)), // End of year
                ("2024-01-01", new DateTime(2024, 1, 1)), // Start of year
                ("1753-01-01", new DateTime(1753, 1, 1)), // SQL Server minimum date
                ("9999-12-31", new DateTime(9999, 12, 31)) // .NET maximum date
            };

            // Act & Assert
            foreach (var (input, expected) in testCases)
            {
                var result = DateTimeParser.Parse(input);
                result.Should().Be(expected, $"should parse edge case date: {input}");
            }
        }

        /// <summary>
        /// Verifies leap year validation for date parsing.
        /// </summary>
        /// <remarks>
        /// Ensures invalid leap year dates are rejected.
        /// </remarks>
        [TestMethod]
        public void Parse_LeapYearValidation_ShouldHandleCorrectly()
        {
            // Arrange
            var invalidLeapYearInputs = new[]
            {
                "1900-02-29", // Not a leap year (divisible by 100 but not 400)
                "2001-02-29", // Not a leap year
                "2023-02-29"  // Not a leap year
            };

            // Act & Assert
            foreach (var input in invalidLeapYearInputs)
            {
                // FormatException is expected for invalid leap year dates and message should include the input
                AssertionHelpers.AssertThrows<FormatException>(() => DateTimeParser.Parse(input), input);
            }
        }

        /// <summary>
        /// Verifies that TryParse returns true and correct value for valid input.
        /// </summary>
        /// <remarks>
        /// Ensures TryParse provides a safe alternative to Parse for valid input.
        /// </remarks>
        [TestMethod]
        public void TryParse_ValidDateTime_ShouldReturnTrueAndCorrectValue()
        {
            // Arrange
            var input = "2024-01-15T10:30:45";
            var expected = new DateTime(2024, 1, 15, 10, 30, 45);

            // Act
            var success = DateTimeParser.TryParse(input, out var result);

            // Assert
            success.Should().BeTrue();
            result.Should().Be(expected);
        }

        /// <summary>
        /// Verifies that TryParse returns false and default value for invalid input.
        /// </summary>
        /// <remarks>
        /// Ensures TryParse provides a safe alternative to Parse for invalid input.
        /// </remarks>
        [TestMethod]
        public void TryParse_InvalidDateTime_ShouldReturnFalseAndDefaultValue()
        {
            // Arrange
            var input = "invalid_date";

            // Act
            var success = DateTimeParser.TryParse(input, out var result);

            // Assert
            success.Should().BeFalse();
            result.Should().Be(default);
        }

        /// <summary>
        /// Verifies that Parse supports different date separators.
        /// </summary>
        /// <remarks>
        /// Ensures flexibility in date parsing for various formats.
        /// </remarks>
        [TestMethod]
        public void Parse_DifferentDateSeparators_ShouldParseCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                "2024-01-15",
                "2024/01/15",
                "2024.01.15",
                "01-15-2024",
                "01/15/2024",
                "01.15.2024"
            };

            // Act & Assert
            foreach (var input in testCases)
            {
                var result = DateTimeParser.Parse(input);
                result.Year.Should().Be(2024);
                result.Month.Should().Be(1);
                result.Day.Should().Be(15);
            }
        }

        /// <summary>
        /// Verifies that Parse supports AM/PM time formats.
        /// </summary>
        /// <remarks>
        /// Ensures support for 12-hour time formats.
        /// </remarks>
        [TestMethod]
        public void Parse_AmPmFormat_ShouldParseCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                ("10:30 AM", 10, 30),
                ("10:30 PM", 22, 30),
                ("12:00 AM", 0, 0), // Midnight
                ("12:00 PM", 12, 0), // Noon
                ("1:15 AM", 1, 15),
                ("11:45 PM", 23, 45)
            };

            // Act & Assert
            foreach (var (input, expectedHour, expectedMinute) in testCases)
            {
                var result = DateTimeParser.Parse(input);
                result.Hour.Should().Be(expectedHour, $"hour should be correct for {input}");
                result.Minute.Should().Be(expectedMinute, $"minute should be correct for {input}");
            }
        }

        /// <summary>
        /// Verifies that Parse supports various date-time formats.
        /// </summary>
        /// <remarks>
        /// Ensures flexibility in parsing for user input and data sources.
        /// </remarks>
        [TestMethod]
        public void Parse_DateTimeWithDifferentFormats_ShouldParseCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                "2024-01-15 10:30:45",
                "01/15/2024 10:30:45",
                "15.01.2024 10:30:45",
                "Jan 15, 2024 10:30:45",
                "January 15, 2024 10:30:45"
            };

            // Act & Assert
            foreach (var input in testCases)
            {
                var result = DateTimeParser.Parse(input);
                result.Year.Should().Be(2024);
                result.Month.Should().Be(1);
                result.Day.Should().Be(15);
                result.Hour.Should().Be(10);
                result.Minute.Should().Be(30);
                result.Second.Should().Be(45);
            }
        }

        /// <summary>
        /// Performance test for multiple date parses.
        /// </summary>
        /// <remarks>
        /// Ensures date parsing is efficient for repeated use.
        /// </remarks>
        [TestMethod]
        public void Parse_Performance_ShouldHandleMultipleParsesQuickly()
        {
            // Arrange
            var inputs = new[]
            {
                "2024-01-15T10:30:45",
                "01/15/2024",
                "15.01.2024",
                "10:30:45",
                "2024-12-31T23:59:59.999"
            };
            var iterations = 1000;
            var startTime = DateTime.UtcNow;

            // Act
            for (var i = 0; i < iterations; i++)
            {
                foreach (var input in inputs)
                {
                    var result = DateTimeParser.Parse(input);
                    result.Should().NotBe(default, $"parsing should succeed for {input}");
                }
            }

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(2), 
                $"should parse {iterations * inputs.Length} dates quickly");
        }

        /// <summary>
        /// Verifies thread safety for concurrent date parsing.
        /// </summary>
        /// <remarks>
        /// Ensures the parser can be used safely in multi-threaded environments.
        /// </remarks>
        [TestMethod]
        public void Parse_ThreadSafety_ShouldHandleConcurrentAccess()
        {
            // Arrange
            var input = "2024-01-15T10:30:45";
            var expected = new DateTime(2024, 1, 15, 10, 30, 45);
            var tasks = new System.Threading.Tasks.Task[10];

            // Act
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    for (var j = 0; j < 100; j++)
                    {
                        var result = DateTimeParser.Parse(input);
                        result.Should().Be(expected, "concurrent parsing should produce consistent results");
                    }
                });
            }

            // Assert
            System.Threading.Tasks.Task.WaitAll(tasks, TimeSpan.FromSeconds(5))
                .Should().BeTrue("all concurrent parsing tasks should complete");
        }
    }
}