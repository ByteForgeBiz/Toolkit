using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Represents a configuration section that can be loaded and saved from the configuration root.
    /// </summary>
    /// <typeparam name="T">The type of the configuration section.</typeparam>
    internal class ConfigurationSection<T> : IConfigurationSection<T> where T : class, new()
    {
        private readonly string _sectionName;
        private readonly IConfigurationRoot _root;
        private readonly PropertyInfo[] _properties;
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationSection{T}"/> class.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <param name="root">The root configuration.</param>
        public ConfigurationSection(string sectionName, IConfigurationRoot root)
        {
            _sectionName = sectionName;
            _root = root;
            Value = new T();
            _properties = typeof(T)
                            .GetProperties(BindingFlags.Public | BindingFlags.Instance);
            ((IConfigurationSection)this).LoadFromConfiguration();
        }

        /// <summary>
        /// Loads the configuration values from the configuration root.
        /// </summary>
        void IConfigurationSection.LoadFromConfiguration()
        {
            lock (_lock)
            {
                var section = _root.GetSection(_sectionName);
                foreach (var prop in _properties)
                {
                    var attr = prop.GetCustomAttribute<PropertyNameAttribute>();
                    var name = attr?.Name ?? prop.Name;
                    var value = section[name];
                    var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                    if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(attr?.DefaultValueProviderName))
                    {
                        var objValue = attr.GetDefaultValue(prop, _sectionName);
                        prop.SetValue(Value, objValue);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(value) && prop.CanWrite)
                        prop.SetValue(Value, Parser.Parse(propType, value));
                }
            }
        }

        /// <summary>
        /// Saves the current values to the configuration root.
        /// </summary>
        void IConfigurationSection.SaveToConfiguration()
        {
            lock (_lock)
            {
                foreach (var prop in _properties.Where(p => p.GetCustomAttributes<DoNotPersistAttribute>().Any() == false))
                {
                    var nameAttribute = prop.GetCustomAttribute<PropertyNameAttribute>();
                    var name = nameAttribute?.Name ?? prop.Name;
                    var value = prop.GetValue(Value)?.ToString();
                    _root[_sectionName + ":" + name] = value;
                }
            }
        }

        /// <summary>
        /// Converts the configuration section to a dictionary.
        /// </summary>
        /// <returns>A dictionary containing the configuration section's properties and their values.</returns>
        public IDictionary<string, string> AsDictionary()
        {
            var dic = new Dictionary<string, string>();
            foreach (var prop in _properties)
            {
                var nameAttribute = prop.GetCustomAttribute<PropertyNameAttribute>();
                var name = nameAttribute?.Name ?? prop.Name;
                var value = prop.GetValue(Value)?.ToString();
                dic.Add(name, value);
            }
            return dic;
        }

        /// <summary>
        /// Gets the value of the configuration section.
        /// </summary>
        public T Value { get; }

        object IConfigurationSection.Value => (object)Value;
    }
}