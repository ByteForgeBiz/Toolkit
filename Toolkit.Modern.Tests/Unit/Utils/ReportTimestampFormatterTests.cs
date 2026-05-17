using AwesomeAssertions;
using ByteForge.Toolkit.Utilities;

namespace ByteForge.Toolkit.Tests.Unit.Utils
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Utils")]
    public class ReportTimestampFormatterTests
    {
        /// <summary>
        /// Verifies that UTC timestamps are formatted with an explicit UTC label.
        /// </summary>
        [TestMethod]
        public void FormatUtc_WithUnspecifiedDateTime_ShouldTreatValueAsUtc()
        {
            var value = new DateTime(2026, 5, 17, 14, 30, 0, DateTimeKind.Unspecified);

            var result = ReportTimestampFormatter.FormatUtc(value, "yyyy-MM-dd HH:mm");

            result.Should().Be("2026-05-17 14:30 UTC");
        }

        /// <summary>
        /// Verifies that nullable default report formatting uses the supplied empty value.
        /// </summary>
        [TestMethod]
        public void FormatDefaultReport_WithNullValue_ShouldReturnEmptyValue()
        {
            var result = ReportTimestampFormatter.FormatDefaultReport(null, "yyyy-MM-dd", "n/a");

            result.Should().Be("n/a");
        }

        /// <summary>
        /// Verifies that known Windows time zone IDs are rendered as stable IANA labels.
        /// </summary>
        [TestMethod]
        public void GetTimeZoneLabel_WithKnownWindowsZone_ShouldReturnIanaLabel()
        {
            var timeZone = TimeZoneInfo.CreateCustomTimeZone(
                "Eastern Standard Time",
                TimeSpan.FromHours(-5),
                "Eastern",
                "Eastern");

            var result = ReportTimestampFormatter.GetTimeZoneLabel(timeZone, new DateTime(2026, 1, 15));

            result.Should().Be("America/New_York");
        }

        /// <summary>
        /// Verifies that unavailable time zones fall back to UTC instead of throwing.
        /// </summary>
        [TestMethod]
        public void FormatTimeZone_WithUnknownTimeZone_ShouldFallBackToUtc()
        {
            var value = new DateTime(2026, 5, 17, 14, 30, 0, DateTimeKind.Utc);

            var result = ReportTimestampFormatter.FormatTimeZone(value, "No Such Zone", "yyyy-MM-dd HH:mm");

            result.Should().Be("2026-05-17 14:30 UTC");
        }
    }
}
