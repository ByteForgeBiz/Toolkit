using ByteForge.Toolkit.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public class Configuration : DynamicObject
    {

        private static readonly object _lock =  new object();
        private static readonly Lazy<Configuration> _instance = new Lazy<Configuration>(() => new Configuration());
        private static bool _initialized;

        /// <summary>
        /// Gets the dynamic object that provides access to the configuration settings.
        /// </summary>
        public static Configuration Obj => _instance.Value;

        /// <summary>
        /// Gets the root of the configuration.
        /// </summary>
        public static IConfigurationRoot Root => Obj.InternalRoot;

        /// <summary>
        /// Gets the globalization information.
        /// </summary>
        public static readonly Lazy<GlobalizationInfo> Globalization = new Lazy<GlobalizationInfo>(() => GetSection<GlobalizationInfo>("Globalization"));

        /// <summary>
        /// Initializes the configuration settings by loading the INI file from the specified path.
        /// </summary>
        /// <param name="path">The full path to the INI file.</param>
        public static void Initialize(string path) => Initialize(Path.GetDirectoryName(path), Path.GetFileName(path));

        /// <summary>
        /// Initializes the configuration settings by loading the INI file from the specified directory and file name.
        /// </summary>
        /// <param name="directory">The directory where the INI file is located.</param>
        /// <param name="fileName">The name of the INI file.</param>
        public static void Initialize(string directory, string fileName)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (_initialized)
                throw new InvalidOperationException("The configuration settings have already been initialized.");

            IniDirectory = directory;
            IniFileName = fileName;
            Obj.InternalInitialize();
        }

        /// <summary>
        /// Initializes the configuration settings by loading the default INI file.
        /// </summary>
        public static void Initialize() => Obj.InternalInitialize();

        /// <summary>
        /// Saves the current configuration settings to the INI file.
        /// </summary>
        public static void Save() => Obj.InternalSave();

        /// <summary>
        /// Adds a new section to the configuration.
        /// </summary>
        /// <typeparam name="T">The type of the section to add.</typeparam>
        /// <param name="sectionName">The name of the section. If null, the type name is used.</param>
        public static T AddSection<T>(string sectionName = null) where T : class, new() => Obj.InternalAddSection<T>(sectionName);


        /// <summary>
        /// Gets a section of the configuration.
        /// </summary>
        /// <typeparam name="T">The type of the section to get.</typeparam>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>The section of the configuration.</returns>
        public static T GetSection<T>(string sectionName = null) where T : class, new()
        {
            sectionName = sectionName ?? typeof(T).Name;
            if (Obj._sections.TryGetValue(sectionName, out var section))
                return ((IConfigurationSection<T>)section).Value;
            return AddSection<T>(sectionName);
        }

        /// <summary>
        /// Gets or sets the directory where the INI file is located.
        /// </summary>
        public static string IniDirectory
        {
            get
            {
                if (!string.IsNullOrEmpty(_iniFileDirectory))
                    return _iniFileDirectory;

                if (HttpContext.Current != null)
                    return _iniFileDirectory = HttpContext.Current.Server.MapPath("~/bin");

                return _iniFileDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
            set => _iniFileDirectory = value;
        }
        private static string _iniFileDirectory = string.Empty;

        /// <summary>
        /// Gets or sets the name of the INI file.
        /// </summary>
        public static string IniFileName
        {
            get => Obj._iniFileName;
            set => Obj.SetIniFileName(value);
        }

        /// <summary>
        /// Gets the full path to the INI file.
        /// </summary>
        public static string IniFilePath => Path.Combine(IniDirectory, IniFileName);

        /// <summary>
        /// Gets or sets the path to the log file.
        /// </summary>
        public static string LogFilePath
        {
            get => Environment.ExpandEnvironmentVariables(Root["Logging:sLogFile"] ??
                    Path.ChangeExtension((Assembly.GetEntryAssembly() ??
                                          Assembly.GetCallingAssembly() ??
                                          Assembly.GetExecutingAssembly()).Location, ".log"));
            set => Root["Logging:sLogFile"] = value;
        }

        /// <summary>
        /// Gets the trace log level.
        /// </summary>
        public static LogLevel TraceLogLevel
        {
            get
            {
                if (Obj._logLevel != null)
                    return Obj._logLevel.Value;

                if (Enum.TryParse(Root["Logging:sLogLevel"], out LogLevel level))
                {
                    Obj._logLevel = level;
                    return level;
                }

                return LogLevel.Verbose;
            }
            set
            {
                Obj._logLevel = value;
                Root["Logging:sLogLevel"] = value.ToString();
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether to display errors.
        /// </summary>
        public static bool DisplayErrors
        {
            get => bool.Parse(Root["Debugging:DisplayErrors"] ?? "true");
            set => Root["Debugging:DisplayErrors"] = value.ToString();
        }

        private volatile IConfigurationRoot _root;
        private readonly ConcurrentDictionary<string, object> _sections = new ConcurrentDictionary<string, object>();
        private LogLevel? _logLevel;
        private string _iniFileName = "Local.ini";

        /// <summary>
        /// Initializes the configuration settings by loading the INI file.
        /// </summary>
        private void InternalInitialize()
        {
            lock (_lock)
            {
                if (_initialized)
                    throw new InvalidOperationException("The configuration settings have already been initialized.");

                if (!Directory.Exists(IniDirectory))
                    throw new DirectoryNotFoundException($@"The following directory was not found: {IniDirectory}");

                var path = Path.Combine(IniDirectory, IniFileName);
                if (!File.Exists(path))
                    throw new FileNotFoundException($@"The file was not found: {path}");

                var builder = new ConfigurationBuilder()
                    .SetBasePath(IniDirectory)
                    .AddIniFile(IniFileName);

                _root = builder.Build();
                _initialized = true;
            }
        }
        /// <summary>
        /// Saves the current configuration settings to the INI file.
        /// </summary>
        /// <remarks>
        /// This method preserves comments and only updates modified data.
        /// </remarks>
        private void InternalSave()
        {
            var section = string.Empty;
            var iniData = new List<string>();
            var existingKeys = new HashSet<string>();
            var existingSections = new HashSet<string>();
            var sectionEndPositions = new Dictionary<string, int>();
            var currentPosition = 0;

            var iniFilePath = Path.Combine(IniDirectory, IniFileName);
            var iniLines = File.ReadAllLines(iniFilePath);

            /*
             * Saves all the sections to the Root
             */
            foreach (var s in _sections.Values.Cast<IConfigurationSection>())
                s.SaveToConfiguration();

            foreach (var line in iniLines)
            {
                var trimmedLine = line.Trim();
                currentPosition = iniData.Count;

                // Preserve comments and empty lines
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                {
                    iniData.Add(line);
                    continue;
                }

                // Detect section headers
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    section = trimmedLine.Trim('[', ']');
                    existingSections.Add(section);
                    iniData.Add(line);
                    continue;
                }

                // Detect key-value pairs
                var equalsIndex = trimmedLine.IndexOf('=');
                if (equalsIndex == -1)
                {
                    iniData.Add(line);
                    continue;
                }

                var key = trimmedLine.Substring(0, equalsIndex).Trim();
                var value = trimmedLine.Substring(equalsIndex + 1).Trim();

                // Check if the key exists in the configuration root
                var configValue = _root?.GetSection($"{section}:{key}").Value;
                if (configValue != null && value != configValue)
                    value = configValue;

                existingKeys.Add($"{section}:{key}");
                iniData.Add($"{key}={value}");

                // Update the end position of this section
                if (!string.IsNullOrEmpty(section))
                    sectionEndPositions[section] = currentPosition;
            }

            // Update the end position of the last section
            if (!string.IsNullOrEmpty(section))
                sectionEndPositions[section] = iniData.Count;

            // Add new sections and keys
            foreach (var configSection in _root.GetChildren())
            {
                if (!existingSections.Contains(configSection.Key))
                {
                    // Add new section
                    iniData.Add(string.Empty);  // Add blank line before new section
                    iniData.Add($"[{configSection.Key}]");
                    foreach (var child in configSection.GetChildren())
                        if (!string.IsNullOrEmpty(child.Value))
                            iniData.Add($"{child.Key}={child.Value}");
                    iniData.Add(string.Empty);  // Add blank line after new section
                }
                else
                {
                    // Add new keys to existing section
                    var insertPosition = sectionEndPositions[configSection.Key];
                    var keysAdded = 0;

                    foreach (var child in configSection.GetChildren())
                    {
                        if ((!string.IsNullOrEmpty(child.Value)) && (!existingKeys.Contains($"{configSection.Key}:{child.Key}")))
                        {
                            iniData.Insert(insertPosition, $"{child.Key}={child.Value}");
                            keysAdded++;
                        }
                    }

                    // Update all section end positions after the insertPosition
                    if (keysAdded > 0)
                    {
                        foreach (var kvp in sectionEndPositions.Where(x => x.Value >= insertPosition).ToList())
                            sectionEndPositions[kvp.Key] += keysAdded;
                    }
                }
            }

            File.WriteAllLines(iniFilePath, iniData);
        }

        /// <summary>
        /// Sets the name of the INI file.
        /// </summary>
        /// <param name="value">The name of the INI file.</param>
        /// <exception cref="ArgumentNullException">The value is null or empty.</exception>
        /// <exception cref="ArgumentException">The value contains invalid characters.</exception>
        private void SetIniFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));
            if (value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("The file name contains invalid characters.", nameof(value));
            if (_iniFileName == value)
                return;

            lock (_lock)
            {
                _iniFileName = value;
                _root = null;
                _initialized = false;
            }
        }

        /// <summary>
        /// Gets the root of the configuration that is built from the INI file.
        /// This property will automatically initialize the configuration if it has not been initialized yet.
        /// </summary>
        private IConfigurationRoot InternalRoot
        {
            get
            {
                if (_root == null)
                    Initialize();
                return _root;
            }
        }

        /// <summary>
        /// Adds a new section to the configuration.
        /// </summary>
        /// <typeparam name="T">The type of the section to add.</typeparam>
        /// <param name="sectionName">The name of the section. If null, the type name is used.</param>
        private T InternalAddSection<T>(string sectionName = null) where T : class, new()
        {
            sectionName = sectionName ?? typeof(T).Name;
            var section = new ConfigurationSection<T>(sectionName, InternalRoot);
            if (_sections.TryAdd(sectionName, section))
                return (T)section.Value;
            throw new InvalidOperationException($"The section '{sectionName}' already exists.");
        }

        /// <summary>
        /// Tries to get the value of a member.
        /// </summary>
        /// <param name="binder">The binder that provides information about the member.</param>
        /// <param name="result">When this method returns, contains the value of the member, if found; otherwise, null.</param>
        /// <returns>true if the member was found; otherwise, false.</returns>
        private bool InternalTryGetMember(GetMemberBinder binder, out object result)
        {
            // First check if we have a registered section
            if (_sections.TryGetValue(binder.Name, out var section))
            {
                var valueProperty = section.GetType().GetProperty("Value");
                result = valueProperty?.GetValue(section);
                return true;
            }

            // Then check if it's a regular configuration value
            var value = InternalRoot[binder.Name];
            if (value != null)
            {
                result = value;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Tries to set the value of a member.
        /// </summary>
        /// <param name="binder">The binder that provides information about the member.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>true if the member was set; otherwise, false.</returns>
        private bool InternalTrySetMember(SetMemberBinder binder, object value)
        {
            if (_sections.ContainsKey(binder.Name))
                return false; // Can't set section directly

            InternalRoot[binder.Name] = value?.ToString();
            return true;
        }

        /// <summary>
        /// Tries to get the value of a member.
        /// </summary>
        /// <param name="binder">The binder that provides information about the member.</param>
        /// <param name="result">When this method returns, contains the value of the member, if found; otherwise, null.</param>
        /// <returns>true if the member was found; otherwise, false.</returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result) => InternalTryGetMember(binder, out result);

        /// <summary>
        /// Tries to set the value of a member.
        /// </summary>
        /// <param name="binder">The binder that provides information about the member.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>true if the member was set; otherwise, false.</returns>
        public override bool TrySetMember(SetMemberBinder binder, object value) => InternalTrySetMember(binder, value);
    }
}