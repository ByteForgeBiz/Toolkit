using System;
using System.Collections.Generic;
using System.Linq;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Provides enhanced methods to parse string representations into <see cref="bool"/> values,
    /// with support for various common boolean representations beyond the standard "true" and "false".
    /// </summary>
    /// <remarks>
    /// This class extends the functionality of the standard <see cref="bool.Parse"/> method
    /// by recognizing a wider range of string representations for boolean values.
    /// <para>
    /// By default, the following string values are recognized:
    /// <list type="bullet">
    ///   <item>True values: "true", "t", "yes", "y", "1", "on", "enabled"</item>
    ///   <item>False values: "false", "f", "no", "n", "0", "off", "disabled"</item>
    /// </list>
    /// </para>
    /// <para>
    /// The parser is case-insensitive, so "TRUE", "True", and "true" are all recognized as true.
    /// </para>
    /// <para>
    /// Additionally, non-zero integer values are interpreted as true, and zero is interpreted as false.
    /// </para>
    /// <para>
    /// The class also provides methods to register custom string representations for true and false values.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage with static methods
    /// bool result1 = BooleanParser.Parse("yes");        // true
    /// bool result2 = BooleanParser.Parse("OFF");        // false
    /// bool result3 = BooleanParser.Parse("42");         // true (non-zero)
    /// 
    /// // TryParse usage
    /// if (BooleanParser.TryParse("enabled", out bool result4))
    /// {
    ///     // result4 is true
    /// }
    /// 
    /// // Register custom values
    /// BooleanParser.RegisterTrueValue("oui");  // French for "yes"
    /// BooleanParser.RegisterFalseValue("nein"); // German for "no"
    /// 
    /// bool result5 = BooleanParser.Parse("oui");  // true
    /// bool result6 = BooleanParser.Parse("nein"); // false
    /// 
    /// // Or create your own instance with custom values
    /// var customParser = new BooleanParser();
    /// customParser.RegisterTrueValue("ja");  // German for "yes"
    /// customParser.RegisterFalseValue("non"); // French for "no"
    /// bool result7 = customParser.ParseValue("ja");  // true
    /// </code>
    /// </example>
    public class BooleanParser
    {
        private readonly HashSet<string> _trueValues;
        private readonly HashSet<string> _falseValues;
        
        /// <summary>
        /// Gets the default instance of the <see cref="BooleanParser"/> class.
        /// </summary>
        public static BooleanParser Default => _defaultInstance.Value;

        private static readonly Lazy<BooleanParser> _defaultInstance = new Lazy<BooleanParser>(() => new BooleanParser());

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanParser"/> class with default true and false values.
        /// </summary>
        public BooleanParser()
        {
            _trueValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "true",
                "t",
                ".T.", // Clipper-style true
                "yes",
                "y",
                "1",
                "on",
                "enabled"
            };

            _falseValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "false",
                "f",
                ".F.", // Clipper-style false
                "no",
                "n",
                "0",
                "off",
                "disabled"
            };
        }

        /// <summary>
        /// Parses the specified string into a <see cref="bool"/> value.
        /// </summary>
        /// <param name="input">The string to parse.</param>
        /// <returns>
        /// A <see cref="bool"/> value that represents the parsed string:
        /// <list type="bullet">
        ///   <item>True if the input matches any value in the true values collection</item>
        ///   <item>False if the input matches any value in the false values collection</item>
        ///   <item>True if the input can be parsed as a non-zero integer</item>
        ///   <item>False if the input can be parsed as zero</item>
        ///   <item>False if the input is null</item>
        /// </list>
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the input string cannot be parsed into a valid <see cref="bool"/>.
        /// This occurs when the input is not recognized as a boolean value and cannot
        /// be interpreted as a numeric value.
        /// </exception>
        /// <remarks>
        /// This method is similar to <see cref="bool.Parse(string)"/> but recognizes a wider
        /// range of string representations for boolean values.
        /// <para>
        /// The method first trims whitespace from the input and handles null inputs as false.
        /// It then checks if the input matches any known true or false values.
        /// If not, it attempts to parse the input as an integer, treating non-zero values as true.
        /// </para>
        /// <para>
        /// If none of these approaches succeeds, a <see cref="FormatException"/> is thrown.
        /// </para>
        /// </remarks>
        public bool ParseValue(string input)
        {
            input = input?.Trim();

            if (input == null)
                return false;

            if (_trueValues.Contains(input))
                return true;

            if (_falseValues.Contains(input))
                return false;

            // Try parsing as integer
            if (int.TryParse(input, out var numericValue))
                return numericValue != 0;

            throw new FormatException($"String '{input}' was not recognized as a valid boolean.");
        }

        /// <summary>
        /// Tries to parse the specified string into a <see cref="bool"/> value.
        /// </summary>
        /// <param name="input">The string to parse.</param>
        /// <param name="result">
        /// When this method returns, contains the <see cref="bool"/> value equivalent 
        /// to the string contained in <paramref name="input"/>, if the conversion succeeded, or <c>false</c> 
        /// if the conversion failed. The conversion fails if the <paramref name="input"/> parameter is null 
        /// or does not contain a valid string representation of a boolean value.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <paramref name="input"/> parameter was converted successfully; 
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method is similar to <see cref="bool.TryParse(string, out bool)"/> but recognizes
        /// a wider range of string representations for boolean values.
        /// <para>
        /// Unlike <see cref="ParseValue"/>, this method does not throw an exception when the conversion fails.
        /// Instead, it returns <c>false</c> and sets <paramref name="result"/> to <c>false</c>.
        /// </para>
        /// <para>
        /// This method internally calls <see cref="ParseValue"/> and catches any exceptions that might occur.
        /// </para>
        /// </remarks>
        public bool TryParseValue(string input, out bool result)
        {
            try
            {
                result = ParseValue(input);
                return true;
            }
            catch
            {
                result = false;
                return false;
            }
        }

        /// <summary>
        /// Registers a new string value to be recognized as true.
        /// </summary>
        /// <param name="value">The string value to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the value is already registered as false.
        /// </exception>
        /// <remarks>
        /// This method allows you to extend the set of string representations that are recognized
        /// as true. This can be useful for internationalization or supporting domain-specific
        /// boolean representations.
        /// <para>
        /// The value is automatically converted to lowercase and trimmed of whitespace before
        /// being added to the set of recognized true values.
        /// </para>
        /// <para>
        /// If the value is already in the set of recognized true values, this method has no effect.
        /// </para>
        /// <para>
        /// If the value is in the set of recognized false values, an <see cref="ArgumentException"/>
        /// is thrown to prevent ambiguity.
        /// </para>
        /// </remarks>
        public void AddTrueValue(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            value = value.ToLowerInvariant().Trim();

            if (_falseValues.Contains(value))
                throw new ArgumentException($"Value '{value}' is already registered as false.", nameof(value));

            _trueValues.Add(value);
        }

        /// <summary>
        /// Registers a new string value to be recognized as false.
        /// </summary>
        /// <param name="value">The string value to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the value is already registered as true.
        /// </exception>
        /// <remarks>
        /// This method allows you to extend the set of string representations that are recognized
        /// as false. This can be useful for internationalization or supporting domain-specific
        /// boolean representations.
        /// <para>
        /// The value is automatically converted to lowercase and trimmed of whitespace before
        /// being added to the set of recognized false values.
        /// </para>
        /// <para>
        /// If the value is already in the set of recognized false values, this method has no effect.
        /// </para>
        /// <para>
        /// If the value is in the set of recognized true values, an <see cref="ArgumentException"/>
        /// is thrown to prevent ambiguity.
        /// </para>
        /// </remarks>
        public void AddFalseValue(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            value = value.ToLowerInvariant().Trim();

            if (_trueValues.Contains(value))
                throw new ArgumentException($"Value '{value}' is already registered as true.", nameof(value));

            _falseValues.Add(value);
        }

        #region Static Methods

        /// <summary>
        /// Parses the specified string into a <see cref="bool"/> value.
        /// </summary>
        /// <param name="input">The string to parse.</param>
        /// <returns>
        /// A <see cref="bool"/> value that represents the parsed string.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the input string cannot be parsed into a valid <see cref="bool"/>.
        /// </exception>
        /// <remarks>
        /// This static method uses the default BooleanParser instance.
        /// </remarks>
        public static bool Parse(string input)
        {
            return Default.ParseValue(input);
        }

        /// <summary>
        /// Tries to parse the specified string into a <see cref="bool"/> value.
        /// </summary>
        /// <param name="input">The string to parse.</param>
        /// <param name="result">When this method returns, contains the parsed value or default.</param>
        /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This static method uses the default BooleanParser instance.
        /// </remarks>
        public static bool TryParse(string input, out bool result)
        {
            return Default.TryParseValue(input, out result);
        }

        /// <summary>
        /// Registers a new string value to be recognized as true.
        /// </summary>
        /// <param name="value">The string value to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the value is already registered as false.</exception>
        /// <remarks>
        /// This static method uses the default BooleanParser instance.
        /// </remarks>
        public static void RegisterTrueValue(string value)
        {
            Default.AddTrueValue(value);
        }

        /// <summary>
        /// Registers a new string value to be recognized as false.
        /// </summary>
        /// <param name="value">The string value to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the value is already registered as true.</exception>
        /// <remarks>
        /// This static method uses the default BooleanParser instance.
        /// </remarks>
        public static void RegisterFalseValue(string value)
        {
            Default.AddFalseValue(value);
        }

        #endregion
    }
}