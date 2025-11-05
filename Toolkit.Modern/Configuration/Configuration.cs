using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
#if NETFRAMEWORK
using System.Web;
#endif
using ByteForge.Toolkit;

namespace ByteForge.Toolkit
{
    /*
     *   ___           __ _                    _   _          
     *  / __|___ _ _  / _(_)__ _ _  _ _ _ __ _| |_(_)___ _ _  
     * | (__/ _ \ ' \|  _| / _` | || | '_/ _` |  _| / _ \ ' \ 
     *  \___\___/_||_|_| |_\__, |\_,_|_| \__,_|\__|_\___/_||_|
     *                     |___/                              
     */
    /// <summary>
    /// Provides access to configuration settings.
    /// </summary>
    public class Configuration : IConfigurationManager
    {
        // ===========================
        // Private Static Fields
        // ===========================

        /// <summary>
        /// Default instance for static access.
        /// </summary>
        private static IConfigurationManager _defaultInstance;

        /// <summary>
        /// Gets the default configuration instance for static access.
        /// </summary>
        private static IConfigurationManager DefaultInstance => _defaultInstance ??= new Configuration();

        // ===========================
        // Public Static Properties
        // ===========================

        /// <summary>
        /// Gets the globalization information.
        /// </summary>
        public static GlobalizationInfo Globalization => DefaultInstance.Globalization;

        /// <summary>
        /// Gets a value indicating whether the configuration has been initialized.
        /// </summary>
        public static bool IsInitialized => DefaultInstance.IsInitialized;

        // ===========================
        // Public Static Methods
        // ===========================

        /// <summary>
        /// Adds a new section to the configuration.
        /// </summary>
        /// <typeparam name="T">The type of the section to add.</typeparam>
        /// <param name="sectionName">The name of the section. If null, uses the type name.</param>
        /// <returns>The added section instance.</returns>
        public static T AddSection<T>(string sectionName = null) where T : class, new() 
        => DefaultInstance.AddSection<T>(sectionName);

        /// <summary>
        /// Gets a section of the configuration.
        /// </summary>
        /// <typeparam name="T">The type of the section to get.</typeparam>
        /// <param name="sectionName">The name of the section. If null, uses the type name.</param>
        /// <returns>The section instance.</returns>
        public static T GetSection<T>(string sectionName = null) where T : class, new() 
        => DefaultInstance.GetSection<T>(sectionName);

        /// <summary>
        /// Initializes the configuration settings by loading the INI file from the specified path.
        /// </summary>
        /// <param name="path">The path to the INI file.</param>
        public static void Initialize(string path) 
        => DefaultInstance.Initialize(path);

        /// <summary>
        /// Initializes the configuration settings by loading the INI file from the specified directory and file name.
        /// </summary>
        /// <param name="directory">The directory containing the INI file.</param>
        /// <param name="fileName">The INI file name.</param>
        /// <exception cref="ArgumentNullException">Thrown if directory or fileName is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if already initialized.</exception>
        public static void Initialize(string directory, string fileName) 
        => DefaultInstance.Initialize(directory, fileName);

        /// <summary>
        /// Initializes the configuration settings by loading the default INI file.
        /// </summary>
        public static void Initialize() 
        => DefaultInstance.Initialize();

        /// <summary>
        /// Saves the current configuration settings to the INI file.
        /// </summary>
        public static void Save() 
        => DefaultInstance.Save();

        // ===========================
        // Private Instance Fields
        // ===========================

        /// <summary>
        /// Stores configuration sections by name.
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Type, object>> _sections = 
                     new ConcurrentDictionary<string, ConcurrentDictionary<Type, object>>();

        /// <summary>
        /// Stores section names that represent arrays.
        /// </summary>
        private readonly HashSet<string> _arraySectionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Stores section names that represent dictionaries.
        /// </summary>
        private readonly HashSet<string> _dictionarySectionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The directory where the configuration file is located.
        /// </summary>
        private string _configDirectory = null;

        /// <summary>
        /// The configuration file name.
        /// </summary>
        private string _configFile = string.Empty;

        /// <summary>
        /// The root configuration object.
        /// </summary>
        private volatile IConfigurationRoot _root;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Indicates whether this instance was manually initialized.
        /// </summary>
        private bool _manuallyInitialized;

        /// <summary>
        /// Represents globalization settings and information used throughout the application.
        /// </summary>
        private GlobalizationInfo _globalizationInfo;

        /// <summary>
        /// Lock object for thread safety on this instance.
        /// </summary>
        private readonly object _instanceLock = new object();

        /// <summary>
        /// Lock object for static Save method to ensure thread safety.
        /// </summary>
        private static readonly object _staticLock = new object();

        /// <summary>
        /// Reader-writer lock to coordinate configuration access between readers (loading) and writers (saving).
        /// Multiple readers can access concurrently, but writers require exclusive access.
        /// </summary>
        private readonly ReaderWriterLockSlim _configLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Gets the configuration lock for coordinating access between readers and writers.
        /// </summary>
        internal ReaderWriterLockSlim ConfigLock => _configLock;

        // ==========================
        // Public Instance Properties
        // ==========================

        /// <summary>
        /// Gets or sets the directory where the INI file is located.
        /// </summary>
        public string ConfigDirectory
        {
            get
            {
                if (!string.IsNullOrEmpty(_configDirectory))
                    return _configDirectory;

#if NETFRAMEWORK
                if (HttpContext.Current != null)
                    return _configDirectory = HttpContext.Current.Server.MapPath("~/bin");
#endif

                return _configDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        // ===========================
        // Private Instance Properties
        // ===========================

        /// <summary>
        /// Gets the root of the configuration that is built from the INI file.
        /// This property will automatically initialize the configuration if it has not been initialized yet.
        /// </summary>
        private IConfigurationRoot InternalRoot
        {
            get
            {
                if (_root == null)
                {
                    lock (_instanceLock)
                    {
                        if (_root == null) // Double-check pattern
                            ((IConfigurationManager)this).Initialize();
                    }
                }
                return _root;
            }
        }

        // ===========================
        // Private Instance Methods
        // ===========================

        /// <summary>
        /// Gets the appropriate default INI filename based on the application type.
        /// </summary>
        /// <returns>The default INI file name.</returns>
        /// <exception cref="AssemblyException">
        /// Thrown if unable to determine the entry assembly.<br/>
        /// If this exception is ever thrown, look for the Four Horsemen of the .NET Apocalypse: THE END IS NIGH!
        /// </exception>
        private string GetDefaultConfigFile()
        {
            // For web applications, use a standard "local.ini" file
#if NETFRAMEWORK
            if (HttpContext.Current != null)
                return "local.ini";
#endif

            // For desktop/service applications, use the executable name as the INI name
            // This creates a natural association between the app and its config
            var assembly = Assembly.GetEntryAssembly() ?? 
                           Assembly.GetCallingAssembly() ?? 
                           Assembly.GetExecutingAssembly() ??
                           throw new AssemblyException("Unable to determine the entry assembly for configuration file naming.");

            // Assembly.GetEntryAssembly() might return null in certain scenarios:
            // 1. Code running in a non-.NET executable (COM interop)
            // 2. Code running in SQL CLR
            // 3. Code in a dynamically generated assembly
            // So we use a fallback chain to get the most appropriate assembly
            var exeName = Path.GetFileNameWithoutExtension(assembly.Location);
            return $"{exeName}.ini";
        }

        // ===========================
        // Explicit Interface Implementation
        // ===========================

        /// <summary>
        /// Gets a value indicating whether the configuration has been initialized.
        /// </summary>
        bool IConfigurationManager.IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the globalization information for culture-aware formatting.
        /// </summary>
        GlobalizationInfo IConfigurationManager.Globalization
        {
            get
            {
                // Ensure configuration is initialized before accessing globalization info
                if (!_isInitialized)
                    ((IConfigurationManager)this).Initialize();
                return _globalizationInfo ??= GetSection<GlobalizationInfo>("Globalization");
            }
        }

        /// <summary>
        /// Initializes the configuration settings by loading the INI file from the specified path.
        /// </summary>
        /// <param name="path">The full path to the INI file.</param>
        void IConfigurationManager.Initialize(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            ((IConfigurationManager)this).Initialize(directory, fileName);
        }

        /// <summary>
        /// Initializes the configuration settings by loading the INI file from the specified directory and file name.
        /// </summary>
        /// <param name="directory">The directory containing the INI file.</param>
        /// <param name="fileName">The INI file name.</param>
        void IConfigurationManager.Initialize(string directory, string fileName)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            _manuallyInitialized = true;
            _configDirectory = directory;
            _configFile = fileName;
            ((IConfigurationManager)this).Initialize();
        }

        /// <summary>
        /// Initializes the configuration settings by loading the default INI file.
        /// </summary>
        void IConfigurationManager.Initialize()
        {
            // Thread synchronization to prevent multiple initialization attempts
            lock (_instanceLock)
            {
                // Prevent re-initialization which could cause inconsistent state
                if (_isInitialized)
                    throw new InvalidOperationException("The configuration settings have already been initialized.");

                // If not manually initialized and no config file specified, determine the default
                if (!_manuallyInitialized && string.IsNullOrEmpty(_configFile))
                    _configFile = GetDefaultConfigFile();

                // Verify directory exists before attempting to load configuration
                // This provides a more specific error than what would happen at file load
                if (!Directory.Exists(ConfigDirectory))
                    throw new DirectoryNotFoundException($@"The following directory was not found: {ConfigDirectory}");

                // Build the full path and verify the file exists
                var path = Path.Combine(ConfigDirectory, _configFile);
                if (!File.Exists(path))
                    throw new FileNotFoundException($@"The file was not found: {path}");

                // Use Microsoft.Extensions.Configuration to build the configuration
                // This leverages the standard configuration provider model
                var builder = new ConfigurationBuilder()
                    .SetBasePath(ConfigDirectory)
                    .AddIniFile(_configFile);

                // Store the root and mark initialization complete
                _root = builder.Build();
                _isInitialized = true;

                // Initialize globalization info lazily - this avoids overhead when not needed
                // The Lazy<T> pattern defers creation until first access
                _globalizationInfo = ((IConfigurationManager)this).GetSection<GlobalizationInfo>("Globalization");
            }
        }

        /// <summary>
        /// Adds a new section to the configuration.
        /// </summary>
        /// <typeparam name="T">The type of the section to add.</typeparam>
        /// <param name="sectionName">The name of the section. If null, uses the type name.</param>
        /// <returns>The added section instance.</returns>
        T IConfigurationManager.AddSection<T>(string sectionName)
        {
            // If no section name is provided, use the type name as a convention
            sectionName ??= typeof(T).Name;

            // Create a wrapper that binds the configuration section to the concrete type
            // This enables strongly-typed access to configuration values
            var section = new ConfigSection<T>(sectionName, InternalRoot, _arraySectionNames, _dictionarySectionNames, _configLock);

            // Prepare to add to the concurrent dictionary
            if (_sections.TryGetValue(sectionName, out var dic))
            {
                if (!dic.TryAdd(typeof(T), section))
                    throw new InvalidOperationException($"The section '{sectionName}' is already registered with type '{typeof(T).FullName}'.");

                return (T)section.Value;
            }

            // Thread-safe addition to the concurrent dictionary
            // Only add if the key doesn't already exist
            dic = new ConcurrentDictionary<Type, object>();
            if (dic.TryAdd(typeof(T), section) && 
                _sections.TryAdd(sectionName, dic))
                return (T)section.Value;

            // Section with this name already exists - prevent duplicates to avoid confusion
            throw new InvalidOperationException($"The section '{sectionName}' already exists.");
        }

        /// <summary>
        /// Gets a section of the configuration.
        /// </summary>
        /// <typeparam name="T">The type of the section to get.</typeparam>
        /// <param name="sectionName">The name of the section. If null, uses the type name.</param>
        /// <returns>The section instance.</returns>
        T IConfigurationManager.GetSection<T>(string sectionName)
        {
            sectionName ??= typeof(T).Name;
            if (_sections.TryGetValue(sectionName, out var dic) && 
                dic.TryGetValue(typeof(T), out var section))
                    return (T)((IConfigSection<T>)section).Value;
            lock (_instanceLock)
            {
                // Double-check pattern
                if (_sections.TryGetValue(sectionName, out var dic2) &&
                    dic2.TryGetValue(typeof(T), out var section2))
                    return (T)((IConfigSection<T>)section2).Value;

                // Section doesn't exist yet - add it now
                return ((IConfigurationManager)this).AddSection<T>(sectionName);
            }
        }

        /// <summary>
        /// Saves the current configuration settings to the INI file.
        /// </summary>
        /// <remarks>
        /// The method updates the INI file to reflect the current state of the in-memory configuration.<br/>
        /// It preserves the structure and comments of the existing INI file while adding or updating 
        /// sections and keys based on the in-memory configuration.<br/>
        /// Any keys set to <see langword="null"/> in the configuration are omitted from the file.<br/>
        /// </remarks>
        void IConfigurationManager.Save()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("The configuration settings must be initialized before saving.");
            // Locking to ensure thread safety during the save operation
            lock (_staticLock)
            {
                // Use write lock to prevent concurrent reads during configuration modification
                _configLock.EnterWriteLock();
                try
                {
                    ThreadUnsafeSave();
                }
                finally
                {
                    _configLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Saves the current configuration state to the INI file.<br/>
        /// This method is not thread-safe and should not be called concurrently from multiple threads.
        /// </summary>
        /// <remarks>
        /// This method updates the INI file to reflect the current state of the in-memory configuration. 
        /// It preserves the structure and comments of the existing INI file while adding or updating
        /// sections and keys based on the in-memory configuration. New sections and keys are appended as needed, and
        /// keys set to <see langword="null"/> are excluded from the output.  Callers must ensure that no other threads are reading or
        /// writing to the INI file while this method is executing to avoid data corruption or inconsistent
        /// state.</remarks>
        void ThreadUnsafeSave()
        {
            var section = string.Empty;
            var iniData = new List<string>();
            var existingKeys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var existingSections = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var sectionEndPositions = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            var currentPosition = 0;

            // Get the full path to the INI file
            var iniFilePath = Path.Combine(_configDirectory, _configFile);
            
            // Read all lines from the existing file to preserve comments and structure
            var iniLines = File.ReadAllLines(iniFilePath);

            // First, ensure all sections are saved to the configuration root
            // This updates the in-memory configuration with any changes made to section objects
            foreach (var dic in _sections.Values)
                foreach (var s in dic.Values.OfType<IConfigSection>())
                    s.SaveToConfiguration();

            // Process the existing INI file line by line, preserving structure and comments
            foreach (var line in iniLines)
            {
                var trimmedLine = line.Trim();
                currentPosition = iniData.Count;

                // Preserve comments and empty lines as-is
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                {
                    iniData.Add(line);
                    continue;
                }

                // Detect section headers [SectionName]
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    // Extract section name without brackets
                    section = trimmedLine.Trim('[', ']');
                    iniData.Add(line);
                    existingSections.Add(section);
                    // Track where this section ends for later insertion of new keys
                    sectionEndPositions[section] = currentPosition + 1;
                    continue;
                }

                // Skip array and dictionary section values - these are handled specially
                if (_arraySectionNames.Contains(section) || _dictionarySectionNames.Contains(section))
                    continue;

                // Process key-value pairs
                var equalsIndex = trimmedLine.IndexOf('=');
                if (equalsIndex == -1)
                {
                    iniData.Add(line);
                    continue;
                }

                var key = trimmedLine.Substring(0, equalsIndex).Trim();
                var value = trimmedLine.Substring(equalsIndex + 1).Trim();

                // Build the fully qualified configuration key (section:key)
                var configKey = $"{section}:{key}";
                var configValue = _root?.GetSection(configKey).Value;

                if (configValue != null)
                {
                    // Update the value if it changed in memory
                    if (value != configValue)
                        value = configValue;
                }
                else
                {
                    // Skip keys that were set to null (likely default values)
                    continue;
                }

                // Track that we've processed this key
                existingKeys.Add(configKey);
                iniData.Add($"{key}={value}");

                // Update the end position of the current section
                if (!string.IsNullOrEmpty(section))
                    sectionEndPositions[section] = currentPosition;
            }

            // Update the last section's end position
            if (!string.IsNullOrEmpty(section))
                sectionEndPositions[section] = iniData.Count;

            // Process all sections in the configuration root
            // Create defensive copy to avoid "Collection was modified during enumeration" in concurrent scenarios
            var configSections = _root.GetChildren()?.ToArray() ?? Array.Empty<IConfigurationSection>();
            foreach (var configSection in configSections)
            {
                // Add new sections that don't exist in the file yet
                if (!existingSections.Contains(configSection.Key))
                {
                    var newSection = new List<string>
                    {
                        // Add a blank line before new sections for readability
                        string.Empty,
                        $"[{configSection.Key}]"
                    };

                    // Add all keys in this new section
                    // Create defensive copy to avoid concurrent modification issues
                    var configChildren = configSection.GetChildren()?.ToArray() ?? Array.Empty<IConfigurationSection>();
                    foreach (var child in configChildren)
                    {
                        if (!string.IsNullOrEmpty(child.Value))
                            newSection.Add($"{child.Key}={child.Value}");
                    }

                    // Add a blank line after new sections for readability
                    newSection.Add(string.Empty);

                    // Only add the section if it has keys (more than just the section header and blank lines)
                    if (newSection.Count > 3)
                        iniData.AddRange(newSection);
                }
                else
                {
                    // For existing sections, add any new keys not already in the file
                    var insertPosition = sectionEndPositions[configSection.Key];
                    var keysAdded = 0;

                    // Create defensive copy to avoid concurrent modification issues
                    var existingChildren = configSection.GetChildren()?.ToArray() ?? Array.Empty<IConfigurationSection>();
                    foreach (var child in existingChildren)
                    {
                        var configKey = $"{configSection.Key}:{child.Key}";
                        if (!string.IsNullOrEmpty(child.Value) && !existingKeys.Contains(configKey))
                            iniData.Insert(insertPosition + (keysAdded++), $"{child.Key}={child.Value}");
                    }

                    // If we added keys, update the section end positions that come after
                    if (keysAdded > 0)
                    {
                        foreach (var kvp in sectionEndPositions.Where(x => x.Value >= insertPosition).ToList())
                            sectionEndPositions[kvp.Key] += keysAdded;
                    }
                }
            }

            // Write the updated INI data back to the file
            File.WriteAllLines(iniFilePath, iniData);
        }
    }
}