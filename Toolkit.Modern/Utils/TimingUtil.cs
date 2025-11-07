using ByteForge.Toolkit.Logging;

namespace ByteForge.Toolkit.Utils;
/*
 *  _____ _       _           _   _ _   _ _ 
 * |_   _(_)_ __ (_)_ _  __ _| | | | |_(_) |
 *   | | | | '  \| | ' \/ _` | |_| |  _| | |
 *   |_| |_|_|_|_|_|_||_\__, |\___/ \__|_|_|
 *                      |___/               
 */
/// <summary>
/// Utility class for measuring and logging the time taken to execute actions and functions.
/// </summary>
public class TimingUtil
{
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimingUtil"/> class.
    /// Sets the log level to Debug and initializes the logger to log to a file in the same directory as the executing assembly.
    /// </summary>
    public TimingUtil()
    {
        logger = Log.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimingUtil"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to be used for logging.</param>
    public TimingUtil(ILogger logger)
    {
        this.logger = logger;
        logger.MinLogLevel = LogLevel.Debug;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimingUtil"/> class with the specified log file.
    /// </summary>
    /// <param name="logFile">The path to the log file.</param>
    public TimingUtil(string logFile)
    {
        this.logger = new FileLogger(logFile);
        logger.MinLogLevel = LogLevel.Debug;
    }

    /// <summary>
    /// Measures the time taken to execute the specified action and logs the elapsed time.
    /// </summary>
    /// <param name="action">The action to be executed and timed.</param>
    /// <param name="message">An optional message to be logged with the elapsed time.</param>
    public void Time(Action action, string message  = "")
    {
        var start = DateTime.Now;
        action();
        var elapsed = DateTime.Now - start;
        logger.LogDebug($"{message} ({elapsed.TotalMilliseconds}ms)");
    }

    /// <summary>
    /// Measures the time taken to execute the specified function and logs the elapsed time.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="action">The function to be executed and timed.</param>
    /// <param name="message">An optional message to be logged with the elapsed time.</param>
    /// <returns>The result of the function execution.</returns>
    public T Time<T>(Func<T> action, string message  = "")
    {
        var start = DateTime.Now;
        var result = action();
        var elapsed = DateTime.Now - start;
        logger.LogDebug($"{message} ({elapsed.TotalMilliseconds}ms)");
        return result;
    }

    /// <summary>
    /// Asynchronously measures the time taken to execute the specified asynchronous action and logs the elapsed time.
    /// </summary>
    /// <param name="action">The asynchronous action to be executed and timed.</param>
    /// <param name="message">An optional message to be logged with the elapsed time.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task TimeAsync(Func<Task> action, string message  = "")
    {
        var start = DateTime.Now;
        await action();
        var elapsed = DateTime.Now - start;
        logger.LogDebug($"{message} ({elapsed.TotalMilliseconds}ms)");
    }
}
