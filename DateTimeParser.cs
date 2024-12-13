using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ByteForge.Toolkit;

/// <summary>
/// Provides methods to parse date and time strings into <see cref="DateTime"/> objects.
/// </summary>
public static class DateTimeParser
{
    private static readonly ConcurrentDictionary<string, string> _successfulFormats = new();

    private static readonly string[] TimeZones =
    [
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
    ];

    private static readonly string[] DateFormats =
    [
        "MM/dd/yyyy", "M/d/yyyy", "dd/MM/yyyy", "d/M/yyyy",
        "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd",
        "dd.MM.yyyy", "MM.dd.yyyy",
        "MMM dd yyyy", "MMMM dd yyyy",
        "dd MMM yyyy", "dd MMMM yyyy",
        // Add more as needed
    ];

    private static readonly string[] TimeFormats =
    [
        "HH:mm:ss", "HH:mm",
        "hh:mm:ss tt", "hh:mm tt",
        "HH:mm:ss.fff", "HH:mm:ss.ffffff",
        "HHmmss"
        // Add more as needed
    ];

    private static readonly string[] OffsetFormats =
    [
        "zzz", "'Z'", "zz:zz", " zzz", " zz:zz"
    ];

    /// <summary>
    /// Parses the specified date and time string into a <see cref="DateTime"/> object.
    /// </summary>
    /// <param name="input">The date and time string to parse.</param>
    /// <returns>A <see cref="DateTime"/> object that represents the parsed date and time.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the input string is null or empty.</exception>
    /// <exception cref="FormatException">Thrown when the input string cannot be parsed into a valid <see cref="DateTime"/>.</exception>
    public static DateTime Parse(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentNullException(nameof(input));

        // Try using the last successful format first
        if (_successfulFormats.TryGetValue(GetPatternKey(input), out var lastFormat) && 
            TryParseWithCulture(input, lastFormat, out var result))
                return result;

        // Clean up the input
        input = CleanInput(input);

        // Generate all possible format combinations
        var formatAttempts = GenerateFormatCombinations(input);

        // Try each format
        foreach (var format in formatAttempts)
            if (TryParseWithCulture(input, format, out result))
            {
                // Cache the successful format
                _successfulFormats.AddOrUpdate(GetPatternKey(input), format, (_, _) => format);
                return result;
            }

        throw new FormatException($"Could not parse datetime: {input}");
    }

    /// <summary>
    /// Tries to parse the specified date and time string into a <see cref="DateTime"/> object.
    /// </summary>
    /// <param name="input">The date and time string to parse.</param>
    /// <param name="result">When this method returns, contains the <see cref="DateTime"/> value equivalent to the date and time contained in <paramref name="input"/>, if the conversion succeeded, or <see cref="DateTime.MinValue"/> if the conversion failed. The conversion fails if the <paramref name="input"/> parameter is null or an empty string, or does not contain a valid string representation of a date and time. This parameter is passed uninitialized.</param>
    /// <returns><c>true</c> if the <paramref name="input"/> parameter was converted successfully; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string input, out DateTime result)
    {
        try
        {
            result = Parse(input);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Tries to parse the specified date and time string using the given format and culture information.
    /// </summary>
    /// <param name="input">The date and time string to parse.</param>
    /// <param name="format">The format specifier that defines the required format of <paramref name="input"/>.</param>
    /// <param name="result">When this method returns, contains the <see cref="DateTime"/> value equivalent to the date and time contained in <paramref name="input"/>, if the conversion succeeded, or <see cref="DateTime.MinValue"/> if the conversion failed. This parameter is passed uninitialized.</param>
    /// <returns><c>true</c> if the <paramref name="input"/> parameter was converted successfully; otherwise, <c>false</c>.</returns>
    private static bool TryParseWithCulture(string input, string format, out DateTime result)
    {
        return DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result) || 
               DateTime.TryParseExact(input, format, CultureInfo.CurrentCulture, DateTimeStyles.None, out result);
    }

    /// <summary>
    /// Cleans the input string by removing timezone abbreviations and extra spaces.
    /// </summary>
    /// <param name="input">The input string to clean.</param>
    /// <returns>The cleaned input string.</returns>
    private static string CleanInput(string input)
    {
        // Remove any timezone abbreviations but keep the offset
        var tz = TimeZones.FirstOrDefault(x => input.Contains(x));
        if (!string.IsNullOrEmpty(tz))
        {
            input = input.Replace(tz, "");
        }

        // Clean up multiple spaces
        return string.Join(" ", input.Split([' '],
            StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Generates a pattern key based on the input format.
    /// </summary>
    /// <param name="input">The input string to generate the pattern key for.</param>
    /// <returns>The generated pattern key.</returns>
    private static string GetPatternKey(string input)
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
    private static IEnumerable<string> GenerateFormatCombinations(string input)
    {
        var formats = new List<string>();

        // Has time component?
        var hasTime = input.Contains(":");
        // Has timezone/offset?
        var hasOffset = input.Contains("+") || input.Contains("-") ||
                        input.Contains("Z") || input.EndsWith("UTC");

        foreach (var dateFormat in DateFormats)
        {
            if (!hasTime)
            {
                formats.Add(dateFormat);
                continue;
            }

            foreach (var timeFormat in TimeFormats)
            {
                if (!hasOffset)
                {
                    formats.Add($"{dateFormat} {timeFormat}");
                    continue;
                }

                foreach (var offsetFormat in OffsetFormats)
                {
                    formats.Add($"{dateFormat} {timeFormat}{offsetFormat}");
                }
            }
        }

        return formats;
    }

    /// <summary>
    /// Clears the cache of successful formats.
    /// </summary>
    public static void ClearCache()
    {
        _successfulFormats.Clear();
    }
}
