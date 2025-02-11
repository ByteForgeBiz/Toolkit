using System;
using System.Globalization;

namespace ByteForge.Toolkit
{
    public partial class DBAccess
    {
        /// <summary>
        /// Provides methods for converting values to a specified type.
        /// </summary>
        public static class TypeConverter
        {
            /// <summary>
            /// Converts the specified value to the specified type <typeparamref name="T"/>.
            /// </summary>
            /// <typeparam name="T">The type to convert the value to.</typeparam>
            /// <param name="value">The value to convert.</param>
            /// <returns>The converted value of type <typeparamref name="T"/>.</returns>
            public static T ConvertTo<T>(object value)
            {
                // Handle null values
                if (value == null || value == DBNull.Value)
                    return default(T);

                var targetType = typeof(T);
                var sourceType = value.GetType();

                // Handle nullable types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    targetType = Nullable.GetUnderlyingType(targetType);

                // If source and target types are the same, return directly
                if (sourceType == targetType)
                    return (T)value;

                // Handle Enum conversions
                if (targetType.IsEnum)
                    return (T)ConvertToEnum(value, targetType);

                // Handle special case conversions
                if (IsSpecialConversion(sourceType, targetType, value, out T result))
                    return result;

                // Default conversion
                return (T)Convert.ChangeType(value, targetType);
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
            /// Determines if a special conversion is required and performs the conversion if applicable.
            /// </summary>
            /// <typeparam name="T">The type to convert the value to.</typeparam>
            /// <param name="sourceType">The source type of the value.</param>
            /// <param name="targetType">The target type to convert the value to.</param>
            /// <param name="value">The value to convert.</param>
            /// <param name="result">The converted value if the conversion is successful.</param>
            /// <returns>True if a special conversion is performed; otherwise, false.</returns>
            private static bool IsSpecialConversion<T>(Type sourceType, Type targetType, object value, out T result)
            {
                result = default(T);

                // DateTime conversions
                if (targetType == typeof(DateTime) && TryConvertToDateTime(value, out var dateResult))
                {
                    result = (T)(object)dateResult;
                    return true;
                }

                // Guid conversions
                if (targetType == typeof(Guid) && TryConvertToGuid(value, out var guidResult))
                {
                    result = (T)(object)guidResult;
                    return true;
                }

                // TimeSpan conversions
                if (targetType == typeof(TimeSpan) && TryConvertToTimeSpan(value, out var timeSpanResult))
                {
                    result = (T)(object)timeSpanResult;
                    return true;
                }

                // String to numeric conversions with culture handling
                if (IsNumericType(targetType) && value is string && TryConvertToNumeric(value as string, targetType, out var numericResult))
                {
                    result = (T)numericResult;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Tries to convert the specified value to a DateTime.
            /// </summary>
            /// <param name="value">The value to convert.</param>
            /// <param name="result">The converted DateTime value if the conversion is successful.</param>
            /// <returns>True if the conversion is successful; otherwise, false.</returns>
            private static bool TryConvertToDateTime(object value, out DateTime result)
            {
                result = default(DateTime);

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
            /// Tries to convert the specified value to a Guid.
            /// </summary>
            /// <param name="value">The value to convert.</param>
            /// <param name="result">The converted Guid value if the conversion is successful.</param>
            /// <returns>True if the conversion is successful; otherwise, false.</returns>
            private static bool TryConvertToGuid(object value, out Guid result)
            {
                result = default(Guid);

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
            /// Tries to convert the specified value to a TimeSpan.
            /// </summary>
            /// <param name="value">The value to convert.</param>
            /// <param name="result">The converted TimeSpan value if the conversion is successful.</param>
            /// <returns>True if the conversion is successful; otherwise, false.</returns>
            private static bool TryConvertToTimeSpan(object value, out TimeSpan result)
            {
                result = default(TimeSpan);

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
        }
    }
}