namespace ByteForge.Toolkit.Logging;
/*
 *  _              _                _ 
 * | |   ___  __ _| |   _____ _____| |
 * | |__/ _ \/ _` | |__/ -_) V / -_) |
 * |____\___/\__, |____\___|\_/\___|_|
 *           |___/                    
 */
/// <summary>
/// Specifies the level of logging.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Logs that contain extremely detailed messages, typically showing every step of program execution.
    /// These messages are primarily for tracing code execution and may contain sensitive application data.
    /// </summary>
    Trace,

    /// <summary>
    /// Logs that are used for interactive investigation during development.
    /// These logs should primarily contain information useful for debugging and have no long-term value.
    /// </summary>
    Debug,

    /// <summary>
    /// Logs that are detailed and contain extended information beyond what's necessary for basic application monitoring.
    /// While less detailed than Trace, these logs still provide significant granularity for troubleshooting.
    /// </summary>
    Verbose,

    /// <summary>
    /// Logs that track the general flow of the application.
    /// These logs should have long-term value and provide context for understanding normal application behavior.
    /// </summary>
    Info,

    /// <summary>
    /// Logs that highlight noteworthy events that are still considered normal operation.
    /// These events might deserve special handling or attention but don't represent a problem condition.
    /// </summary>
    Notice,

    /// <summary>
    /// Logs that highlight abnormal or unexpected events in the application flow.
    /// These represent potential problems or situations that might lead to errors if not addressed,
    /// but do not cause the application execution to stop.
    /// </summary>
    Warning,

    /// <summary>
    /// Logs that highlight when the current flow of execution is stopped due to a failure.
    /// These indicate a failure in the current activity or operation, but not an application-wide failure.
    /// The application can typically continue running despite the error.
    /// </summary>
    Error,

    /// <summary>
    /// Logs that describe severe error conditions that might still allow the application to continue running,
    /// but require immediate attention and likely intervention to prevent system-wide failures.
    /// </summary>
    Critical,

    /// <summary>
    /// Logs that describe unrecoverable conditions that result in immediate application termination.
    /// These represent the most severe error conditions that prevent the application from continuing to run.
    /// </summary>
    Fatal,

    /// <summary>
    /// Specifies that no logging should be performed.
    /// </summary>
    None = int.MaxValue,

    /// <summary>
    /// Specifies that logging should be performed at all defined levels.
    /// </summary>
    All = int.MinValue,
}
