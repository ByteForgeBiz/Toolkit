using System.Collections.Generic;

namespace ByteForge.Toolkit.Configuration
{
    /// <summary>
    /// Describes the editable keys exposed by a registered strongly typed configuration section.
    /// </summary>
    public sealed class ConfigSectionSchema
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigSectionSchema"/> class.
        /// </summary>
        /// <param name="name">The configuration section name.</param>
        /// <param name="items">The editable item schemas declared for the section.</param>
        public ConfigSectionSchema(string name, IReadOnlyCollection<ConfigItemSchema> items)
        {
            Name = name;
            Items = items ?? new List<ConfigItemSchema>();
        }

        /// <summary>
        /// Gets the configuration section name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the editable item schemas declared for the section.
        /// </summary>
        public IReadOnlyCollection<ConfigItemSchema> Items { get; }
    }

    /// <summary>
    /// Describes an editable strongly typed configuration property.
    /// </summary>
    public sealed class ConfigItemSchema
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigItemSchema"/> class.
        /// </summary>
        /// <param name="key">The configuration key name.</param>
        /// <param name="propertyName">The reflected property name.</param>
        /// <param name="defaultValue">The stringified default value for the key.</param>
        /// <param name="isEncrypted">Whether the key stores encrypted values.</param>
        /// <param name="displayType">The friendly reflected data type label for the property.</param>
        /// <param name="isBoolean">Whether the key maps to a boolean property.</param>
        /// <param name="enumValues">The valid enum member names when this item maps to an enum property.</param>
        /// <param name="options">The selectable editor options when this item has a provider-backed editor.</param>
        /// <param name="isArray">Whether this item stores values in an array backing section.</param>
        /// <param name="isDictionary">Whether this item stores values in a dictionary backing section.</param>
        /// <param name="collectionSectionName">The configured backing section name for array or dictionary values.</param>
        public ConfigItemSchema(
            string key,
            string propertyName,
            string defaultValue,
            bool isEncrypted,
            string displayType,
            bool isBoolean = false,
            IReadOnlyCollection<string> enumValues = null,
            IReadOnlyCollection<ConfigItemOption> options = null,
            bool isArray = false,
            bool isDictionary = false,
            string collectionSectionName = null)
        {
            Key = key;
            PropertyName = propertyName;
            DefaultValue = defaultValue;
            IsEncrypted = isEncrypted;
            DisplayType = displayType;
            IsBoolean = isBoolean;
            EnumValues = enumValues ?? new List<string>();
            Options = options ?? new List<ConfigItemOption>();
            IsArray = isArray;
            IsDictionary = isDictionary;
            CollectionSectionName = collectionSectionName;
        }

        /// <summary>
        /// Gets the configuration key name.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the reflected property name.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the stringified default value for the key.
        /// </summary>
        public string DefaultValue { get; }

        /// <summary>
        /// Gets a value indicating whether the key stores encrypted values.
        /// </summary>
        public bool IsEncrypted { get; }

        /// <summary>
        /// Gets the friendly reflected data type label for the property.
        /// </summary>
        public string DisplayType { get; }

        /// <summary>
        /// Gets a value indicating whether this item maps to a boolean property.
        /// </summary>
        public bool IsBoolean { get; }

        /// <summary>
        /// Gets a value indicating whether this item stores values in an array backing section.
        /// </summary>
        public bool IsArray { get; }

        /// <summary>
        /// Gets a value indicating whether this item stores values in a dictionary backing section.
        /// </summary>
        public bool IsDictionary { get; }

        /// <summary>
        /// Gets the configured backing section name for array or dictionary values.
        /// </summary>
        public string CollectionSectionName { get; }

        /// <summary>
        /// Gets the valid enum member names when this item maps to an enum property.
        /// </summary>
        public IReadOnlyCollection<string> EnumValues { get; }

        /// <summary>
        /// Gets the selectable editor options when this item has a provider-backed editor.
        /// </summary>
        public IReadOnlyCollection<ConfigItemOption> Options { get; }

        /// <summary>
        /// Gets a value indicating whether this item maps to an enum property.
        /// </summary>
        public bool IsEnum => EnumValues.Count > 0;

        /// <summary>
        /// Gets a value indicating whether this item has selectable editor options.
        /// </summary>
        public bool HasOptions => Options.Count > 0;
    }

    /// <summary>
    /// Describes one selectable configuration editor option.
    /// </summary>
    public sealed class ConfigItemOption
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigItemOption"/> class.
        /// </summary>
        /// <param name="value">The value persisted to configuration when selected.</param>
        /// <param name="label">The label displayed by the editor.</param>
        public ConfigItemOption(string value, string label)
        {
            Value = value ?? string.Empty;
            Label = string.IsNullOrWhiteSpace(label) ? Value : label;
        }

        /// <summary>
        /// Gets the value persisted to configuration when selected.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the label displayed by the editor.
        /// </summary>
        public string Label { get; }
    }
}
