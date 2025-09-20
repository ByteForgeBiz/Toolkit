using ByteForge.Toolkit.Logging;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace ByteForge.Toolkit
{
    /*
     *  _              ___      _   _   _              
     * | |   ___  __ _/ __| ___| |_| |_(_)_ _  __ _ ___
     * | |__/ _ \/ _` \__ \/ -_)  _|  _| | ' \/ _` (_-<
     * |____\___/\__, |___/\___|\__|\__|_|_||_\__, /__/
     *           |___/                        |___/    
     */
    /// <summary>
    /// Represents the options for configuring the logging system.
    /// </summary>
    /// <remarks>
    /// This class provides configuration settings that control how the <see cref="Log"/>
    /// system behaves, including where log files are stored, what level of detail is
    /// included in logs, and whether logs are cleared on application startup.
    /// <para>
    /// LogOptions is typically loaded from configuration files using the <see cref="Configuration"/>
    /// system, with property names mapped through <see cref="ConfigNameAttribute"/>.
    /// </para>
    /// <para>
    /// The logging system supports multiple logging targets (console, file, etc.)
    /// which can be configured through this options class.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Load logging options from configuration
    /// var logOptions = Configuration.Default.GetSection&lt;LogOptions&gt;("Logging");
    /// 
    /// // Configure the logging system
    /// Log.Configure(logOptions);
    /// 
    /// // Log messages
    /// Log.Info("Application started");
    /// Log.Error(exception, "An error occurred");
    /// </code>
    /// </example>
    public class LogSettings
    {
        private const LogLevel DefaultTraceLogLevel = LogLevel.All;
        private const bool DefaultClearLogOnStartup = false;
        private const bool DefaultUseSessionLogging = false;
        /// <summary>
        /// Gets or sets the path to the log file.
        /// </summary>
        /// <remarks>
        /// The default log file path is determined by the <see cref="GetDefaultLogPath"/> method,
        /// which creates a log file in the same directory as the executing assembly,
        /// with the same name as the assembly but with a .log extension.
        /// <para>
        /// When this property is not explicitly set in configuration, the <see cref="GetDefaultLogPath"/>
        /// method is called to provide a default value.
        /// </para>
        /// </remarks>
        [ConfigName("sLogFile")]
        [DefaultValueProvider(typeof(LogSettings), nameof(GetDefaultLogPath))]
        public string LogFilePath { get; set; }

        /// <summary>
        /// Gets or sets the log level for tracing.
        /// </summary>
        /// <remarks>
        /// Determines the minimum severity level of messages that will be logged.
        /// Messages with a severity level less than this value will be ignored.
        /// <para>
        /// The levels, in increasing order of severity, are:
        /// <list type="bullet">
        ///   <item><see cref="LogLevel.Trace"/> - Very detailed messages for tracing execution</item>
        ///   <item><see cref="LogLevel.Debug"/> - Debugging information</item>
        ///   <item><see cref="LogLevel.Verbose"/> - Very detailed information</item>
        ///   <item><see cref="LogLevel.Info"/> - Normal application flow information</item>
        ///   <item><see cref="LogLevel.Warning"/> - Potential issues that don't stop execution</item>
        ///   <item><see cref="LogLevel.Error"/> - Errors that allow continued execution</item>
        ///   <item><see cref="LogLevel.Critical"/> - Critical errors that stop execution</item>
        ///   <item><see cref="LogLevel.None"/> - Disables all logging</item>
        ///   <item><see cref="LogLevel.All"/> - Enables all logging levels</item>
        /// </list>
        /// </para>
        /// <para>
        /// For example, setting this to <see cref="LogLevel.Info"/> will log Info, Warning,
        /// Error, and Fatal messages, but ignore Trace and Debug messages.
        /// </para>
        /// </remarks>
        [DefaultValue(DefaultTraceLogLevel)]
        [ConfigName("eTraceLogLevel")]
        public LogLevel TraceLogLevel { get; set; } = DefaultTraceLogLevel;

        /// <summary>
        /// Gets or sets a value indicating whether the log should be cleared on startup.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the log should be cleared on startup; otherwise, <see langword="false" />.
        /// </value>
        /// <remarks>
        /// When set to true, the log file will be cleared (truncated to zero length)
        /// when the application starts. This is useful for keeping logs from growing
        /// too large over time, but means that information from previous runs will be lost.
        /// <para>
        /// For production systems, it's often better to set this to false and implement
        /// a log rotation strategy instead.
        /// </para>
        /// </remarks>
        [DefaultValue(DefaultClearLogOnStartup)]
        [ConfigName("bClearLogOnStartup")]
        public bool ClearLogOnStartup { get; set; } = DefaultClearLogOnStartup;

        /// <summary>
        /// Gets or sets a value indicating whether to use session-based logging.
        /// When true, each application run gets its own unique log file.
        /// </summary>
        [DefaultValue(DefaultUseSessionLogging)]
        [ConfigName("bUseSessionLogging")]
        public bool UseSessionLogging { get; set; } = DefaultUseSessionLogging;

        /// <summary>
        /// Gets the default log file path.
        /// </summary>
        /// <returns>The default log file path.</returns>
        /// <remarks>
        /// This method is called automatically by the configuration system when the
        /// <see cref="LogFilePath"/> property is not explicitly set. It generates a
        /// default path based on the executing assembly's location and name.
        /// <para>
        /// The method attempts to get the entry assembly first (the main application),
        /// falling back to the calling assembly, and finally the executing assembly
        /// if neither of those are available.
        /// </para>
        /// </remarks>
        private static string GetDefaultLogPath()
        {
            // paramName is ignored because the default log file path is determined by the calling assembly.

            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly() ?? Assembly.GetExecutingAssembly();
            return Path.Combine(Path.GetDirectoryName(asm.Location), $"{asm.GetName().Name}.log");
        }
    }
}