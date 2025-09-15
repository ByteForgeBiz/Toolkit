using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ByteForge.Toolkit
{
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
        /// Logs an error that occurred during query execution.
        /// </summary>
        /// <param name="ex">The exception that occurred.</param>
        /// <param name="query">The SQL query that was executed.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        private void LogQueryError(Exception ex, string query, object[] arguments)
        {
            var prms = ParseParameters(query, allowRepetition: Options.DatabaseType == DataBaseType.ODBC);
            var msg = $"Error executing query";
            if (arguments != null && arguments.Length > 0)
            {
                var args = arguments.Zip(prms, (a, p) => $"{p} = '{a ?? "null"}'");
                msg += $"\nArguments: {string.Join(Environment.NewLine, args)}";
            }
            Log.Error(msg, ex);
        }
    }
}