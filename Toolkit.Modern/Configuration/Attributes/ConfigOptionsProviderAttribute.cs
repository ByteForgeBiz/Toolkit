using System;

namespace ByteForge.Toolkit.Configuration
{
    /// <summary>
    /// Declares a provider that supplies selectable configuration values for a reflected property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ConfigOptionsProviderAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigOptionsProviderAttribute"/> class.
        /// </summary>
        /// <param name="providerType">The concrete option provider type.</param>
        public ConfigOptionsProviderAttribute(Type providerType)
        {
            if (providerType == null)
                throw new ArgumentNullException(nameof(providerType));

            if (!typeof(IConfigOptionsProvider).IsAssignableFrom(providerType))
                throw new ArgumentException("The provider type must implement IConfigOptionsProvider.", nameof(providerType));

            ProviderType = providerType;
        }

        /// <summary>
        /// Gets the concrete option provider type.
        /// </summary>
        public Type ProviderType { get; }

        /// <summary>
        /// Creates a provider instance for the attributed property.
        /// </summary>
        /// <returns>The configured option provider instance.</returns>
        public IConfigOptionsProvider CreateProvider()
        {
            return (IConfigOptionsProvider)Activator.CreateInstance(ProviderType);
        }
    }
}
