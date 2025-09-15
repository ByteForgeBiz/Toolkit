using System;
using System.Data;
using System.Data.SqlClient;
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
        private readonly Regex rxParam = new Regex(@"(?<![a-zA-Z0-9_.])@[A-Za-z]\w*(?=(?:[^']*'[^']*')*[^']*$)");

        /// <summary>
        /// Adds parameters to a command if applicable.
        /// </summary>
        /// <param name="cmd">The command to which parameters will be added.</param>
        /// <param name="query">The SQL batch containing the parameters.</param>
        /// <param name="arguments">The arguments to be added as parameters.</param>
        /// <exception cref="ParamArgumentsMismatchException">Thrown when the number of parameters does not match the number of arguments.</exception>
        private void AddParameters(IDbCommand cmd, string query, object[] arguments)
        {
            var parameters = ParseParameters(query);
            if (parameters.Count != arguments.Length)
                throw new ParamArgumentsMismatchException(
                    $"The number of parameters ({parameters.Count}) does not match " +
                    $"the number of arguments ({arguments.Length}).");

            for (var i = 0; i < parameters.Count; i++)
            {
                var prm = cmd.CreateParameter();
                prm.ParameterName = parameters[i];
                prm.Value = arguments[i] ?? DBNull.Value;
                DefineDbType(prm, prm.Value);
                cmd.Parameters.Add(prm);
            }
        }

        /// <summary>
        /// Defines the database type for the specified parameter based on the value.
        /// </summary>
        /// <param name="prm">The database parameter.</param>
        /// <param name="value">The value to determine the database type.</param>
        private void DefineDbType(IDbDataParameter prm, object value)
        {
            /*
             * First we deal with null values
             */
            if (value == null)
            {
                prm.DbType = System.Data.DbType.String;
                return;
            }

            /*
             * Special case for DataTables
             */
            if (value is DataTable dt)
            {
                if (!(prm is SqlParameter sqlParam))
                    throw new NotSupportedException("Only SQL Server supports table-valued parameters.");
                sqlParam.SqlDbType = SqlDbType.Structured;
                sqlParam.TypeName = $"dbo.{dt.TableName}";
                return;
            }

            /*
             * Now we deal with the rest of the types
             */
            var type = value.GetType();

            /*
             * String is omitted because it's the default type
             * Special handling for ODBC which has more restrictive type mappings
             */
            if (Options.DatabaseType == DataBaseType.ODBC)
            {
                prm.DbType = type.Name switch
                {
                    "Boolean" => System.Data.DbType.Boolean,
                    "Byte" => System.Data.DbType.Byte,
                    "DateTime" => System.Data.DbType.DateTime,
                    "Decimal" => System.Data.DbType.Double, // Use Double for ODBC compatibility with Access Currency
                    "Double" => System.Data.DbType.Double,
                    "Guid" => System.Data.DbType.String, // GUID as string for ODBC
                    "Int16" => System.Data.DbType.Int16,
                    "Int32" => System.Data.DbType.Int32,
                    "Int64" => System.Data.DbType.Int32, // Access doesn't have BigInt, use Int32
                    "Single" => System.Data.DbType.Single,
                    _ => System.Data.DbType.String,
                };
            }
            else
            {
                prm.DbType = type.Name switch
                {
                    "Boolean" => System.Data.DbType.Boolean,
                    "Byte" => System.Data.DbType.Byte,
                    "DateTime" => System.Data.DbType.DateTime,
                    "Decimal" => System.Data.DbType.Decimal,
                    "Double" => System.Data.DbType.Double,
                    "Guid" => System.Data.DbType.Guid,
                    "Int16" => System.Data.DbType.Int16,
                    "Int32" => System.Data.DbType.Int32,
                    "Int64" => System.Data.DbType.Int64,
                    "Single" => System.Data.DbType.Single,
                    _ => System.Data.DbType.String,
                };
            }
        }
    }
}