using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// Stores mapping of property names to dictionary section names.
        /// </summary>
        private readonly Dictionary<string, string> _dictionaryNames = new Dictionary<string, string>();

        private readonly HashSet<string> _arraySections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _dictionarySections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigSection{T}"/> class.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <param name="root">The root configuration.</param>
        public ConfigSection(string sectionName, IConfigurationRoot root) : this(sectionName, root, null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigSection{T}"/> class.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <param name="root">The root configuration.</param>
        /// <param name="arraySections">Existing array sections to track.</param>
        /// <param name="dictionarySections">Existing dictionary sections to track.</param>
        public ConfigSection(string sectionName, IConfigurationRoot root, HashSet<string> arraySections, HashSet<string> dictionarySections) 
        {
            _root = root;
            _sectionName = sectionName;
            _properties = typeof(T)
                            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(p => p.GetCustomAttributes<IgnoreAttribute>().Any() == false)
                            .ToArray();
            _arraySections = arraySections ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _dictionarySections = dictionarySections ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                var section = _root.GetSection(_sectionName);
                
                // Process each writable property
                foreach (var prop in _properties.Where(p => p.CanWrite))
                {
                    LoadPropertyFromConfiguration(section, prop);
                }
            }
        }

        /// <summary>
        /// Loads a single property from the configuration section.
        /// </summary>
        /// <param name="section">The configuration section containing the property data.</param>
        /// <param name="prop">The property to load.</param>
        private void LoadPropertyFromConfiguration(IConfigurationSection section, PropertyInfo prop)
        {
            var attr = prop.GetCustomAttribute<ConfigNameAttribute>();
            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var name = attr?.Name ?? prop.Name;
            var value = section[name];
            
            // Normalize value
            value = string.IsNullOrWhiteSpace(value) ? null : value?.Trim();

            // Handle special property types
            if (TryLoadArrayProperty(prop, name, value)) return;
            if (TryLoadDictionaryProperty(prop, name, value)) return;
            if (TryLoadDefaultValue(prop, value)) return;
            
            // Load standard property value
            LoadStandardProperty(prop, propType, value);
        }

        /// <summary>
        /// Attempts to load an array property decorated with <see cref="ArrayAttribute"/>.
        /// </summary>
        /// <param name="prop">The property to load.</param>
        /// <param name="name">The property name for configuration lookup.</param>
        /// <param name="value">The raw configuration value.</param>
        /// <returns>True if the property was an array and was loaded; otherwise, false.</returns>
        private bool TryLoadArrayProperty(PropertyInfo prop, string name, string value)
        {
            var arrayAttr = prop.GetCustomAttribute<ArrayAttribute>();
            if (arrayAttr == null) return false;

            var arrName = value ?? arrayAttr.SectionName ?? $"{name}Array";
            
            if (!_arrayNames.ContainsKey(name))
                _arrayNames[name] = arrName;

            var (list, elementType) = CreateListFromPropertyType(prop.PropertyType);
            var arraySection = _root.GetSection(arrName);
            
            foreach (var child in arraySection?.GetChildren())
            {
                var elementValue = child.Value;
                if (string.IsNullOrWhiteSpace(elementValue))
                    break;

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

            SetArrayValue(prop, list);
            return true;
        }

        /// <summary>
        /// Attempts to load a dictionary property decorated with <see cref="DictionaryAttribute"/>.
        /// </summary>
        /// <param name="prop">The property to load.</param>
        /// <param name="name">The property name for configuration lookup.</param>
        /// <param name="value">The raw configuration value.</param>
        /// <returns>True if the property was a dictionary and was loaded; otherwise, false.</returns>
        private bool TryLoadDictionaryProperty(PropertyInfo prop, string name, string value)
        {
            var dictAttr = prop.GetCustomAttribute<DictionaryAttribute>();
            if (dictAttr == null) return false;

            if (!IsSupportedDictionaryType(prop.PropertyType))
            {
                throw new NotSupportedException($"Property '{prop.Name}' with DictionaryAttribute must be of a supported dictionary type (Dictionary<string, string>, IDictionary<string, string>, IReadOnlyDictionary<string, string>, ICollection<KeyValuePair<string, string>>, IEnumerable<KeyValuePair<string, string>>, IReadOnlyCollection<KeyValuePair<string, string>>). Found: {prop.PropertyType.FullName}");
            }

            var dictName = value ?? dictAttr.SectionName ?? $"{name}Dict";
            
            if (!_dictionaryNames.ContainsKey(name))
                _dictionaryNames[name] = dictName;

            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var dictSection = _root.GetSection(dictName);
            
            foreach (var child in dictSection?.GetChildren())
            {
                if (!string.IsNullOrWhiteSpace(child.Key) && !string.IsNullOrWhiteSpace(child.Value))
                {
                    dictionary[child.Key] = child.Value;
                }
            }

            SetDictionaryValue(prop, dictionary);
            return true;
        }

        /// <summary>
        /// Attempts to load a default value for a property with missing or empty configuration.
        /// </summary>
        /// <param name="prop">The property to load.</param>
        /// <param name="value">The raw configuration value.</param>
        /// <returns>True if a default value was applied; otherwise, false.</returns>
        private bool TryLoadDefaultValue(PropertyInfo prop, string value)
        {
            if (!string.IsNullOrEmpty(value)) return false;

            var propValue = prop.GetValue(Value);
            var defaultValue = DefaultValueHelper.ResolveDefaultValue(prop);
            
            if (Equals(propValue, defaultValue) == false)
                prop.SetValue(Value, defaultValue);
            
            return true;
        }

        /// <summary>
        /// Loads a standard property value using the Parser utility.
        /// </summary>
        /// <param name="prop">The property to load.</param>
        /// <param name="propType">The property type.</param>
        /// <param name="value">The raw configuration value.</param>
        private void LoadStandardProperty(PropertyInfo prop, Type propType, string value)
        {
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
                throw new NotSupportedException($"Property type '{propertyType.FullName}' is not a supported array or list type.");

            var listType = typeof(List<>).MakeGenericType(elementType);
            return ((IList)Activator.CreateInstance(listType), elementType);
        }

        /// <summary>
        /// Determines if the property type is a supported dictionary type.
        /// </summary>
        /// <param name="propertyType">The property type to check.</param>
        /// <returns>True if the type is supported; otherwise, false.</returns>
        private bool IsSupportedDictionaryType(Type propertyType)
        {
            if (propertyType == typeof(Dictionary<string, string>))
                return true;

            if (!propertyType.IsGenericType)
                return false;

            var genericDef = propertyType.GetGenericTypeDefinition();
            
            // Dictionary interfaces
            if (genericDef == typeof(IDictionary<,>) || 
                genericDef == typeof(IReadOnlyDictionary<,>))
            {
                var types = propertyType.GetGenericArguments();
                return types.Length == 2 && types[0] == typeof(string) && types[1] == typeof(string);
            }
            
            // KeyValuePair collection interfaces
            if (genericDef == typeof(ICollection<>) || 
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(IReadOnlyCollection<>))
            {
                var types = propertyType.GetGenericArguments();
                if (types.Length == 1)
                {
                    var elementType = types[0];
                    if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                    {
                        var kvpTypes = elementType.GetGenericArguments();
                        return kvpTypes.Length == 2 && kvpTypes[0] == typeof(string) && kvpTypes[1] == typeof(string);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the value of a dictionary property.
        /// </summary>
        /// <param name="prop">The property to set.</param>
        /// <param name="dictionary">The dictionary to assign.</param>
        private void SetDictionaryValue(PropertyInfo prop, Dictionary<string, string> dictionary)
        {
            var propType = prop.PropertyType;
            
            // Only assign if the property type can accept our dictionary
            if (propType.IsAssignableFrom(typeof(Dictionary<string, string>)))
            {
                prop.SetValue(Value, dictionary);
                return;
            }
            
            // We've already validated in IsSupportedDictionaryType that the property has correct generic parameters
            // (<string, string> or KeyValuePair<string, string>), so we can safely set the value
            // if the validation passed
            if (IsSupportedDictionaryType(propType))
            {
                prop.SetValue(Value, dictionary);
                return;
            }

            throw new InvalidOperationException($"Property type '{propType.FullName}' is not a supported dictionary type.");
        }

        /// <summary>
        /// Saves the current values of the section's properties to the configuration root.
        /// </summary>
        /// <remarks>
        /// This method persists all properties of the object that are both readable and writable, 
        /// unless they are marked with the <see cref="DoNotPersistAttribute"/>.
        /// Properties are saved under their corresponding configuration keys, which are determined by the  
        /// <see cref="ConfigNameAttribute"/> if present, or the property name otherwise.
        /// Array properties are stored in a dedicated configuration section, and their entries are
        /// cleared and rewritten during the save operation. Default values, as determined by 
        /// <see cref="DefaultValueHelper.ResolveDefaultValue"/>, are not persisted to the configuration.
        /// </remarks>
        void IConfigSection.SaveToConfiguration()
        {
            lock (_lock)
                ThreadUnsafeSaveToConfiguration();
        }

        /// <summary>
        /// Saves the current object's properties to the configuration storage.
        /// </summary>
        /// <remarks>
        /// This method persists all properties of the object that are both readable and writable, 
        /// unless they are marked with the <see cref="DoNotPersistAttribute"/>.
        /// Properties are saved under their corresponding configuration keys, which are determined by the  
        /// <see cref="ConfigNameAttribute"/> if present, or the property name otherwise.
        /// Array properties are stored in a dedicated configuration section, and their entries are
        /// cleared and rewritten during the save operation. Default values, as determined by 
        /// <see cref="DefaultValueHelper.ResolveDefaultValue"/>, are not persisted to the configuration.<br/>
        /// <b>Note:</b> This method is not thread-safe.<br/>
        /// Ensure that concurrent access is managed appropriately when calling this method.
        /// </remarks>
        void ThreadUnsafeSaveToConfiguration()
        {
            // Only properties that can be read and written will be persisted
            // Unless marked with DoNotPersistAttribute
            foreach (var prop in _properties.Where(p => p.CanRead && p.CanWrite && p.GetCustomAttributes<DoNotPersistAttribute>().Any() == false))
            {
                SavePropertyToConfiguration(prop);
            }
        }

        /// <summary>
        /// Saves a single property to the configuration.
        /// </summary>
        /// <param name="prop">The property to save.</param>
        private void SavePropertyToConfiguration(PropertyInfo prop)
        {
            var attr = prop.GetCustomAttribute<ConfigNameAttribute>();
            var name = attr?.Name ?? prop.Name;
            var propValue = prop.GetValue(Value);

            // Handle special property types
            if (TrySaveArrayProperty(prop, name, propValue)) return;
            if (TrySaveDictionaryProperty(prop, name, propValue)) return;
            if (TrySaveDefaultProperty(prop, name, propValue)) return;
            
            // Save standard property value
            SaveStandardProperty(name, propValue);
        }

        /// <summary>
        /// Attempts to save an array property decorated with ArrayAttribute.
        /// </summary>
        /// <param name="prop">The property to save.</param>
        /// <param name="name">The property name for configuration storage.</param>
        /// <param name="propValue">The property value.</param>
        /// <returns>True if the property was an array and was saved; otherwise, false.</returns>
        private bool TrySaveArrayProperty(PropertyInfo prop, string name, object propValue)
        {
            var arrayAttr = prop.GetCustomAttribute<ArrayAttribute>();
            if (arrayAttr == null) return false;

            var arraySectionName = _arrayNames.ContainsKey(name) ? _arrayNames[name] : arrayAttr.SectionName ?? $"{name}Array";
            _root[_sectionName + ":" + name] = arraySectionName;
            _arraySections.Add(arraySectionName);

            // Clear existing array entries
            ClearSection(arraySectionName);

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

            return true;
        }

        /// <summary>
        /// Attempts to save a dictionary property decorated with DictionaryAttribute.
        /// </summary>
        /// <param name="prop">The property to save.</param>
        /// <param name="name">The property name for configuration storage.</param>
        /// <param name="propValue">The property value.</param>
        /// <returns>True if the property was a dictionary and was saved; otherwise, false.</returns>
        private bool TrySaveDictionaryProperty(PropertyInfo prop, string name, object propValue)
        {
            var dictAttr = prop.GetCustomAttribute<DictionaryAttribute>();
            if (dictAttr == null) return false;

            var dictionarySectionName = _dictionaryNames.ContainsKey(name) ? _dictionaryNames[name] : dictAttr.SectionName ?? $"{name}Dict";
            _root[_sectionName + ":" + name] = dictionarySectionName;
            _dictionarySections.Add(dictionarySectionName);

            // Clear existing dictionary entries
            ClearSection(dictionarySectionName);

            if (propValue is IDictionary<string, string> dictionary)
            {
                foreach (var kvp in dictionary)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value != null)
                    {
                        _root[$"{dictionarySectionName}:{kvp.Key}"] = kvp.Value;
                    }
                }
            }
            else if (propValue is IEnumerable<KeyValuePair<string, string>> keyValuePairs)
            {
                foreach (var kvp in keyValuePairs)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value != null)
                    {
                        _root[$"{dictionarySectionName}:{kvp.Key}"] = kvp.Value;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Attempts to save a property by checking if it equals the default value.
        /// </summary>
        /// <param name="prop">The property to save.</param>
        /// <param name="name">The property name for configuration storage.</param>
        /// <param name="propValue">The property value.</param>
        /// <returns>True if the property had a default value and was handled; otherwise, false.</returns>
        private bool TrySaveDefaultProperty(PropertyInfo prop, string name, object propValue)
        {
            if (!Equals(propValue, DefaultValueHelper.ResolveDefaultValue(prop))) return false;

            // Remove the key if the value is the default (no need to store default values)
            _root[_sectionName + ":" + name] = null;
            return true;
        }

        /// <summary>
        /// Saves a standard property value to the configuration.
        /// </summary>
        /// <param name="name">The property name for configuration storage.</param>
        /// <param name="propValue">The property value.</param>
        private void SaveStandardProperty(string name, object propValue)
        {
            var value = propValue?.ToString();
            
            // Convert DateTime to ISO 8601 format
            if (propValue is DateTime dateTimeValue)
                value = dateTimeValue.ToString("o", CultureInfo.InvariantCulture);
                
            _root[_sectionName + ":" + name] = value;
        }

        /// <summary>
        /// Clears all entries from a configuration section.
        /// </summary>
        /// <param name="sectionName">The section name to clear.</param>
        private void ClearSection(string sectionName)
        {
            var sectionToClear = _root.GetSection(sectionName);
            var keysToRemove = sectionToClear.GetChildren()
                                    .Select(c => c.Path)
                                    .ToList();
            foreach (var key in keysToRemove)
                _root[key] = null;
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