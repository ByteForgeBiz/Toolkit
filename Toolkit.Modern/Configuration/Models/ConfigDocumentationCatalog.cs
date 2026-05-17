using System;
using System.Collections.Generic;

namespace ByteForge.Toolkit.Configuration
{
    /// <summary>
    /// Stores physical and attribute-based documentation for INI configuration sections and keys.
    /// </summary>
    public sealed class ConfigDocumentationCatalog
    {
        /// <summary>
        /// Stores section documentation entries by section name.
        /// </summary>
        private readonly Dictionary<string, ConfigSectionDocumentation> _sections =
            new Dictionary<string, ConfigSectionDocumentation>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Stores key documentation entries by fully qualified configuration key.
        /// </summary>
        private readonly Dictionary<string, ConfigItemDocumentation> _items =
            new Dictionary<string, ConfigItemDocumentation>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets an empty documentation catalog.
        /// </summary>
        public static ConfigDocumentationCatalog Empty => new ConfigDocumentationCatalog();

        /// <summary>
        /// Gets the documented sections keyed by section name.
        /// </summary>
        public IReadOnlyDictionary<string, ConfigSectionDocumentation> Sections => _sections;

        /// <summary>
        /// Gets the documented items keyed by fully qualified configuration key.
        /// </summary>
        public IReadOnlyDictionary<string, ConfigItemDocumentation> Items => _items;

        /// <summary>
        /// Adds or replaces physical documentation for a section.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="description">The section description.</param>
        /// <param name="sourcePath">The source INI file path.</param>
        /// <param name="lineNumber">The line number where the section was declared.</param>
        public void SetPhysicalSection(string section, string description, string sourcePath, int lineNumber)
        {
            if (string.IsNullOrWhiteSpace(description))
                return;

            _sections[section] = new ConfigSectionDocumentation(section, description, sourcePath, lineNumber, true);
        }

        /// <summary>
        /// Adds or replaces physical documentation for a configuration item.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="key">The item key.</param>
        /// <param name="description">The item description.</param>
        /// <param name="sourcePath">The source INI file path.</param>
        /// <param name="lineNumber">The line number where the item was declared.</param>
        public void SetPhysicalItem(string section, string key, string description, string sourcePath, int lineNumber)
        {
            if (string.IsNullOrWhiteSpace(description))
                return;

            _items[BuildItemKey(section, key)] = new ConfigItemDocumentation(section, key, description, sourcePath, lineNumber, true);
        }

        /// <summary>
        /// Adds fallback documentation for a section when physical documentation is missing.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="description">The section description.</param>
        public void SetFallbackSection(string section, string description)
        {
            if (string.IsNullOrWhiteSpace(description) || _sections.ContainsKey(section))
                return;

            _sections[section] = new ConfigSectionDocumentation(section, description, null, 0, false);
        }

        /// <summary>
        /// Adds fallback documentation for an item when physical documentation is missing.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="key">The item key.</param>
        /// <param name="description">The item description.</param>
        public void SetFallbackItem(string section, string key, string description)
        {
            var itemKey = BuildItemKey(section, key);
            if (string.IsNullOrWhiteSpace(description) || _items.ContainsKey(itemKey))
                return;

            _items[itemKey] = new ConfigItemDocumentation(section, key, description, null, 0, false);
        }

        /// <summary>
        /// Merges fallback documentation from another catalog without replacing physical documentation.
        /// </summary>
        /// <param name="fallbackCatalog">The catalog containing fallback documentation.</param>
        public void MergeFallbacks(ConfigDocumentationCatalog fallbackCatalog)
        {
            if (fallbackCatalog == null)
                return;

            foreach (var section in fallbackCatalog.Sections.Values)
                SetFallbackSection(section.Name, section.Description);

            foreach (var item in fallbackCatalog.Items.Values)
                SetFallbackItem(item.Section, item.Key, item.Description);
        }

        /// <summary>
        /// Gets documentation for a section.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <returns>The section documentation, or <see langword="null"/> when no documentation exists.</returns>
        public ConfigSectionDocumentation GetSection(string section)
        {
            if (string.IsNullOrEmpty(section))
                return null;

            return _sections.TryGetValue(section, out var value) ? value : null;
        }

        /// <summary>
        /// Gets documentation for an item.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="key">The item key.</param>
        /// <returns>The item documentation, or <see langword="null"/> when no documentation exists.</returns>
        public ConfigItemDocumentation GetItem(string section, string key)
        {
            if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(key))
                return null;

            var itemKey = BuildItemKey(section, key);
            return _items.TryGetValue(itemKey, out var value) ? value : null;
        }

        /// <summary>
        /// Builds the canonical documentation key for a section item.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="key">The item key.</param>
        /// <returns>The canonical item key.</returns>
        public static string BuildItemKey(string section, string key) => $"{section}:{key}";
    }

    /// <summary>
    /// Describes an INI configuration section.
    /// </summary>
    public sealed class ConfigSectionDocumentation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigSectionDocumentation"/> class.
        /// </summary>
        /// <param name="name">The section name.</param>
        /// <param name="description">The section description.</param>
        /// <param name="sourcePath">The source INI file path, when the description came from the file.</param>
        /// <param name="lineNumber">The source line number, when the description came from the file.</param>
        /// <param name="isPhysical">Whether the description came from the physical INI file.</param>
        public ConfigSectionDocumentation(string name, string description, string sourcePath, int lineNumber, bool isPhysical)
        {
            Name = name;
            Description = description;
            SourcePath = sourcePath;
            LineNumber = lineNumber;
            IsPhysical = isPhysical;
        }

        /// <summary>
        /// Gets the section name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the section description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the source INI file path, when available.
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// Gets the source line number, when available.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Gets a value indicating whether the description came from the physical INI file.
        /// </summary>
        public bool IsPhysical { get; }
    }

    /// <summary>
    /// Describes an INI configuration key.
    /// </summary>
    public sealed class ConfigItemDocumentation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigItemDocumentation"/> class.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="key">The item key.</param>
        /// <param name="description">The item description.</param>
        /// <param name="sourcePath">The source INI file path, when the description came from the file.</param>
        /// <param name="lineNumber">The source line number, when the description came from the file.</param>
        /// <param name="isPhysical">Whether the description came from the physical INI file.</param>
        public ConfigItemDocumentation(string section, string key, string description, string sourcePath, int lineNumber, bool isPhysical)
        {
            Section = section;
            Key = key;
            Description = description;
            SourcePath = sourcePath;
            LineNumber = lineNumber;
            IsPhysical = isPhysical;
        }

        /// <summary>
        /// Gets the section name.
        /// </summary>
        public string Section { get; }

        /// <summary>
        /// Gets the item key.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the item description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the source INI file path, when available.
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// Gets the source line number, when available.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Gets a value indicating whether the description came from the physical INI file.
        /// </summary>
        public bool IsPhysical { get; }
    }
}
