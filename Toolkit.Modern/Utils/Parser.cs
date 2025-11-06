using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ByteForge.Toolkit
{
    /*
     *  ___                      
     * | _ \__ _ _ _ ___ ___ _ _ 
     * |  _/ _` | '_(_-</ -_) '_|
     * |_| \__,_|_| /__/\___|_|  
     *                           
     */

    /// <summary>
    /// Provides methods for parsing strings into various types and converting objects back to string representations.
    /// </summary>
    /// <remarks>
    /// The <see cref="Parser"/> offers:
    /// <list type="bullet">
    /// <item><description>Built-in support for primitive types, common framework types (e.g., <see cref="DateTime"/>, <see cref="Guid"/>, <see cref="Uri"/>), and arrays of primitive characters and bytes.</description></item>
    /// <item><description>Enum support via <see cref="Enum.Parse(Type, string)"/>.</description></item>
    /// <item><description>Fallback to JSON (de)serialization (Newtonsoft.Json) for unknown types.</description></item>
    /// <item><description>Custom extensibility using <see cref="RegisterType(Type, Func{string, object}, Func{object, string})"/>.</description></item>
    /// </list>
    /// <para>
    /// Thread Safety: The instance is not thread-safe regarding type registration. If registration and parsing
    /// occur concurrently from multiple threads, external synchronization is required. Read-only usage after
    /// graph initialization is safe.
    /// </para>
    /// </remarks>
    public class Parser : IParser
    {
        #region Fields

        /// <summary>
        /// Stores the lazily created default singleton instance of the <see cref="Parser"/> class.
        /// </summary>
        private static readonly Lazy<Parser> _default = new Lazy<Parser>();

        /// <summary>
        /// Maps concrete <see cref="Type"/> instances to parsing delegates that convert a <see cref="string"/> into an object instance.
        /// </summary>
        /// <remarks>
        /// This dictionary only contains explicitly registered or built-in known types.
        /// When a type is not found here (and is not an enum), JSON deserialization is attempted.
        /// </remarks>
        private readonly Dictionary<Type, Func<string, object>> _typeParsers;

        /// <summary>
        /// Maps concrete <see cref="Type"/> instances to delegates that convert an object instance into its canonical string representation.
        /// </summary>
        /// <remarks>
        /// For unknown types (and non-enum types) not present in this dictionary, JSON serialization is used as a fallback.
        /// </remarks>
        private readonly Dictionary<Type, Func<object, string>> _typeStringifiers;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the default singleton <see cref="IParser"/> instance.
        /// </summary>
        public static IParser Default => _default.Value;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser"/> class with built-in type parsers and stringifiers.
        /// </summary>
        public Parser() : this(CultureInfo.InvariantCulture) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser"/> class with built-in type parsers, stringifiers, and the specified culture settings.
        /// </summary>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> used to parse and format values.<br/>
        /// This culture determines how numbers, dates, and other culture-sensitive data are interpreted and converted.
        /// </param>
        /// <remarks>
        /// The <see cref="Parser"/> class provides functionality to parse strings into strongly-typed objects 
        /// and to convert objects back into their string representations. The parsing and formatting behavior 
        /// is influenced by the specified <paramref name="culture"/>.  Supported types include
        /// primitive types (e.g., <see cref="int"/>, <see cref="double"/>),  common .NET types (e.g., <see
        /// cref="DateTime"/>, <see cref="Guid"/>), and other types such as  <see cref="Uri"/> and <see
        /// cref="Version"/>. For unsupported types, a JSON fallback will be used.
        /// </remarks>
        public Parser(CultureInfo culture)
        {
            Culture = culture ?? CultureInfo.InvariantCulture;
            _typeParsers = new Dictionary<Type, Func<string, object>>
            {
                { typeof(bool), value => BooleanParser.Parse(value) },
                { typeof(byte), value => byte.Parse(value, culture) },
                { typeof(byte[]), value => Convert.FromBase64String(value) },
                { typeof(char), value => value[0] },
                { typeof(char[]), value => value.ToCharArray() },
                { typeof(CultureInfo), value => CultureInfo.GetCultureInfo(value) },
                { typeof(DateTime), value => DateTimeParser.Parse(value) },
                { typeof(DateTimeOffset), value => DateTimeOffset.Parse(value, culture) },
                { typeof(decimal), value => decimal.Parse(value, culture) },
                { typeof(double), value => double.Parse(value, culture) },
                { typeof(float), value => float.Parse(value, culture) },
                { typeof(Guid), value => Guid.Parse(value) },
                { typeof(int), value => int.Parse(value, culture) },
                { typeof(IntPtr), value => new IntPtr(long.Parse(value, culture)) },
                { typeof(long), value => long.Parse(value, culture) },
                { typeof(sbyte), value => sbyte.Parse(value, culture) },
                { typeof(short), value => short.Parse(value, culture) },
                { typeof(string), value => value },
                { typeof(TimeSpan), value => TimeSpan.Parse(value, culture) },
                { typeof(Type), value => Type.GetType(value) ?? throw new TypeLoadException($"Type '{value}' could not be loaded.") },
                { typeof(uint), value => uint.Parse(value, culture) },
                { typeof(UIntPtr), value => new UIntPtr(ulong.Parse(value, culture)) },
                { typeof(ulong), value => ulong.Parse(value, culture) },
                { typeof(Uri), value => new Uri(value) },
                { typeof(ushort), value => ushort.Parse(value, culture) },
                { typeof(Version), value => Version.Parse(value) },
                { typeof(Encoding), value => Encoding.GetEncoding(value) },
            };

            _typeStringifiers = new Dictionary<Type, Func<object, string>>
            {
                { typeof(bool), value => value.ToString()!.ToLowerInvariant() },
                { typeof(byte), value => ((byte)value).ToString(culture) },
                { typeof(byte[]), value => Convert.ToBase64String((byte[])value) },
                { typeof(char), value => value.ToString()! },
                { typeof(char[]), value => new string((char[])value) },
                { typeof(CultureInfo), value => ((CultureInfo)value).Name },
                { typeof(DateTime), value => ((DateTime)value).ToString("o", culture) },
                { typeof(DateTimeOffset), value => ((DateTimeOffset)value).ToString("o", culture) },
                { typeof(decimal), value => ((decimal)value).ToString(culture) },
                { typeof(double), value => ((double)value).ToString(culture) },
                { typeof(float), value => ((float)value).ToString(culture) },
                { typeof(Guid), value => ((Guid)value).ToString("D") },
                { typeof(int), value => ((int)value).ToString(culture) },
                { typeof(IntPtr), value => ((IntPtr)value).ToInt64().ToString(culture) },
                { typeof(long), value => ((long)value).ToString(culture) },
                { typeof(sbyte), value => ((sbyte)value).ToString(culture) },
                { typeof(short), value => ((short)value).ToString(culture) },
                { typeof(string), value => (string)value },
                { typeof(TimeSpan), value => ((TimeSpan)value).ToString("c", culture) },
                { typeof(Type), value => ((Type)value).AssemblyQualifiedName! },
                { typeof(uint), value => ((uint)value).ToString(culture) },
                { typeof(UIntPtr), value => ((UIntPtr)value).ToUInt64().ToString(culture) },
                { typeof(ulong), value => ((ulong)value).ToString(culture) },
                { typeof(Uri), value => ((Uri)value).AbsoluteUri },
                { typeof(ushort), value => ((ushort)value).ToString(culture) },
                { typeof(Version), value => ((Version)value).ToString() },
                { typeof(Encoding), value => ((Encoding)value).WebName },
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the culture information used for formatting and parsing operations.
        /// </summary>
        /// <remarks>
        /// This property determines the culture-specific formatting rules, such as date, time, number, and currency formats.
        /// </remarks>
        public CultureInfo Culture { get; }

        #endregion

        #region IParser Implementation

        /// <inheritdoc />
        bool IParser.IsKnownType(Type type)
        {
            return _typeParsers.ContainsKey(type) && _typeStringifiers.ContainsKey(type);
        }

        /// <inheritdoc />
        /// <remarks>
        /// If <paramref name="type"/> is:
        /// <list type="bullet">
        /// <item><description><see langword="null"/>: returns <see langword="null"/>.</description></item>
        /// <item><description>An enum: uses <see cref="Enum.Parse(Type, string)"/>.</description></item>
        /// <item><description>Registered/known: invokes its custom parser delegate.</description></item>
        /// <item><description>Otherwise: falls back to <see cref="JsonConvert.DeserializeObject(string, Type)"/>.</description></item>
        /// </list>
        /// </remarks>
        object? IParser.Parse(Type? type, string value)
        {
            if (type == null)
                return null;
            if (type.IsEnum)
                return Enum.Parse(type, value);
            if (_typeParsers.TryGetValue(type, out var parser))
                return parser(value);

            return JsonConvert.DeserializeObject(value, type);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/>, <paramref name="parser"/>, or <paramref name="stringfier"/> is <see langword="null"/>.</exception>
        void IParser.RegisterType(Type? type, Func<string, object> parser, Func<object, string> stringfier)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));
            _typeParsers[type] = parser ?? throw new ArgumentNullException(nameof(parser));
            _typeStringifiers[type] = stringfier ?? throw new ArgumentNullException(nameof(stringfier));
        }

        /// <inheritdoc />
        /// <remarks>
        /// If <paramref name="type"/> is an enum, the raw <see cref="object.ToString"/> value is returned.
        /// For unknown types, JSON serialization is used.
        /// </remarks>
        string IParser.Stringify(Type? type, object? value)
        {
            if (value == null || type == null)
                return string.Empty;
            if (type.IsEnum)
                return value.ToString() ?? "";
            if (_typeStringifiers.TryGetValue(type, out var stringifier))
                return stringifier(value);

            return JsonConvert.SerializeObject(value, Formatting.None);
        }

        /// <inheritdoc />
        bool IParser.TryParse(Type? type, string value, out object? result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                result = ((IParser)this).Parse(type, value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generic version of <see cref="IParser.Parse(Type, string)"/>.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <param name="value">String representation.</param>
        /// <returns>Parsed instance of <typeparamref name="T"/>.</returns>
        T? IParser.Parse<T>(string value) where T : default => (T?)((IParser)this).Parse(typeof(T), value);

        /// <summary>
        /// Attempts to parse a string into a value of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Target result type.</typeparam>
        /// <param name="value">Input string.</param>
        /// <param name="result">Parsed value if successful; otherwise default.</param>
        /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
        /// <remarks>
        /// Returns <see langword="false" /> if <paramref name="value"/> is null or empty, or an exception occurs.
        /// </remarks>
        bool IParser.TryParse<T>(string value, out T? result) where T : default
        {
            result = default;
            if (string.IsNullOrEmpty(value))
                return false;

            try
            {
                result = (T?)((IParser)this).Parse(typeof(T), value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Generic version of <see cref="IParser.Stringify(Type, object)"/>.
        /// </summary>
        /// <typeparam name="T">Type to stringify as.</typeparam>
        /// <param name="value">Value to convert.</param>
        /// <returns>String form of <paramref name="value"/>.</returns>
        string IParser.Stringify<T>(object? value) => ((IParser)this).Stringify(typeof(T), value);

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Determines whether the specified type is a known type that can be parsed and/or stringified.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true" /> if the type is a known type; otherwise, <see langword="false" />.</returns>
        public static bool IsKnownType(Type type) => Default.IsKnownType(type);

        /// <summary>
        /// Parses the specified string value into an object of the specified type.
        /// </summary>
        /// <param name="type">The type to parse the string value into.</param>
        /// <param name="value">The string value to parse.</param>
        /// <returns>An object of the specified type.</returns>
        public static object? Parse(Type type, string value) => Default.Parse(type, value);

        /// <summary>
        /// Converts the specified string representation of a value to its equivalent strongly-typed object of the specified type.
        /// </summary>
        /// <param name="value">The string representation of the value to convert.</param>
        /// <typeparam name="T">The type to which the string value should be converted.</typeparam>
        /// <returns>An object of type <typeparamref name="T"/> if the conversion is successful; otherwise, <see langword="null"/>.</returns>
        /// <remarks>
        /// This method attempts to parse the string value into the specified type <typeparamref name="T"/>. 
        /// If the conversion fails, the method returns <see langword="null"/> instead of throwing an exception.
        /// </remarks>
        public static T? Parse<T>(string value) => Parse(typeof(T), value) is T result ? result : default;

        /// <summary>
        /// Parses the specified string value into an object of the specified generic type.
        /// </summary>
        /// <typeparam name="T">The type to parse the string value into.</typeparam>
        /// <param name="value">The string value to parse.</param>
        /// <returns>An object of the specified generic type.</returns>
        // public static T Parse<T>(string value) => (T)Default.Parse(typeof(T), value);

        /// <summary>
        /// Registers a custom parser for the specified type.
        /// </summary>
        /// <param name="type">The type to register the parser for.</param>
        /// <param name="parser">A function that parses a string value into an object of the specified type.</param>
        /// <param name="stringifier">A function that converts an object of the specified type into its string representation.</param>
        public static void RegisterType(Type type,
                                        Func<string, object> parser,
                                        Func<object, string> stringifier) => Default.RegisterType(type, parser, stringifier);

        /// <summary>
        /// Converts an object to its string representation.
        /// </summary>
        /// <param name="value">The object to convert to a string.</param>
        /// <param name="type">The type of the object. If null, the type is inferred from <paramref name="value"/>.</param>
        /// <returns>The string representation of the object.</returns>
        public static string Stringify(object value, Type? type = null) => Default.Stringify(type ?? value?.GetType(), value);

        /// <summary>
        /// Converts an object of the specified generic type to its string representation.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="value">The object to convert to a string.</param>
        /// <returns>The string representation of the object.</returns>
        public static string Stringify<T>(T value) => Default.Stringify(typeof(T), value);

        /// <summary>
        /// Tries to parse the specified string value into an object of the specified type.
        /// </summary>
        /// <param name="type">The type to parse the string value into.</param>
        /// <param name="value">The string value to parse.</param>
        /// <param name="result">When this method returns, contains the parsed value, if the parsing succeeded, or null if the parsing failed.</param>
        /// <returns><see langword="true" /> if the parsing succeeded; otherwise, <see langword="false" />.</returns>
        public static bool TryParse(Type? type, string value, out object? result) => Default.TryParse(type, value, out result);

        /// <summary>
        /// Tries to parse the specified string value into an object of the specified generic type.
        /// </summary>
        /// <typeparam name="T">The type to parse the string value into.</typeparam>
        /// <param name="value">The string value to parse.</param>
        /// <param name="result">When this method returns, contains the parsed value, if the parsing succeeded, or the default value of the type if the parsing failed.</param>
        /// <returns><see langword="true" /> if the parsing succeeded; otherwise, <see langword="false" />.</returns>
        public static bool TryParse<T>(string value, out T? result)
        {
            result = default;
            if (Default.TryParse(typeof(T), value, out var result2))
            {
                result = (T?)result2;
                return true;
            }
            return false;
        }

        #endregion
    }
}
