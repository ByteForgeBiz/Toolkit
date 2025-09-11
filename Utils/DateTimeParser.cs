using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ByteForge.Toolkit
{
    /*
     *  ___       _      _____ _           ___                      
     * |   \ __ _| |_ __|_   _(_)_ __  ___| _ \__ _ _ _ ___ ___ _ _ 
     * | |) / _` |  _/ -_)| | | | '  \/ -_)  _/ _` | '_(_-</ -_) '_|
     * |___/\__,_|\__\___||_| |_|_|_|_\___|_| \__,_|_| /__/\___|_|  
     *                                                              
     */
    /// <summary>
    /// Provides methods to parse date and time strings into <see cref="DateTime"/> objects.
    /// </summary>
    /// <remarks>
    /// This class provides enhanced date and time parsing capabilities, supporting a wide
    /// variety of formats beyond the standard .NET parsing functions. It intelligently
    /// attempts multiple formats and caches successful patterns for improved performance.
    /// <para>
    /// The class can be used through static methods (which use a shared singleton instance)
    /// or by creating instance(s) with potentially different configurations.
    /// </para>
    /// </remarks>
    public class DateTimeParser
    {
        /// <summary>
        /// Gets the default instance of the <see cref="DateTimeParser"/> class.
        /// </summary>
        public static DateTimeParser Default => _defaultInstance.Value;

        private static readonly Lazy<DateTimeParser> _defaultInstance = new Lazy<DateTimeParser>();

        private readonly ConcurrentDictionary<string, string> _successfulFormats = new ConcurrentDictionary<string, string>();

        private readonly string[] _timeZones = new string[]
        {
            "UTC",  // Coordinated Universal Time
            "GMT",  // Greenwich Mean Time
            "AST",  // Atlantic Standard Time
            "ADT",  // Atlantic Daylight Time
            "EST",  // Eastern Standard Time
            "EDT",  // Eastern Daylight Time
            "CST",  // Central Standard Time
            "CDT",  // Central Daylight Time
            "MST",  // Mountain Standard Time
            "MDT",  // Mountain Daylight Time
            "PST",  // Pacific Standard Time
            "PDT",  // Pacific Daylight Time
            "HST",  // Hawaii Standard Time
            "HDT",  // Hawaii Daylight Time
            "AKST", // Alaska Standard Time
            "AKDT", // Alaska Daylight Time
            // Add more as needed
        };

        private readonly string[] _dateFormats = new string[]
        {
            "MM/dd/yyyy", "M/d/yyyy", "dd/MM/yyyy", "d/M/yyyy",
            "MMM/dd/yyyy", "MMM/d/yyyy", "dd/MMM/yyyy", "d/MMM/yyyy",
            "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd",
            "yyyy-MMM-dd", "yyyy/MMM/dd", "yyyy.MMM.dd",
            "dd.MM.yyyy", "MM.dd.yyyy", "dd.MMM.yyyy", "MMM.dd.yyyy",
            "MMM dd yyyy", "MMMM dd yyyy",
            "dd MMM yyyy", "dd MMMM yyyy",
            "yyyyMMdd"
            // Add more as needed
        };

        private readonly string[] _timeFormats = new string[]
        {
            "HH:mm:ss", "HH:mm",
            "hh:mm:ss tt", "hh:mm tt",
            "HH:mm:ss.fff", "HH:mm:ss.ffffff",
            "HHmmss"
            // Add more as needed
        };

        private readonly string[] _offsetFormats = new string[]
        {
            "zzz", "'Z'", "zz:zz", " zzz", " zz:zz"
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeParser"/> class.
        /// </summary>
        public DateTimeParser()
        {
            // Default constructor uses the predefined format arrays
        }

        /// <summary>
        /// Parses the specified date and time string into a <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="input">The date and time string to parse.</param>
        /// <returns>A <see cref="DateTime"/> object that represents the parsed date and time.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the input string is null or empty.</exception>
        /// <exception cref="FormatException">Thrown when the input string cannot be parsed into a valid <see cref="DateTime"/>.</exception>
        public DateTime ParseValue(string input) => ParseValue(input, CultureInfo.InvariantCulture);

        /// <summary>
        /// Parses the specified date and time string into a <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="input">The date and time string to parse.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information about <paramref name="input"/>.</param>
        /// <returns>A <see cref="DateTime"/> object that represents the parsed date and time.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the input string is null or empty.</exception>
        /// <exception cref="FormatException">Thrown when the input string cannot be parsed into a valid <see cref="DateTime"/>.</exception>
        public DateTime ParseValue(string input, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentNullException(nameof(input));

            // Try using the last successful format first
            if (_successfulFormats.TryGetValue(GetPatternKey(input), out var lastFormat) &&
                TryParseWithCulture(input, lastFormat, formatProvider, out var result))
                return result;

            // Clean up the input
            input = CleanInput(input);

            // Generate all possible format combinations
            var formatAttempts = GenerateFormatCombinations(input);

            // Try each format
            foreach (var format in formatAttempts)
                if (TryParseWithCulture(input, format, formatProvider, out result))
                {
                    // Cache the successful format
                    _successfulFormats.AddOrUpdate(GetPatternKey(input), format, (a, b) => format);
                    return result;
                }

            // Try a last resort with DateTime.Parse
            if (DateTime.TryParse(input, formatProvider, DateTimeStyles.None, out result))
                return result;

            throw new FormatException($"Could not parse datetime: {input}");
        }

        /// <summary>
        /// Tries to parse the specified date and time string into a <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="input">The date and time string to parse.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateTime"/> value equivalent to the date and time contained in <paramref name="input"/>, if the conversion succeeded, or <see cref="DateTime.MinValue"/> if the conversion failed. The conversion fails if the <paramref name="input"/> parameter is null or an empty string, or does not contain a valid string representation of a date and time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" /> if the <paramref name="input"/> parameter was converted successfully; otherwise, <see langword="false" />.</returns>
        public bool TryParseValue(string input, out DateTime result) => 
            TryParseValue(input, CultureInfo.InvariantCulture, out result);

        /// <summary>
        /// Tries to parse the specified date and time string into a <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="input">The date and time string to parse.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information about <paramref name="input"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateTime"/> value equivalent to the date and time contained in <paramref name="input"/>, if the conversion succeeded, or <see cref="DateTime.MinValue"/> if the conversion failed. The conversion fails if the <paramref name="input"/> parameter is null or an empty string, or does not contain a valid string representation of a date and time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" /> if the <paramref name="input"/> parameter was converted successfully; otherwise, <see langword="false" />.</returns>
        public bool TryParseValue(string input, IFormatProvider formatProvider, out DateTime result)
        {
            try
            {
                result = ParseValue(input, formatProvider);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Clears the cache of successful formats.
        /// </summary>
        public void ClearFormatCache()
        {
            _successfulFormats.Clear();
        }

        /// <summary>
        /// Tries to parse the specified date and time string using the given format and culture information.
        /// </summary>
        /// <param name="input">The date and time string to parse.</param>
        /// <param name="format">The format specifier that defines the required format of <paramref name="input"/>.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information about <paramref name="input"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateTime"/> value equivalent to the date and time contained in <paramref name="input"/>, if the conversion succeeded, or <see cref="DateTime.MinValue"/> if the conversion failed. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" /> if the <paramref name="input"/> parameter was converted successfully; otherwise, <see langword="false" />.</returns>
        private bool TryParseWithCulture(string input, string format, IFormatProvider formatProvider, out DateTime result)
        {
            return DateTime.TryParseExact(input, format, formatProvider, DateTimeStyles.None, out result) ||
                   DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result) ||
                   DateTime.TryParseExact(input, format, CultureInfo.CurrentCulture, DateTimeStyles.None, out result);
        }

        /// <summary>
        /// Cleans the input string by removing timezone abbreviations and extra spaces.
        /// </summary>
        /// <param name="input">The input string to clean.</param>
        /// <returns>The cleaned input string.</returns>
        private string CleanInput(string input)
        {
            // Remove any timezone abbreviations but keep the offset
            var tz = _timeZones.FirstOrDefault(x => input.Contains(x));
            if (!string.IsNullOrEmpty(tz))
            {
                input = input.Replace(tz, "");
            }

            // Clean up multiple spaces
            return string.Join(" ", input.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Generates a pattern key based on the input format.
        /// </summary>
        /// <param name="input">The input string to generate the pattern key for.</param>
        /// <returns>The generated pattern key.</returns>
        private string GetPatternKey(string input)
        {
            // Create a pattern key based on the input format
            var pattern = input;

            // Replace digits with 'n'
            pattern = Regex.Replace(pattern, @"\d", "n");

            // Replace common separators with 's'
            pattern = Regex.Replace(pattern, @"[:/\.-]", "s");

            // Replace AM/PM markers with 't'
            pattern = Regex.Replace(pattern, @"[APM]", "t");

            return pattern;
        }

        /// <summary>
        /// Generates all possible format combinations based on the input string.
        /// </summary>
        /// <param name="input">The input string to generate format combinations for.</param>
        /// <returns>An enumerable of format combinations.</returns>
        private IEnumerable<string> GenerateFormatCombinations(string input)
        {
            var formats = new List<string>();

            // Has time component?
            var hasTime = input.Contains(":");

            // Improved offset detection using regex to avoid false positives from date separators
            var offsetRegex = new Regex(@"([+-](?:0[0-9]|1[0-4])(?::?[0-5][0-9])?|Z)$");
            var hasOffset = offsetRegex.IsMatch(input) || input.EndsWith("UTC");

            foreach (var dateFormat in _dateFormats)
            {
                if (!hasTime)
                {
                    formats.Add(dateFormat);
                    continue;
                }

                foreach (var timeFormat in _timeFormats)
                {
                    if (!hasOffset)
                    {
                        formats.Add($"{dateFormat} {timeFormat}");
                        continue;
                    }

                    foreach (var offsetFormat in _offsetFormats)
                        formats.Add($"{dateFormat} {timeFormat}{offsetFormat}");
                }
            }

            return formats;
        }

        #region Static Methods

        /// <summary>
        /// Parses the specified date and time string into a <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="input">The date and time string to parse.</param>
        /// <returns>A <see cref="DateTime"/> object that represents the parsed date and time.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the input string is null or empty.</exception>
        /// <exception cref="FormatException">Thrown when the input string cannot be parsed into a valid <see cref="DateTime"/>.</exception>
        public static DateTime Parse(string input) => Default.ParseValue(input);

        /// <summary>
        /// Parses the specified date and time string into a <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="input">The date and time string to parse.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information about <paramref name="input"/>.</param>
        /// <returns>A <see cref="DateTime"/> object that represents the parsed date and time.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the input string is null or empty.</exception>
        /// <exception cref="FormatException">Thrown when the input string cannot be parsed into a valid <see cref="DateTime"/>.</exception>
        public static DateTime Parse(string input, IFormatProvider formatProvider) => 
            Default.ParseValue(input, formatProvider);

        /// <summary>
        /// Tries to parse the specified date and time string into a <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="input">The date and time string to parse.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateTime"/> value equivalent to the date and time contained in <paramref name="input"/>, if the conversion succeeded, or <see cref="DateTime.MinValue"/> if the conversion failed. The conversion fails if the <paramref name="input"/> parameter is null or an empty string, or does not contain a valid string representation of a date and time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" /> if the <paramref name="input"/> parameter was converted successfully; otherwise, <see langword="false" />.</returns>
        public static bool TryParse(string input, out DateTime result) => 
            Default.TryParseValue(input, out result);

        /// <summary>
        /// Tries to parse the specified date and time string into a <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="input">The date and time string to parse.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information about <paramref name="input"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="DateTime"/> value equivalent to the date and time contained in <paramref name="input"/>, if the conversion succeeded, or <see cref="DateTime.MinValue"/> if the conversion failed. The conversion fails if the <paramref name="input"/> parameter is null or an empty string, or does not contain a valid string representation of a date and time. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" /> if the <paramref name="input"/> parameter was converted successfully; otherwise, <see langword="false" />.</returns>
        public static bool TryParse(string input, IFormatProvider formatProvider, out DateTime result) => 
            Default.TryParseValue(input, formatProvider, out result);

        /// <summary>
        /// Clears the cache of successful formats in the default instance.
        /// </summary>
        public static void ClearCache() => Default.ClearFormatCache();

        #endregion
    }
}