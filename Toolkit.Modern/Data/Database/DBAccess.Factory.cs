using System;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;

namespace ByteForge.Toolkit
{
    /*
     *  ___  ___   _                     ___        _                
     * |   \| _ ) /_\  __ __ ___ ______ | __|_ _ __| |_ ___ _ _ _  _ 
     * | |) | _ \/ _ \/ _/ _/ -_)_-<_-<_| _/ _` / _|  _/ _ \ '_| || |
     * |___/|___/_/ \_\__\__\___/__/__(_)_|\__,_\__|\__\___/_|  \_, |
     *                                                          |__/ 
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
            var cmd = connection.CreateCommand();
            cmd.CommandTimeout = Options.CommandTimeout;
            cmd.CommandText = query;

            Log.Verbose($"Command created for query:{Environment.NewLine}{query}");
            AddParametersToCommand(cmd, query, arguments);

            return cmd;
        }
    }
}