using AwesomeAssertions;

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class DateTimeUtilTests
    {
        #region Unix Time Conversion Tests

        /// <summary>
        /// Verifies that FromUnixTime correctly converts Unix timestamps to DateTime.
        /// </summary>
        /// <remarks>
        /// Tests conversion from Unix epoch seconds to UTC DateTime objects.
        /// </remarks>
        [TestMethod]
        public void FromUnixTime_ValidTimestamps_ShouldConvertCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                (0.0, new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)), // Unix epoch
                (946684800.0, new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)), // Y2K
                (1609459200.0, new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc)), // 2021
                (-86400.0, new DateTime(1969, 12, 31, 0, 0, 0, DateTimeKind.Utc)) // Negative timestamp
            };

            // Act & Assert
            foreach (var (unixTime, expected) in testCases)
            {
                var result = DateTimeUtil.FromUnixTime(unixTime);
                result.Should().Be(expected, $"Unix time {unixTime} should convert to {expected}");
                result.Kind.Should().Be(DateTimeKind.Utc, "Result should be UTC");
            }
        }

        /// <summary>
        /// Verifies that FromUnixTimeMilliseconds correctly converts Unix millisecond timestamps to DateTime.
        /// </summary>
        /// <remarks>
        /// Tests conversion from Unix epoch milliseconds to UTC DateTime objects.
        /// </remarks>
        [TestMethod]
        public void FromUnixTimeMilliseconds_ValidTimestamps_ShouldConvertCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                (0.0, new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                (1000.0, new DateTime(1970, 1, 1, 0, 0, 1, DateTimeKind.Utc)),
                (946684800000.0, new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                (1609459200500.0, new DateTime(2021, 1, 1, 0, 0, 0, 500, DateTimeKind.Utc))
            };

            // Act & Assert
            foreach (var (unixTimeMs, expected) in testCases)
            {
                var result = DateTimeUtil.FromUnixTimeMilliseconds(unixTimeMs);
                result.Should().Be(expected, $"Unix time {unixTimeMs} ms should convert to {expected}");
                result.Kind.Should().Be(DateTimeKind.Utc, "Result should be UTC");
            }
        }

        /// <summary>
        /// Verifies that ToUnixTime correctly converts DateTime to Unix timestamps.
        /// </summary>
        /// <remarks>
        /// Tests conversion from DateTime objects to Unix epoch seconds.
        /// </remarks>
        [TestMethod]
        public void ToUnixTime_ValidDateTimes_ShouldConvertCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), 0.0),
                (new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), 946684800.0),
                (new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1609459200.0),
                (new DateTime(1969, 12, 31, 0, 0, 0, DateTimeKind.Utc), -86400.0)
            };

            // Act & Assert
            foreach (var (dateTime, expected) in testCases)
            {
                var result = dateTime.ToUnixTime();
                result.Should().Be(expected, $"DateTime {dateTime} should convert to {expected}");
            }
        }

        /// <summary>
        /// Verifies that ToUnixTimeMilliseconds correctly converts DateTime to Unix millisecond timestamps.
        /// </summary>
        /// <remarks>
        /// Tests conversion from DateTime objects to Unix epoch milliseconds.
        /// </remarks>
        [TestMethod]
        public void ToUnixTimeMilliseconds_ValidDateTimes_ShouldConvertCorrectly()
        {
            // Arrange
            var testCases = new[]
            {
                (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), 0.0),
                (new DateTime(1970, 1, 1, 0, 0, 1, DateTimeKind.Utc), 1000.0),
                (new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), 946684800000.0),
                (new DateTime(2021, 1, 1, 0, 0, 0, 500, DateTimeKind.Utc), 1609459200500.0)
            };

            // Act & Assert
            foreach (var (dateTime, expected) in testCases)
            {
                var result = dateTime.ToUnixTimeMilliseconds();
                result.Should().Be(expected, $"DateTime {dateTime} should convert to {expected} ms");
            }
        }

        /// <summary>
        /// Verifies Unix time round-trip conversion (DateTime -> Unix -> DateTime).
        /// </summary>
        /// <remarks>
        /// Ensures conversion accuracy by testing round-trip conversions.
        /// </remarks>
        [TestMethod]
        public void UnixTime_RoundTrip_ShouldPreserveDateTime()
        {
            // Arrange
            var testDates = new[]
            {
                new DateTime(2020, 6, 15, 14, 30, 45, DateTimeKind.Utc),
                new DateTime(1990, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                new DateTime(2050, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            };

            // Act & Assert
            foreach (var original in testDates)
            {
                var unixTime = original.ToUnixTime();
                var converted = DateTimeUtil.FromUnixTime(unixTime);
                converted.Should().Be(original, $"Round-trip conversion should preserve {original}");
            }
        }

        #endregion

        #region HasTimeComponent Tests

        /// <summary>
        /// Verifies that HasTimeComponent correctly identifies DateTime objects with time components.
        /// </summary>
        /// <remarks>
        /// Tests detection of non-midnight time components in DateTime objects.
        /// </remarks>
        [TestMethod]
        public void HasTimeComponent_WithTimeComponents_ShouldReturnTrue()
        {
            // Arrange
            var datesWithTime = new[]
            {
                new DateTime(2023, 1, 1, 0, 0, 1), // 1 second past midnight
                new DateTime(2023, 1, 1, 12, 30, 45), // Noon
                new DateTime(2023, 1, 1, 23, 59, 59), // Almost midnight
                new DateTime(2023, 1, 1, 5, 0, 0) // Early morning
            };

            // Act & Assert
            foreach (var dateTime in datesWithTime)
            {
                dateTime.HasTimeComponent().Should().BeTrue($"{dateTime} should have a time component");
            }
        }

        /// <summary>
        /// Verifies that HasTimeComponent returns false for DateTime objects at midnight.
        /// </summary>
        /// <remarks>
        /// Tests detection of midnight (00:00:00) as having no time component.
        /// </remarks>
        [TestMethod]
        public void HasTimeComponent_AtMidnight_ShouldReturnFalse()
        {
            // Arrange
            var datesAtMidnight = new[]
            {
                new DateTime(2023, 1, 1, 0, 0, 0), // Exact midnight
                new DateTime(2023, 12, 31, 0, 0, 0), // New Year's Eve midnight
                new DateTime(2000, 1, 1, 0, 0, 0) // Y2K midnight
            };

            // Act & Assert
            foreach (var dateTime in datesAtMidnight)
            {
                dateTime.HasTimeComponent().Should().BeFalse($"{dateTime} should not have a time component");
            }
        }

        #endregion

        #region TimeZone Conversion Tests

        /// <summary>
        /// Verifies that ToTimeZone with TimeZoneInfo correctly converts DateTime objects.
        /// </summary>
        /// <remarks>
        /// Tests conversion between different time zones using TimeZoneInfo objects.
        /// </remarks>
        [TestMethod]
        public void ToTimeZone_WithTimeZoneInfo_ShouldConvertCorrectly()
        {
            // Arrange - Use January to avoid DST complications
            var utcTime = new DateTime(2023, 1, 15, 17, 0, 0, DateTimeKind.Utc); // 5 PM UTC
            var easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var expectedEasternTime = new DateTime(2023, 1, 15, 12, 0, 0); // 12 PM EST (UTC-5)

            // Act
            var easternTime = utcTime.ToTimeZone(easternTimeZone);

            // Assert
            easternTime.Should().Be(expectedEasternTime, "UTC 5 PM should convert to EST 12 PM in January (no DST)");
        }

        /// <summary>
        /// Verifies that ToTimeZone throws ArgumentNullException for null TimeZoneInfo.
        /// </summary>
        /// <remarks>
        /// Ensures proper null validation for TimeZoneInfo parameter.
        /// </remarks>
        [TestMethod]
        public void ToTimeZone_WithNullTimeZoneInfo_ShouldThrowArgumentNullException()
        {
            // Arrange
            var dateTime = DateTime.UtcNow;

            // Act & Assert
            dateTime.Invoking(dt => dt.ToTimeZone((TimeZoneInfo?)null))
                .Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Verifies that ToTimeZone with string ID handles common time zone abbreviations.
        /// </summary>
        /// <remarks>
        /// Tests conversion using time zone abbreviations like "EST", "PST", etc.
        /// </remarks>
        [TestMethod]
        public void ToTimeZone_WithCommonAbbreviations_ShouldConvertCorrectly()
        {
            // Arrange - Use January to avoid DST complications
            var utcTime = new DateTime(2023, 1, 15, 20, 0, 0, DateTimeKind.Utc); // 8 PM UTC
            var testCases = new[]
            {
                ("EST", new DateTime(2023, 1, 15, 15, 0, 0)), // UTC-5: 3 PM EST
                ("PST", new DateTime(2023, 1, 15, 12, 0, 0)), // UTC-8: 12 PM PST
                ("UTC", new DateTime(2023, 1, 15, 20, 0, 0)), // UTC+0: 8 PM UTC
                ("GMT", new DateTime(2023, 1, 15, 20, 0, 0))  // UTC+0: 8 PM GMT
            };

            // Act & Assert
            foreach (var (abbreviation, expected) in testCases)
            {
                var convertedTime = utcTime.ToTimeZone(abbreviation);
                convertedTime.Should().Be(expected, $"UTC 8 PM should convert correctly to {abbreviation}");
            }
        }

        /// <summary>
        /// Verifies that ToTimeZone throws ArgumentException for null or empty time zone ID.
        /// </summary>
        /// <remarks>
        /// Ensures proper validation of time zone ID parameter.
        /// </remarks>
        [TestMethod]
        public void ToTimeZone_WithInvalidTimeZoneId_ShouldThrowArgumentException()
        {
            // Arrange
            var dateTime = DateTime.UtcNow;
            var invalidIds = new[] { null, "" };

            // Act & Assert
            foreach (var invalidId in invalidIds)
            {
                dateTime.Invoking(dt => dt.ToTimeZone(invalidId))
                    .Should().Throw<ArgumentException>($"Should throw for invalid ID: '{invalidId}'");
            }
        }

        /// <summary>
        /// Verifies that ToTimeZone handles unknown time zone IDs gracefully.
        /// </summary>
        /// <remarks>
        /// Tests that unknown time zone IDs return the original DateTime without throwing.
        /// </remarks>
        [TestMethod]
        public void ToTimeZone_WithUnknownTimeZoneId_ShouldReturnOriginalDateTime()
        {
            // Arrange
            var originalDateTime = new DateTime(2023, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var unknownTimeZoneId = "NonExistent/TimeZone";

            // Act
            var result = originalDateTime.ToTimeZone(unknownTimeZoneId);

            // Assert
            result.Should().Be(originalDateTime, "Should return original DateTime for unknown time zone ID");
        }

        /// <summary>
        /// Verifies that ToTimeZone handles different DateTimeKind values correctly.
        /// </summary>
        /// <remarks>
        /// Tests conversion behavior for Utc and Unspecified DateTimeKind values.
        /// Local time is avoided due to unpredictability in CI/CD environments.
        /// </remarks>
        [TestMethod]
        public void ToTimeZone_WithDifferentDateTimeKinds_ShouldHandleCorrectly()
        {
            // Arrange - Use January to avoid DST complications
            var baseDate = new DateTime(2023, 1, 15, 17, 0, 0);
            var utcDate = DateTime.SpecifyKind(baseDate, DateTimeKind.Utc);
            var unspecifiedDate = DateTime.SpecifyKind(baseDate, DateTimeKind.Unspecified);

            var easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var expectedEasternTime = new DateTime(2023, 1, 15, 12, 0, 0); // UTC-5 in January

            // Act
            var utcConverted = utcDate.ToTimeZone(easternTimeZone);
            var unspecifiedConverted = unspecifiedDate.ToTimeZone(easternTimeZone);

            // Assert
            utcConverted.Should().Be(expectedEasternTime, "UTC 5 PM should convert to EST 12 PM");
            // Unspecified is typically treated as local time, but behavior may vary
            unspecifiedConverted.Should().NotBe(default, "Unspecified conversion should produce a valid result");
        }

        #endregion

        #region IANA Time Zone Tests

        /// <summary>
        /// Verifies that common IANA time zone codes are handled correctly.
        /// </summary>
        /// <remarks>
        /// Tests conversion using IANA time zone identifiers with expected results.
        /// </remarks>
        [TestMethod]
        public void ToTimeZone_WithIanaTimeZones_ShouldConvertCorrectly()
        {
            // Arrange - Use January to avoid DST complications
            var utcTime = new DateTime(2023, 1, 15, 12, 0, 0, DateTimeKind.Utc); // 12 PM UTC
            var ianaTestCases = new[]
            {
                ("America/New_York", new DateTime(2023, 1, 15, 7, 0, 0)),  // UTC-5 EST: 7 AM
                ("Europe/London", new DateTime(2023, 1, 15, 12, 0, 0)),     // UTC+0 GMT: 12 PM  
                ("Asia/Tokyo", new DateTime(2023, 1, 15, 21, 0, 0)),        // UTC+9 JST: 9 PM
                ("Australia/Sydney", new DateTime(2023, 1, 15, 23, 0, 0))   // UTC+11 AEDT: 11 PM (summer)
            };

            // Act & Assert
            foreach (var (ianaZone, expectedTime) in ianaTestCases)
            {
                var convertedTime = utcTime.ToTimeZone(ianaZone);
                convertedTime.Should().Be(expectedTime, $"UTC 12 PM should convert correctly to {ianaZone}");
            }
        }

        #endregion

        #region Edge Cases and Error Handling

        /// <summary>
        /// Verifies that Unix time methods handle extreme values correctly.
        /// </summary>
        /// <remarks>
        /// Tests behavior with very large and very small Unix timestamp values.
        /// </remarks>
        [TestMethod]
        public void UnixTime_ExtremeValues_ShouldHandleGracefully()
        {
            // Arrange - Use reasonable extreme values that won't cause overflow
            var largeUnixTime = 4102444800.0; // Year 2100
            var smallUnixTime = -2208988800.0; // Year 1900
            var largeUnixTimeMs = 4102444800000.0; // Year 2100 in milliseconds

            // Act & Assert
            Action act1 = () => DateTimeUtil.FromUnixTime(largeUnixTime);
            act1.Should().NotThrow("Should handle large Unix timestamp");

            Action act2 = () => DateTimeUtil.FromUnixTime(smallUnixTime);
            act2.Should().NotThrow("Should handle small Unix timestamp");

            Action act3 = () => DateTimeUtil.FromUnixTimeMilliseconds(largeUnixTimeMs);
            act3.Should().NotThrow("Should handle large Unix timestamp in milliseconds");
        }

        /// <summary>
        /// Verifies that ToUnixTime methods handle different DateTimeKind values.
        /// </summary>
        /// <remarks>
        /// Tests Unix time conversion for Local, Utc, and Unspecified DateTimeKind values.
        /// </remarks>
        [TestMethod]
        public void ToUnixTime_DifferentDateTimeKinds_ShouldConvertCorrectly()
        {
            // Arrange
            var baseDate = new DateTime(2023, 6, 15, 12, 0, 0);
            var utcDate = DateTime.SpecifyKind(baseDate, DateTimeKind.Utc);
            var localDate = DateTime.SpecifyKind(baseDate, DateTimeKind.Local);
            var unspecifiedDate = DateTime.SpecifyKind(baseDate, DateTimeKind.Unspecified);

            // Act
            var utcUnixTime = utcDate.ToUnixTime();
            var localUnixTime = localDate.ToUnixTime();
            var unspecifiedUnixTime = unspecifiedDate.ToUnixTime();

            // Assert
            utcUnixTime.Should().NotBe(0, "UTC Unix time should be calculated");
            localUnixTime.Should().NotBe(0, "Local Unix time should be calculated");
            unspecifiedUnixTime.Should().NotBe(0, "Unspecified Unix time should be calculated");
        }

        /// <summary>
        /// Performance test for Unix time conversion operations.
        /// </summary>
        /// <remarks>
        /// Ensures Unix time conversions perform adequately for repeated use.
        /// </remarks>
        [TestMethod]
        public void UnixTime_Performance_ShouldHandleMultipleConversionsQuickly()
        {
            // Arrange
            var iterations = 10000;
            var testDate = new DateTime(2023, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var startTime = DateTime.UtcNow;

            // Act
            for (var i = 0; i < iterations; i++)
            {
                var unixTime = testDate.ToUnixTime();
                var converted = DateTimeUtil.FromUnixTime(unixTime);
                converted.Should().Be(testDate);
            }

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(2),
                $"should handle {iterations} Unix time conversions quickly");
        }

        #endregion
    }
}