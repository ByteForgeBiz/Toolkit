using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ByteForge.Toolkit
{
    /*
     *  _____                ___                     _           
     * |_   _|  _ _ __  ___ / __|___ _ ___ _____ _ _| |_ ___ _ _ 
     *   | || || | '_ \/ -_) (__/ _ \ ' \ V / -_) '_|  _/ -_) '_|
     *   |_| \_, | .__/\___|\___\___/_||_\_/\___|_|  \__\___|_|  
     *       |__/|_|                                             
     */
    /// <summary>
    /// Provides methods for converting values to a specified type.
    /// </summary>
    public static class TypeConverter
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<PropertyInfo, string>> _typeCache =
                            new ConcurrentDictionary<Type, Dictionary<PropertyInfo, string>>();

        /// <summary>
        /// Converts a <see cref="DataRow"/> to an instance of type <typeparamref name="T"/>.<br/>
        /// The mapping is based on property names or <see cref="DBColumnAttribute"/> if present.
        /// </summary>
        /// <typeparam name="T">The type to convert the DataRow to. Must be a class with a parameterless constructor.</typeparam>
        /// <param name="row">The <see cref="DataRow"/> to convert.</param>
        /// <returns>An instance of <typeparamref name="T"/> with properties set from the DataRow.</returns>
        public static T ConvertDataRowTo<T>(DataRow row) where T : class, new()
        {
            var obj = new T();
            PopulateObjectFromDataRow(row, obj);
            return obj;
        }

        /// <summary>
        /// Populates the properties of an object with values from a <see cref="DataRow"/>.
        /// </summary>
        /// <param name="row">The <see cref="DataRow"/> containing the data to populate the object. Cannot be <see langword="null"/>.</param>
        /// <param name="obj">The object whose properties will be populated. Cannot be <see langword="null"/>.</param>
        /// <remarks>
        /// This method maps the columns in the <paramref name="row"/> to the properties of the <paramref name="obj"/><br/>
        /// based on the property names or the <see cref="DBColumnAttribute"/> applied to the properties.<br/>  
        /// If a column name matches a property name or the name specified in the <see cref="DBColumnAttribute"/>,<br/>
        /// the property is set to the corresponding value from the <paramref name="row"/>.<br/> 
        /// Only properties with public or non-public instance accessors are considered. 
        /// The method uses a cached mapping of property-to-column names for performance optimization.
        /// </remarks>
        public static void PopulateObjectFromDataRow(DataRow row, object obj)
        {
            if (row == null || obj == null)
                return;

            var objType = obj.GetType();

            if (!_typeCache.TryGetValue(objType, out var propertyColumnMap))
            {
                _typeCache[objType] = propertyColumnMap = objType
                                 .GetProperties(BindingFlags.Public |
                                                BindingFlags.NonPublic |
                                                BindingFlags.Instance)
                                 .Where(p => p.CanWrite)
                                 .ToDictionary(
                                    keySelector: p => p,
                                    elementSelector: p => p.GetCustomAttribute<DBColumnAttribute>()?.Name ?? p.Name
                                 );
            }

            foreach (var kvp in propertyColumnMap)
            {
                var prop = kvp.Key;
                var colName = kvp.Value;

                if (row.Table.Columns.Contains(colName))
                {
                    var value = ConvertTo(prop.PropertyType, row[colName]);
                    prop.SetValue(obj, value);
                }
            }
        }

        /// <summary>
        /// Converts the specified value to the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value of type <typeparamref name="T"/>.</returns>
        public static T ConvertTo<T>(object value) => (T)ConvertTo(typeof(T), value);

        /// <summary>
        /// Converts the specified value to the specified <see cref="Type"/>.
        /// Handles nulls, nullable types, enums, and special cases.
        /// </summary>
        /// <param name="targetType">The type to convert the value to.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value as an object.</returns>
        public static object ConvertTo(Type targetType, object value)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            // Handle null values
            if (value == null || value == DBNull.Value)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            var sourceType = value.GetType();

            // Handle nullable types
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                targetType = Nullable.GetUnderlyingType(targetType);

            // If source and target types are the same, return directly
            if (sourceType == targetType)
                return value;

            // Handle Enum conversions
            if (targetType.IsEnum)
                return ConvertToEnum(value, targetType);

            // Handle special case conversions
            if (TryConvertToType(targetType, value, out var result))
                return result;

            // Default conversion
            return Convert.ChangeType(value, targetType);
        }

        /// <summary>
        /// Attempts to convert the specified value to the given target type using custom logic for DateTime, Guid, TimeSpan, and numeric types.
        /// </summary>
        /// <param name="targetType">The target type to convert to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="result">The converted value if successful; otherwise, null.</param>
        /// <returns>True if conversion succeeded; otherwise, false.</returns>
        private static bool TryConvertToType(Type targetType, object value, out object result)
        {
            result = null;

            // DateTime conversions
            if (targetType == typeof(DateTime) && TryConvertToDateTime(value, out var dateResult))
            {
                result = dateResult;
                return true;
            }

            // Guid conversions
            if (targetType == typeof(Guid) && TryConvertToGuid(value, out var guidResult))
            {
                result = guidResult;
                return true;
            }

            // TimeSpan conversions
            if (targetType == typeof(TimeSpan) && TryConvertToTimeSpan(value, out var timeSpanResult))
            {
                result = timeSpanResult;
                return true;
            }

            // String to numeric conversions with culture handling
            if (IsNumericType(targetType) && value is string && TryConvertToNumeric(value as string, targetType, out var numericResult))
            {
                result = numericResult;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts the specified value to the specified enum type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="enumType">The enum type to convert the value to.</param>
        /// <returns>The converted enum value.</returns>
        /// <exception cref="InvalidCastException">Thrown when the value cannot be converted to the specified enum type.</exception>
        private static object ConvertToEnum(object value, Type enumType)
        {
            // Handle string to enum
            if (value is string stringValue)
                return Enum.Parse(enumType, stringValue, true);

            // Handle numeric to enum
            if (value is IConvertible)
                return Enum.ToObject(enumType, value);

            throw new InvalidCastException($"Cannot convert value of type {value.GetType()} to enum type {enumType}");
        }

        /// <summary>
        /// Tries to convert the specified value to a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="result">The converted <see cref="DateTime"/> value if the conversion is successful.</param>
        /// <returns>True if the conversion is successful; otherwise, false.</returns>
        private static bool TryConvertToDateTime(object value, out DateTime result)
        {
            result = default;

            if (value is string stringValue)
                return DateTime.TryParse(stringValue, out result);

            if (value is double doubleValue)
            {
                try
                {
                    result = DateTime.FromOADate(doubleValue);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to convert the specified value to a <see cref="Guid"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="result">The converted <see cref="Guid"/> value if the conversion is successful.</param>
        /// <returns>True if the conversion is successful; otherwise, false.</returns>
        private static bool TryConvertToGuid(object value, out Guid result)
        {
            result = default;

            if (value is string stringValue)
                return Guid.TryParse(stringValue, out result);

            if (value is byte[] bytes && bytes.Length == 16)
            {
                result = new Guid(bytes);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to convert the specified value to a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="result">The converted <see cref="TimeSpan"/> value if the conversion is successful.</param>
        /// <returns>True if the conversion is successful; otherwise, false.</returns>
        private static bool TryConvertToTimeSpan(object value, out TimeSpan result)
        {
            result = default;

            if (value is string stringValue)
                return TimeSpan.TryParse(stringValue, out result);

            if (value is double doubleValue)
            {
                try
                {
                    result = TimeSpan.FromDays(doubleValue);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to convert the specified string value to a numeric type.
        /// </summary>
        /// <param name="value">The string value to convert.</param>
        /// <param name="targetType">The target numeric type to convert the value to.</param>
        /// <param name="result">The converted numeric value if the conversion is successful.</param>
        /// <returns>True if the conversion is successful; otherwise, false.</returns>
        private static bool TryConvertToNumeric(string value, Type targetType, out object result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(value)) return false;

            try
            {
                // Handle different numeric formats
                value = value.Trim();
                if (value.Contains(",") || value.Contains("."))
                    value = value.Replace(",", ".");

                result = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if the specified type is a numeric type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a numeric type; otherwise, false.</returns>
        private static bool IsNumericType(Type type)
        {
            if (type == null) return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }
    }
}