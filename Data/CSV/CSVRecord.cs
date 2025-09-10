using ByteForge.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ByteForge.Toolkit
{
    /*
     *   ___ _____   _____                   _ 
     *  / __/ __\ \ / / _ \___ __ ___ _ _ __| |
     * | (__\__ \\ V /|   / -_) _/ _ \ '_/ _` |
     *  \___|___/ \_/ |_|_\___\__\___/_| \__,_|
     *                                         
     */
    /// <summary>
    /// Represents the base class for records with property and column mapping.
    /// </summary>
    public abstract class CSVRecord : ICSVRecord
    {
        private readonly PropertyInfo[] properties;
        private readonly Dictionary<PropertyInfo, string> propertyColumnMapping;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSVRecord"/> class.
        /// </summary>
        protected CSVRecord()
        {
            // Initialize the properties and column mapping.
            properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                .Where(p => p.GetCustomAttributes(typeof(CSVColumnAttribute)).Any())
                .ToArray();
            propertyColumnMapping = new Dictionary<PropertyInfo, string>();
            foreach (var property in properties)
            {
                var columnAttribute = property.GetCustomAttribute<CSVColumnAttribute>();
                if (columnAttribute != null)
                    propertyColumnMapping.Add(property, columnAttribute.Name ?? property.Name);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSVRecord"/> class with the specified values.
        /// </summary>
        /// <param name="values">A dictionary containing the property values.</param>
        protected CSVRecord(Dictionary<string, object> values) : this()
        {
            // Set the property values from the dictionary.
            foreach (var property in properties)
                if (property.CanWrite &&                                                // Can we set the property?
                    propertyColumnMapping.TryGetValue(property, out var columnName) &&  // Has the property been mapped to a column?
                    values.TryGetValue(columnName, out var value))                      // Do we have a value for the column?
                    property.SetValue(this, value);                                     // Then set the property value.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSVRecord"/> class with the specified values.
        /// </summary>
        /// <param name="values">A dictionary containing the property values as strings.</param>
        /// <remarks>
        /// This class will attempt to convert the string values to the appropriate property types.
        /// </remarks>
        /// <exception cref="ConversionException">A property value cannot be converted to the property type.</exception>
        protected CSVRecord(Dictionary<string, string> values) : this()
        {
            foreach (var property in properties)
            {
                // If we can't write to the property, skip it.
                if (!property.CanWrite ||                                               // If we can't write to the property,
                    !propertyColumnMapping.TryGetValue(property, out var columnName) || // Or if the property isn't mapped to a column,
                    !values.TryGetValue(columnName, out var value))                     // Or if we don't have a value for the column,
                    continue;                                                           // Then move on to the next property.

                var targetType = property.PropertyType;

                // Handle nullable types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        property.SetValue(this, null);
                        continue;
                    }
                    targetType = Nullable.GetUnderlyingType(targetType);
                }

                try
                {
                    property.SetValue(this, Parser.Parse(targetType, value));
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    throw new ConversionException($@"Error converting property value ({columnName} {{{targetType}}} - ""{value}"").", columnName, value, ex);
                }
            }
        }

        /// <summary>
        /// Determines whether the record is valid.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the record is valid; otherwise, <see langword="false" />.
        /// </returns>
        public virtual bool IsValid()
        {
            ValidationErrors.Clear();
            return true;
        }

        /// <summary>
        /// Gets the validation errors for the record.
        /// </summary>
        public ValidationErrors ValidationErrors { get; } = new ValidationErrors();
    }
}