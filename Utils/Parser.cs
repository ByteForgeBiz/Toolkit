using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// Provides methods for parsing strings into various types.
    /// </summary>
    public class Parser : IParser
    {
        #region Fields

        /// <summary>
        /// Stores the default instance of the <see cref="Parser"/> class.
        /// </summary>
        private static readonly Lazy<Parser> _default = new Lazy<Parser>();

        /// <summary>
        /// Dictionary mapping types to their corresponding parsing functions.
        /// </summary>
        private readonly Dictionary<Type, Func<string, object>> _typeParsers;

        /// <summary>
        /// Dictionary mapping types to their corresponding stringification functions.
        /// </summary>
        private readonly Dictionary<Type, Func<object, string>> _typeStringifiers;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the default instance of the <see cref="Parser"/> class.
        /// </summary>
        public static IParser Default => _default.Value;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser"/> class.
        /// </summary>
        public Parser()
        {
            _typeParsers = new Dictionary<Type, Func<string, object>>
            {
                { typeof(bool), value => BooleanParser.Parse(value) },
                { typeof(byte), value => byte.Parse(value) },
                { typeof(byte[]), value => Convert.FromBase64String(value) },
                { typeof(char), value => value[0] },
                { typeof(char[]), value => value.ToCharArray() },
                { typeof(CultureInfo), value => CultureInfo.GetCultureInfo(value) },
                { typeof(DateTime), value => DateTimeParser.Parse(value) },
                { typeof(DateTimeOffset), value => DateTimeOffset.Parse(value) },
                { typeof(decimal), value => decimal.Parse(value) },
                { typeof(double), value => double.Parse(value) },
                { typeof(float), value => float.Parse(value) },
                { typeof(Guid), value => Guid.Parse(value) },
                { typeof(int), value => int.Parse(value) },
                { typeof(IntPtr), value => new IntPtr(long.Parse(value)) },
                { typeof(long), value => long.Parse(value) },
                { typeof(sbyte), value => sbyte.Parse(value) },
                { typeof(short), value => short.Parse(value) },
                { typeof(string), value => value },
                { typeof(TimeSpan), value => TimeSpan.Parse(value) },
                { typeof(Type), value => Type.GetType(value) },
                { typeof(uint), value => uint.Parse(value) },
                { typeof(UIntPtr), value => new UIntPtr(ulong.Parse(value)) },
                { typeof(ulong), value => ulong.Parse(value) },
                { typeof(Uri), value => new Uri(value) },
                { typeof(ushort), value => ushort.Parse(value) },
                { typeof(Version), value => Version.Parse(value) },
            };

            _typeStringifiers = new Dictionary<Type, Func<object, string>>
            {
                { typeof(bool), value => value.ToString().ToLowerInvariant() },
                { typeof(byte), value => ((byte)value).ToString(CultureInfo.InvariantCulture) },
                { typeof(byte[]), value => Convert.ToBase64String((byte[])value) },
                { typeof(char), value => value.ToString() },
                { typeof(char[]), value => new string((char[])value) },
                { typeof(CultureInfo), value => ((CultureInfo)value).Name },
                { typeof(DateTime), value => ((DateTime)value).ToString("o") },
                { typeof(DateTimeOffset), value => ((DateTimeOffset)value).ToString("o") },
                { typeof(decimal), value => ((decimal)value).ToString(CultureInfo.InvariantCulture) },
                { typeof(double), value => ((double)value).ToString(CultureInfo.InvariantCulture) },
                { typeof(float), value => ((float)value).ToString(CultureInfo.InvariantCulture) },
                { typeof(Guid), value => ((Guid)value).ToString("D") },
                { typeof(int), value => ((int)value).ToString(CultureInfo.InvariantCulture) },
                { typeof(IntPtr), value => ((IntPtr)value).ToInt64().ToString(CultureInfo.InvariantCulture) },
                { typeof(long), value => ((long)value).ToString(CultureInfo.InvariantCulture) },
                { typeof(sbyte), value => ((sbyte)value).ToString(CultureInfo.InvariantCulture) },
                { typeof(short), value => ((short)value).ToString(CultureInfo.InvariantCulture) },
                { typeof(string), value => (string)value },
                { typeof(TimeSpan), value => ((TimeSpan)value).ToString("c") },
                { typeof(Type), value => ((Type)value).AssemblyQualifiedName },
                { typeof(uint), value => ((uint)value).ToString(CultureInfo.InvariantCulture) },
                { typeof(UIntPtr), value => ((UIntPtr)value).ToUInt64().ToString(CultureInfo.InvariantCulture) },
                { typeof(ulong), value => ((ulong)value).ToString(CultureInfo.InvariantCulture) },
                { typeof(Uri), value => ((Uri)value).AbsoluteUri },
                { typeof(ushort), value => ((ushort)value).ToString(CultureInfo.InvariantCulture) },
                { typeof(Version), value => ((Version)value).ToString() },
            };
        }

        #endregion

        #region IParser Implementation

        /// <summary>
        /// Determines whether the specified type is a known type that can be parsed.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true" /> if the type is a known type; otherwise, <see langword="false" />.</returns>
        bool IParser.IsKnownType(Type type)
        {
            return _typeParsers.ContainsKey(type) && _typeStringifiers.ContainsKey(type);
        }

        /// <summary>
        /// Parses the specified string value into an object of the specified type.
        /// </summary>
        /// <param name="type">The type to parse the string value into.</param>
        /// <param name="value">The string value to parse.</param>
        /// <returns>An object of the specified type.</returns>
        object IParser.Parse(Type type, string value)
        {
            if (type == null)
                return null;
            if (type.IsEnum)
                return Enum.Parse(type, value);
            if (_typeParsers.TryGetValue(type, out var parser))
                return parser(value);

            return JsonConvert.DeserializeObject(value, type);
        }

        /// <summary>
        /// Registers a type with its corresponding parser and stringifier functions.
        /// </summary>
        /// <param name="type">The type to register the parser for.</param>
        /// <param name="parser">The parser function to register.</param>
        /// <param name="stringfier">The stringifier function to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when the type, parser, or stringifier is <see langword="null"/>.</exception>
        void IParser.RegisterType(Type type, Func<string, object> parser, Func<object, string> stringfier)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            _typeParsers[type] = parser ?? throw new ArgumentNullException(nameof(parser));
            _typeStringifiers[type] = stringfier ?? throw new ArgumentNullException(nameof(stringfier));
        }

        /// <summary>
        /// Converts an object of the specified type to its string representation.
        /// </summary>
        /// <param name="type">The type of the object.</param>
        /// <param name="value">The object to convert to a string.</param>
        /// <returns>The string representation of the object.</returns>
        string IParser.Stringify(Type type, object value)
        {
            if (value == null || type == null)
                return string.Empty;
            if (type.IsEnum)
                return value.ToString();
            if (_typeStringifiers.TryGetValue(type, out var stringifier))
                return stringifier(value);

            return JsonConvert.SerializeObject(value, Formatting.None);
        }

        /// <summary>
        /// Tries to parse the specified string value into an object of the specified type.
        /// </summary>
        /// <param name="type">The type to parse the string value into.</param>
        /// <param name="value">The string value to parse.</param>
        /// <param name="result">When this method returns, contains the parsed value, if the parsing succeeded, or null if the parsing failed.</param>
        /// <returns><see langword="true" /> if the parsing succeeded; otherwise, <see langword="false" />.</returns>
        bool IParser.TryParse(Type type, string value, out object result)
        {
            try
            {
                result = ((IParser)this).Parse(type, value);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

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
        public static object Parse(Type type, string value) => Default.Parse(type, value);

        /// <summary>
        /// Parses the specified string value into an object of the specified generic type.
        /// </summary>
        /// <typeparam name="T">The type to parse the string value into.</typeparam>
        /// <param name="value">The string value to parse.</param>
        /// <returns>An object of the specified generic type.</returns>
        public static T Parse<T>(string value) => (T)Default.Parse(typeof(T), value);

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
        /// <param name="type">The type of the object. If null, the type is determined from the value.</param>
        /// <returns>The string representation of the object.</returns>
        public static string Stringify(object value, Type type = null) => Default.Stringify(type ?? value?.GetType(), value);

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
        public static bool TryParse(Type type, string value, out object result) => Default.TryParse(type, value, out result);

        /// <summary>
        /// Tries to parse the specified string value into an object of the specified generic type.
        /// </summary>
        /// <typeparam name="T">The type to parse the string value into.</typeparam>
        /// <param name="value">The string value to parse.</param>
        /// <param name="result">When this method returns, contains the parsed value, if the parsing succeeded, or the default value of the type if the parsing failed.</param>
        /// <returns><see langword="true" /> if the parsing succeeded; otherwise, <see langword="false" />.</returns>
        public static bool TryParse<T>(string value, out T result)
        {
            result = default;
            if (Default.TryParse(typeof(T), value, out var result2))
            {
                result = (T)result2;
                return true;
            }
            return false;
        }

        #endregion
    }
}
