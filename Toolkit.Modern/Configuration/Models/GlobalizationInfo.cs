using System.ComponentModel;
using System.Globalization;

namespace ByteForge.Toolkit.Configuration;
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

    /// <summary>
    /// Gets or sets a value indicating whether UTC offsets are rendered as <c>Z</c>
    /// instead of <c>+00:00</c> in offset-aware formatting methods.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, any <see cref="DateTimeOffset"/> value whose offset is zero
    /// is formatted with a trailing <c>Z</c> rather than <c>+00:00</c>.<br/>
    /// Non-UTC offsets are always rendered as <c>+HH:mm</c> / <c>-HH:mm</c> regardless of this setting.
    /// </remarks>
    [DefaultValue(false)]
    public bool UseZuluTime { get; set; } = false;

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
     *  ___       _      _____ _            ___   __  __         _       ___                   _      
     * |   \ __ _| |_ __|_   _(_)_ __  ___ / _ \ / _|/ _|___ ___| |_    | __|__ _ _ _ __  __ _| |_ ___
     * | |) / _` |  _/ -_)| | | | '  \/ -_) (_) |  _|  _(_-</ -_)  _|   | _/ _ \ '_| '  \/ _` |  _(_-<
     * |___/\__,_|\__\___||_| |_|_|_|_\___|\___/|_| |_| /__/\___|\__|   |_|\___/_| |_|_|_\__,_|\__/__/
     *                                                                                                
     */

    /// <summary>Gets the 24-hour time format string with UTC offset (e.g. <c>14:30:45 -03:00</c>).</summary>
    public string TimeOffsetFormat => TimeFormat + " zzz";

    /// <summary>Gets the short 24-hour time format string with UTC offset (e.g. <c>14:30 -03:00</c>).</summary>
    public string ShortTimeOffsetFormat => ShortTimeFormat + " zzz";

    /// <summary>Gets the long 24-hour time format string with UTC offset (e.g. <c>14:30:45.123 -03:00</c>).</summary>
    public string LongTimeOffsetFormat => LongTimeFormat + " zzz";

    /// <summary>Gets the 12-hour time format string with UTC offset (e.g. <c>02:30:45 PM -03:00</c>).</summary>
    public string Time12OffsetFormat => Time12Format + " zzz";

    /// <summary>Gets the short 12-hour time format string with UTC offset (e.g. <c>2:30 PM -03:00</c>).</summary>
    public string ShortTime12OffsetFormat => ShortTime12Format + " zzz";

    /// <summary>Gets the long 12-hour time format string with UTC offset (e.g. <c>02:30:45.123 PM -03:00</c>).</summary>
    public string LongTime12OffsetFormat => LongTime12Format + " zzz";

    /// <summary>Gets the combined date and 24-hour time with offset format string (e.g. <c>03/09/2023 14:30:45 -03:00</c>).</summary>
    public string DateTimeOffsetFormat => DateFormat + " " + TimeOffsetFormat;

    /// <summary>Gets the combined date and short 24-hour time with offset format string (e.g. <c>03/09/2023 14:30 -03:00</c>).</summary>
    public string ShortDateTimeOffsetFormat => DateFormat + " " + ShortTimeOffsetFormat;

    /// <summary>Gets the combined long date and long 24-hour time with offset format string (e.g. <c>Thursday, March 9, 2023 14:30:45.123 -03:00</c>).</summary>
    public string LongDateTimeOffsetFormat => LongDateFormat + " " + LongTimeOffsetFormat;

    /// <summary>Gets the combined date and short 12-hour time with offset format string (e.g. <c>03/09/2023 2:30 PM -03:00</c>).</summary>
    public string ShortDateTime12OffsetFormat => DateFormat + " " + ShortTime12OffsetFormat;

    /// <summary>Gets the combined long date and long 12-hour time with offset format string (e.g. <c>Thursday, March 9, 2023 02:30:45.123 PM -03:00</c>).</summary>
    public string LongDateTime12OffsetFormat => LongDateFormat + " " + LongTime12OffsetFormat;

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
    public string FormatDate(DateTime? value, string nullValue  = "") => value?.ToString(DateFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable value as a date and time using the <see cref="DateTimeFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a date and time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatDateTime(DateTime? value, string nullValue  = "") => value?.ToString(DateTimeFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable value as a long date using the <see cref="LongDateFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a long date, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatLongDate(DateTime? value, string nullValue  = "") => value?.ToString(LongDateFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable value as a long time using the <see cref="LongTimeFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a long time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatLongTime(DateTime? value, string nullValue  = "") => value?.ToString(LongTimeFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable value as a short time using the <see cref="ShortTimeFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a short time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatShortTime(DateTime? value, string nullValue  = "") => value?.ToString(ShortTimeFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable value as a time using the <see cref="TimeFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatTime(DateTime? value, string nullValue  = "") => value?.ToString(TimeFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable value as a long 12-hour time using the <see cref="LongTime12Format"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a long 12-hour time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatLongTime12(DateTime? value, string nullValue  = "") => value?.ToString(LongTime12Format, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable value as a short 12-hour time using the <see cref="ShortTime12Format"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a short 12-hour time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatShortTime12(DateTime? value, string nullValue  = "") => value?.ToString(ShortTime12Format, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable value as a 12-hour time using the <see cref="Time12Format"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a 12-hour time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatTime12(DateTime? value, string nullValue  = "") => value?.ToString(Time12Format, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable value as a short date and time using the <see cref="ShortDateTimeFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a short date and time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatShortDateTime(DateTime? value, string nullValue  = "") => value?.ToString(ShortDateTimeFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable value as a long date and time using the <see cref="LongDateTimeFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a long date and time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatLongDateTime(DateTime? value, string nullValue  = "") => value?.ToString(LongDateTimeFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable value as a short date and 12-hour time using the <see cref="ShortDateTime12Format"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a short date and 12-hour time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatShortDateTime12(DateTime? value, string nullValue  = "") => value?.ToString(ShortDateTime12Format, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable value as a long date and 12-hour time using the <see cref="LongDateTime12Format"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a long date and 12-hour time, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatLongDateTime12(DateTime? value, string nullValue  = "") => value?.ToString(LongDateTime12Format, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable 32-bit signed integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable 32-bit signed integer value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatInteger(int? value, string nullValue  = "") => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable 64-bit signed integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable 64-bit signed integer value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatInteger(long? value, string nullValue  = "") => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable 16-bit signed integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable 16-bit signed integer value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatInteger(short? value, string nullValue  = "") => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable 8-bit unsigned integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable 8-bit unsigned integer value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatInteger(byte? value, string nullValue  = "") => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable 32-bit unsigned integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable 32-bit unsigned integer value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatInteger(uint? value, string nullValue  = "") => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable 64-bit unsigned integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable 64-bit unsigned integer value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatInteger(ulong? value, string nullValue  = "") => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable 16-bit unsigned integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable 16-bit unsigned integer value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatInteger(ushort? value, string nullValue  = "") => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable 8-bit signed integer value as an integer using the <see cref="IntegerFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable 8-bit signed integer value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as an integer, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatInteger(sbyte? value, string nullValue  = "") => value?.ToString(IntegerFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable double-precision floating-point value as a number using the <see cref="NumberFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable double-precision floating-point value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a number, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatNumber(double? value, string nullValue  = "") => value?.ToString(NumberFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable single-precision floating-point value as a number using the <see cref="NumberFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable single-precision floating-point value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a number, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatNumber(float? value, string nullValue  = "") => value?.ToString(NumberFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable decimal value as a number using the <see cref="NumberFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable decimal value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a number, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatNumber(decimal? value, string nullValue  = "") => value?.ToString(NumberFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable double-precision floating-point value as a currency using the <see cref="CurrencyFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable double-precision floating-point value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a currency, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatCurrency(double? value, string nullValue  = "") => value?.ToString(CurrencyFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable single-precision floating-point value as a currency using the <see cref="CurrencyFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable single-precision floating-point value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a currency, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatCurrency(float? value, string nullValue  = "") => value?.ToString(CurrencyFormat, CultureInfo) ?? nullValue;

    /// <summary>
    /// Formats the specified nullable decimal value as a currency using the <see cref="CurrencyFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable decimal value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>null</c>.</param>
    /// <returns>A string representation of the value formatted as a currency, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatCurrency(decimal? value, string nullValue  = "") => value?.ToString(CurrencyFormat, CultureInfo) ?? nullValue;

    /*
     *  ___       _      _____ _            ___   __  __         _       ___                   _   _   _               __  __     _   _            _    
     * |   \ __ _| |_ __|_   _(_)_ __  ___ / _ \ / _|/ _|___ ___| |_    | __|__ _ _ _ __  __ _| |_| |_(_)_ _  __ _    |  \/  |___| |_| |_  ___  __| |___
     * | |) / _` |  _/ -_)| | | | '  \/ -_) (_) |  _|  _(_-</ -_)  _|   | _/ _ \ '_| '  \/ _` |  _|  _| | ' \/ _` |   | |\/| / -_)  _| ' \/ _ \/ _` (_-<
     * |___/\__,_|\__\___||_| |_|_|_|_\___|\___/|_| |_| /__/\___|\__|   |_|\___/_| |_|_|_\__,_|\__|\__|_|_||_\__, |   |_|  |_\___|\__|_||_\___/\__,_/__/
     *                                                                                                       |___/                                      
     */
    
    /// <summary>
    /// Formats the specified nullable <see cref="DateTimeOffset"/> value as a time with UTC offset using <see cref="TimeOffsetFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>""</c>.</param>
    /// <returns>A formatted string such as <c>14:30:45 -03:00</c>, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatTimeOffset(DateTimeOffset? value, string nullValue = "") => ApplyOffset(value, TimeOffsetFormat, nullValue);

    /// <summary>
    /// Formats the specified nullable <see cref="DateTimeOffset"/> value as a short time with UTC offset using <see cref="ShortTimeOffsetFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>""</c>.</param>
    /// <returns>A formatted string such as <c>14:30 -03:00</c>, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatShortTimeOffset(DateTimeOffset? value, string nullValue = "") => ApplyOffset(value, ShortTimeOffsetFormat, nullValue);

    /// <summary>
    /// Formats the specified nullable <see cref="DateTimeOffset"/> value as a long time with UTC offset using <see cref="LongTimeOffsetFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>""</c>.</param>
    /// <returns>A formatted string such as <c>14:30:45.123 -03:00</c>, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatLongTimeOffset(DateTimeOffset? value, string nullValue = "") => ApplyOffset(value, LongTimeOffsetFormat, nullValue);

    /// <summary>
    /// Formats the specified nullable <see cref="DateTimeOffset"/> value as a 12-hour time with UTC offset using <see cref="Time12OffsetFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>""</c>.</param>
    /// <returns>A formatted string such as <c>02:30:45 PM -03:00</c>, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatTime12Offset(DateTimeOffset? value, string nullValue = "") => ApplyOffset(value, Time12OffsetFormat, nullValue);

    /// <summary>
    /// Formats the specified nullable <see cref="DateTimeOffset"/> value as a short 12-hour time with UTC offset using <see cref="ShortTime12OffsetFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>""</c>.</param>
    /// <returns>A formatted string such as <c>2:30 PM -03:00</c>, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatShortTime12Offset(DateTimeOffset? value, string nullValue = "") => ApplyOffset(value, ShortTime12OffsetFormat, nullValue);

    /// <summary>
    /// Formats the specified nullable <see cref="DateTimeOffset"/> value as a long 12-hour time with UTC offset using <see cref="LongTime12OffsetFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>""</c>.</param>
    /// <returns>A formatted string such as <c>02:30:45.123 PM -03:00</c>, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatLongTime12Offset(DateTimeOffset? value, string nullValue = "") => ApplyOffset(value, LongTime12OffsetFormat, nullValue);

    /// <summary>
    /// Formats the specified nullable <see cref="DateTimeOffset"/> value as a date and 24-hour time with UTC offset using <see cref="DateTimeOffsetFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>""</c>.</param>
    /// <returns>A formatted string such as <c>03/09/2023 14:30:45 -03:00</c>, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatDateTimeOffset(DateTimeOffset? value, string nullValue = "") => ApplyOffset(value, DateTimeOffsetFormat, nullValue);

    /// <summary>
    /// Formats the specified nullable <see cref="DateTimeOffset"/> value as a date and short 24-hour time with UTC offset using <see cref="ShortDateTimeOffsetFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>""</c>.</param>
    /// <returns>A formatted string such as <c>03/09/2023 14:30 -03:00</c>, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatShortDateTimeOffset(DateTimeOffset? value, string nullValue = "") => ApplyOffset(value, ShortDateTimeOffsetFormat, nullValue);

    /// <summary>
    /// Formats the specified nullable <see cref="DateTimeOffset"/> value as a long date and long 24-hour time with UTC offset using <see cref="LongDateTimeOffsetFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>""</c>.</param>
    /// <returns>A formatted string such as <c>Thursday, March 9, 2023 14:30:45.123 -03:00</c>, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatLongDateTimeOffset(DateTimeOffset? value, string nullValue = "") => ApplyOffset(value, LongDateTimeOffsetFormat, nullValue);

    /// <summary>
    /// Formats the specified nullable <see cref="DateTimeOffset"/> value as a date and short 12-hour time with UTC offset using <see cref="ShortDateTime12OffsetFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>""</c>.</param>
    /// <returns>A formatted string such as <c>03/09/2023 2:30 PM -03:00</c>, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatShortDateTime12Offset(DateTimeOffset? value, string nullValue = "") => ApplyOffset(value, ShortDateTime12OffsetFormat, nullValue);

    /// <summary>
    /// Formats the specified nullable <see cref="DateTimeOffset"/> value as a long date and long 12-hour time with UTC offset using <see cref="LongDateTime12OffsetFormat"/> and <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">The nullable value to format.</param>
    /// <param name="nullValue">The value to return if <paramref name="value"/> is <c>null</c>. Defaults to <c>""</c>.</param>
    /// <returns>A formatted string such as <c>Thursday, March 9, 2023 02:30:45.123 PM -03:00</c>, or <paramref name="nullValue"/> if <paramref name="value"/> is <c>null</c>.</returns>
    public string FormatLongDateTime12Offset(DateTimeOffset? value, string nullValue = "") => ApplyOffset(value, LongDateTime12OffsetFormat, nullValue);

    /// <summary>
    /// Formats a <see cref="DateTimeOffset"/> using the given format string and applies the
    /// Zulu-time substitution when <see cref="UseZuluTime"/> is <c>true</c>.
    /// </summary>
    private string ApplyOffset(DateTimeOffset? value, string format, string nullValue)
    {
        if (value is null) return nullValue;

        if (UseZuluTime && value.Value.Offset == TimeSpan.Zero)
            return value.Value.ToString(GetZuluFormat(format), CultureInfo);

        return value.Value.ToString(format, CultureInfo);
    }

    /// <summary>
    /// Returns a copy of <paramref name="format"/> with the first unquoted <c>zzz</c> specifier
    /// replaced by the literal <c>'Z'</c>, so that <see cref="DateTimeOffset.ToString(string, IFormatProvider)"/>
    /// emits a literal <c>Z</c> rather than <c>+00:00</c> for UTC values.
    /// Single-quoted literals and backslash-escaped characters in the format string are skipped correctly.
    /// </summary>
    private static string GetZuluFormat(string format)
    {
        var zzzIndex = -1;
        var inSingleQuote = false;

        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];

            if (c == '\'')
            {
                // Escaped single-quote: '' → literal apostrophe; skip the second quote and stay in current mode.
                if (i + 1 < format.Length && format[i + 1] == '\'')
                {
                    i++;
                    continue;
                }

                inSingleQuote = !inSingleQuote;
                continue;
            }

            if (inSingleQuote)
                continue;

            if (c == '\\')
            {
                i++; // skip the escaped character
                continue;
            }

            if (i + 2 < format.Length && format[i] == 'z' && format[i + 1] == 'z' && format[i + 2] == 'z')
            {
                zzzIndex = i;
                break;
            }
        }

        return zzzIndex >= 0
            ? format.Substring(0, zzzIndex) + "'Z'" + format.Substring(zzzIndex + 3)
            : format;
    }

    private static CultureInfo GetDefaultCultureInfo() => CultureInfo.InvariantCulture;
}
