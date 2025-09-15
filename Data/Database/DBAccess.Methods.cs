using System;
using System.Data;
using System.Linq;
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

    /*
     *  ___  ___   _                   
     * |   \| _ ) /_\  __ __ ___ ______
     * | |) | _ \/ _ \/ _/ _/ -_)_-<_-<
     * |___/|___/_/ \_\__\__\___/__/__/
     *                                 
     */
    public partial class DBAccess
    {
        /// <summary>
        /// Tests the connection to the database by executing a simple query.
        /// </summary>
        /// <returns><see langword="true"/> if the connection to the database is successful; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method attempts to verify the database connection by executing a lightweight query.<br/>
        /// It is intended to be used as a health check or to confirm connectivity before performing other operations.
        /// </remarks>
        public bool TestConnection()
        {
            if (TryGetValue<bool>(out var result, "SELECT 1", null))
                return result;
            return false;
        }

        /// <summary>
        /// Asynchronously tests the connection to the database by executing a simple query.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> representing the asynchronous operation, with the result indicating whether the connection to the database is successful.
        /// </returns>
        public async Task<bool> TestConnectionAsync()
        {
            return await Task.Run(() => TestConnection());
        }

        /// <summary>
        /// Tries to get a value of type <typeparamref name="T"/> from the database using the specified query and arguments.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="value">When this method returns, contains the value of type <typeparamref name="T"/> retrieved from the database, or the default value if the operation fails.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns><see langword="true"/> if the value was retrieved successfully; otherwise, <see langword="false"/>.</returns>
        public bool TryGetValue<T>(out T value, string query, params object[] arguments)
        {
            value = default;

            try
            {
                value = GetValue<T>(query, arguments);
                return LastException == null;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Gets a value of type <typeparamref name="T"/> from the database using the specified query and arguments.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns>The value of type <typeparamref name="T"/> retrieved from the database.</returns>
        public T GetValue<T>(string query, params object[] arguments)
        {
            var rst = GetValue(query, arguments);
            return TypeConverter.ConvertTo<T>(rst);
        }

        /// <summary>
        /// Asynchronously tries to get a value of type <typeparamref name="T"/> from the database using the specified query and arguments.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> representing the asynchronous operation, with the result being a tuple containing a boolean indicating success and the retrieved value.
        /// </returns>
        public async Task<(bool Success, T Value)> TryGetValueAsync<T>(string query, params object[] arguments)
        {
            return await Task.Run(() =>
            {
                var success = TryGetValue<T>(out var value, query, arguments);
                return (success, value);
            });
        }

        /// <summary>
        /// Asynchronously gets a value of type <typeparamref name="T"/> from the database using the specified query and arguments.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> representing the asynchronous operation, with the result being the value of type <typeparamref name="T"/> retrieved from the database.
        /// </returns>
        public async Task<T> GetValueAsync<T>(string query, params object[] arguments)
        {
            return await Task.Run(() => GetValue<T>(query, arguments));
        }

        /// <summary>
        /// Tries to get a value from the database using the specified query and arguments.
        /// </summary>
        /// <param name="value">When this method returns, contains the value retrieved from the database, or <see langword="null"/> if the operation fails.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns><see langword="true"/> if the value was retrieved successfully; otherwise, <see langword="false"/>.</returns>
        public bool TryGetValue(out object value, string query, params object[] arguments)
        {
            value = GetValue(query, arguments);
            return LastException == null;
        }

        /// <summary>
        /// Gets a value from the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns>The value retrieved from the database, or <see langword="null"/> if no value is found.</returns>
        public object GetValue(string query, params object[] arguments)
        {
            var result = ExecuteCommand<object>(query, cmd => cmd.ExecuteScalar(), arguments);
            return (result == null || result == DBNull.Value) ? null : result;
        }

        /// <summary>
        /// Asynchronously tries to get a value from the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> representing the asynchronous operation, with the result being a tuple containing a boolean indicating success and the retrieved value.
        /// </returns>
        public async Task<(bool Success, object Value)> TryGetValueAsync(string query, params object[] arguments)
        {
            return await Task.Run(() =>
            {
                var success = TryGetValue(out var value, query, arguments);
                return (success, value);
            });
        }

        /// <summary>
        /// Asynchronously gets a value from the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns>
        /// A <see cref="Task{Object}"/> representing the asynchronous operation, with the result being the value retrieved from the database.
        /// </returns>
        public async Task<object> GetValueAsync(string query, params object[] arguments)
        {
            return await Task.Run(() => GetValue(query, arguments));
        }

        /// <summary>
        /// Retrieves the first record that matches the specified query.
        /// </summary>
        /// <param name="query">The SQL query string used to retrieve the record.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are
        /// needed.</param>
        /// <returns>A <see cref="DataRow"/> representing the first record that matches the query, or <see langword="null"/> if
        /// no records are found.</returns>
        /// <remarks>This method is a convenience wrapper for retrieving a single record. If multiple records match the query, only the first one is returned.</remarks>
        public DataRow GetRecord(string query, params object[] arguments)
        {
            var rows = GetRecords(query, arguments);
            return rows?.Count > 0 ? rows[0] : null;
        }

        /// <summary>
        /// Retrieves a single record from the data source and converts it to the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to which the retrieved record will be converted. Must be a reference type with a parameterless constructor.</typeparam>
        /// <param name="query">The query string used to retrieve the record.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns>An instance of type <typeparamref name="T"/> representing the retrieved record, or a new instance of <typeparamref name="T"/> if no record is found.</returns>
        /// <remarks>
        /// The method uses the provided query to retrieve a single record from the data source.
        /// The record is then converted to the specified type <typeparamref name="T"/> using a type conversion
        /// utility.
        /// </remarks>
        public T GetRecord<T>(string query, params object[] arguments) where T : class, new()
        {
            var row = GetRecord(query, arguments);
            return TypeConverter.ConvertDataRowTo<T>(row);
        }

        /// <summary>
        /// Asynchronously retrieves a single <see cref="DataRow"/> record from the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns>A <see cref="Task{DataRow}"/> representing the asynchronous operation, with the result being the retrieved <see cref="DataRow"/> or <see langword="null"/> if no record is found.</returns>
        public async Task<DataRow> GetRecordAsync(string query, params object[] arguments)
        {
            return await Task.Run(() => GetRecord(query, arguments));
        }

        /// <summary>
        /// Executes the specified query and converts the resulting rows into an array of objects of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects to create from the query results. Must be a reference type with a parameterless constructor.</typeparam>
        /// <param name="query">The SQL query to execute. Cannot be null or empty.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no parameters are required.</param>
        /// <returns>
        /// An array of objects of type <typeparamref name="T"/> created from the query results.  
        /// Returns an empty array if no rows are returned by the query.
        /// </returns>
        /// <remarks>
        /// This method uses a type conversion mechanism to map each row in the query result to
        /// an object of type <typeparamref name="T"/>. Ensure that the column names in the query result match the
        /// property names of <typeparamref name="T"/> for successful mapping.
        /// </remarks>
        public T[] GetRecords<T>(string query, params object[] arguments) where T : class, new()
        {
            var rows = GetRecords(query, arguments);
            return rows?.Count > 0 ? rows.Cast<DataRow>().Select(r => TypeConverter.ConvertDataRowTo<T>(r)).ToArray() : Array.Empty<T>();
        }

        /// <summary>
        /// Asynchronously executes the specified query and converts the resulting rows into an array of objects of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects to create from the query results. Must be a reference type with a parameterless constructor.</typeparam>
        /// <param name="query">The SQL query to execute. Cannot be null or empty.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no parameters are required.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> representing the asynchronous operation, with the result being an array of objects of type <typeparamref name="T"/> created from the query results.
        /// </returns>
        public async Task<T[]> GetRecordsAsync<T>(string query, params object[] arguments) where T : class, new()
        {
            return await Task.Run(() => GetRecords<T>(query, arguments));
        }

        /// <summary>
        /// Asynchronously retrieves multiple records from the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns>A <see cref="Task{DataRowCollection}"/> representing the asynchronous operation, with the result being the retrieved <see cref="DataRowCollection"/>.</returns>
        public async Task<DataRowCollection> GetRecordsAsync(string query, params object[] arguments)
        {
            return await Task.Run(() => GetRecords(query, arguments));
        }

        /// <summary>
        /// Gets multiple records from the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns>The <see cref="DataRowCollection"/> containing the retrieved records, or <see langword="null"/> if no records are found.</returns>
        public DataRowCollection GetRecords(string query, params object[] arguments)
        {
            var data = ExecuteCommand<DataSet>(query, cmd => {
                var ds = new DataSet();
                var adpt = CreateDataAdapter(cmd);
                adpt.Fill(ds);
                return ds;
            }, arguments);
            return data?.Tables.Count > 0 ? data.Tables[0].Rows : null;
        }

        /// <summary>
        /// Asynchronously executes a query against the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns>A <see cref="Task{Boolean}"/> representing the asynchronous operation, with the result indicating whether the query was executed successfully.</returns>
        public async Task<bool> ExecuteQueryAsync(string query, params object[] arguments)
        {
            return await Task.Run(() => ExecuteQuery(query, arguments));
        }

        /// <summary>
        /// Executes a query against the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns><see langword="true"/> if the query was executed successfully; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Use <see cref="ExecuteQuery"/> when you need to execute a single SQL command that does not require capturing multiple result sets.
        /// This method is ideal for executing DML (Data Manipulation Language) statements such as INSERT, UPDATE, DELETE, or procedures that do not return data.
        /// </remarks>
        public bool ExecuteQuery(string query, params object[] arguments)
        {
            RecordsAffected = ExecuteCommand<int>(query, cmd => cmd.ExecuteNonQuery(), arguments);
            return LastException == null;
        }

        /// <summary>
        /// Executes a database command using the provided query, execution delegate, and arguments, and returns the result.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the execution delegate.</typeparam>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="execute">A delegate that defines how to execute the command and obtain the result.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <returns>The result of type <typeparamref name="T"/> returned by the execution delegate, or the default value of <typeparamref name="T"/> if an exception occurs.</returns>
        private T ExecuteCommand<T>(string query, Func<IDbCommand, T> execute, params object[] arguments)
        {
            try
            {
                using var dbConn = CreateConnection();
                dbConn.Open();
                var cmd = CreateCommand(dbConn, query, arguments);
                RecordsAffected = -1;
                LastException = null;
                return TimeFunc(() => execute(cmd));
            }
            catch (Exception ex)
            {
                LastException = ex;
                LogQueryError(ex, query, arguments);
                return default;
            }
        }

        /// <summary>
        /// Measures the time taken to execute a function, logs the duration, and returns the result.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the function.</typeparam>
        /// <param name="func">The function to be executed and timed.</param>
        /// <returns>The result of the function execution.</returns>
        private T TimeFunc<T>(Func<T> func)
        {
            var watch = new System.Diagnostics.Stopwatch();
            var hasError = false;
            try
            {
                var result = func();
                return result;
            }
            catch
            {
                hasError = true;
                throw;
            }
            finally
            {
                watch.Stop();
                var elapsed = watch.Elapsed;
                if (hasError)
                    Log.Warning($"Query failed after {elapsed}.");
                else
                    Log.Verbose($"Query executed in {elapsed}.");
            }
        }
    }
}