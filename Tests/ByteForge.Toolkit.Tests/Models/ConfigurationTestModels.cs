using System;
using System.Collections.Generic;
using System.ComponentModel;
using ByteForge.Toolkit;

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
}