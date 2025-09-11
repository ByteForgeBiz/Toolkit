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
                AssertionHelpers.AssertThrows<FormatException>(() => DateTimeParser.Parse(input),
                    $"invalid input should throw FormatException: '{input}'");
            }
        }

        [TestMethod]
        public void Parse_NullInput_ShouldThrowException()
        {
            // Arrange
            string? input = null;

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => DateTimeParser.Parse(input));
        }

        [TestMethod]
        public void Parse_EmptyInput_ShouldThrowException()
        {
            // Arrange
            var input = string.Empty;

            // Act & Assert
            AssertionHelpers.AssertThrows<ArgumentNullException>(() => DateTimeParser.Parse(input));
        }

        [TestMethod]
        public void Parse_WhitespaceInput_ShouldThrowException()
        {
            // Arrange
            var inputs = new[] { " ", "\t", "\n", "\r\n", "   " };

            // Act & Assert
            foreach (var input in inputs)
            {
                AssertionHelpers.AssertThrows<FormatException>(() => DateTimeParser.Parse(input),
                    $"whitespace input should throw FormatException: '{input}'");
            }
        }

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
                AssertionHelpers.AssertThrows<FormatException>(() => DateTimeParser.Parse(input),
                    $"invalid leap year date should throw FormatException: {input}");
            }
        }

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

        [TestMethod]
        public void Parse_DifferentTimeSeparators_ShouldParseCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                "10:30:45",
                "10.30.45",
                "10-30-45"
            };

            // Act & Assert
            foreach (var input in testCases)
            {
                var result = DateTimeParser.Parse(input);
                result.Hour.Should().Be(10);
                result.Minute.Should().Be(30);
                result.Second.Should().Be(45);
            }
        }

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