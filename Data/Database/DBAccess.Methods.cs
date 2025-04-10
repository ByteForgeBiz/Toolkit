using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ByteForge.Toolkit
{

    /*
     * The `ExecuteScript` and `ExecuteQuery` methods both execute SQL commands against the database, but they have some key differences:
     *
     * Similarities:
     * - Both methods open a database connection and execute SQL commands.
     * - Both methods handle exceptions and log errors.
     * - Both methods use a similar pattern for creating and executing commands.
     *
     * Differences:
     * - `ExecuteQuery` executes a single SQL query, while `ExecuteScript` can execute multiple batches of SQL commands separated by "GO".
     * - `ExecuteScript` can capture and return multiple result sets, while `ExecuteQuery` only returns the number of records affected.
     * - `ExecuteScript` has additional logic to handle DDL statements and parameter parsing, which `ExecuteQuery` does not.
     * - `ExecuteScript` returns a `ScriptExecutionResult` object containing detailed information about the execution, while `ExecuteQuery` returns a boolean indicating success or failure.
     */

    public partial class DBAccess
    {
        /// <summary>
        /// Asynchronously gets a value of type T from the database.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <param name="caller">The name of the calling method.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task<T> GetValueAsync<T>(string query, object[] arguments = null, CancellationToken cancellationToken = default(CancellationToken), [CallerMemberName] string caller = "") =>
            Task.Run(() => GetValue<T>(query, arguments, caller), cancellationToken);

        /// <summary>
        /// Asynchronously gets a value from the database.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <param name="caller">The name of the calling method.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task<object> GetValueAsync(string query, object[] arguments = null, CancellationToken cancellationToken = default(CancellationToken), [CallerMemberName] string caller = "") =>
            Task.Run(() => GetValue(query, arguments, caller), cancellationToken);

        /// <summary>
        /// Asynchronously gets a record from the database.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <param name="caller">The name of the calling method.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task<DataRow> GetRecordAsync(string query, object[] arguments = null, CancellationToken cancellationToken = default(CancellationToken), [CallerMemberName] string caller = "") =>
            Task.Run(() => GetRecord(query, arguments, caller), cancellationToken);

        /// <summary>
        /// Asynchronously gets multiple records from the database.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <param name="caller">The name of the calling method.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task<DataRowCollection> GetRecordsAsync(string query, object[] arguments = null, CancellationToken cancellationToken = default(CancellationToken), [CallerMemberName] string caller = "") =>
            Task.Run(() => GetRecords(query, arguments, caller), cancellationToken);

        /// <summary>
        /// Asynchronously executes a query against the database.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <param name="caller">The name of the calling method.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task<bool> ExecuteQueryAsync(string query, object[] arguments = null, CancellationToken cancellationToken = default(CancellationToken), [CallerMemberName] string caller = "") =>
            Task.Run(() => ExecuteQuery(query, arguments, caller), cancellationToken);

        /// <summary>
        /// Tries to get a value of type T from the database.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="value">The retrieved value.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <param name="caller">The name of the calling method.</param>
        /// <returns>True if the value was retrieved successfully; otherwise, false.</returns>
        public bool TryGetValue<T>(out T value, string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            value = GetValue<T>(query, arguments, $"{caller} > TryGetValue{{T}}");
            return LastException == null;
        }

        /// <summary>
        /// Gets a value of type T from the database.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <param name="caller">The name of the calling method.</param>
        /// <returns>The retrieved value.</returns>
        public T GetValue<T>(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            var rst = GetValue(query, arguments, caller);
            return TypeConverter.ConvertTo<T>(rst);
        }

        /// <summary>
        /// Tries to get a value from the database.
        /// </summary>
        /// <param name="value">The retrieved value.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <param name="caller">The name of the calling method.</param>
        /// <returns>True if the value was retrieved successfully; otherwise, false.</returns>
        public bool TryGetValue(out object value, string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            value = GetValue(query, arguments, $"{caller} > TryGetValue");
            return LastException == null;
        }

        /// <summary>
        /// Gets a value from the database.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <param name="caller">The name of the calling method.</param>
        /// <returns>The retrieved value.</returns>
        public object GetValue(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            try
            {
                using (var dbConn = CreateConnection())
                {
                    dbConn.Open();
                    var cmd = CreateCommand(dbConn, query, arguments);

                    RecordsAffected = -1;
                    LastException = null;

                    var result = TimeFunc(() => cmd.ExecuteScalar());
                    return (result == null || result == DBNull.Value) ? null : result;
                }
            }
            catch (Exception ex)
            {
                LastException = ex;
                LogQueryError(ex, query, arguments, caller);
                return null;
            }
        }

        /// <summary>
        /// Gets a record from the database.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <param name="caller">The name of the calling method.</param>
        /// <returns>The retrieved record.</returns>
        public DataRow GetRecord(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            var rows = GetRecords(query, arguments, caller);
            return rows?.Count > 0 ? rows[0] : null;
        }

        /// <summary>
        /// Gets multiple records from the database.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <param name="caller">The name of the calling method.</param>
        /// <returns>The retrieved records.</returns>
        public DataRowCollection GetRecords(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            try
            {
                using (var dbConn = CreateConnection())
                {
                    dbConn.Open();
                    var cmd = CreateCommand(dbConn, query, arguments);
                    var ds = new DataSet();
                    var adpt = CreateDataAdapter(cmd);

                    TimeFunc(() => adpt.Fill(ds));
                    return ds.Tables.Count > 0 ? ds.Tables[0].Rows : null;
                }
            }
            catch (Exception ex)
            {
                LastException = ex;
                LogQueryError(ex, query, arguments, caller);
                return null;
            }
        }

        /// <summary>
        /// Executes a query against the database.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <param name="caller">The name of the calling method.</param>
        /// <returns>True if the query was executed successfully; otherwise, false.</returns>
        /// <remarks>
        /// Use `ExecuteQuery` when you need to execute a single SQL command that does not require capturing multiple result sets.
        /// This method is ideal for executing DML (Data Manipulation Language) statements such as INSERT, UPDATE, DELETE, or procedures that do not return data.
        /// </remarks>
        public bool ExecuteQuery(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            try
            {
                using (var dbConn = CreateConnection())
                {
                    dbConn.Open();
                    var cmd = CreateCommand(dbConn, query, arguments);

                    RecordsAffected = TimeFunc(() => cmd.ExecuteNonQuery());
                    LastException = null;
                    return true;
                }
            }
            catch (Exception ex)
            {
                LastException = ex;
                LogQueryError(ex, query, arguments, caller);
                return false;
            }
        }

        /// <summary>
        /// Measures the time taken to execute an action and logs the duration.
        /// </summary>
        /// <param name="action">The action to be executed and timed.</param>
        private void TimeAction(Action action)
        {
            var start = DateTime.Now;
            action();
            var elapsed = DateTime.Now - start;
            Log.Verbose($"Query executed in {elapsed}.");
        }

        /// <summary>
        /// Measures the time taken to execute a function, logs the duration, and returns the result.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the function.</typeparam>
        /// <param name="func">The function to be executed and timed.</param>
        /// <returns>The result of the function execution.</returns>
        private T TimeFunc<T>(Func<T> func)
        {
            var start = DateTime.Now;
            var result = func();
            var elapsed = DateTime.Now - start;
            Log.Verbose($"Query executed in {elapsed}.");
            return result;
        }
    }
}