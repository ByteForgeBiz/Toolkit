using ByteForge.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly Dictionary<string, PropertyInfo> columnPropertyMapping;

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
            columnPropertyMapping = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in properties)
            {
                var columnAttribute = property.GetCustomAttribute<CSVColumnAttribute>();
                if (columnAttribute != null)
                {
                    propertyColumnMapping.Add(property, columnAttribute.Name ?? property.Name);
                    columnPropertyMapping.Add(columnAttribute.Name ?? property.Name, property);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSVRecord"/> class with the specified values.
        /// </summary>
        /// <param name="values">A dictionary containing the property values.</param>
        protected CSVRecord(IDictionary<string, object> values) : this()
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
        protected CSVRecord(IDictionary<string, string> values) : this()
        {
            foreach (var property in properties)
            {
                // If we can't write to the property, skip it.
                if (!property.CanWrite ||                                               // If we can't write to the property,
                    !propertyColumnMapping.TryGetValue(property, out var columnName) || // Or if the property isn't mapped to a column,
                    !values.TryGetValue(columnName, out var stringValue))               // Or if we don't have a value for the column,
                    continue;                                                           // Then move on to the next property.

                var targetType = property.PropertyType;

                // Handle nullable types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    if (string.IsNullOrEmpty(stringValue))
                    {
                        property.SetValue(this, null);
                        continue;
                    }
                    targetType = Nullable.GetUnderlyingType(targetType);
                }

                try
                {
                    if (Parser.TryParse(targetType, stringValue, out var value))
                        property.SetValue(this, value);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    throw new ConversionException($@"Error converting property value ({columnName} {{{targetType}}} - ""{stringValue}"").", columnName, stringValue, ex);
                }
            }
        }

        /// <summary>
        /// Converts the current object into a dictionary representation, where the keys are property names and the
        /// values are the corresponding property values.
        /// </summary>
        /// <remarks>The keys in the resulting dictionary are case-insensitive, and their values are
        /// derived from the properties mapped in the <c>propertyColumnMapping</c> field. This method is useful for
        /// scenarios where an object needs to be serialized or inspected dynamically.</remarks>
        /// <returns>A dictionary containing property names as keys and their corresponding values as the dictionary values.</returns>
        public IDictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in propertyColumnMapping)
                dict[kvp.Value] = kvp.Key.GetValue(this);
            return dict;
        }

        /// <inheritdoc/>
        public bool IsValid()
        {
            ValidationErrors.Clear();
            Validate();
            return ValidationErrors.Count == 0;
        }

        /// <inheritdoc/>
        public abstract void Validate();

        /// <inheritdoc/>
        public ValidationErrors ValidationErrors { get; } = new ValidationErrors();
    }
}