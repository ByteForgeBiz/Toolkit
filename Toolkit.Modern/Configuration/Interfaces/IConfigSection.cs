namespace ByteForge.Toolkit;
/*
 *  ___ ___           __ _      ___         _   _          
 * |_ _/ __|___ _ _  / _(_)__ _/ __| ___ __| |_(_)___ _ _  
 *  | | (__/ _ \ ' \|  _| / _` \__ \/ -_) _|  _| / _ \ ' \ 
 * |___\___\___/_||_|_| |_\__, |___/\___\__|\__|_\___/_||_|
 *                        |___/                            
 */
/// <summary>
/// Represents a configuration section that can be loaded and saved from the configuration root.
/// </summary>
/// <remarks>
/// This interface is the core of ByteForge's configuration system, defining how
/// configuration sections interact with the configuration storage. It provides
/// a non-generic base for all configuration sections with methods to load and save
/// configuration data.
/// <para>
/// Configuration sections are typically created and managed by the <see cref="Configuration"/>
/// class, which serves as the configuration root. Each section represents a named
/// collection of related settings (e.g., "Database", "Logging", etc.).
/// </para>
/// <para>
/// The interface is internal since it's intended for use only within the ByteForge
/// configuration system implementation. External code should use the <see cref="Configuration"/>
/// class to interact with configuration sections.
/// </para>
/// </remarks>
internal interface IConfigSection
{
    /// <summary>
    /// Gets the value of the configuration section.
    /// </summary>
    /// <remarks>
    /// This property provides access to the configuration data represented by this
    /// section. The returned object can be of any type, as determined by the concrete
    /// implementation of the interface.
    /// <para>
    /// For the generic version <see cref="IConfigSection{T}"/>, this property
    /// returns a strongly-typed value.
    /// </para>
    /// </remarks>
    object Value { get; }

    /// <summary>
    /// Loads the configuration values from the configuration root.
    /// </summary>
    /// <remarks>
    /// This method reads values from the underlying configuration storage (typically
    /// a file) and populates the <see cref="Value"/> property with the loaded data.
    /// <para>
    /// The method is called internally by the configuration system when a section
    /// is first accessed or when a reload is requested.
    /// </para>
    /// <para>
    /// Implementations should handle missing sections or properties gracefully,
    /// providing default values when appropriate.
    /// </para>
    /// </remarks>
    void LoadFromConfiguration();

    /// <summary>
    /// Saves the current values to the configuration root.
    /// </summary>
    /// <remarks>
    /// This method writes the current state of the <see cref="Value"/> property
    /// back to the underlying configuration storage.
    /// <para>
    /// The method is called internally by the configuration system when changes
    /// to a section need to be persisted.
    /// </para>
    /// <para>
    /// Implementations should ensure thread-safety when writing to shared
    /// configuration storage.
    /// </para>
    /// </remarks>
    void SaveToConfiguration();
}

/// <summary>
/// Represents a configuration section with a specific type that can be loaded and saved from the configuration root.
/// </summary>
/// <typeparam name="T">The type of the configuration section value.</typeparam>
/// <remarks>
/// This generic interface extends <see cref="IConfigSection"/> to provide
/// strongly-typed access to configuration values. It's particularly useful for
/// providing intellisense and compile-time type checking when working with
/// configuration objects.
/// <para>
/// Implementations of this interface typically map a section of the configuration
/// to a specific class or struct, with properties that represent individual
/// configuration settings.
/// </para>
/// <para>
/// The generic type parameter T is usually a class with properties decorated with
/// <see cref="ConfigNameAttribute"/> to map between configuration keys and
/// class properties.
/// </para>
/// </remarks>
/// <example>
/// This example shows how a configuration section might be implemented and used:
/// <code>
/// // Internal implementation
/// internal class ConfigSection&lt;T&gt; : IConfigurationSection&lt;T&gt; where T : new()
/// {
///     private readonly string _sectionName;
///     private readonly Configuration _configuration;
///     private T _value;
///     
///     public ConfigSection(string sectionName, Configuration configuration)
///     {
///         _sectionName = sectionName;
///         _configuration = configuration;
///         _value = new T();
///     }
///     
///     public T Value => _value;
///     
///     object IConfigurationSection.Value => Value;
///     
///     public void LoadFromConfiguration()
///     {
///         // Implementation to load from configuration storage
///     }
///     
///     public void SaveToConfiguration()
///     {
///         // Implementation to save to configuration storage
///     }
/// }
/// 
/// // External usage (through Configuration class)
/// var dbOptions = Configuration.Default.GetSection&lt;DatabaseOptions&gt;("Database");
/// using (var connection = new SqlConnection(dbOptions.GetConnectionString()))
/// {
///     // Use the configuration values
/// }
/// </code>
/// </example>
internal interface IConfigSection<T> : IConfigSection
{
    /// <summary>
    /// Gets the value of the configuration section.
    /// </summary>
    /// <remarks>
    /// This property provides strongly-typed access to the configuration data
    /// represented by this section. The return type is specified by the generic
    /// type parameter T.
    /// <para>
    /// This property hides the base <see cref="IConfigSection.Value"/>
    /// property and returns the same value but with the specific type T instead
    /// of object.
    /// </para>
    /// <para>
    /// Implementations should ensure that this property never returns null.
    /// Default instances should be created for missing sections.
    /// </para>
    /// </remarks>
    new T Value { get; }
}
