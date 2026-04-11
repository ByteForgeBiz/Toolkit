namespace ByteForge.Toolkit.Configuration;
/// <summary>
/// Interface for configuration management providing access to INI-based configuration with strongly-typed sections.
/// </summary>
/// <remarks>
/// This interface defines the contract for configuration management, enabling both static convenience methods
/// and instance-based testing scenarios. Implementations should support INI file loading, typed section access,
/// array/collection handling, and thread-safe operations.
/// </remarks>
public interface IConfigurationManager
{
    /// <summary>
    /// Gets a value indicating whether the configuration has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets the globalization information for culture-aware formatting.
    /// </summary>
    GlobalizationInfo Globalization { get; }

    /// <summary>
    /// Initializes the configuration settings by loading the INI file from the specified path.
    /// </summary>
    /// <param name="path">The full path to the INI file.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if path is null or empty.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown if already initialized.</exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">Thrown if the directory does not exist.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown if the file does not exist.</exception>
    void Initialize(string path);

    /// <summary>
    /// Initializes the configuration settings by loading the INI file from the specified directory and file name.
    /// </summary>
    /// <param name="directory">The directory containing the INI file.</param>
    /// <param name="fileName">The INI file name.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if directory or fileName is null or empty.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown if already initialized.</exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">Thrown if the directory does not exist.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown if the file does not exist.</exception>
    void Initialize(string directory, string fileName);

    /// <summary>
    /// Initializes the configuration settings by loading the default INI file.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown if already initialized.</exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">Thrown if the configuration directory does not exist.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown if the configuration file does not exist.</exception>
    void Initialize();

    /// <summary>
    /// Adds a new section to the configuration.
    /// </summary>
    /// <typeparam name="T">The type of the section to add.</typeparam>
    /// <param name="sectionName">The name of the section. If null, uses the type name.</param>
    /// <returns>The added section instance.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown if the section already exists.</exception>
    T AddSection<T>(string sectionName  = "") where T : class, new();

    /// <summary>
    /// Gets a section of the configuration.
    /// </summary>
    /// <typeparam name="T">The type of the section to get.</typeparam>
    /// <param name="sectionName">The name of the section. If null, uses the type name.</param>
    /// <returns>The section instance.</returns>
    T GetSection<T>(string sectionName  = "") where T : class, new();

    /// <summary>
    /// Gets a string value from the specified section and key.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="key">The key name.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>The string value, or the default value if not found.</returns>
    string? GetString(string section, string key, string? defaultValue = null);

    /// <summary>
    /// Sets a string value in the specified section and key.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="key">The key name.</param>
    /// <param name="value">The value to set.</param>
    void SetString(string section, string key, string? value);

    /// <summary>
    /// Gets an integer value from the specified section and key.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="key">The key name.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>The integer value, or the default value if not found.</returns>
    int GetInt(string section, string key, int defaultValue = 0);

    /// <summary>
    /// Gets a boolean value from the specified section and key.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="key">The key name.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>The boolean value, or the default value if not found.</returns>
    bool GetBool(string section, string key, bool defaultValue = false);

    /// <summary>
    /// Retrieves the value associated with the specified section and key, or returns a default value if the key is not found.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="section">The name of the section containing the key.</param>
    /// <param name="key">The key whose value to retrieve.</param>
    /// <param name="defaultValue">The value to return if the specified key does not exist.</param>
    /// <returns>The value associated with the specified section and key if found; otherwise, the specified default value.</returns>
    T GetValue<T>(string section, string key, T defaultValue);

    /// <summary>
    /// Registers a key as encrypted so that <see cref="GetString"/>, <see cref="SetString"/>,
    /// and <see cref="GetSectionValues"/> automatically decrypt and encrypt its value.
    /// Use this for keys that do not follow the <c>es</c>-prefix naming convention.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="key">The key name within the section.</param>
    void RegisterEncrypted(string section, string key);

    /// <summary>
    /// Registers a key as encrypted so that <see cref="GetString"/>, <see cref="SetString"/>,
    /// and <see cref="GetSectionValues"/> automatically decrypt and encrypt its value.
    /// Use this for keys that do not follow the <c>es</c>-prefix naming convention.
    /// </summary>
    /// <param name="sectionKey">The fully qualified key in <c>Section:Key</c> format.</param>
    void RegisterEncrypted(string sectionKey);

    /// <summary>
    /// Returns the names of all sections present in the INI file.
    /// Internal storage sections (array/dictionary properties) are excluded.
    /// Returns an empty sequence if the configuration has not been initialized.
    /// </summary>
    IEnumerable<string> GetSectionNames();

    /// <summary>
    /// Returns all key-value pairs in the specified section as raw strings.
    /// Returns an empty dictionary if the section does not exist or the configuration
    /// has not been initialized.
    /// </summary>
    /// <param name="sectionName">The section name (case-insensitive).</param>
    IReadOnlyDictionary<string, string> GetSectionValues(string sectionName);

    /// <summary>
    /// Saves the current configuration settings to the INI file.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown if not initialized.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown if the file is read-only or access is denied.</exception>
    void Save();
}
