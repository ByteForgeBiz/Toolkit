using System;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
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
        /// Tries to get a value of type <typeparamref name="T"/> from the database using the specified query and arguments.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="value">When this method returns, contains the value of type <typeparamref name="T"/> retrieved from the database, or the default value if the operation fails.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <param name="caller">The name of the calling method. This is automatically populated by the compiler if not explicitly provided.</param>
        /// <returns><see langword="true"/> if the value was retrieved successfully; otherwise, <see langword="false"/>.</returns>
        public bool TryGetValue<T>(out T value, string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            value = default;

            try
            {
                value = GetValue<T>(query, arguments, $"{caller} > TryGetValue{{T}}");
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
        /// <param name="caller">The name of the calling method. This is automatically populated by the compiler if not explicitly provided.</param>
        /// <returns>The value of type <typeparamref name="T"/> retrieved from the database.</returns>
        public T GetValue<T>(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            var rst = GetValue(query, arguments, caller);
            return TypeConverter.ConvertTo<T>(rst);
        }

        /// <summary>
        /// Tries to get a value from the database using the specified query and arguments.
        /// </summary>
        /// <param name="value">When this method returns, contains the value retrieved from the database, or <see langword="null"/> if the operation fails.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <param name="caller">The name of the calling method. This is automatically populated by the compiler if not explicitly provided.</param>
        /// <returns><see langword="true"/> if the value was retrieved successfully; otherwise, <see langword="false"/>.</returns>
        public bool TryGetValue(out object value, string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            value = GetValue(query, arguments, $"{caller} > TryGetValue");
            return LastException == null;
        }

        /// <summary>
        /// Gets a value from the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <param name="caller">The name of the calling method. This is automatically populated by the compiler if not explicitly provided.</param>
        /// <returns>The value retrieved from the database, or <see langword="null"/> if no value is found.</returns>
        public object GetValue(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            var result = ExecuteCommand<object>(query, cmd => cmd.ExecuteScalar(), arguments, caller);
            return (result == null || result == DBNull.Value) ? null : result;
        }

        /// <summary>
        /// Retrieves a single record from the data source and converts it to the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to which the retrieved record will be converted. Must be a reference type with a parameterless constructor.</typeparam>
        /// <param name="query">The query string used to retrieve the record.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <param name="caller">The name of the calling member. This is automatically populated by the compiler if not explicitly provided.</param>
        /// <returns>An instance of type <typeparamref name="T"/> representing the retrieved record, or a new instance of <typeparamref name="T"/> if no record is found.</returns>
        /// <remarks>
        /// The method uses the provided query to retrieve a single record from the data source.
        /// The record is then converted to the specified type <typeparamref name="T"/> using a type conversion
        /// utility.
        /// </remarks>
        public T GetRecord<T>(string query, object[] arguments = null, [CallerMemberName] string caller = "") where T : class, new()
        {
            var row = GetRecord(query, arguments, caller);
            return TypeConverter.ConvertDataRowTo<T>(row);
        }

        /// <summary>
        /// Executes the specified query and converts the resulting rows into an array of objects of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects to create from the query results. Must be a reference type with a parameterless constructor.</typeparam>
        /// <param name="query">The SQL query to execute. Cannot be null or empty.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no parameters are required.</param>
        /// <param name="caller">The name of the calling member, automatically provided by the compiler. Defaults to the caller's name.</param>
        /// <returns>
        /// An array of objects of type <typeparamref name="T"/> created from the query results.  
        /// Returns an empty array if no rows are returned by the query.
        /// </returns>
        /// <remarks>
        /// This method uses a type conversion mechanism to map each row in the query result to
        /// an object of type <typeparamref name="T"/>. Ensure that the column names in the query result match the
        /// property names of <typeparamref name="T"/> for successful mapping.
        /// </remarks>
        public T[] GetRecords<T>(string query, object[] arguments = null, [CallerMemberName] string caller = "") where T : class, new()
        {
            var rows = GetRecords(query, arguments, caller);
            return rows?.Count > 0 ? rows.Cast<DataRow>().Select(r => TypeConverter.ConvertDataRowTo<T>(r)).ToArray() : Array.Empty<T>();
        }

        /// <summary>
        /// Asynchronously retrieves a single <see cref="DataRow"/> record from the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <param name="caller">The name of the calling method. This is automatically populated by the compiler if not explicitly provided.</param>
        /// <returns>A <see cref="Task{DataRow}"/> representing the asynchronous operation, with the result being the retrieved <see cref="DataRow"/> or <see langword="null"/> if no record is found.</returns>
        public async Task<DataRow> GetRecordAsync(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            return await Task.Run(() => GetRecord(query, arguments, caller));
        }

        /// <summary>
        /// Gets a record from the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <param name="caller">The name of the calling method. This is automatically populated by the compiler if not explicitly provided.</param>
        /// <returns>The first <see cref="DataRow"/> from the result set, or <see langword="null"/> if no records are found.</returns>
        public DataRow GetRecord(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            var rows = GetRecords(query, arguments, caller);
            return rows?.Count > 0 ? rows[0] : null;
        }

        /// <summary>
        /// Asynchronously retrieves multiple records from the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <param name="caller">The name of the calling method. This is automatically populated by the compiler if not explicitly provided.</param>
        /// <returns>A <see cref="Task{DataRowCollection}"/> representing the asynchronous operation, with the result being the retrieved <see cref="DataRowCollection"/>.</returns>
        public async Task<DataRowCollection> GetRecordsAsync(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            return await Task.Run(() => GetRecords(query, arguments, caller));
        }

        /// <summary>
        /// Gets multiple records from the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <param name="caller">The name of the calling method. This is automatically populated by the compiler if not explicitly provided.</param>
        /// <returns>The <see cref="DataRowCollection"/> containing the retrieved records, or <see langword="null"/> if no records are found.</returns>
        public DataRowCollection GetRecords(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            var data = ExecuteCommand<DataSet>(query, cmd => {
                var ds = new DataSet();
                var adpt = CreateDataAdapter(cmd);
                adpt.Fill(ds);
                return ds;
            }, arguments, caller);
            return data?.Tables.Count > 0 ? data.Tables[0].Rows : null;
        }

        /// <summary>
        /// Asynchronously executes a query against the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <param name="caller">The name of the calling method. This is automatically populated by the compiler if not explicitly provided.</param>
        /// <returns>A <see cref="Task{Boolean}"/> representing the asynchronous operation, with the result indicating whether the query was executed successfully.</returns>
        public async Task<bool> ExecuteQueryAsync(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            return await Task.Run(() => ExecuteQuery(query, arguments, caller));
        }

        /// <summary>
        /// Executes a query against the database using the specified query and arguments.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <param name="caller">The name of the calling method. This is automatically populated by the compiler if not explicitly provided.</param>
        /// <returns><see langword="true"/> if the query was executed successfully; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Use <see cref="ExecuteQuery"/> when you need to execute a single SQL command that does not require capturing multiple result sets.
        /// This method is ideal for executing DML (Data Manipulation Language) statements such as INSERT, UPDATE, DELETE, or procedures that do not return data.
        /// </remarks>
        public bool ExecuteQuery(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            RecordsAffected = ExecuteCommand<int>(query, cmd => cmd.ExecuteNonQuery(), arguments, caller);
            return LastException == null;
        }

        /// <summary>
        /// Executes a database command using the provided query, execution delegate, and arguments, and returns the result.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the execution delegate.</typeparam>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="execute">A delegate that defines how to execute the command and obtain the result.</param>
        /// <param name="arguments">An optional array of arguments to parameterize the query. Can be <see langword="null"/> if no arguments are required.</param>
        /// <param name="caller">The name of the calling method. This is automatically populated by the compiler if not explicitly provided.</param>
        /// <returns>The result of type <typeparamref name="T"/> returned by the execution delegate, or the default value of <typeparamref name="T"/> if an exception occurs.</returns>
        private T ExecuteCommand<T>(string query, Func<IDbCommand, T> execute, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            try
            {
                using (var dbConn = CreateConnection())
                {
                    dbConn.Open();
                    var cmd = CreateCommand(dbConn, query, arguments);
                    RecordsAffected = -1;
                    LastException = null;
                    return TimeFunc(() => execute(cmd));
                }
            }
            catch (Exception ex)
            {
                LastException = ex;
                LogQueryError(ex, query, arguments, caller);
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
            var start = DateTime.Now;
            var result = func();
            var elapsed = DateTime.Now - start;
            Log.Verbose($"Query executed in {elapsed}.");
            return result;
        }
    }
}