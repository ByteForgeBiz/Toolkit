using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ByteForge.Toolkit
{
    /*
     *   ___           __ _      ___         _   _          _________  
     *  / __|___ _ _  / _(_)__ _/ __| ___ __| |_(_)___ _ _ / /_   _\ \ 
     * | (__/ _ \ ' \|  _| / _` \__ \/ -_) _|  _| / _ \ ' < <  | |  > >
     *  \___\___/_||_|_| |_\__, |___/\___\__|\__|_\___/_||_\_\ |_| /_/ 
     *                     |___/                                       
     */
    /// <summary>
    /// Represents a configuration section that can be loaded and saved from the configuration root.
    /// </summary>
    /// <typeparam name="T">The type of the configuration section.</typeparam>
    internal class ConfigSection<T> : IConfigSection<T> where T : class, new()
    {
        /// <summary>
        /// Synchronization lock for thread safety.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The name of the configuration section.
        /// </summary>
        private readonly string _sectionName;

        /// <summary>
        /// The root configuration object.
        /// </summary>
        private readonly IConfigurationRoot _root;

        /// <summary>
        /// The property information for the type <typeparamref name="T"/>.
        /// </summary>
        private readonly PropertyInfo[] _properties;

        /// <summary>
        /// Stores mapping of property names to array section names.
        /// </summary>
        private readonly Dictionary<string, string> _arrayNames = new Dictionary<string, string>();

        private readonly HashSet<string> _arraySections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigSection{T}"/> class.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <param name="root">The root configuration.</param>
        public ConfigSection(string sectionName, IConfigurationRoot root) : this(sectionName, root, null) { }

        public ConfigSection(string sectionName, IConfigurationRoot root, HashSet<string> arraySections) 
        {
            _root = root;
            _sectionName = sectionName;
            _properties = typeof(T)
                            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(p => p.GetCustomAttributes<IgnoreAttribute>().Any() == false)
                            .ToArray();
            _arraySections = arraySections ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Value = new T();
            ((IConfigSection)this).LoadFromConfiguration();
        }

        /// <summary>
        /// Loads the configuration values from the configuration root and populates the section's properties.
        /// </summary>
        void IConfigSection.LoadFromConfiguration()
        {
            // Thread-safety lock ensures only one thread can modify the configuration at a time
            lock (_lock)
            {
                // Get the specified section from the configuration root
                // This creates a virtual view into the configuration data - not a copy
                var section = _root.GetSection(_sectionName);
                
                // Iterate through all properties that weren't decorated with IgnoreAttribute
                // Skip properties that are read-only since we can't set their values
                foreach (var prop in _properties.Where(p=>p.CanWrite))
                {
                    // Check for custom configuration name through ConfigNameAttribute
                    // This allows for different property names in code versus config files
                    var attr = prop.GetCustomAttribute<ConfigNameAttribute>();
                    
                    // Handle nullable types by getting the underlying type if needed
                    // This allows proper parsing of nullable value types like int?, double?, etc.
                    var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    
                    // Use the attribute name if provided, otherwise use the property name
                    var name = attr?.Name ?? prop.Name;
                    
                    // Get the raw string value from the configuration
                    var value = section[name];
                    
                    // Normalize value by trimming whitespace and converting empty strings to null
                    // This helps with consistent parsing behavior
                    value = string.IsNullOrWhiteSpace(value) ? null : value?.Trim();

                    // SPECIAL CASE: Array/Collection properties handling
                    // Properties decorated with ArrayAttribute are stored differently in config
                    var arrayAttr = prop.GetCustomAttribute<ArrayAttribute>();
                    if (arrayAttr != null)
                    {
                        // Determine the array section name through a cascading priority:
                        // 1. Use the value from the config (if present)
                        // 2. Use the SectionName from the attribute (if specified)
                        // 3. Fall back to a default naming convention based on property name
                        var arrName = value ?? arrayAttr.SectionName ?? $"{name}Array";
                        
                        // Cache the array section name for later use during saving
                        if (!_arrayNames.ContainsKey(name))
                            _arrayNames[name] = arrName;

                        // Create appropriate collection instance based on property type
                        (var list, var elementType) = CreateListFromPropertyType(prop.PropertyType);
                        
                        // Get the array-specific section from configuration
                        var arraySection = _root.GetSection(arrName);
                        
                        // Populate the list with values from the array section
                        // Each child of the section represents one array element
                        foreach (var child in arraySection?.GetChildren())
                        {
                            var elementValue = child.Value;
                            // Stop processing if we encounter an empty element
                            if (string.IsNullOrWhiteSpace(elementValue))
                                break;

                            // Use the Parser utility to convert string to the correct element type
                            try
                            {
                                var element = Parser.Parse(elementType, elementValue);
                                list.Add(element);
                            }
                            catch (Exception ex)
                            {
                                throw new FormatException($"Failed to parse array element '{elementValue}' for property '{prop.Name}' of type '{elementType.FullName}'.", ex);
                            }
                        }

                        // Set the property value with the appropriate array/collection type
                        SetArrayValue(prop, list);
                        continue;
                    }

                    // IMPORTANT CASE: Handle missing or empty configuration values
                    if (string.IsNullOrEmpty(value))
                    {
                        // Get the current property value
                        var propValue = prop.GetValue(Value);
                        
                        // Resolve the default value for this property based on attributes or type
                        var defaultValue = DefaultValueHelper.ResolveDefaultValue(prop);
                        
                        // Only reset to default if the current value differs from the default
                        // This avoids unnecessary property updates
                        if (Equals(propValue, defaultValue) == false)
                            prop.SetValue(Value, defaultValue);
                        continue;
                    }

                    // STANDARD CASE: Parse the string value to the property's type and set it
                    // The Parser utility handles conversion for various data types
                    // including primitives, DateTime, enums, and custom registered types
                    try
                    {
                        var parsedValue = Parser.Parse(propType, value);
                        prop.SetValue(Value, parsedValue);
                    }
                    catch (Exception ex)
                    {
                        throw new FormatException($"Failed to parse value '{value}' for property '{prop.Name}' of type '{propType.FullName}'.", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the value of an array or list property.
        /// </summary>
        /// <param name="prop">The property to set.</param>
        /// <param name="list">The list of values to assign.</param>
        private void SetArrayValue(PropertyInfo prop, IList list)
        {
            var propType = prop.PropertyType;
            if (propType.IsArray)
            {
                var array = Array.CreateInstance(propType.GetElementType(), list.Count);
                list?.CopyTo(array, 0);
                prop.SetValue(Value, array);
                return;
            }

            var genericDef = propType.IsGenericType ? propType.GetGenericTypeDefinition() : null;
            if (genericDef == typeof(List<>) || 
                genericDef == typeof(IList<>) || 
                genericDef == typeof(ICollection<>) || 
                genericDef == typeof(IEnumerable<>) ||
                propType == typeof(IList) || 
                propType == typeof(ICollection) || 
                propType == typeof(IEnumerable)
                )
            {
                prop.SetValue(Value, list);
                return;
            }

            throw new InvalidOperationException($"Property type '{propType.FullName}' is not a supported array or list type.");
        }

        /// <summary>
        /// Creates a list instance and determines the element type based on the property type.
        /// </summary>
        /// <param name="propertyType">The property type.</param>
        /// <returns>A tuple containing the list instance and the element type.</returns>
        private (IList, Type) CreateListFromPropertyType(Type propertyType)
        {
            Type elementType;
            var genericDef = propertyType.IsGenericType ? propertyType.GetGenericTypeDefinition() : null;
            if (propertyType.IsArray)
                elementType = propertyType.GetElementType();
            else if (genericDef == typeof(List<>) ||
                     genericDef == typeof(IList<>) ||
                     genericDef == typeof(ICollection<>) ||
                     genericDef == typeof(IEnumerable<>))
            {
                var types = propertyType.GetGenericArguments();
                if (types.Length != 1)
                    throw new InvalidOperationException($"Property type '{propertyType.FullName}' has an invalid number of generic arguments. Expected 1.");
                elementType = types[0];
            }
            else if (typeof(IEnumerable).IsAssignableFrom(propertyType))
                elementType = typeof(object);
            else
                throw new InvalidOperationException($"Property type '{propertyType.FullName}' is not a supported array or list type.");

            var listType = typeof(List<>).MakeGenericType(elementType);
            return ((IList)Activator.CreateInstance(listType), elementType);
        }

        /// <summary>
        /// Saves the current values of the section's properties to the configuration root.
        /// </summary>
        void IConfigSection.SaveToConfiguration()
        {
            lock (_lock)
            {
                foreach (var prop in _properties.Where(p => p.CanRead && p.GetCustomAttributes<DoNotPersistAttribute>().Any() == false))
                {
                    var attr = prop.GetCustomAttribute<ConfigNameAttribute>();
                    var name = attr?.Name ?? prop.Name;

                    var propValue = prop.GetValue(Value);

                    var arrayAttr = prop.GetCustomAttribute<ArrayAttribute>();
                    if (arrayAttr != null)
                    {
                        var arraySectionName = _arrayNames.ContainsKey(name) ? _arrayNames[name] : arrayAttr.SectionName ?? $"{name}Array";
                        _root[_sectionName + ":" + name] = arraySectionName;
                        _arraySections.Add(arraySectionName);

                        // Clear existing array entries
                        var sectionToClear = _root.GetSection(arraySectionName);
                        var keysToRemove = sectionToClear.GetChildren()
                                                .Select(c => c.Path)
                                                .ToList();
                        foreach (var key in keysToRemove)
                            _root[key] = null;

                        var i = 0;
                        if (propValue is IEnumerable enumerable)
                        {
                            var num = enumerable.Cast<object>().Count().ToString().Length;
                            foreach (var item in enumerable)
                            {
                                if (item == null) continue;
                                _root[$"{arraySectionName}:{(i++).ToString().PadLeft(num, '0')}"] = item.ToString();
                            }
                        }

                        continue;
                    }

                    if (propValue == DefaultValueHelper.ResolveDefaultValue(prop))
                    {
                        // Remove the key if the value is the default
                        // No need to store default values
                        _root[_sectionName + ":" + name] = null;
                        continue;
                    }

                    var value = propValue?.ToString();
                    // Convert DateTime to ISO 8601 format
                    if (propValue is DateTime dateTimeValue)
                        value = dateTimeValue.ToString("o", CultureInfo.InvariantCulture);
                    _root[_sectionName + ":" + name] = value;
                }
            }
        }

        /// <summary>
        /// Gets the strongly-typed value of the configuration section.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Gets the value of the configuration section as an object.
        /// </summary>
        object IConfigSection.Value => (object)Value;
    }
}