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

                    Log.Verbose($"Executing query: {query}");
                    if (cmd.Parameters.Count > 0)
                        Log.Debug(string.Join(", ", cmd.Parameters.Cast<DbParameter>().Select(p => $"{p.ParameterName} = '{p.Value}'")));

                    var result = cmd.ExecuteScalar();
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

                    adpt.Fill(ds);
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
        public bool ExecuteQuery(string query, object[] arguments = null, [CallerMemberName] string caller = "")
        {
            try
            {
                using (var dbConn = CreateConnection())
                {
                    dbConn.Open();
                    var cmd = CreateCommand(dbConn, query, arguments);

                    Log.Verbose($"Executing query: {query}");
                    if (cmd.Parameters.Count > 0)
                        Log.Debug(string.Join(", ", cmd.Parameters.Cast<DbParameter>().Select(p => $"{p.ParameterName} = '{p.Value}'")));

                    RecordsAffected = cmd.ExecuteNonQuery();
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
    }
}