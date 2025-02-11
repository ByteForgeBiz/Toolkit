using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ByteForge.Toolkit
{
    public partial class DBAccess
    {
        /// <summary>
        /// Logs an error that occurred during query execution.
        /// </summary>
        /// <param name="ex">The exception that occurred.</param>
        /// <param name="query">The SQL query that was executed.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <param name="caller">The name of the calling method.</param>
        private void LogQueryError(Exception ex, string query, object[] arguments, string caller)
        {
            var msg = $"Error executing query '{query}' in '{caller}'";
            if (arguments != null && arguments.Length > 0)
            {
                var args = arguments.Zip(rxParam.Matches(query).Cast<Match>(),
                    (a, p) => $"{p.Value} = '{a ?? "null"}'");
                msg += $"\nArguments: {string.Join(Environment.NewLine, args)}";
            }
            Log.Error(ex, msg);
        }

        /// <summary>
        /// Logs verbose information about the query being executed.
        /// </summary>
        /// <param name="query">The SQL query being executed.</param>
        /// <param name="parameters">Optional parameters being used in the query.</param>
        private void LogVerboseQuery(string query, object[] parameters = null)
        {
            Log.Verbose($"Executing query: {query}");
            if (parameters != null && parameters.Length > 0)
            {
                var parameterInfo = parameters.Select((p, i) => $"@p{i} = '{p ?? "null"}'");
                Log.Debug($"Parameters: {string.Join(", ", parameterInfo)}");
            }
        }

        /// <summary>
        /// Logs information about a batch execution.
        /// </summary>
        /// <param name="batchNumber">The current batch number.</param>
        /// <param name="totalBatches">The total number of batches.</param>
        /// <param name="affectedRows">The number of rows affected by the batch.</param>
        private void LogBatchExecution(int batchNumber, int totalBatches, int affectedRows)
        {
            Log.Info($"Executed batch {batchNumber} of {totalBatches}. Rows affected: {affectedRows}");
        }

        /// <summary>
        /// Logs details about a database connection attempt.
        /// </summary>
        /// <param name="server">The server being connected to.</param>
        /// <param name="database">The database being accessed.</param>
        private void LogConnectionAttempt(string server, string database)
        {
            Log.Debug($"Attempting to connect to server '{server}', database '{database}'");
        }

        /// <summary>
        /// Logs information about query execution time.
        /// </summary>
        /// <param name="milliseconds">The time taken to execute the query in milliseconds.</param>
        private void LogQueryExecutionTime(long milliseconds)
        {
            Log.Debug($"Query executed in {milliseconds}ms");
        }
    }
}