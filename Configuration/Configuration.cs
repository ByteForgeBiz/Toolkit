using ByteForge.Toolkit.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

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
    public class Configuration
    {
        // ===========================
        // Private Static Fields
        // ===========================

        /// <summary>
        /// Singleton instance of <see cref="Configuration"/>.
        /// </summary>
        private static readonly Lazy<Configuration> _instance = new Lazy<Configuration>();

        /// <summary>
        /// Lazy-loaded globalization info section.
        /// </summary>
        private static readonly Lazy<GlobalizationInfo> _globalizationInfo = new Lazy<GlobalizationInfo>(() => GetSection<GlobalizationInfo>("Globalization"));

        /// <summary>
        /// Lock object for thread safety.
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// Indicates whether the configuration was manually initialized.
        /// </summary>
        private static bool _manuallyInitialized;

        // ===========================
        // Public Static Properties
        // ===========================

        /// <summary>
        /// Gets the globalization information.
        /// </summary>
        public static GlobalizationInfo Globalization => _globalizationInfo.Value;

        /// <summary>
        /// Gets the dynamic object that provides access to the configuration settings.
        /// </summary>
        public static Configuration Obj => _instance.Value;

        /// <summary>
        /// Gets the root of the configuration.
        /// </summary>
        public static IConfigurationRoot Root => Obj.InternalRoot;

        /// <summary>
        /// Gets a value indicating whether the configuration has been initialized.
        /// </summary>
        public static bool IsInitialized { get; private set; }

        // ===========================
        // Public Static Methods
        // ===========================

        /// <summary>
        /// Adds a new section to the configuration.
        /// </summary>
        /// <typeparam name="T">The type of the section to add.</typeparam>
        /// <param name="sectionName">The name of the section. If null, uses the type name.</param>
        /// <returns>The added section instance.</returns>
        public static T AddSection<T>(string sectionName = null) where T : class, new() => Obj.InternalAddSection<T>(sectionName);

        /// <summary>
        /// Gets a section of the configuration.
        /// </summary>
        /// <typeparam name="T">The type of the section to get.</typeparam>
        /// <param name="sectionName">The name of the section. If null, uses the type name.</param>
        /// <returns>The section instance.</returns>
        public static T GetSection<T>(string sectionName = null) where T : class, new()
        {
            sectionName = sectionName ?? typeof(T).Name;
            if (Obj._sections.TryGetValue(sectionName, out var section))
                return (T)((IConfigSection<T>)section).Value;
            return AddSection<T>(sectionName);
        }

        /// <summary>
        /// Initializes the configuration settings by loading the INI file from the specified path.
        /// </summary>
        /// <param name="path">The path to the INI file.</param>
        public static void Initialize(string path) => Initialize(Path.GetDirectoryName(path), Path.GetFileName(path));

        /// <summary>
        /// Initializes the configuration settings by loading the INI file from the specified directory and file name.
        /// </summary>
        /// <param name="directory">The directory containing the INI file.</param>
        /// <param name="fileName">The INI file name.</param>
        /// <exception cref="ArgumentNullException">Thrown if directory or fileName is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if already initialized.</exception>
        public static void Initialize(string directory, string fileName)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (IsInitialized)
                throw new InvalidOperationException("The configuration settings have already been initialized.");

            _manuallyInitialized = true;
            Obj._configDirectory = directory;
            Obj._configFile = fileName;
            Obj.InternalInitialize();
        }

        /// <summary>
        /// Initializes the configuration settings by loading the default INI file.
        /// </summary>
        public static void Initialize() => Obj.InternalInitialize();

        /// <summary>
        /// Saves the current configuration settings to the INI file.
        /// </summary>
        public static void Save() => Obj.SaveInternal();

        // ===========================
        // Private Instance Fields
        // ===========================

        /// <summary>
        /// Stores configuration sections by name.
        /// </summary>
        private readonly ConcurrentDictionary<string, object> _sections = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Stores section names that represent arrays.
        /// </summary>
        private readonly HashSet<string> _arraySectionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

                if (HttpContext.Current != null)
                    return _configDirectory = HttpContext.Current.Server.MapPath("~/bin");

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
                    lock (_lock)
                    {
                        if (_root == null) // Double-check pattern
                            Initialize();
                    }
                }
                return _root;
            }
        }

        // ===========================
        // Private Instance Methods
        // ===========================

        /// <summary>
        /// Adds a new section to the configuration.
        /// </summary>
        /// <typeparam name="T">The type of the section to add.</typeparam>
        /// <param name="sectionName">The name of the section. If null, uses the type name.</param>
        /// <returns>The added section instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the section already exists.</exception>
        private T InternalAddSection<T>(string sectionName = null) where T : class, new()
        {
            sectionName = sectionName ?? typeof(T).Name;
            var section = new ConfigSection<T>(sectionName, InternalRoot, _arraySectionNames);
            if (_sections.TryAdd(sectionName, section))
                return (T)section.Value;
            throw new InvalidOperationException($"The section '{sectionName}' already exists.");
        }

        /// <summary>
        /// Gets the appropriate default INI filename based on the application type.
        /// </summary>
        /// <returns>The default INI file name.</returns>
        private string GetDefaultConfigFile()
        {
            if (HttpContext.Current != null)
                return "local.ini";

            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly() ?? Assembly.GetExecutingAssembly();
            var exeName = Path.GetFileNameWithoutExtension(assembly.Location);
            return $"{exeName}.ini";
        }

        /// <summary>
        /// Initializes the configuration settings by loading the INI file.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if already initialized.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown if the configuration directory does not exist.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the configuration file does not exist.</exception>
        private void InternalInitialize()
        {
            lock (_lock)
            {
                if (IsInitialized)
                    throw new InvalidOperationException("The configuration settings have already been initialized.");

                if (!_manuallyInitialized && string.IsNullOrEmpty(_configFile))
                    _configFile = GetDefaultConfigFile();

                if (!Directory.Exists(ConfigDirectory))
                    throw new DirectoryNotFoundException($@"The following directory was not found: {ConfigDirectory}");

                var path = Path.Combine(ConfigDirectory, _configFile);
                if (!File.Exists(path))
                    throw new FileNotFoundException($@"The file was not found: {path}");

                var builder = new ConfigurationBuilder()
                    .SetBasePath(ConfigDirectory)
                    .AddIniFile(_configFile);

                _root = builder.Build();
                IsInitialized = true;
            }
        }

        /// <summary>
        /// Saves the current configuration settings to the INI file.
        /// </summary>
        private void SaveInternal()
        {
            var section = string.Empty;
            var iniData = new List<string>();
            var existingKeys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var existingSections = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var sectionEndPositions = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            var currentPosition = 0;

            var iniFilePath = Path.Combine(_configDirectory, _configFile);
            var iniLines = File.ReadAllLines(iniFilePath);

            foreach (var s in _sections.Values.Cast<IConfigSection>())
                s.SaveToConfiguration();

            foreach (var line in iniLines)
            {
                var trimmedLine = line.Trim();
                currentPosition = iniData.Count;

                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                {
                    iniData.Add(line);
                    continue;
                }

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    section = trimmedLine.Trim('[', ']');
                    iniData.Add(line);
                    existingSections.Add(section);
                    sectionEndPositions[section] = currentPosition + 1;
                    continue;
                }

                if (_arraySectionNames.Contains(section))
                    continue;

                var equalsIndex = trimmedLine.IndexOf('=');
                if (equalsIndex == -1)
                {
                    iniData.Add(line);
                    continue;
                }

                var key = trimmedLine.Substring(0, equalsIndex).Trim();
                var value = trimmedLine.Substring(equalsIndex + 1).Trim();

                var configKey = $"{section}:{key}";
                var configValue = _root?.GetSection(configKey).Value;

                if (configValue != null)
                {
                    if (value != configValue)
                        value = configValue;
                }
                else
                {
                    // ConfigSection set this to null (likely a default value), skip it
                    continue;
                }

                existingKeys.Add(configKey);
                iniData.Add($"{key}={value}");

                if (!string.IsNullOrEmpty(section))
                    sectionEndPositions[section] = currentPosition;
            }

            if (!string.IsNullOrEmpty(section))
                sectionEndPositions[section] = iniData.Count;

            foreach (var configSection in _root.GetChildren())
            {
                if (!existingSections.Contains(configSection.Key))
                {
                    iniData.Add(string.Empty);
                    iniData.Add($"[{configSection.Key}]");

                    foreach (var child in configSection.GetChildren())
                    {
                        if (!string.IsNullOrEmpty(child.Value))
                            iniData.Add($"{child.Key}={child.Value}");
                    }

                    iniData.Add(string.Empty);
                }
                else
                {
                    var insertPosition = sectionEndPositions[configSection.Key];
                    var keysAdded = 0;

                    foreach (var child in configSection.GetChildren())
                    {
                        var configKey = $"{configSection.Key}:{child.Key}";
                        if (!string.IsNullOrEmpty(child.Value) && !existingKeys.Contains(configKey))
                            iniData.Insert(insertPosition + (keysAdded++), $"{child.Key}={child.Value}");
                    }

                    if (keysAdded > 0)
                    {
                        foreach (var kvp in sectionEndPositions.Where(x => x.Value >= insertPosition).ToList())
                            sectionEndPositions[kvp.Key] += keysAdded;
                    }
                }
            }

            File.WriteAllLines(iniFilePath, iniData);
        }
    }
}