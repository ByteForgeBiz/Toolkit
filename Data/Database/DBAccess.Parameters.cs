using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
        /// Adds parameters to a command if applicable.
        /// </summary>
        /// <param name="cmd">The command to which parameters will be added.</param>
        /// <param name="query">The SQL batch containing the parameters.</param>
        /// <param name="arguments">The arguments to be added as parameters.</param>
        /// <exception cref="ParamArgumentsMismatchException">Thrown when the number of parameters does not match the number of arguments.</exception>
        private void AddParametersToCommand(IDbCommand cmd, string query, object[] arguments)
        {
            arguments ??= Array.Empty<object>();
            var matches = ParseParameters(query, allowRepetition: true);
            var distinctParams = matches.Distinct().ToList();

            if (distinctParams.Count != arguments.Length)
                throw new ParamArgumentsMismatchException(
                    $"The number of parameters ({distinctParams.Count}) does not match " +
                    $"the number of arguments ({arguments.Length}).");

            // In SQL Server we can reuse the same parameter multiple times in a query
            if (Options.DatabaseType == DataBaseType.SQLServer)
                matches = distinctParams;

            foreach (var paramName in matches)
            {
                var idx = distinctParams.IndexOf(paramName);
                var arg = arguments[idx];
                var prm = CreateParameter(cmd, paramName, arg);
                cmd.Parameters.Add(prm);

                if (Options.DatabaseType == DataBaseType.ODBC)
                {
                    // ODBC does not support named parameters, 
                    // so we need to replace the parameter name with a question mark
                    cmd.CommandText = cmd.CommandText.Replace(paramName, "?");
                }
            }

            if (cmd.Parameters.Count > 0)
                Log.Debug(string.Join(", " + Environment.NewLine, cmd.Parameters.Cast<DbParameter>().Select(p => $"{p.ParameterName} = '{p.Value}'")));
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

        /// <summary>
        /// Extracts parameter names from a SQL query string, optionally allowing duplicates.
        /// </summary>
        /// <param name="query">The SQL query string to parse. The query may contain parameters prefixed with '@'.</param>
        /// <param name="allowRepetition">A <see langword="bool"/> value indicating whether duplicate parameter names should be included in the result.<br/>
        /// If <see langword="true"/>, duplicate parameter names are added to the result; otherwise, duplicates are ignored.</param>
        /// <returns>
        /// A list of parameter names found in the query string.<br/>
        /// The list may include duplicates if <paramref name="allowRepetition"/> is <see langword="true"/>.<br/>
        /// If no parameters are found, an empty list is returned.
        /// </returns>
        /// <remarks>
        /// This method removes SQL comments (both block comments and single-line comments)
        /// before extracting parameter names. Parameters within string literals are ignored.
        /// Parameter names are case-insensitive and must start with '@', followed by a letter and 
        /// then alphanumeric characters or underscores.
        /// </remarks>
        private static List<string> ParseParameters(string query, bool allowRepetition = false)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parameters = new List<string>();

            // Remove /* */ style comments
            var noBlockComments = Regex.Replace(query, @"/\*.*?\*/", "", RegexOptions.Singleline);

            // Remove -- style comments
            var noComments = Regex.Replace(noBlockComments, @"--.+?$", "", RegexOptions.Multiline);

            // Find parameters (excluding those in string literals)
            var matches = Regex.Matches(noComments, @"(?<![a-zA-Z0-9_.])@[A-Za-z]\w*(?=(?:[^']*'[^']*')*[^']*$)");

            foreach (Match match in matches)
                if (set.Add(match.Value) || allowRepetition)
                    parameters.Add(match.Value);

            return parameters;
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