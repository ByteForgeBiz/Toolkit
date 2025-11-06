using System;
using System.ComponentModel;

namespace ByteForge.Toolkit.Logging;
/*
 *  ___            _          ___ _ _     _                            ___       _   _             
 * / __| ___ _____(_)___ _ _ | __(_) |___| |   ___  __ _ __ _ ___ _ _ / _ \ _ __| |_(_)___ _ _  ___
 * \__ \/ -_)_-<_-< / _ \ ' \| _|| | / -_) |__/ _ \/ _` / _` / -_) '_| (_) | '_ \  _| / _ \ ' \(_-<
 * |___/\___/__/__/_\___/_||_|_| |_|_\___|____\___/\__, \__, \___|_|  \___/| .__/\__|_\___/_||_/__/
 *                                                 |___/|___/              |_|                     
 */
/// <summary>
/// Configuration options for the <see cref="SessionFileLogger"/>.
/// Inherits all file logger options and adds session-specific settings.
/// </summary>
public class SessionFileLoggerOptions : FileLoggerOptions
{
    private const SessionIdFormat DefaultSessionIdFormat = SessionIdFormat.Timestamp;
    private const bool DefaultWriteSessionHeader = true;
    private const bool DefaultWriteSessionFooter = true;
    private const bool DefaultCleanupOldSessions = true;
    private const string DefaultFileNamingPattern = "{basename}_{sessionid}";

    /// <inheritdoc/>
    [DefaultValue(DefaultFileNamingPattern)]
    public override string FileNamingPattern { get; set; } = DefaultFileNamingPattern;

    /// <summary>
    /// Gets or sets the format for generating session IDs.
    /// The default is <see cref="SessionIdFormat.Timestamp"/>.
    /// This is used to replace the {sessionid} placeholder in the FileNamingPattern.
    /// </summary>
    [DefaultValue(DefaultSessionIdFormat)]
    public SessionIdFormat SessionIdFormat { get; set; } = DefaultSessionIdFormat;

    /// <summary>
    /// Gets or sets a value indicating whether to write a session header with metadata
    /// at the beginning of the log file.
    /// The default is <see langword="true"/>.
    /// </summary>
    [DefaultValue(DefaultWriteSessionHeader)]
    public bool WriteSessionHeader { get; set; } = DefaultWriteSessionHeader;

    /// <summary>
    /// Gets or sets a value indicating whether to write a session footer when closing
    /// the log file.
    /// The default is <see langword="true"/>.
    /// </summary>
    [DefaultValue(DefaultWriteSessionFooter)]
    public bool WriteSessionFooter { get; set; } = DefaultWriteSessionFooter;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically clean up old session files
    /// on startup, according to <see cref="AsyncOptions.RetentionDays"/>.
    /// The default is <see langword="true"/>.
    /// </summary>
    [DefaultValue(DefaultCleanupOldSessions)]
    public bool CleanupOldSessions { get; set; } = DefaultCleanupOldSessions;

    /// <summary>
    /// Gets or sets a custom session ID provider function.
    /// Only used when <see cref="SessionIdFormat"/> is set to <see cref="SessionIdFormat.Custom"/>.
    /// The returned value is used to replace the {sessionid} placeholder in the FileNamingPattern.
    /// </summary>
    public Func<string>? CustomSessionIdProvider { get; set; }
}

/// <summary>
/// Specifies the format for generating session IDs.
/// </summary>
public enum SessionIdFormat
{
    /// <summary>
    /// Uses timestamp format: yyyyMMdd-HHmmss.
    /// </summary>
    Timestamp,

    /// <summary>
    /// Uses timestamp with milliseconds: yyyyMMdd-HHmmss-fff.
    /// </summary>
    TimestampWithMilliseconds,

    /// <summary>
    /// Uses the first 8 characters of a GUID.
    /// </summary>
    Guid,

    /// <summary>
    /// Uses timestamp with process ID: yyyyMMdd-HHmmss-P1234.
    /// </summary>
    ProcessId,

    /// <summary>
    /// Uses a custom provider function for the session ID.
    /// </summary>
    Custom
}
