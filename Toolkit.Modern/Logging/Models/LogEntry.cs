namespace ByteForge.Toolkit.Logging;
/*
 *  _              ___     _            
 * | |   ___  __ _| __|_ _| |_ _ _ _  _ 
 * | |__/ _ \/ _` | _|| ' \  _| '_| || |
 * |____\___/\__, |___|_||_\__|_|  \_, |
 *           |___/                 |__/ 
 */
/// <summary>
/// Represents a log entry with details about the log message, level, timestamp, and other properties.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets or sets the timestamp of the log entry.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID associated with the log entry.
    /// </summary>
    public string? CorrelationId  { get; set; }

    /// <summary>
    /// Gets or sets the source of the log entry. 
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the log level of the log entry.
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    public string? Message  { get; set; }

    /// <summary>
    /// Gets or sets additional properties associated with the log entry.
    /// </summary>
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the exception associated with the log entry, if any.
    /// </summary>
    public Exception? Exception { get; set; }
}
