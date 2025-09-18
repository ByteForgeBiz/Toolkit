using System;
using System.ComponentModel;
using System.Globalization;

namespace ByteForge.Toolkit
{
    /*
     *   ___ _     _          _ _         _   _          ___       __     
     *  / __| |___| |__  __ _| (_)_____ _| |_(_)___ _ _ |_ _|_ _  / _|___ 
     * | (_ | / _ \ '_ \/ _` | | |_ / _` |  _| / _ \ ' \ | || ' \|  _/ _ \
     *  \___|_\___/_.__/\__,_|_|_/__\__,_|\__|_\___/_||_|___|_||_|_| \___/
     *                                                                    
     */

    /// <summary>
    /// Provides globalization information such as culture, date format, and time format.
    /// </summary>
    public class GlobalizationInfo
    {
        private const string DefaultDateFormat = "MM'/'dd'/'yyyy";
        private const string DefaultLongDateFormat = "dddd, MMMM d, yyyy";
        private const string DefaultTimeFormat = "HH':'mm':'ss";
        private const string DefaultShortTimeFormat = "HH':'mm";
        private const string DefaultLongTimeFormat = "HH':'mm':'ss'.'fff";
        private const string DefaultTime12Format = "hh':'mm':'ss tt";
        private const string DefaultShortTime12Format = "h':'mm tt";
        private const string DefaultLongTime12Format = "hh':'mm':'ss'.'fff tt";
        private const string DefaultCurrencyFormat = "'$' #,##0.00";
        private const string DefaultIntegerFormat = "#,##0";
        private const string DefaultDecimalFormat = "#,##0.00";
        private const string DefaultPercentFormat = "#0.00'%'";

         /*
          *   ___     _ _                ___       __     
          *  / __|  _| | |_ _  _ _ _ ___|_ _|_ _  / _|___ 
          * | (_| || | |  _| || | '_/ -_)| || ' \|  _/ _ \
          *  \___\_,_|_|\__|\_,_|_| \___|___|_||_|_| \___/
          *                                               
          */
        
        /// <summary>
        /// Gets or sets the culture information used for formatting.
        /// </summary>
        /// <remarks>
        /// The <see cref="CultureInfo"/> property determines the culture-specific formatting for all values in this class.<br/>
        /// For example, setting this to <c>CultureInfo.GetCultureInfo("fr-FR")</c> will use French formatting conventions.
        /// </remarks>
        [DefaultValueProvider(typeof(GlobalizationInfo), nameof(GetDefaultCultureInfo))]
        public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;

         /*
          *  ___       _           ___                   _      
          * |   \ __ _| |_ ___    | __|__ _ _ _ __  __ _| |_ ___
          * | |) / _` |  _/ -_)   | _/ _ \ '_| '  \/ _` |  _(_-<
          * |___/\__,_|\__\___|   |_|\___/_| |_|_|_\__,_|\__/__/
          *                                                     
          */
        
        /// <summary>
        /// Gets or sets the date format string.
        /// </summary>
        /// <remarks>
        /// The format string should follow the standard .NET date and time format patterns.<br/>
        /// For example, "MM/dd/yyyy" represents a date in month/day/year format.
        /// </remarks>
        [DefaultValue(DefaultDateFormat)]
        public string DateFormat { get; set; } = DefaultDateFormat;

        /// <summary>
        /// Gets or sets the long date format string.
        /// </summary>
        /// <remarks>
        /// The format string should follow the standard .NET date and time format patterns.<br/>
        /// For example, "dddd, MMMM d, yyyy" represents a long date with day of week, month, day, and year.
        /// </remarks>
        [DefaultValue(DefaultLongDateFormat)]
        public string LongDateFormat { get; set; } = DefaultLongDateFormat;

        /// <summary>
        /// Gets the combined date and time format string.
        /// </summary>
        /// <remarks>
        /// This property combines <see cref="DateFormat"/> and <see cref="TimeFormat"/> to provide a full date and time format string.
        /// </remarks>
        public string DateTimeFormat => DateFormat + " " + TimeFormat;

        /// <summary>
        /// Gets the combined short date and short time format string.
        /// </summary>
        public string ShortDateTimeFormat => DateFormat + " " + ShortTimeFormat;

        /// <summary>
        /// Gets the combined long date and long time format string.
        /// </summary>
        public string LongDateTimeFormat => LongDateFormat + " " + LongTimeFormat;

        /// <summary>
        /// Gets the combined short date and short 12-hour time format string.
        /// </summary>
        public string ShortDateTime12Format => DateFormat + " " + ShortTime12Format;

        /// <summary>
        /// Gets the combined long date and long 12-hour time format string.
        /// </summary>
        public string LongDateTime12Format => LongDateFormat + " " + LongTime12Format;

        /*
         *  _____ _               ___                   _           _____ _ _          _                   __  
         * |_   _(_)_ __  ___    | __|__ _ _ _ __  __ _| |_ ___    / /_  ) | |   ___  | |_  ___ _  _ _ _ __\ \ 
         *   | | | | '  \/ -/)   | _/ _ \ '_| '  \/ _` |  _(_-<   | | / /|_  _| |___| | ' \/ _ \ || | '_(_-<| |
         *   |_| |_|_|_|_\___|   |_|\___/_| |_|_|_\__,_|\__/__/   | |/___| |_|        |_||_\___/\_,_|_| /__/| |
         *                                                         \_\                                     /_/ 
         */

        /// <summary>
        /// Gets or sets the long time format string.
        /// </summary>
        /// <remarks>
        /// The format string should follow the standard .NET date and time format patterns.<br/>
        /// For example, "HH:mm:ss.fff" represents hours, minutes, seconds, and milliseconds in 24-hour format.
        /// </remarks>
        [DefaultValue(DefaultLongTimeFormat)]
        public string LongTimeFormat { get; set; } = DefaultLongTimeFormat;

        /// <summary>
        /// Gets or sets the short time format string.
        /// </summary>
        /// <remarks>
        /// The format string should follow the standard .NET date and time format patterns.<br/>
        /// For example, "HH:mm" represents hours and minutes in 24-hour format.
        /// </remarks>
        [DefaultValue(DefaultShortTimeFormat)]
        public string ShortTimeFormat { get; set; } = DefaultShortTimeFormat;

        /// <summary>
        /// Gets or sets the time format string.
        /// </summary>
        /// <remarks>
        /// The format string should follow the standard .NET date and time format patterns.<br/>
        /// For example, "HH:mm:ss" represents hours, minutes, and seconds in 24-hour format.
        /// </remarks>
        [DefaultValue(DefaultTimeFormat)]
        public string TimeFormat { get; set; } = DefaultTimeFormat;

        /*
         *  _____ _               ___                   _           ___ ___         _                   __  
         * |_   _(_)_ __  ___    | __|__ _ _ _ __  __ _| |_ ___    / / |_  )  ___  | |_  ___ _  _ _ _ __\ \ 
         *   | | | | '  \/ -/)   | _/ _ \ '_| '  \/ _` |  _(_-<   | || |/ /  |___| | ' \/ _ \ || | '_(_-<| |
         *   |_| |_|_|_|_\___|   |_|\___/_| |_|_|_\__,_|\__/__/   | ||_/___|       |_||_\___/\_,_|_| /__/| |
         *                                                         \_\                                  /_/ 
         */

        /// <summary>
        /// Gets or sets the long 12-hour time format string.
        /// </summary>
        /// <remarks>
        /// The format string should follow the standard .NET date and time format patterns.<br/>
        /// For example, "hh:mm:ss.fff tt" represents hours, minutes, seconds, milliseconds, and an AM/PM designator.
        /// </remarks>
        [DefaultValue(DefaultLongTime12Format)]
        public string LongTime12Format { get; set; } = DefaultLongTime12Format;

        /// <summary>
        /// Gets or sets the short 12-hour time format string.
        /// </summary>
        /// <remarks>
        /// The format string should follow the standard .NET date and time format patterns.<br/>
        /// For example, "h:mm tt" represents hours and minutes with an AM/PM designator.
        /// </remarks>
        [DefaultValue(DefaultShortTime12Format)]
        public string ShortTime12Format { get; set; } = DefaultShortTime12Format;

        /// <summary>
        /// Gets or sets the format string used to display time in a 12-hour clock format.
        /// </summary>
        /// <remarks>
        /// The format string should follow the standard .NET date and time format patterns.<br/>
        /// For example, "hh:mm tt" represents hours and minutes with an AM/PM designator.
        /// </remarks>
        [DefaultValue(DefaultTime12Format)]
        public string Time12Format { get; set; } = DefaultTime12Format;

         /*
          *  _  _                    _        ___                   _      
          * | \| |_  _ _ __  ___ _ _(_)__    | __|__ _ _ _ __  __ _| |_ ___
          * | .` | || | '  \/ -_) '_| / _|   | _/ _ \ '_| '  \/ _` |  _(_-<
          * |_|\_|\_,_|_|_|_\___|_| |_\__|   |_|\___/_| |_|_|_\__,_|\__/__/
          *                                                                
          */
                         
        /// <summary>
        /// Gets or sets the currency format string.
        /// </summary>
        /// <remarks>
        /// The format string should follow the standard .NET numeric format patterns.<br/>
        /// For example, "'$' #,##0.00" represents a currency value with a dollar sign and two decimal places.
        /// </remarks>
        [DefaultValue(DefaultCurrencyFormat)]
        public string CurrencyFormat { get; set; } = DefaultCurrencyFormat;

        /// <summary>
        /// Gets or sets the format string used for displaying integers.
        /// </summary>
        /// <remarks>
        /// The format string should follow the standard .NET numeric format patterns.<br/>
        /// For example, "#,##0" represents an integer with thousand separators.
        /// </remarks>
        [DefaultValue(DefaultIntegerFormat)]
        public string IntegerFormat { get; set; } = DefaultIntegerFormat;

        /// <summary>
        /// Gets or sets the number format string.
        /// </summary>
        /// <remarks>
        /// The format string should follow the standard .NET numeric format patterns.<br/>
        /// For example, "#,##0.00" represents a number with thousand separators and two decimal places.
        /// </remarks>
        [DefaultValue(DefaultDecimalFormat)]
        public string NumberFormat { get; set; } = DefaultDecimalFormat;

        /// <summary>
        /// Gets or sets the percent format string.
        /// </summary>
        /// <remarks>
        /// The format string should follow the standard .NET numeric format patterns.<br/>
        /// For example, "#0.00'%'" represents a percent value with two decimal places and a percent sign.
        /// </remarks>
        [DefaultValue(DefaultPercentFormat)]
        public string PercentFormat { get; set; } = DefaultPercentFormat;

         /*
          *  ___       _           ___                   _   _   _               __  __     _   _            _    
          * |   \ __ _| |_ ___    | __|__ _ _ _ __  __ _| |_| |_(_)_ _  __ _    |  \/  |___| |_| |_  ___  __| |___
          * | |) / _` |  _/ -/)   | _/ _ \ '_| '  \/ _` |  _|  _| | ' \/ _` |   | |\/| / -_)  _| ' \/ _ \/ _` (_-<
          * |___/\__,_|\__\___|   |_|\___/_| |_|_|_\__,_|\__|\__|_|_||_\__, |   |_|  |_\___|\__|_||_\___/\__,_/__/
          *                                                            |___/                                      
          */
        
        /// <summary>
        /// Formats the specified nullable value as a date using the <see cref="DateFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a date, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatDate(DateTime? value, string nullValue = null) => value?.ToString(DateFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable value as a date and time using the <see cref="DateTimeFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a date and time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatDateTime(DateTime? value, string nullValue = null) => value?.ToString(DateTimeFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable value as a long date using the <see cref="LongDateFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a long date, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatLongDate(DateTime? value, string nullValue = null) => value?.ToString(LongDateFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable value as a long time using the <see cref="LongTimeFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a long time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatLongTime(DateTime? value, string nullValue = null) => value?.ToString(LongTimeFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable value as a short time using the <see cref="ShortTimeFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a short time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatShortTime(DateTime? value, string nullValue = null) => value?.ToString(ShortTimeFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable value as a time using the <see cref="TimeFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatTime(DateTime? value, string nullValue = null) => value?.ToString(TimeFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable value as a long 12-hour time using the <see cref="LongTime12Format"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a long 12-hour time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatLongTime12(DateTime? value, string nullValue = null) => value?.ToString(LongTime12Format, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable value as a short 12-hour time using the <see cref="ShortTime12Format"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a short 12-hour time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatShortTime12(DateTime? value, string nullValue = null) => value?.ToString(ShortTime12Format, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable value as a 12-hour time using the <see cref="Time12Format"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a 12-hour time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatTime12(DateTime? value, string nullValue = null) => value?.ToString(Time12Format, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable value as a short date and time using the <see cref="ShortDateTimeFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a short date and time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatShortDateTime(DateTime? value, string nullValue = null) => value?.ToString(ShortDateTimeFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable value as a long date and time using the <see cref="LongDateTimeFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a long date and time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatLongDateTime(DateTime? value, string nullValue = null) => value?.ToString(LongDateTimeFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable value as a short date and 12-hour time using the <see cref="ShortDateTime12Format"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a short date and 12-hour time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatShortDateTime12(DateTime? value, string nullValue = null) => value?.ToString(ShortDateTime12Format, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable value as a long date and 12-hour time using the <see cref="LongDateTime12Format"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a long date and 12-hour time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatLongDateTime12(DateTime? value, string nullValue = null) => value?.ToString(LongDateTime12Format, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable 32-bit signed integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable 32-bit signed integer value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatInteger(int? value, string nullValue = null) => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable 64-bit signed integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable 64-bit signed integer value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatInteger(long? value, string nullValue = null) => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable 16-bit signed integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable 16-bit signed integer value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatInteger(short? value, string nullValue = null) => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable 8-bit unsigned integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable 8-bit unsigned integer value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatInteger(byte? value, string nullValue = null) => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable 32-bit unsigned integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable 32-bit unsigned integer value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatInteger(uint? value, string nullValue = null) => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable 64-bit unsigned integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable 64-bit unsigned integer value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatInteger(ulong? value, string nullValue = null) => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable 16-bit unsigned integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable 16-bit unsigned integer value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatInteger(ushort? value, string nullValue = null) => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable 8-bit signed integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable 8-bit signed integer value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatInteger(sbyte? value, string nullValue = null) => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable double-precision floating-point value as a number using the <see cref="NumberFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable double-precision floating-point value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a number, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatNumber(double? value, string nullValue = null) => value?.ToString(NumberFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable single-precision floating-point value as a number using the <see cref="NumberFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable single-precision floating-point value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a number, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatNumber(float? value, string nullValue = null) => value?.ToString(NumberFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable decimal value as a number using the <see cref="NumberFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable decimal value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a number, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatNumber(decimal? value, string nullValue = null) => value?.ToString(NumberFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable double-precision floating-point value as a currency using the <see cref="CurrencyFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable double-precision floating-point value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a currency, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatCurrency(double? value, string nullValue = null) => value?.ToString(CurrencyFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable single-precision floating-point value as a currency using the <see cref="CurrencyFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable single-precision floating-point value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a currency, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatCurrency(float? value, string nullValue = null) => value?.ToString(CurrencyFormat, CultureInfo) ?? nullValue;

        /// <summary>
        /// Formats the specified nullable decimal value as a currency using the <see cref="CurrencyFormat"/> and <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="value">The nullable decimal value to format.</param>
        /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
        /// <returns>A string representation of the value formatted as a currency, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
        public string FormatCurrency(decimal? value, string nullValue = null) => value?.ToString(CurrencyFormat, CultureInfo) ?? nullValue;

        private static CultureInfo GetDefaultCultureInfo() => CultureInfo.InvariantCulture;
    }
}
