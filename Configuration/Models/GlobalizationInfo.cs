using System;
using System.ComponentModel;
using System.Globalization;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Provides globalization information such as culture, date format, and time format.
    /// </summary>
    public class GlobalizationInfo
    {
        private const string DefaultDateFormat = "MM'/'dd'/'yyyy";
        private const string DefaultTimeFormat = "HH':'mm':'ss";
        private const string DefaultShortTime12Format = "h':'mm tt";
        private const string DefaultCurrencyFormat = "'$' #,##0.00";
        private const string DefaultNumberFormat = "#,##0.00";
        private const string DefaultPercentFormat = "#0.00'%'";

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalizationInfo"/> class.
        /// </summary>
        public GlobalizationInfo() { }

        /// <summary>
        /// Gets or sets the culture information.
        /// </summary>
        public CultureInfo CultureInfo { get; set; } = new CultureInfo("en-US");

        /// <summary>
        /// Gets or sets the date format string.
        /// </summary>
        [DefaultValue(DefaultDateFormat)]
        public string DateFormat { get; set; } = DefaultDateFormat;

        /// <summary>
        /// Gets or sets the time format string.
        /// </summary>
        [DefaultValue(DefaultTimeFormat)]
        public string TimeFormat { get; set; } = DefaultTimeFormat;

        /// <summary>
        /// Gets or sets the short 12-hour time format string.
        /// </summary>
        [DefaultValue(DefaultShortTime12Format)]
        public string ShortTime12Format { get; set; } = DefaultShortTime12Format;

        /// <summary>
        /// Gets the combined date and time format string.
        /// </summary>
        public string DateTimeFormat => DateFormat + " " + TimeFormat;

        /// <summary>
        /// Gets or sets the currency format string.
        /// </summary>
        [DefaultValue(DefaultCurrencyFormat)]
        public string CurrencyFormat { get; set; } = DefaultCurrencyFormat;

        /// <summary>
        /// Gets or sets the number format string.
        /// </summary>
        [DefaultValue(DefaultNumberFormat)]
        public string NumberFormat { get; set; } = DefaultNumberFormat;

        /// <summary>
        /// Gets or sets the percent format string.
        /// </summary>
        [DefaultValue(DefaultPercentFormat)]
        public string PercentFormat { get; set; } = DefaultPercentFormat;

        /// <summary>
        /// Formats the specified value as currency using the <see cref="CurrencyFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>A string representation of the value formatted as currency.</returns>
        public string FormatCurrency(decimal? value) => value?.ToString(CurrencyFormat, CultureInfo);

        /// <summary>
        /// Formats the specified value as a number using the <see cref="NumberFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>A string representation of the value formatted as a number.</returns>
        public string FormatNumber(decimal? value) => value?.ToString(NumberFormat, CultureInfo);

        /// <summary>
        /// Formats the specified value as a percent using the <see cref="PercentFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>A string representation of the value formatted as a percent.</returns>
        public string FormatPercent(decimal? value) => value?.ToString(PercentFormat, CultureInfo);

        /// <summary>
        /// Formats the specified value as a date and time using the <see cref="DateTimeFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>A string representation of the value formatted as a date and time.</returns>
        public string FormatDateTime(DateTime? value) => value?.ToString(DateTimeFormat, CultureInfo);

        /// <summary>
        /// Formats the specified value as a date using the <see cref="DateFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>A string representation of the value formatted as a date.</returns>
        public string FormatDate(DateTime? value) => value?.ToString(DateFormat, CultureInfo);

        /// <summary>
        /// Formats the specified value as a time using the <see cref="TimeFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>A string representation of the value formatted as a time.</returns>
        public string FormatTime(DateTime? value) => value?.ToString(TimeFormat, CultureInfo);

        /// <summary>
        /// Formats the specified value as a short 12-hour time using the <see cref="ShortTime12Format"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="showSeconds">True to show seconds; otherwise, false.</param>
        /// <returns>A string representation of the value formatted as a short 12-hour time.</returns>
        public string FormatShortTime12(DateTime? value, bool showSeconds = false)
        {
        var fmt = ShortTime12Format;
            if (showSeconds && fmt.Contains("mm"))
            {
                fmt = fmt.Replace("mm", "mm:ss");
            }
            return value?.ToString(fmt, CultureInfo);
        }
    }
}
