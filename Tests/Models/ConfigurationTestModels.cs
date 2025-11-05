using System.ComponentModel;

namespace ByteForge.Toolkit.Tests.Models
{
    /// <summary>
    /// Basic test configuration model with fundamental property types.
    /// </summary>
    public class BasicTestConfig
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
        public bool BoolValue { get; set; }
        public double DoubleValue { get; set; }
        public DateTime DateValue { get; set; }
        
        [DefaultValue(30)]
        public int TimeoutValue { get; set; }
        
        [ConfigName("CustomName")]
        public string MappedProperty { get; set; }
        
        [DoNotPersist]
        public DateTime LastAccessed { get; set; }
        
        [Ignore]
        public string ComputedProperty => $"{StringValue}_computed";
    }

    /// <summary>
    /// Configuration model for testing array and collection support.
    /// </summary>
    public class ArrayTestConfig
    {
        [Array]
        public string[] StringArray { get; set; }
        
        [Array("CustomArraySection")]
        public List<string> CustomNamedArray { get; set; }
        
        [Array]
        public List<int> NumberList { get; set; }
        
        [Array]
        public IList<string> InterfaceList { get; set; }
        
        [Array]
        public IEnumerable<string> EnumerableCollection { get; set; }
        
        // Property to reference array section name from INI
        public string ArrayValue { get; set; }
    }

    /// <summary>
    /// Database configuration model for real-world testing scenarios.
    /// </summary>
    public class DatabaseConfig
    {
        public string Server { get; set; }
        
        [DefaultValue(1433)]
        public int Port { get; set; }
        
        public string Username { get; set; }
        public string Password { get; set; }
        
        [DefaultValue(30)]
        public int Timeout { get; set; }
        
        [DefaultValue(false)]
        public bool UseSSL { get; set; }
        
        [ConfigName("ConnectionString")]
        public string ConnString { get; set; }
        
        [DoNotPersist]
        public DateTime LastConnectionTime { get; set; }
        
        [Ignore]
        public string FullConnectionString => $"Server={Server};Port={Port};User={Username};Password={Password};SSL={UseSSL}";
    }

    /// <summary>
    /// Globalization settings configuration for culture testing.
    /// </summary>
    public class GlobalizationConfig
    {
        [DefaultValue("en-US")]
        public string Culture { get; set; }
        
        [DefaultValue("MM/dd/yyyy")]
        public string DateFormat { get; set; }
        
        [DefaultValue("#,##0.00")]
        public string NumberFormat { get; set; }
        
        [DefaultValue("$")]
        public string CurrencySymbol { get; set; }
    }

    /// <summary>
    /// Complex configuration model with nested scenarios.
    /// </summary>
    public class ComplexTestConfig
    {
        public string Name { get; set; }
        
        [Array("Features")]
        public List<string> EnabledFeatures { get; set; }
        
        [Array("Environments")]
        public string[] SupportedEnvironments { get; set; }
        
        [DefaultValue(true)]
        public bool IsEnabled { get; set; }
        
        [ConfigName("MaxRetries")]
        public int RetryCount { get; set; }
        
        [DoNotPersist]
        public DateTime CreatedTime { get; set; }
        
        [Ignore]
        public int FeatureCount => EnabledFeatures?.Count ?? 0;
    }

    /// <summary>
    /// Configuration model for testing nullable types.
    /// </summary>
    public class NullableTestConfig
    {
        public int? NullableInt { get; set; }
        public bool? NullableBool { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public double? NullableDouble { get; set; }
        
        [DefaultValue(null)]
        public string OptionalString { get; set; }
    }

    /// <summary>
    /// Configuration model for testing enum support.
    /// </summary>
    public class EnumTestConfig
    {
        public LogLevel Level { get; set; }
        public DatabaseType DbType { get; set; }
        
        [DefaultValue(ProcessingMode.Async)]
        public ProcessingMode Mode { get; set; }
    }

    /// <summary>
    /// Test enum for log levels.
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Test enum for database types.
    /// </summary>
    public enum DatabaseType
    {
        SqlServer,
        MySQL,
        PostgreSQL,
        Oracle
    }

    /// <summary>
    /// Test enum for processing modes.
    /// </summary>
    public enum ProcessingMode
    {
        Sync,
        Async,
        Batch
    }

    /// <summary>
    /// Configuration with custom default value provider.
    /// </summary>
    public class CustomDefaultConfig
    {
        [DefaultValueProvider(typeof(ConfigDefaults), nameof(ConfigDefaults.GetDefaultPath))]
        public string DataPath { get; set; }
        
        [DefaultValueProvider(typeof(ConfigDefaults), nameof(ConfigDefaults.GetDefaultTimeout))]
        public int ConnectionTimeout { get; set; }
        
        public string RegularProperty { get; set; }
    }

    /// <summary>
    /// Static class providing custom default values.
    /// </summary>
    public static class ConfigDefaults
    {
        public static string GetDefaultPath() => @"C:\Temp\DefaultData";
        public static int GetDefaultTimeout() => 60;
    }

    /// <summary>
    /// Configuration model for testing dictionary support with all supported types.
    /// </summary>
    public class DictionaryTestConfig
    {
        [Dictionary]
        public Dictionary<string, string> FileFormats { get; set; }
        
        [Dictionary("MyCustomSection")]
        public Dictionary<string, string> CustomSettings { get; set; }
        
        [Dictionary]
        public IDictionary<string, string> InterfaceDictionary { get; set; }
        
        [Dictionary]
        public IReadOnlyDictionary<string, string> ReadOnlyDictionary { get; set; }
        
        [Dictionary]
        public ICollection<KeyValuePair<string, string>> CollectionKeyValuePairs { get; set; }
        
        [Dictionary]
        public IEnumerable<KeyValuePair<string, string>> EnumerableKeyValuePairs { get; set; }
        
        [Dictionary]
        public IReadOnlyCollection<KeyValuePair<string, string>> ReadOnlyCollectionKeyValuePairs { get; set; }
    }

    /// <summary>
    /// Configuration model with unsupported dictionary types for error testing.
    /// </summary>
    public class UnsupportedDictionaryTestConfig
    {
        [Dictionary]
        public Dictionary<int, string> UnsupportedDict { get; set; }
    }

    /// <summary>
    /// Real-world configuration model similar to GHM export system.
    /// </summary>
    public class GHMExportTestConfig
    {
        public string WiredFolder { get; set; }
        public string WirelessFolder { get; set; }
        public int RetainFor { get; set; }
        
        [Dictionary("GHMFormats")]
        public Dictionary<string, string> FormatConfigurations { get; set; }
        
        [Dictionary]
        public Dictionary<string, string> OutputPaths { get; set; }
    }

    /// <summary>
    /// Complex dictionary configuration for testing nested scenarios.
    /// </summary>
    public class ComplexDictionaryTestConfig
    {
        public string Name { get; set; }
        
        [Dictionary("DatabaseConnections")]
        public Dictionary<string, string> Connections { get; set; }
        
        [Dictionary("FeatureFlags")]
        public IDictionary<string, string> Features { get; set; }
        
        [Array("Environments")]
        public List<string> SupportedEnvironments { get; set; }
        
        [DefaultValue(true)]
        public bool IsEnabled { get; set; }
        
        [DoNotPersist]
        public DateTime LastUpdated { get; set; }
        
        [Ignore]
        public int ConnectionCount => Connections?.Count ?? 0;
    }

    /// <summary>
    /// Configuration model for testing unsupported dictionary value types.
    /// </summary>
    public class UnsupportedDictionaryValueTestConfig
    {
        [Dictionary]
        public Dictionary<string, object> UnsupportedValueDict { get; set; }
    }

    /// <summary>
    /// Configuration model for testing KeyValuePair collections with unsupported types.
    /// </summary>
    public class UnsupportedKeyValuePairTestConfig
    {
        [Dictionary]
        public ICollection<KeyValuePair<int, string>> UnsupportedKeyCollection { get; set; }
        
        [Dictionary]
        public IEnumerable<KeyValuePair<string, int>> UnsupportedValueCollection { get; set; }
    }

    /// <summary>
    /// Enhanced test model for testing complex naming scenarios.
    /// </summary>
    public class NamingConventionTestConfig
    {
        [Array]
        public string[] DefaultNamedArray { get; set; }
        
        [Array("SpecificArraySection")]
        public List<string> CustomNamedArray { get; set; }
        
        [Dictionary]
        public Dictionary<string, string> DefaultNamedDict { get; set; }
        
        [Dictionary("SpecificDictSection")]
        public IDictionary<string, string> CustomNamedDict { get; set; }
        
        public string RegularProperty { get; set; }
    }

    /// <summary>
    /// Test model for validating complex inheritance scenarios with defaults.
    /// </summary>
    public class InheritanceTestConfig : DatabaseConfig
    {
        [DefaultValue("Extended")]
        public string ExtendedProperty { get; set; }
        
        [Array("ExtendedFeatures")]
        public List<string> AdditionalFeatures { get; set; }
        
        [Dictionary("ExtendedSettings")]
        public Dictionary<string, string> ExtendedSettings { get; set; }
    }

    /// <summary>
    /// Test model for complex mixed array and dictionary scenarios.
    /// </summary>
    public class MixedComplexTestConfig
    {
        public string Name { get; set; }
        
        [Array("PrimaryArray")]
        public string[] PrimaryItems { get; set; }
        
        [Array("SecondaryArray")]  
        public List<int> SecondaryItems { get; set; }
        
        [Dictionary("PrimaryDict")]
        public Dictionary<string, string> PrimarySettings { get; set; }
        
        [Dictionary("SecondaryDict")]
        public IReadOnlyDictionary<string, string> SecondarySettings { get; set; }
        
        [DefaultValue(true)]
        public bool IsActive { get; set; }
        
        [DoNotPersist]
        public DateTime ProcessedTime { get; set; }
        
        [Ignore]
        public int TotalItems => (PrimaryItems?.Length ?? 0) + (SecondaryItems?.Count ?? 0);
    }

    /// <summary>
    /// Test model for custom TimeSpan type registration testing.
    /// </summary>
    public class CustomTimeSpanTestConfig
    {
        public string StringValue { get; set; }
        public TimeSpan TimeSpanValue { get; set; }
    }
}