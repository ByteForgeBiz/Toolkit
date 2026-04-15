using ByteForge.Toolkit.Configuration;
using System.ComponentModel;

namespace ByteForge.Toolkit.Logging;

/*
 *  ___       _        _                  _                            ___       _   _             
 * |   \ __ _| |_ __ _| |__  __ _ ___ ___| |   ___  __ _ __ _ ___ _ _ / _ \ _ __| |_(_)___ _ _  ___
 * | |) / _` |  _/ _` | '_ \/ _` (_-</ -_) |__/ _ \/ _` / _` / -_) '_| (_) | '_ \  _| / _ \ ' \(_-<
 * |___/\__,_|\__\__,_|_.__/\__,_/__/\___|____\___/\__, \__, \___|_|  \___/| .__/\__|_\___/_||_/__/
 *                                                 |___/|___/              |_|                     
 */
/// <summary>
/// Configuration for the <see cref="DatabaseLogger"/>.
/// </summary>
public class DatabaseLoggerOptions
{
    private const bool DefaultEnabled = true;
    private const bool DefaultAutoCreateTable = true;
    private const LogLevel DefaultMinLogLevel = LogLevel.All;
    private const string DefaultTableName = "__ApplicationLogs__";

    /// <summary>
    /// Gets or sets a value indicating whether the database logger is enabled when selected by the main logging settings.
    /// </summary>
    [DefaultValue(DefaultEnabled)]
    [ConfigName("bEnabled")]
    public bool Enabled { get; set; } = DefaultEnabled;

    /// <summary>
    /// Gets or sets the optional named database configuration section to use for log persistence.
    /// </summary>
    [ConfigName("sDatabaseSection")]
    public string? DatabaseSection { get; set; }

    /// <summary>
    /// Gets or sets the name of the table used to store log entries.
    /// </summary>
    [DefaultValue(DefaultTableName)]
    [ConfigName("sTableName")]
    public string TableName { get; set; } = DefaultTableName;

    /// <summary>
    /// Gets or sets a value indicating whether the logger should create the table if it does not exist.
    /// </summary>
    [DefaultValue(DefaultAutoCreateTable)]
    [ConfigName("bAutoCreateTable")]
    public bool AutoCreateTable { get; set; } = DefaultAutoCreateTable;

    /// <summary>
    /// Gets or sets the minimum level written to the database sink.
    /// </summary>
    [DefaultValue(DefaultMinLogLevel)]
    [ConfigName("eMinLogLevel")]
    public LogLevel MinLogLevel { get; set; } = DefaultMinLogLevel;
}
