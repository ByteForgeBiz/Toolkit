using System;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
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
        /// Creates the correct <see cref="IDbConnection"/> object based on the database type.
        /// </summary>
        /// <returns>The correct <see cref="IDbConnection"/> object.</returns>
        /// <exception cref="NotSupportedException">Thrown when the database type is not supported.</exception>
        private IDbConnection CreateConnection()
        {
            var conn = ConnectionString;

            return DbType switch
            {
                DataBaseType.SQLServer => new SqlConnection(conn),
                DataBaseType.ODBC => new OdbcConnection(conn),
                _ => throw new NotSupportedException($"The database type {DbType} is not supported."),
            };
        }

        /// <summary>
        /// Creates the correct <see cref="DbDataAdapter"/> object based on the database type.
        /// </summary>
        /// <param name="command">A <see cref="DbCommand"/> object that contains the query.</param>
        /// <returns>The correct <see cref="DbDataAdapter"/> object.</returns>
        /// <exception cref="NotSupportedException">Thrown when the database type is not supported.</exception>
        private IDbDataAdapter CreateDataAdapter(IDbCommand command)
        {
            return DbType switch
            {
                DataBaseType.SQLServer => new SqlDataAdapter((SqlCommand)command),
                DataBaseType.ODBC => new OdbcDataAdapter((OdbcCommand)command),
                _ => throw new NotSupportedException($"The database type {DbType} is not supported."),
            };
        }

        /// <summary>
        /// Creates a command for the specified connection and query.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="arguments">The arguments for the SQL query.</param>
        /// <returns>The created command.</returns>
        private IDbCommand CreateCommand(IDbConnection connection, string query, object[] arguments)
        {
            arguments ??= Array.Empty<object>();
            var matches = rxParam.Matches(query).Cast<Match>().Select(x => x.Value).ToList();
            var prms = matches.Distinct().ToList();
            if (prms.Count != arguments.Length)
                throw new ParamArgumentsMismatchException(
                    $"The number of parameters ({prms.Count}) does not match " +
                    $"the number of arguments ({arguments.Length}).");

            /*
             * In SQL Server we can reuse the same parameter multiple times in a query
             */
            if (Options.DatabaseType == DataBaseType.SQLServer)
                matches = prms;

            var cmd = connection.CreateCommand();
            cmd.CommandTimeout = Options.CommandTimeout;
            cmd.CommandText = query;

            foreach(var m in matches)
            {
                var idx = prms.IndexOf(m);
                var arg = arguments[idx];
                var prm = CreateParameter(cmd, m, arg);
                cmd.Parameters.Add(prm);

                if (Options.DatabaseType == DataBaseType.ODBC)
                {
                    /*
                     * ODBC does not support named parameters, 
                     * so we need to replace the parameter name with a question mark
                     */
                    cmd.CommandText = cmd.CommandText.Replace(m, "?");
                }
            }

            Log.Verbose($"Command created for query:{Environment.NewLine}{query}");
            if (cmd.Parameters.Count > 0)
                Log.Debug(string.Join(", " + Environment.NewLine, cmd.Parameters.Cast<DbParameter>().Select(p => $"{p.ParameterName} = '{p.Value}'")));

            return cmd;
        }

        /// <summary>
        /// Creates and configures a new database parameter for the specified command.
        /// </summary>
        /// <param name="cmd">The database command to which the parameter will be added. Must not be <see langword="null"/>.</param>
        /// <param name="name">The name of the parameter. Must not be <see langword="null"/> or empty.</param>
        /// <param name="value">The value to assign to the parameter. Can be <see langword="null"/>.</param>
        /// <returns>A configured <see cref="IDataParameter"/> instance with the specified name and value.</returns>
        /// <remarks>
        /// If the <paramref name="value"/> is a string and the <c>AutoTrimStrings</c> option is enabled, 
        /// leading and trailing whitespace will be removed from the string before assigning it to the
        /// parameter.<br/>
        /// The method also determines and sets the appropriate database type for the parameter based on its value.
        /// </remarks>
        private IDataParameter CreateParameter(IDbCommand cmd, string name, object value)
        {
            var prm = cmd.CreateParameter();
            prm.ParameterName = name;
            prm.Value = value ?? DBNull.Value;
            if (Options.AutoTrimStrings && prm.Value is string str)
                prm.Value = str.Trim();
            DefineDbType(prm, prm.Value);
            return prm;
        }
    }
}