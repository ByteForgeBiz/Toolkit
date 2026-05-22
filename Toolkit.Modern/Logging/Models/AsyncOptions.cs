using System.ComponentModel;

namespace ByteForge.Toolkit.Logging;
/*
 *    _                     ___       _   _             
 *   /_\   ____  _ _ _  __ / _ \ _ __| |_(_)___ _ _  ___
 *  / _ \ (_-< || | ' \/ _| (_) | '_ \  _| / _ \ ' \(_-<
 * /_/ \_\/__/\_, |_||_\__|\___/| .__/\__|_\___/_||_/__/
 *            |__/              |_|                     
 */
/// <summary>
/// Provides base options for asynchronous logging configuration.
/// </summary>
[Description("Asynchronous logging queue and retention settings.")]
public abstract class AsyncOptions
{
    private const int DefaultRetentionDays = 0;
    private const bool DefaultUseAsyncLogging = false;
    private const int DefaultAsyncQueueSize = 1000;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncOptions"/> class.
    /// </summary>
    protected AsyncOptions() { }

    /// <summary>
    /// Gets or sets the number of days to retain log files.
    /// A value of 0 indicates unlimited retention.
    /// </summary>
    [DefaultValue(DefaultRetentionDays)]
    [Description("Number of days to retain log files. Use 0 for unlimited retention.")]
    public int RetentionDays { get; set; } = DefaultRetentionDays;

    /// <summary>
    /// Gets or sets a value indicating whether to use asynchronous logging.
    /// </summary>
    [DefaultValue(DefaultUseAsyncLogging)]
    [Description("Writes log entries through an asynchronous queue when enabled.")]
    public bool UseAsyncLogging { get; set; } = DefaultUseAsyncLogging;

    /// <summary>
    /// Gets or sets the size of the asynchronous logging queue.
    /// The default value is 1000.
    /// </summary>
    [DefaultValue(DefaultAsyncQueueSize)]
    [Description("Maximum number of log entries buffered by the asynchronous queue.")]
    public int AsyncQueueSize { get; set; } = DefaultAsyncQueueSize;
}
