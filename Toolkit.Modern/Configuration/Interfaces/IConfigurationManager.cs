using Microsoft.Extensions.Configuration;

namespace ByteForge.Toolkit
{
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
        T AddSection<T>(string sectionName = null) where T : class, new();

        /// <summary>
        /// Gets a section of the configuration.
        /// </summary>
        /// <typeparam name="T">The type of the section to get.</typeparam>
        /// <param name="sectionName">The name of the section. If null, uses the type name.</param>
        /// <returns>The section instance.</returns>
        T GetSection<T>(string sectionName = null) where T : class, new();

        /// <summary>
        /// Saves the current configuration settings to the INI file.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown if not initialized.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Thrown if the file is read-only or access is denied.</exception>
        void Save();
    }
}