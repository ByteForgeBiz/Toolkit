using System;
using System.Reflection;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Specifies a custom name for a property in the configuration section.<br/><br/>
    /// This is useful when the property name in the class does not match the name used in the 
    /// configuration file. <br/>
    /// By applying this attribute to a property, you can map the property 
    /// to a different name in the configuration file.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PropertyNameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyNameAttribute"/> class.
        /// </summary>
        /// <param name="name">The custom name of the property.</param>
        public PropertyNameAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the custom name of the property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the name of the method that provides the default value for the property.
        /// </summary>
        public string DefaultValueProviderName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyNameAttribute"/> class with a custom default value provider.
        /// </summary>
        /// <param name="name">The custom name of the property.</param>
        /// <param name="getDefaultValueFunction">The name of the method that provides the default value for the property.</param>
        public PropertyNameAttribute(string name, string getDefaultValueFunction) : this(name)
        {
            DefaultValueProviderName = getDefaultValueFunction;
        }

        /// <summary>
        /// Gets the default value for the property using the specified method.
        /// </summary>
        /// <param name="property">The property for which to get the default value.</param>
        /// <param name="sectionName">The name of the configuration section.</param>
        /// <returns>The default value for the property.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the default value method is not found or the return type is not assignable to the property type.
        /// </exception>
        public object GetDefaultValue(PropertyInfo property, string sectionName)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (string.IsNullOrWhiteSpace(sectionName)) throw new ArgumentNullException(nameof(sectionName));

            var declaringType = property.DeclaringType;
            var method = declaringType.GetMethod(DefaultValueProviderName, BindingFlags.IgnoreCase |
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                throw new InvalidOperationException($"Default value method '{DefaultValueProviderName}' not found in type {declaringType.FullName}.");

            var propName = $"{sectionName}:{Name}";

            if (property.PropertyType.IsAssignableFrom(method.ReturnType))
                return method.Invoke(null, new object[] { propName });
            else
                throw new InvalidOperationException(
                    $"Invalid return type for property '{propName}'. Method {method.DeclaringType.FullName}.{method.Name} " +
                    $"returns {method.ReturnType.FullName} which cannot be assigned to {property.PropertyType.FullName}.");
        }
    }
}