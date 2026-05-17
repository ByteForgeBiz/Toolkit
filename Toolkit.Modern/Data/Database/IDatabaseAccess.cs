using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace ByteForge.Toolkit.Data
{
    /// <summary>
    /// Defines the core database access operations shared by legacy and parallel implementations.
    /// </summary>
    public interface IDatabaseAccess
    {
        /// <summary>
        /// Gets the database options for this instance.
        /// </summary>
        DatabaseOptions Options { get; }

        /// <summary>
        /// Gets the configured database type.
        /// </summary>
        DBAccess.DataBaseType DbType { get; }

        /// <summary>
        /// Gets the connection string for the database.
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Gets the number of records affected by the last executed query.
        /// </summary>
        int RecordsAffected { get; }

        /// <summary>
        /// Gets the last exception that occurred during a database operation.
        /// </summary>
        Exception? LastException { get; }

        /// <summary>
        /// Tests the database connection.
        /// </summary>
        bool TestConnection();

        /// <summary>
        /// Tests the database connection asynchronously.
        /// </summary>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Tries to get a value of type <typeparamref name="T"/>.
        /// </summary>
        bool TryGetValue<T>(out T? value, string query, params object?[]? arguments);

        /// <summary>
        /// Gets a value of type <typeparamref name="T"/>.
        /// </summary>
        T? GetValue<T>(string query, params object?[]? arguments);

        /// <summary>
        /// Tries to get a value of type <typeparamref name="T"/> asynchronously.
        /// </summary>
        Task<(bool Success, T? Value)> TryGetValueAsync<T>(string query, CancellationToken cancellationToken, params object?[]? arguments);

        /// <summary>
        /// Gets a value of type <typeparamref name="T"/> asynchronously.
        /// </summary>
        Task<T?> GetValueAsync<T>(string query, CancellationToken cancellationToken, params object?[]? arguments);

        /// <summary>
        /// Tries to get an untyped value.
        /// </summary>
        bool TryGetValue(out object? value, string query, params object?[]? arguments);

        /// <summary>
        /// Gets an untyped value.
        /// </summary>
        object? GetValue(string query, params object?[]? arguments);

        /// <summary>
        /// Tries to get an untyped value asynchronously.
        /// </summary>
        Task<(bool Success, object? Value)> TryGetValueAsync(string query, CancellationToken cancellationToken, params object?[]? arguments);

        /// <summary>
        /// Gets an untyped value asynchronously.
        /// </summary>
        Task<object?> GetValueAsync(string query, CancellationToken cancellationToken, params object?[]? arguments);

        /// <summary>
        /// Gets the first matching record.
        /// </summary>
        DataRow? GetRecord(string query, params object?[]? arguments);

        /// <summary>
        /// Gets the first matching record converted to <typeparamref name="T"/>.
        /// </summary>
        T GetRecord<T>(string query, params object?[]? arguments) where T : class, new();

        /// <summary>
        /// Gets the first matching record asynchronously.
        /// </summary>
        Task<DataRow?> GetRecordAsync(string query, CancellationToken cancellationToken, params object?[]? arguments);

        /// <summary>
        /// Gets matching records converted to <typeparamref name="T"/>.
        /// </summary>
        T[] GetRecords<T>(string query, params object?[]? arguments) where T : class, new();

        /// <summary>
        /// Gets matching records converted to <typeparamref name="T"/> asynchronously.
        /// </summary>
        Task<T[]> GetRecordsAsync<T>(string query, CancellationToken cancellationToken, params object?[]? arguments) where T : class, new();

        /// <summary>
        /// Gets matching records asynchronously.
        /// </summary>
        Task<DataRowCollection?> GetRecordsAsync(string query, CancellationToken cancellationToken, params object?[]? arguments);

        /// <summary>
        /// Gets matching records.
        /// </summary>
        DataRowCollection? GetRecords(string query, params object?[]? arguments);

        /// <summary>
        /// Executes a non-query asynchronously.
        /// </summary>
        Task<bool> ExecuteQueryAsync(string query, CancellationToken cancellationToken, params object?[]? arguments);

        /// <summary>
        /// Executes a non-query.
        /// </summary>
        bool ExecuteQuery(string query, params object?[]? arguments);

        /// <summary>
        /// Executes a SQL script.
        /// </summary>
        ScriptExecutionResult ExecuteScript(string script, object[]? arguments = null, bool captureResults = false);

        /// <summary>
        /// Executes a SQL script asynchronously.
        /// </summary>
        Task<ScriptExecutionResult> ExecuteScriptAsync(string script, CancellationToken cancellationToken, object[]? arguments = null, bool captureResults = false);
    }
}
