using System;
using System.Collections.Generic;
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
    public class Parser
    {
        private readonly Dictionary<Type, Func<string, object>> _typeParsers;

        /// <summary>
        /// Gets the default instance of the <see cref="Parser"/> class.
        /// </summary>
        public static Parser Default => _defaultInstance.Value;

        private static readonly Lazy<Parser> _defaultInstance = new Lazy<Parser>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser"/> class.
        /// </summary>
        public Parser()
        {
            _typeParsers = new Dictionary<Type, Func<string, object>>
            {
                { typeof(TimeSpan), value => TimeSpan.Parse(value) },
                { typeof(Guid), value => Guid.Parse(value) },
                { typeof(Uri), value => new Uri(value) },
                { typeof(Version), value => Version.Parse(value) },
                { typeof(Type), value => Type.GetType(value) },
                { typeof(bool), value => BooleanParser.Parse(value) },
                { typeof(byte[]), value => Convert.FromBase64String(value) },
                { typeof(char), value => value[0] },
                { typeof(char[]), value => value.ToCharArray() },
                { typeof(DateTime), value => DateTimeParser.Parse(value) },
                { typeof(DateTimeOffset), value => DateTimeOffset.Parse(value) },
                { typeof(decimal), value => decimal.Parse(value) },
                { typeof(double), value => double.Parse(value) },
                { typeof(float), value => float.Parse(value) },
                { typeof(int), value => int.Parse(value) },
                { typeof(long), value => long.Parse(value) },
                { typeof(short), value => short.Parse(value) },
                { typeof(string), value => value },
                { typeof(uint), value => uint.Parse(value) },
                { typeof(ulong), value => ulong.Parse(value) },
                { typeof(ushort), value => ushort.Parse(value) },
                { typeof(CultureInfo), value => CultureInfo.GetCultureInfo(value) },
            };
        }

        /// <summary>
        /// Parses the specified string value into an object of the specified type.
        /// </summary>
        /// <param name="type">The type to parse the string value into.</param>
        /// <param name="value">The string value to parse.</param>
        /// <returns>An object of the specified type.</returns>
        public object ParseValue(Type type, string value)
        {
            if (type == null)
                return null;
            if (type.IsEnum)
                return Enum.Parse(type, value);
            if (_typeParsers.TryGetValue(type, out var parser))
                return parser(value);
            return Convert.ChangeType(value, type);
        }

        /// <summary>
        /// Parses the specified string value into an object of the specified generic type.
        /// </summary>
        /// <typeparam name="T">The type to parse the string value into.</typeparam>
        /// <param name="value">The string value to parse.</param>
        /// <returns>An object of the specified generic type.</returns>
        public T ParseValue<T>(string value)
        {
            return (T)ParseValue(typeof(T), value);
        }

        /// <summary>
        /// Tries to parse the specified string value into an object of the specified type.
        /// </summary>
        /// <param name="type">The type to parse the string value into.</param>
        /// <param name="value">The string value to parse.</param>
        /// <param name="result">When this method returns, contains the parsed value, if the parsing succeeded, or null if the parsing failed.</param>
        /// <returns><see langword="true" /> if the parsing succeeded; otherwise, <see langword="false" />.</returns>
        public bool TryParseValue(Type type, string value, out object result)
        {
            try
            {
                result = ParseValue(type, value);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to parse the specified string value into an object of the specified generic type.
        /// </summary>
        /// <typeparam name="T">The type to parse the string value into.</typeparam>
        /// <param name="value">The string value to parse.</param>
        /// <param name="result">When this method returns, contains the parsed value, if the parsing succeeded, or the default value of the type if the parsing failed.</param>
        /// <returns><see langword="true" /> if the parsing succeeded; otherwise, <see langword="false" />.</returns>
        public bool TryParseValue<T>(string value, out T result)
        {
            try
            {
                result = ParseValue<T>(value);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        /// <summary>
        /// Registers a custom parser for the specified type.
        /// </summary>
        /// <param name="type">The type to register the parser for.</param>
        /// <param name="parser">The parser function to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when the type or parser is null.</exception>
        public void RegisterTypeParser(Type type, Func<string, object> parser)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            _typeParsers[type] = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        #region Static Methods

        /// <summary>
        /// Determines whether the specified type is a known type that can be parsed.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true" /> if the type is a known type; otherwise, <see langword="false" />.</returns>
        public static bool IsKnownType(Type type)
        {
            return Default._typeParsers.ContainsKey(type);
        }

        /// <summary>
        /// Parses the specified string value into an object of the specified type.
        /// </summary>
        /// <param name="type">The type to parse the string value into.</param>
        /// <param name="value">The string value to parse.</param>
        /// <returns>An object of the specified type.</returns>
        public static object Parse(Type type, string value)
        {
            return Default.ParseValue(type, value);
        }

        /// <summary>
        /// Parses the specified string value into an object of the specified generic type.
        /// </summary>
        /// <typeparam name="T">The type to parse the string value into.</typeparam>
        /// <param name="value">The string value to parse.</param>
        /// <returns>An object of the specified generic type.</returns>
        public static T Parse<T>(string value)
        {
            return Default.ParseValue<T>(value);
        }

        /// <summary>
        /// Tries to parse the specified string value into an object of the specified type.
        /// </summary>
        /// <param name="type">The type to parse the string value into.</param>
        /// <param name="value">The string value to parse.</param>
        /// <param name="result">When this method returns, contains the parsed value, if the parsing succeeded, or null if the parsing failed.</param>
        /// <returns><see langword="true" /> if the parsing succeeded; otherwise, <see langword="false" />.</returns>
        public static bool TryParse(Type type, string value, out object result)
        {
            return Default.TryParseValue(type, value, out result);
        }

        /// <summary>
        /// Tries to parse the specified string value into an object of the specified generic type.
        /// </summary>
        /// <typeparam name="T">The type to parse the string value into.</typeparam>
        /// <param name="value">The string value to parse.</param>
        /// <param name="result">When this method returns, contains the parsed value, if the parsing succeeded, or the default value of the type if the parsing failed.</param>
        /// <returns><see langword="true" /> if the parsing succeeded; otherwise, <see langword="false" />.</returns>
        public static bool TryParse<T>(string value, out T result)
        {
            return Default.TryParseValue<T>(value, out result);
        }

        /// <summary>
        /// Registers a custom parser for the specified type.
        /// </summary>
        /// <param name="type">The type to register the parser for.</param>
        /// <param name="parser">A function that parses a string value into an object of the specified type.</param>
        public static void RegisterParser(Type type, Func<string, object> parser)
        {
            Default.RegisterTypeParser(type, parser);
        }

        #endregion
    }
}
