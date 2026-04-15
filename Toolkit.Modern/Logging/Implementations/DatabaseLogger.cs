using ByteForge.Toolkit.Data;
using System.Text.RegularExpressions;

/*
 * This may look weird, but it's necessary to use string.Contains in .NET48.
 */
#if NET20_OR_GREATER
using ByteForge.Toolkit.Utilities;
#elif NET5_0_OR_GREATER
#endif

namespace ByteForge.Toolkit.Logging;

/*
 *  ___       _        _                  _                           
 * |   \ __ _| |_ __ _| |__  __ _ ___ ___| |   ___  __ _ __ _ ___ _ _ 
 * | |) / _` |  _/ _` | '_ \/ _` (_-</ -_) |__/ _ \/ _` / _` / -_) '_|
 * |___/\__,_|\__\__,_|_.__/\__,_/__/\___|____\___/\__, \__, \___|_|  
 *                                                 |___/|___/         
 */
/// <summary>
/// Logger implementation that persists entries to a database table.
/// </summary>
public partial class DatabaseLogger : BaseLogger
{
    /// <summary>
    /// Pattern used to validate SQL table identifiers.
    /// </summary>
#if NET20_OR_GREATER
    private static readonly Regex ValidIdentifierPattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
#elif NET5_0_OR_GREATER
    private static readonly Regex ValidIdentifierPattern = ValidIdentifyerRegex();
#endif

    /// <summary>
    /// Synchronization root for initialization.
    /// </summary>
    private readonly object _syncRoot = new();

    /// <summary>
    /// The database options explicitly configured for this logger, if any.
    /// </summary>
    private readonly DatabaseOptions? _configuredDatabaseOptions;

    /// <summary>
    /// Indicates whether initialization has been attempted.
    /// </summary>
    private bool _initializationAttempted;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseLogger"/> class using configuration-based database resolution.
    /// </summary>
    /// <param name="options">Optional settings for the database logger.</param>
    public DatabaseLogger(DatabaseLoggerOptions? options = null) : this(null, options) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseLogger"/> class with explicit database settings.
    /// </summary>
    /// <param name="databaseOptions">The database options used by this logger.</param>
    /// <param name="options">Optional settings for the database logger.</param>
    public DatabaseLogger(DatabaseOptions? databaseOptions, DatabaseLoggerOptions? options = null) : base("Database")
    {
        _configuredDatabaseOptions = databaseOptions;
        Settings = options
            ?? (Configuration.Configuration.IsInitialized
                ? Configuration.Configuration.GetSection<DatabaseLoggerOptions>("DatabaseLogger")
                : new DatabaseLoggerOptions());

        MinLogLevel = Settings.MinLogLevel;
    }

    /// <summary>
    /// Gets the logger settings.
    /// </summary>
    public DatabaseLoggerOptions Settings { get; }

    /// <summary>
    /// Gets a value indicating whether the logger has been disabled due to missing configuration or a runtime failure.
    /// </summary>
    internal bool IsDisabled { get; private set; }

    /// <summary>
    /// Gets the resolved database options after initialization.
    /// </summary>
    internal DatabaseOptions? ResolvedDatabaseOptions { get; private set; }

    /// <inheritdoc />
    protected internal override void RecordLogEntry(LogEntry entry)
    {
        if (IsDisabled)
            return;

        EnsureInitialized();
        if (IsDisabled || ResolvedDatabaseOptions == null)
            return;

        try
        {
            using var _ = Logging.Log.BeginSuppressedScope();
            var db = CreateDbAccess(ResolvedDatabaseOptions);
            if (!ExecuteNonQuery(db, BuildInsertStatement(), GetInsertArguments(entry)))
                Disable();
        }
        catch
        {
            Disable();
        }
    }

    /// <summary>
    /// Ensures the logger is initialized and ready to log, or disables it if configuration is invalid.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initializationAttempted || IsDisabled)
            return;

        lock (_syncRoot)
        {
            if (_initializationAttempted || IsDisabled)
                return;

            _initializationAttempted = true;

            try
            {
                if (!Settings.Enabled || !IsValidTableName(Settings.TableName))
                {
                    Disable();
                    return;
                }

                ResolvedDatabaseOptions = ResolveDatabaseOptions();
                if (ResolvedDatabaseOptions == null)
                {
                    Disable();
                    return;
                }

                using var _ = Logging.Log.BeginSuppressedScope();
                var db = CreateDbAccess(ResolvedDatabaseOptions);

                if (Settings.AutoCreateTable && !TryEnsureTableExists(db, ResolvedDatabaseOptions))
                {
                    Disable();
                    return;
                }
            }
            catch
            {
                Disable();
            }
        }
    }

    /// <summary>
    /// Ensures the log table exists in the database, creating it if allowed.
    /// </summary>
    /// <param name="db">The database access object.</param>
    /// <param name="databaseOptions">The database options.</param>
    /// <returns>True if the table exists or was created; otherwise, false.</returns>
    internal bool TryEnsureTableExists(DBAccess db, DatabaseOptions databaseOptions)
    {
        if (DoesTableExist(db, databaseOptions.DatabaseType))
            return true;

        if (!CanAutoCreate(databaseOptions))
            return false;

        return ExecuteNonQuery(db, BuildCreateTableStatement(databaseOptions.DatabaseType));
    }

    /// <summary>
    /// Resolves the database options to use for logging.
    /// </summary>
    /// <returns>The resolved <see cref="DatabaseOptions"/> or null if not available.</returns>
    internal DatabaseOptions? ResolveDatabaseOptions()
    {
        if (_configuredDatabaseOptions != null)
            return IsUsableDatabaseOptions(_configuredDatabaseOptions) ? _configuredDatabaseOptions : null;

        if (!Configuration.Configuration.IsInitialized)
            return null;

        var sectionName = Settings.DatabaseSection!;
        if (!string.IsNullOrWhiteSpace(sectionName))
        {
            var namedSection = DatabaseLogger.GetDatabaseOptionsFromSection(sectionName);
            if (namedSection != null)
                return namedSection;
        }

        var rootOptions = Configuration.Configuration.GetSection<DatabaseRootOptions>("Data Source");
        if (!string.IsNullOrWhiteSpace(rootOptions.SelectedDatabase))
            return DatabaseLogger.GetDatabaseOptionsFromSection(rootOptions.SelectedDatabase);

        return null;
    }

    /// <summary>
    /// Determines if the logger can auto-create the log table for the given database options.
    /// </summary>
    /// <param name="databaseOptions">The database options.</param>
    /// <returns>True if auto-creation is allowed; otherwise, false.</returns>
    internal bool CanAutoCreate(DatabaseOptions databaseOptions)
    {
        if (!Settings.AutoCreateTable)
            return false;

        return databaseOptions.DatabaseType switch
        {
            DBAccess.DataBaseType.SQLServer => true,
            DBAccess.DataBaseType.ODBC => IsKnownOdbcProvider(databaseOptions),
            _ => false,
        };
    }

    /// <summary>
    /// Builds the SQL insert statement for the specified database type.
    /// </summary>
    /// <returns>The SQL insert statement.</returns>
    internal string BuildInsertStatement()
    {
        return _buildInsertStatementCache ??= 
            $@"INSERT INTO {QuoteIdentifier(Settings.TableName)}
                ([LogTimestamp], 
                 [LogLevel], 
                 [LoggerName], 
                 [Source], 
                 [Message], 
                 [CorrelationId], 
                 [ThreadId], 
                 [ExceptionType], 
                 [ExceptionMessage], 
                 [ExceptionStackTrace])
                VALUES
                (@LogTimestamp, 
                 @LogLevel, 
                 @LoggerName, 
                 @Source, 
                 @Message, 
                 @CorrelationId, 
                 @ThreadId, 
                 @ExceptionType, 
                 @ExceptionMessage, 
                 @ExceptionStackTrace)";
    }
    private string? _buildInsertStatementCache;

    /// <summary>
    /// Builds the SQL create table statement for the specified database type.
    /// </summary>
    /// <param name="databaseType">The database type.</param>
    /// <returns>The SQL create table statement.</returns>
    internal string BuildCreateTableStatement(DBAccess.DataBaseType databaseType)
    {
        var tableName = QuoteIdentifier(Settings.TableName);

        return databaseType switch
        {
            DBAccess.DataBaseType.SQLServer => $@"IF OBJECT_ID(N'{Settings.TableName}', N'U') IS NULL
BEGIN
    CREATE TABLE {tableName}
    (
        [LogTimestamp] DATETIME2 NOT NULL,
        [LogLevel] NVARCHAR(32) NOT NULL,
        [LoggerName] NVARCHAR(255) NULL,
        [Source] NVARCHAR(255) NULL,
        [Message] NVARCHAR(MAX) NULL,
        [CorrelationId] NVARCHAR(255) NULL,
        [ThreadId] INT NULL,
        [ExceptionType] NVARCHAR(255) NULL,
        [ExceptionMessage] NVARCHAR(MAX) NULL,
        [ExceptionStackTrace] NVARCHAR(MAX) NULL
    );
END",
            DBAccess.DataBaseType.ODBC => $@"CREATE TABLE {tableName}
(
    [LogTimestamp] DATETIME NOT NULL,
    [LogLevel] TEXT(32) NOT NULL,
    [LoggerName] TEXT(255),
    [Source] TEXT(255),
    [Message] MEMO,
    [CorrelationId] TEXT(255),
    [ThreadId] INTEGER,
    [ExceptionType] TEXT(255),
    [ExceptionMessage] MEMO,
    [ExceptionStackTrace] MEMO
)",
            _ => throw new NotSupportedException($"The database type {databaseType} is not supported."),
        };
    }

    /// <summary>
    /// Gets the argument values for the insert statement from the log entry.
    /// </summary>
    /// <param name="entry">The log entry.</param>
    /// <returns>An array of argument values.</returns>
    internal object?[] GetInsertArguments(LogEntry entry)
    {
        return
        [
            entry.Timestamp,
            entry.Level.ToString(),
            entry.Properties.TryGetValue("LoggerName", out var loggerName) ? loggerName?.ToString() : Name,
            entry.Source,
            entry.Message,
            entry.CorrelationId,
            GetThreadId(entry.Properties),
            entry.Exception?.GetType().FullName,
            entry.Exception?.Message,
            entry.Exception?.ToString()
        ];
    }

    /// <summary>
    /// Creates a <see cref="DBAccess"/> instance for the given database options.
    /// </summary>
    /// <param name="databaseOptions">The database options.</param>
    /// <returns>A <see cref="DBAccess"/> instance.</returns>
    protected virtual DBAccess CreateDbAccess(DatabaseOptions databaseOptions) => new(databaseOptions);

    /// <summary>
    /// Executes a non-query SQL command.
    /// </summary>
    /// <param name="db">The database access object.</param>
    /// <param name="query">The SQL query to execute.</param>
    /// <param name="arguments">The query arguments.</param>
    /// <returns>True if the command succeeded; otherwise, false.</returns>
    protected virtual bool ExecuteNonQuery(DBAccess db, string query, params object?[]? arguments) => db.ExecuteQuery(query, arguments);

    /// <summary>
    /// Checks if the log table exists in the database.
    /// </summary>
    /// <param name="db">The database access object.</param>
    /// <param name="databaseType">The database type.</param>
    /// <returns>True if the table exists; otherwise, false.</returns>
    protected virtual bool DoesTableExist(DBAccess db, DBAccess.DataBaseType databaseType)
    {
        return databaseType switch
        {
            DBAccess.DataBaseType.SQLServer => db.TryGetValue<int>(out var value, "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName", Settings.TableName) && value > 0,
            DBAccess.DataBaseType.ODBC => ExecuteNonQuery(db, $"SELECT * FROM {QuoteIdentifier(Settings.TableName)} WHERE 1 = 0"),
            _ => false,
        };
    }

    /// <summary>
    /// Gets the thread ID from the log entry properties, if present.
    /// </summary>
    /// <param name="properties">The log entry properties.</param>
    /// <returns>The thread ID, or null if not present or invalid.</returns>
    private static int? GetThreadId(IDictionary<string, object> properties)
    {
        if (!properties.TryGetValue("ThreadId", out var threadId) || threadId == null)
            return null;

        try
        {
            return Convert.ToInt32(threadId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets database options from a configuration section.
    /// </summary>
    /// <param name="sectionName">The configuration section name.</param>
    /// <returns>The database options, or null if not found or invalid.</returns>
    private static DatabaseOptions? GetDatabaseOptionsFromSection(string sectionName)
    {
        var sectionValues = Configuration.Configuration.GetSectionValues(sectionName);
        if (sectionValues.Count == 0)
            return null;

        var options = Configuration.Configuration.GetSection<DatabaseOptions>(sectionName);
        return IsUsableDatabaseOptions(options) ? options : null;
    }

    /// <summary>
    /// Determines if the given database options are usable for logging.
    /// </summary>
    /// <param name="options">The database options.</param>
    /// <returns>True if usable; otherwise, false.</returns>
    private static bool IsUsableDatabaseOptions(DatabaseOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            return true;

        return options.DatabaseType switch
        {
            DBAccess.DataBaseType.SQLServer => !string.IsNullOrWhiteSpace(options.Server) && !string.IsNullOrWhiteSpace(options.DatabaseName),
            DBAccess.DataBaseType.ODBC => !string.IsNullOrWhiteSpace(options.ServerDSN) ||
                                          !string.IsNullOrWhiteSpace(options.Server) ||
                                          !string.IsNullOrWhiteSpace(options.DatabaseName),
            _ => false,
        };
    }

    /// <summary>
    /// Determines if the ODBC provider is a known supported type (e.g., Access).
    /// </summary>
    /// <param name="options">The database options.</param>
    /// <returns>True if the provider is known; otherwise, false.</returns>
    internal static bool IsKnownOdbcProvider(DatabaseOptions options)
    {
        var connectionText = $"{options.ConnectionString} {options.ServerDSN} {options.Server} {options.DatabaseName}"!;
        return connectionText.Contains(".mdb", StringComparison.OrdinalIgnoreCase) ||
               connectionText.Contains(".accdb", StringComparison.OrdinalIgnoreCase) ||
               connectionText.Contains("Access Driver", StringComparison.OrdinalIgnoreCase) ||
               connectionText.Contains("Microsoft Access", StringComparison.OrdinalIgnoreCase) ||
               connectionText.Contains("ACE", StringComparison.OrdinalIgnoreCase) ||
               connectionText.Contains("Jet OLEDB", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates that the table name is a valid SQL identifier.
    /// </summary>
    /// <param name="tableName">The table name to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    internal static bool IsValidTableName(string tableName) => !string.IsNullOrWhiteSpace(tableName) && ValidIdentifierPattern.IsMatch(tableName);

    /// <summary>
    /// Quotes an identifier for use in SQL statements.
    /// </summary>
    /// <param name="identifier">The identifier to quote.</param>
    /// <returns>The quoted identifier.</returns>
    internal static string QuoteIdentifier(string identifier) => $"[{identifier}]";

    /// <summary>
    /// Disables the logger, preventing further logging.
    /// </summary>
    private void Disable()
    {
        IsDisabled = true;
    }

#if NET5_0_OR_GREATER
    /// <summary>
    /// Provides a compiled regular expression that matches valid identifier names consisting of letters, digits, and
    /// underscores, starting with a letter or underscore.
    /// </summary>
    /// <remarks>The regular expression enforces that the identifier starts with an uppercase or lowercase
    /// letter or an underscore, followed by any combination of letters, digits, or underscores. This pattern is
    /// commonly used to validate programming language identifiers.</remarks>
    /// <returns>A compiled <see cref="Regex"/> instance that matches strings conforming to the identifier naming pattern.</returns>
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex ValidIdentifyerRegex();
#endif
}
