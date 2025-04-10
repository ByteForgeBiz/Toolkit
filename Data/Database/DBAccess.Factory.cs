using System;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ByteForge.Toolkit
{
    public partial class DBAccess
    {
        /// <summary>
        /// Creates the correct <see cref="DbConnection"/> object based on the database type.
        /// </summary>
        /// <returns>The correct <see cref="DbConnection"/> object.</returns>
        /// <exception cref="NotSupportedException">Thrown when the database type is not supported.</exception>
        private DbConnection CreateConnection([CallerMemberName] string caller = "")
        {
            var conn = ConnectionString;
            if (caller.ToLowerInvariant().Contains("async"))
                conn += ";Asynchronous Processing=true;Async=true";

            switch (DbType)
            {
                case DataBaseType.SQLServer:
                    return new SqlConnection(conn);
                case DataBaseType.ODBC:
                    return new OdbcConnection(conn);
                default:
                    throw new NotSupportedException($"The database type {DbType} is not supported.");
            }
        }

        /// <summary>
        /// Creates the correct <see cref="DbDataAdapter"/> object based on the database type.
        /// </summary>
        /// <param name="command">A <see cref="DbCommand"/> object that contains the query.</param>
        /// <returns>The correct <see cref="DbDataAdapter"/> object.</returns>
        /// <exception cref="NotSupportedException">Thrown when the database type is not supported.</exception>
        private DbDataAdapter CreateDataAdapter(DbCommand command)
        {
            switch (DbType)
            {
                case DataBaseType.SQLServer:
                    return new SqlDataAdapter((SqlCommand)command);
                case DataBaseType.ODBC:
                    return new OdbcDataAdapter((OdbcCommand)command);
                default:
                    throw new NotSupportedException($"The database type {DbType} is not supported.");
            }
        }

        /// <summary>
        /// Creates a command for the specified connection and query.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <returns>The created command.</returns>
        private DbCommand CreateCommand(DbConnection connection, string query, object[] arguments)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 240;
            cmd.CommandText = query;

            if (arguments != null)
            {
                var prms = rxParam.Matches(query).Cast<Match>().Select(x => x.Value).Distinct().ToList();
                if (prms.Count != arguments.Length)
                    throw new ParamArgumentsMismatchException(
                        $"The number of parameters ({prms.Count}) does not match " +
                        $"the number of arguments ({arguments.Length}).");

                for (var i = 0; i < prms.Count; i++)
                {
                    var prm = cmd.CreateParameter();
                    prm.ParameterName = prms[i];
                    prm.Value = arguments[i] ?? DBNull.Value;
                    DefineDbType(prm, prm.Value);
                    cmd.Parameters.Add(prm);
                }
            }

            Log.Verbose($"Command created for query: {query}");
            if (cmd.Parameters.Count > 0)
                Log.Debug(string.Join(", " + Environment.NewLine, cmd.Parameters.Cast<DbParameter>().Select(p => $"{p.ParameterName} = '{p.Value}'")));

            return cmd;
        }
    }
}