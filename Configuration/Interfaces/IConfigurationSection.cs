namespace ByteForge.Toolkit
{
    /// <summary>
    /// Represents a configuration section that can be loaded and saved from the configuration root.
    /// </summary>
    internal interface IConfigurationSection
    {
        /// <summary>
        /// Gets the value of the configuration section.
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Loads the configuration values from the configuration root.
        /// </summary>
        void LoadFromConfiguration();

        /// <summary>
        /// Saves the current values to the configuration root.
        /// </summary>
        void SaveToConfiguration();
    }

    /// <summary>
    /// Represents a configuration section with a specific type that can be loaded and saved from the configuration root.
    /// </summary>
    /// <typeparam name="T">The type of the configuration section value.</typeparam>
    internal interface IConfigurationSection<T> : IConfigurationSection
    {
        /// <summary>
        /// Gets the value of the configuration section.
        /// </summary>
        new T Value { get; }
    }
}