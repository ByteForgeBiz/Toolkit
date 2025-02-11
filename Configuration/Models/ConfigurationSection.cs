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

        private static readonly Lazy<Dictionary<Type, Func<string, object>>> 
            typeParsers = new Lazy<Dictionary<Type, Func<string, object>>>(() => 
                new Dictionary<Type, Func<string, object>>
                {
                    { typeof(TimeSpan), value => TimeSpan.Parse(value) },
                    { typeof(Guid), value => Guid.Parse(value) },
                    { typeof(Uri), value => new Uri(value) },
                    { typeof(Version), value => Version.Parse(value) },
                    { typeof(Type), value => Type.GetType(value) },
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
                });

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

                    if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(attr?.DefaultValueProviderName))
                    {
                        var objValue = attr.GetDefaultValue(prop, _sectionName);
                        prop.SetValue(Value, objValue);
                        continue;
                    }

                    if (value != null)
                    {
                        object convertedValue;

                        if (prop.PropertyType.IsEnum)
                            convertedValue = Enum.Parse(prop.PropertyType, value);
                        else if (typeParsers.Value.TryGetValue(prop.PropertyType, out var parser))
                            convertedValue = parser(value);
                        else
                            convertedValue = Convert.ChangeType(value, prop.PropertyType);

                        if (prop.CanWrite)
                            prop.SetValue(Value, convertedValue);
                    }
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