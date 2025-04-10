using ByteForge.Toolkit.Logging;
using System.IO;
using System.Reflection;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Represents the options for configuring the logging system.
    /// </summary>
    /// <remarks>
    /// This class provides configuration settings that control how the <see cref="Log"/>
    /// system behaves, including where log files are stored, what level of detail is
    /// included in logs, and whether logs are cleared on application startup.
    /// <para>
    /// LogOptions is typically loaded from configuration files using the <see cref="Configuration"/>
    /// system, with property names mapped through <see cref="PropertyNameAttribute"/>.
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
    public class LogOptions
    {
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
        [PropertyName("sLogFile", "GetDefaultLogPath")]
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
        ///   <item><see cref="LogLevel.Debug"/> - Debugging information</item>
        ///   <item><see cref="LogLevel.Verbose"/> - Very detailed information</item>
        ///   <item><see cref="LogLevel.Info"/> - Normal application flow information</item>
        ///   <item><see cref="LogLevel.Warning"/> - Potential issues that don't stop execution</item>
        ///   <item><see cref="LogLevel.Error"/> - Errors that allow continued execution</item>
        ///   <item><see cref="LogLevel.Critical"/> - Critical errors that stop execution</item>
        /// </list>
        /// </para>
        /// <para>
        /// For example, setting this to <see cref="LogLevel.Info"/> will log Info, Warning,
        /// Error, and Fatal messages, but ignore Trace and Debug messages.
        /// </para>
        /// </remarks>
        [PropertyName("eTraceLogLevel")]
        public LogLevel TraceLogLevel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the log should be cleared on startup.
        /// </summary>
        /// <value>
        /// <c>true</c> if the log should be cleared on startup; otherwise, <c>false</c>.
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
        [PropertyName("bClearLogOnStartup")]
        public bool ClearLogOnStartup { get; set; }

        /// <summary>
        /// Gets the default log file path.
        /// </summary>
        /// <param name="paramName">The name of the parameter (ignored).</param>
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
        private static string GetDefaultLogPath(string paramName)
        {
            // paramName is ignored because the default log file path is determined by the calling assembly.

            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly() ?? Assembly.GetExecutingAssembly();
            return Path.Combine(Path.GetDirectoryName(asm.Location), $"{asm.GetName().Name}.log");
        }
    }
}