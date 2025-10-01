using System;
using System.Linq;

namespace ByteForge.Toolkit
{
    /*
     *  ___  ___   _                     _                   _           
     * |   \| _ ) /_\  __ __ ___ ______ | |   ___  __ _ __ _(_)_ _  __ _ 
     * | |) | _ \/ _ \/ _/ _/ -_)_-<_-<_| |__/ _ \/ _` / _` | | ' \/ _` |
     * |___/|___/_/ \_\__\__\___/__/__(_)____\___/\__, \__, |_|_||_\__, |
     *                                            |___/|___/       |___/ 
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
            var prms = ParseParameters(query);
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