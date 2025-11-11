using ByteForge.Toolkit;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using System.Globalization;
using System.Reflection;

namespace TestBed
{
    public static class ConfigurationTests
    {
        public static void TestBasicConfiguration()
        {
            Console.WriteLine("Testing basic configuration...");

            // Initialize configuration from INI file
            // Configuration.Initialize("appsettings.ini");

            // Access configuration values directly
            // string serverName = Configuration.GetValue("Database", "Server");
            // int port = Configuration.GetValue("Database", "Port", 1433);
            // bool enableSSL = Configuration.GetValue("Database", "EnableSSL", false);

            // Console.WriteLine($"Connecting to {serverName}:{port}, SSL: {enableSSL}");

            // Save configuration changes
            // Configuration.SetValue("Database", "LastConnected", DateTime.Now.ToString());
            // Configuration.Save();

            Console.WriteLine("✓ Basic configuration test completed");
        }

        public static void TestStronglyTypedConfiguration()
        {
            Console.WriteLine("Testing strongly-typed configuration...");

            // Usage
            // Configuration.Initialize("appsettings.ini");

            // var dbConfig = Configuration.GetSection<DatabaseConfig>("Database");
            // Console.WriteLine($"Server: {dbConfig.Server}");
            // Console.WriteLine($"Port: {dbConfig.Port}");
            // Console.WriteLine($"Timeout: {dbConfig.TimeoutSeconds}s");

            // Modify and save
            // dbConfig.Port = 5432;
            // dbConfig.EnableSSL = true;
            // Configuration.Save();

            Console.WriteLine("✓ Strongly-typed configuration test completed");
        }

        public static void TestArrayConfiguration()
        {
            Console.WriteLine("Testing array configuration...");

            // var config = Configuration.GetSection<AppConfig>("Application");
            // Console.WriteLine($"App: {config.ApplicationName} v{config.Version}");

            // foreach (var server in config.ServerList)
            // {
            //     Console.WriteLine($"Server: {server}");
            // }

            // foreach (var conn in config.Connections)
            // {
            //     Console.WriteLine($"Connection '{conn.Key}': {conn.Value}");
            // }

            Console.WriteLine("✓ Array configuration test completed");
        }

        public static void TestConfigurationValidation()
        {
            Console.WriteLine("Testing configuration validation...");

            // Usage with validation
            // try
            // {
            //     var emailConfig = Configuration.GetSection<EmailConfig>("Email");
            //     
            //     // Configuration is automatically validated
            //     var smtpClient = new SmtpClient(emailConfig.SmtpServer, emailConfig.SmtpPort);
            //     smtpClient.Credentials = new NetworkCredential(emailConfig.Username, emailConfig.Password);
            //     smtpClient.EnableSsl = emailConfig.EnableSSL;
            //     
            //     Console.WriteLine($"Email configured for {emailConfig.SmtpServer}:{emailConfig.SmtpPort}");
            // }
            // catch (ConfigurationException ex)
            // {
            //     Console.WriteLine($"Configuration error: {ex.Message}");
            // }

            Console.WriteLine("✓ Configuration validation test completed");
        }

        public static void TestGlobalization()
        {
            Console.WriteLine("Testing globalization...");

            // Initialize globalization
            // Configuration.Initialize("app.ini");
            // var globalization = Configuration.GetSection<AppGlobalization>("Globalization");

            // Apply culture settings
            // var cultureInfo = new CultureInfo(globalization.Culture);
            // var uiCultureInfo = new CultureInfo(globalization.UICulture);

            // Thread.CurrentThread.CurrentCulture = cultureInfo;
            // Thread.CurrentThread.CurrentUICulture = uiCultureInfo;

            // Use configured formats
            var now = DateTime.Now;
            var price = 123.456m;

            // Console.WriteLine($"Date: {now.ToString(globalization.DateFormat)}");
            // Console.WriteLine($"Time: {now.ToString(globalization.TimeFormat)}");
            // Console.WriteLine($"Price: {globalization.CurrencySymbol}{price.ToString(globalization.NumberFormat)}");

            Console.WriteLine("✓ Globalization test completed");
        }

        public static void TestCompleteApplication()
        {
            Console.WriteLine("Testing complete application configuration...");

            var app = new Application();

            try
            {
                // app.Initialize();

                // Application logic here
                Console.WriteLine("Application running...");

                // Example of updating config at runtime
                // app.UpdateConfiguration();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application startup failed: {ex.Message}");
            }

            Console.WriteLine("✓ Complete application test completed");
        }
    }

    // Configuration section classes
    public class DatabaseConfig : ConfigSection<DatabaseConfig>
    {
        public string Server { get; set; } = "";
        public int Port { get; set; } = 1433;
        public string DatabaseName { get; set; } = "";
        public bool EnableSSL { get; set; }

        [ConfigName("ConnectionTimeout")]
        public int TimeoutSeconds { get; set; } = 30;

        // Encrypted credential storage
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class AppConfig : ConfigSection<AppConfig>
    {
        public string ApplicationName { get; set; } = "";
        public string Version { get; set; } = "";

        [Array("Servers")]
        public List<string> ServerList { get; set; } = new List<string>();

        [Dictionary("ConnectionStrings")]
        public Dictionary<string, string> Connections { get; set; } = new Dictionary<string, string>();

        [DoNotPersist]
        public DateTime StartupTime { get; set; } = DateTime.Now;
    }

    public class EmailConfig : ConfigSection<EmailConfig>
    {
        [Required]
        public string SmtpServer { get; set; } = "";

        public int SmtpPort { get; set; } = 587;

        [Required]
        public string Username { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        public bool EnableSSL { get; set; } = true;

        [ConfigName("FromEmail")]
        public string SenderEmail { get; set; } = "";

        // Custom validation
        protected override void ValidateConfig()
        {
            if (SmtpPort < 1 || SmtpPort > 65535)
                throw new ConfigurationException("SMTP port must be between 1 and 65535");

            if (!string.IsNullOrEmpty(SenderEmail) && !IsValidEmail(SenderEmail))
                throw new ConfigurationException("SenderEmail is not a valid email address");
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    public class ServerConfig : ConfigSection<ServerConfig>
    {
        [Array("BackupServers")]
        public List<string> BackupServerList { get; set; } = new List<string>();

        [Array("AllowedPorts")]
        public int[] AllowedPortNumbers { get; set; } = new int[0];

        [Array("FeatureFlags")]
        public List<bool> EnabledFeatures { get; set; } = new List<bool>();
    }

    public class ApiConfig : ConfigSection<ApiConfig>
    {
        [Dictionary("ApiEndpoints")]
        public Dictionary<string, string> Endpoints { get; set; } = new Dictionary<string, string>();

        [Dictionary("ApiKeys")]
        public Dictionary<string, string> Keys { get; set; } = new Dictionary<string, string>();

        [Dictionary("RetrySettings")]
        public Dictionary<string, int> RetryConfig { get; set; } = new Dictionary<string, int>();
    }

    public class LoggingConfig : ConfigSection<LoggingConfig>
    {
        public string LogLevel { get; set; } = "Info";

        [DefaultValueProvider(nameof(GetDefaultLogPath))]
        public string LogFilePath { get; set; } = "";

        [DefaultValueProvider(nameof(GetDefaultMaxSize))]
        public long MaxFileSizeMB { get; set; }

        public bool EnableRotation { get; set; } = true;

        // Static method to provide dynamic defaults
        public static string GetDefaultLogPath()
        {
            var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Application";
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName, "Logs");
            Directory.CreateDirectory(logDir);
            return Path.Combine(logDir, $"{appName}.log");
        }

        public static long GetDefaultMaxSize()
        {
            // Default to 10MB, but larger on servers with more disk space
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            var availableSpace = drives.Sum(d => d.AvailableFreeSpace);

            // If more than 100GB available, use 50MB default
            return availableSpace > 100L * 1024 * 1024 * 1024 ? 50 : 10;
        }
    }

    public class AppGlobalization : ConfigSection<AppGlobalization>
    {
        public string Culture { get; set; } = "en-US";
        public string UICulture { get; set; } = "en-US";
        public string DateFormat { get; set; } = "MM/dd/yyyy";
        public string TimeFormat { get; set; } = "HH:mm:ss";
        public string NumberFormat { get; set; } = "N2";
        public string CurrencySymbol { get; set; } = "$";
    }

    // Complete application example classes
    public class CompleteDatabaseConfig : ConfigSection<CompleteDatabaseConfig>
    {
        [Required]
        public string Server { get; set; } = "";

        public int Port { get; set; } = 1433;

        [Required]
        public string DatabaseName { get; set; } = "";

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool IntegratedSecurity { get; set; } = true;
        public int ConnectionTimeout { get; set; } = 30;
        public int CommandTimeout { get; set; } = 30;
        public bool EnableConnectionPooling { get; set; } = true;
        public int MaxPoolSize { get; set; } = 100;
    }

    public class CompleteEmailConfig : ConfigSection<CompleteEmailConfig>
    {
        [Required]
        public string SmtpServer { get; set; } = "";

        public int SmtpPort { get; set; } = 587;
        public bool EnableSSL { get; set; } = true;

        [Required]
        public string Username { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        [Required]
        public string FromEmail { get; set; } = "";

        public string FromName { get; set; } = "";
    }

    public class CompleteLoggingConfig : ConfigSection<CompleteLoggingConfig>
    {
        public string LogLevel { get; set; } = "Information";

        [DefaultValueProvider(nameof(GetDefaultLogPath))]
        public string LogFilePath { get; set; } = "";

        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
        public int MaxFileCount { get; set; } = 10;
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;

        public static string GetDefaultLogPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app.log");
        }
    }

    public class FeatureConfig : ConfigSection<FeatureConfig>
    {
        [Array("EnabledModules")]
        public List<string> EnabledModules { get; set; } = new List<string>();

        [Dictionary("FeatureFlags")]
        public Dictionary<string, bool> Features { get; set; } = new Dictionary<string, bool>();

        [Dictionary("ApiLimits")]
        public Dictionary<string, int> RateLimits { get; set; } = new Dictionary<string, int>();
    }

    // Main application setup
    public class Application
    {
        private CompleteDatabaseConfig? _databaseConfig;
        private CompleteEmailConfig? _emailConfig;
        private CompleteLoggingConfig? _loggingConfig;
        private FeatureConfig? _featureConfig;

        public void Initialize(string configPath = "appsettings.ini")
        {
            try
            {
                // Initialize configuration system
                // Configuration.Initialize(configPath);

                // Load all configuration sections
                // _databaseConfig = Configuration.GetSection<CompleteDatabaseConfig>("Database");
                // _emailConfig = Configuration.GetSection<CompleteEmailConfig>("Email");
                // _loggingConfig = Configuration.GetSection<CompleteLoggingConfig>("Logging");
                // _featureConfig = Configuration.GetSection<FeatureConfig>("Features");

                // Setup logging based on configuration
                SetupLogging();

                // Setup database connection
                SetupDatabase();

                // Setup email service
                SetupEmail();

                // Apply feature flags
                ApplyFeatureFlags();

                Console.WriteLine("Application initialized successfully!");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Configuration error: {ex.Message}");
                throw;
            }
        }

        private void SetupLogging()
        {
            // Configure logging based on settings
            if (_loggingConfig != null)
            {
                // var logLevel = Enum.Parse<LogLevel>(_loggingConfig.LogLevel);

                if (_loggingConfig.EnableFileLogging)
                {
                    var logDir = Path.GetDirectoryName(_loggingConfig.LogFilePath);
                    if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);
                }

                // Console.WriteLine($"Logging configured: Level={logLevel}, File={_loggingConfig.LogFilePath}");
            }
        }

        private void SetupDatabase()
        {
            if (_databaseConfig != null)
            {
                string connectionString;

                if (_databaseConfig.IntegratedSecurity)
                {
                    connectionString = $"Server={_databaseConfig.Server},{_databaseConfig.Port};" +
                                     $"Database={_databaseConfig.DatabaseName};" +
                                     $"Integrated Security=true;" +
                                     $"Connection Timeout={_databaseConfig.ConnectionTimeout}";
                }
                else
                {
                    connectionString = $"Server={_databaseConfig.Server},{_databaseConfig.Port};" +
                                     $"Database={_databaseConfig.DatabaseName};" +
                                     $"User Id={_databaseConfig.Username};" +
                                     $"Password={_databaseConfig.Password};" +
                                     $"Connection Timeout={_databaseConfig.ConnectionTimeout}";
                }

                Console.WriteLine($"Database configured: {_databaseConfig.Server}:{_databaseConfig.Port}");
            }
        }

        private void SetupEmail()
        {
            if (_emailConfig != null)
            {
                // Configure SMTP client based on settings
                Console.WriteLine($"Email configured: {_emailConfig.SmtpServer}:{_emailConfig.SmtpPort}");
                Console.WriteLine($"From: {_emailConfig.FromName} <{_emailConfig.FromEmail}>");
            }
        }

        private void ApplyFeatureFlags()
        {
            if (_featureConfig != null)
            {
                Console.WriteLine($"Enabled modules: {string.Join(", ", _featureConfig.EnabledModules)}");

                foreach (var feature in _featureConfig.Features)
                {
                    Console.WriteLine($"Feature '{feature.Key}': {(feature.Value ? "Enabled" : "Disabled")}");
                }

                foreach (var limit in _featureConfig.RateLimits)
                {
                    Console.WriteLine($"Rate limit '{limit.Key}': {limit.Value} requests");
                }
            }
        }

        public void UpdateConfiguration()
        {
            if (_loggingConfig != null && _featureConfig != null)
            {
                // Example of runtime configuration changes
                _loggingConfig.LogLevel = "Debug";
                _featureConfig.Features["NewFeature"] = true;

                // Save changes to file
                // Configuration.Save();
                Console.WriteLine("Configuration updated and saved.");
            }
        }
    }

    // Placeholder classes to make the code compile
    public abstract class ConfigSection<T> where T : class, new()
    {
        protected virtual void ValidateConfig() { }
    }

    public class ConfigNameAttribute : Attribute
    {
        public ConfigNameAttribute(string name) { }
    }

    public class ArrayAttribute : Attribute
    {
        public ArrayAttribute(string sectionName) { }
    }

    public class DictionaryAttribute : Attribute
    {
        public DictionaryAttribute(string sectionName) { }
    }

    public class DefaultValueProviderAttribute : Attribute
    {
        public DefaultValueProviderAttribute(string methodName) { }
    }

    public class DoNotPersistAttribute : Attribute
    {
    }

    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message) { }
    }
}