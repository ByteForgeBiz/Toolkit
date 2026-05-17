using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace ByteForge.Toolkit.Data;

public partial class DBAccess
{
    /// <summary>
    /// Tests the database connection asynchronously while accepting a cancellation token.
    /// </summary>
    /// <param name="cancellationToken">The token that can cancel the operation before it starts.</param>
    /// <returns>A task whose result indicates whether the database connection succeeded.</returns>
    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(TestConnection, cancellationToken);
    }

    /// <summary>
    /// Tries to get a typed value asynchronously while accepting a cancellation token.
    /// </summary>
    /// <typeparam name="T">The value type to return.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The token that can cancel the operation before it starts.</param>
    /// <param name="arguments">The query parameter values.</param>
    /// <returns>A task whose result contains the success flag and value.</returns>
    public Task<(bool Success, T? Value)> TryGetValueAsync<T>(string query, CancellationToken cancellationToken, params object?[]? arguments)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            var success = TryGetValue<T>(out var value, query, arguments);
            return (success, value);
        }, cancellationToken);
    }

    /// <summary>
    /// Gets a typed value asynchronously while accepting a cancellation token.
    /// </summary>
    /// <typeparam name="T">The value type to return.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The token that can cancel the operation before it starts.</param>
    /// <param name="arguments">The query parameter values.</param>
    /// <returns>A task whose result contains the value.</returns>
    public Task<T?> GetValueAsync<T>(string query, CancellationToken cancellationToken, params object?[]? arguments)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => GetValue<T>(query, arguments), cancellationToken);
    }

    /// <summary>
    /// Tries to get an untyped value asynchronously while accepting a cancellation token.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The token that can cancel the operation before it starts.</param>
    /// <param name="arguments">The query parameter values.</param>
    /// <returns>A task whose result contains the success flag and value.</returns>
    public Task<(bool Success, object? Value)> TryGetValueAsync(string query, CancellationToken cancellationToken, params object?[]? arguments)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            var success = TryGetValue(out var value, query, arguments);
            return (success, value);
        }, cancellationToken);
    }

    /// <summary>
    /// Gets an untyped value asynchronously while accepting a cancellation token.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The token that can cancel the operation before it starts.</param>
    /// <param name="arguments">The query parameter values.</param>
    /// <returns>A task whose result contains the value.</returns>
    public Task<object?> GetValueAsync(string query, CancellationToken cancellationToken, params object?[]? arguments)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => GetValue(query, arguments), cancellationToken);
    }

    /// <summary>
    /// Gets the first matching record asynchronously while accepting a cancellation token.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The token that can cancel the operation before it starts.</param>
    /// <param name="arguments">The query parameter values.</param>
    /// <returns>A task whose result contains the first matching row.</returns>
    public Task<DataRow?> GetRecordAsync(string query, CancellationToken cancellationToken, params object?[]? arguments)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => GetRecord(query, arguments), cancellationToken);
    }

    /// <summary>
    /// Gets typed records asynchronously while accepting a cancellation token.
    /// </summary>
    /// <typeparam name="T">The record type to materialize.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The token that can cancel the operation before it starts.</param>
    /// <param name="arguments">The query parameter values.</param>
    /// <returns>A task whose result contains the materialized records.</returns>
    public Task<T[]> GetRecordsAsync<T>(string query, CancellationToken cancellationToken, params object?[]? arguments) where T : class, new()
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => GetRecords<T>(query, arguments), cancellationToken);
    }

    /// <summary>
    /// Gets records asynchronously while accepting a cancellation token.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The token that can cancel the operation before it starts.</param>
    /// <param name="arguments">The query parameter values.</param>
    /// <returns>A task whose result contains the matching rows.</returns>
    public Task<DataRowCollection?> GetRecordsAsync(string query, CancellationToken cancellationToken, params object?[]? arguments)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => GetRecords(query, arguments), cancellationToken);
    }

    /// <summary>
    /// Executes a non-query asynchronously while accepting a cancellation token.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The token that can cancel the operation before it starts.</param>
    /// <param name="arguments">The query parameter values.</param>
    /// <returns>A task whose result indicates whether the query succeeded.</returns>
    public Task<bool> ExecuteQueryAsync(string query, CancellationToken cancellationToken, params object?[]? arguments)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => ExecuteQuery(query, arguments), cancellationToken);
    }

    /// <summary>
    /// Executes a SQL script asynchronously while accepting a cancellation token.
    /// </summary>
    /// <param name="script">The SQL script to execute.</param>
    /// <param name="cancellationToken">The token that can cancel the operation before it starts.</param>
    /// <param name="arguments">The query parameter values.</param>
    /// <param name="captureResults">Whether result sets should be captured.</param>
    /// <returns>A task whose result contains the script execution result.</returns>
    public Task<ScriptExecutionResult> ExecuteScriptAsync(string script, CancellationToken cancellationToken, object[]? arguments = null, bool captureResults = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => ExecuteScript(script, arguments, captureResults), cancellationToken);
    }
}
