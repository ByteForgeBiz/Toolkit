using System;
using System.Collections.Generic;
using System.Globalization;

namespace ByteForge.Toolkit.Utilities
{
    /// <summary>
    /// Formats report timestamps with an explicit time zone label.
    /// </summary>
    public static class ReportTimestampFormatter
    {
        private static readonly Dictionary<string, string> WindowsToIanaTimeZones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Eastern Standard Time"] = "America/New_York",
            ["Central Standard Time"] = "America/Chicago",
            ["Mountain Standard Time"] = "America/Denver",
            ["Pacific Standard Time"] = "America/Los_Angeles",
            ["Alaskan Standard Time"] = "America/Anchorage",
            ["Hawaiian Standard Time"] = "Pacific/Honolulu",
            ["US Eastern Standard Time"] = "America/Indianapolis",
            ["US Mountain Standard Time"] = "America/Phoenix",
            ["Canada Central Standard Time"] = "America/Regina",
            ["Atlantic Standard Time"] = "America/Halifax",
            ["Newfoundland Standard Time"] = "America/St_Johns",
            ["E. South America Standard Time"] = "America/Sao_Paulo"
        };

        /// <summary>
        /// Formats the current local timestamp with a time zone label.
        /// </summary>
        /// <param name="format">The date and time format to use before the time zone label.</param>
        /// <returns>The formatted local timestamp.</returns>
        public static string FormatLocalNow(string format)
        {
            return FormatLocal(DateTime.Now, format);
        }

        /// <summary>
        /// Formats a local timestamp with the local time zone label.
        /// </summary>
        /// <param name="value">The local timestamp to format.</param>
        /// <param name="format">The date and time format to use before the time zone label.</param>
        /// <returns>The formatted timestamp.</returns>
        public static string FormatLocal(DateTime value, string format)
        {
            return Format(value, format, TimeZoneInfo.Local);
        }

        /// <summary>
        /// Formats the current timestamp in the default report time zone.
        /// </summary>
        /// <param name="format">The date and time format to use before the time zone label.</param>
        /// <returns>The formatted current UTC report timestamp.</returns>
        public static string FormatDefaultReportNow(string format)
        {
            return FormatUtcNow(format);
        }

        /// <summary>
        /// Formats a timestamp in the default report time zone.
        /// </summary>
        /// <param name="value">The timestamp to format. Unspecified values are assumed to already be UTC.</param>
        /// <param name="format">The date and time format to use before the time zone label.</param>
        /// <returns>The formatted UTC report timestamp.</returns>
        public static string FormatDefaultReport(DateTime value, string format)
        {
            return FormatUtc(value, format);
        }

        /// <summary>
        /// Formats a nullable timestamp in the default report time zone.
        /// </summary>
        /// <param name="value">The timestamp to format. Unspecified values are assumed to already be UTC.</param>
        /// <param name="format">The date and time format to use before the time zone label.</param>
        /// <param name="emptyValue">The value to return when the timestamp is null.</param>
        /// <returns>The formatted UTC report timestamp, or the empty value.</returns>
        public static string FormatDefaultReport(DateTime? value, string format, string emptyValue)
        {
            return value.HasValue ? FormatDefaultReport(value.Value, format) : emptyValue;
        }

        /// <summary>
        /// Formats the current timestamp in a named time zone.
        /// </summary>
        /// <param name="timeZoneId">The Windows time zone ID to use.</param>
        /// <param name="format">The date and time format to use before the time zone label.</param>
        /// <returns>The formatted timestamp in the named time zone.</returns>
        public static string FormatTimeZoneNow(string timeZoneId, string format)
        {
            return FormatTimeZone(DateTime.UtcNow, timeZoneId, format);
        }

        /// <summary>
        /// Formats a timestamp in a named time zone.
        /// </summary>
        /// <param name="value">The timestamp to format. Unspecified values are assumed to already be UTC.</param>
        /// <param name="timeZoneId">The Windows time zone ID to use.</param>
        /// <param name="format">The date and time format to use before the time zone label.</param>
        /// <returns>The formatted timestamp in the named time zone.</returns>
        public static string FormatTimeZone(DateTime value, string timeZoneId, string format)
        {
            var timeZone = ResolveTimeZone(timeZoneId);
            var utcValue = value.Kind == DateTimeKind.Local
                ? value.ToUniversalTime()
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);

            return Format(TimeZoneInfo.ConvertTimeFromUtc(utcValue, timeZone), format, timeZone);
        }

        /// <summary>
        /// Formats the current UTC timestamp with the UTC time zone label.
        /// </summary>
        /// <param name="format">The date and time format to use before the time zone label.</param>
        /// <returns>The formatted current UTC timestamp.</returns>
        public static string FormatUtcNow(string format)
        {
            return FormatUtc(DateTime.UtcNow, format);
        }

        /// <summary>
        /// Formats a UTC timestamp with the UTC time zone label.
        /// </summary>
        /// <param name="value">The UTC timestamp to format. Unspecified values are assumed to already be UTC.</param>
        /// <param name="format">The date and time format to use before the time zone label.</param>
        /// <returns>The formatted UTC timestamp.</returns>
        public static string FormatUtc(DateTime value, string format)
        {
            var utcValue = value.Kind == DateTimeKind.Local
                ? value.ToUniversalTime()
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} UTC",
                utcValue.ToString(format, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Formats a timestamp with the supplied time zone label.
        /// </summary>
        /// <param name="value">The timestamp to format.</param>
        /// <param name="format">The date and time format to use before the time zone label.</param>
        /// <param name="timeZone">The time zone that describes the timestamp.</param>
        /// <returns>The formatted timestamp.</returns>
        public static string Format(DateTime value, string format, TimeZoneInfo timeZone)
        {
            var resolvedTimeZone = timeZone ?? TimeZoneInfo.Local;
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1}",
                value.ToString(format, CultureInfo.InvariantCulture),
                GetTimeZoneLabel(resolvedTimeZone, value));
        }

        /// <summary>
        /// Gets the preferred report label for a time zone.
        /// </summary>
        /// <param name="timeZone">The time zone to label.</param>
        /// <param name="timestamp">The timestamp used to choose a daylight-aware abbreviation fallback.</param>
        /// <returns>The time zone label.</returns>
        public static string GetTimeZoneLabel(TimeZoneInfo timeZone, DateTime timestamp)
        {
            if (timeZone == null)
                return TimeZoneInfo.Local.Id;

            if (string.Equals(timeZone.Id, "UTC", StringComparison.OrdinalIgnoreCase))
                return "UTC";

            if (WindowsToIanaTimeZones.TryGetValue(timeZone.Id, out var ianaName))
                return ianaName;

            var abbreviation = GetCommonAbbreviation(timeZone, timestamp);
            return string.IsNullOrWhiteSpace(abbreviation) ? timeZone.Id : abbreviation;
        }

        /// <summary>
        /// Gets a common daylight-aware abbreviation for unmapped zones.
        /// </summary>
        /// <param name="timeZone">The time zone to abbreviate.</param>
        /// <param name="timestamp">The timestamp used to choose daylight or standard time.</param>
        /// <returns>The abbreviation, or an empty string when no abbreviation is known.</returns>
        private static string GetCommonAbbreviation(TimeZoneInfo timeZone, DateTime timestamp)
        {
            if (timeZone == null)
                return string.Empty;

            switch (timeZone.Id)
            {
                case "Eastern Standard Time":
                    return timeZone.IsDaylightSavingTime(timestamp) ? "EDT" : "EST";
                case "Central Standard Time":
                    return timeZone.IsDaylightSavingTime(timestamp) ? "CDT" : "CST";
                case "Mountain Standard Time":
                    return timeZone.IsDaylightSavingTime(timestamp) ? "MDT" : "MST";
                case "Pacific Standard Time":
                    return timeZone.IsDaylightSavingTime(timestamp) ? "PDT" : "PST";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Resolves a time zone by ID.
        /// </summary>
        /// <param name="timeZoneId">The Windows time zone ID to resolve.</param>
        /// <returns>The resolved time zone, or UTC if the ID is unavailable.</returns>
        private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Utc;
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.Utc;
            }
        }
    }
}
