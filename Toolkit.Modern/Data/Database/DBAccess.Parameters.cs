using ByteForge.Toolkit.Logging;
using ByteForge.Toolkit.Utilities;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace ByteForge.Toolkit.Data;
/*
 *  ___  ___   _                     ___                         _              
 * |   \| _ ) /_\  __ __ ___ ______ | _ \__ _ _ _ __ _ _ __  ___| |_ ___ _ _ ___
 * | |) | _ \/ _ \/ _/ _/ -_)_-<_-<_|  _/ _` | '_/ _` | '  \/ -_)  _/ -_) '_(_-<
 * |___/|___/_/ \_\__\__\___/__/__(_)_| \__,_|_| \__,_|_|_|_\___|\__\___|_| /__/
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
    private void AddParametersToCommand(IDbCommand cmd, string query, object?[]? arguments)
    {
        arguments ??= [];
        var matches = ParseParameters(query);
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
            Log.Debug(string.Join(", " + Environment.NewLine, 
                        cmd.Parameters.Cast<DbParameter>()
                                      .Select(p => @$"{p.ParameterName} = '{(p.Value is DateTime dt ? 
                                                                                dt.HasTimeComponent() ?
                                                                                dt.ToString("yyyy-MM-dd HH:mm:ss.fff") :
                                                                                dt.ToString("yyyy-MM-dd") :
                                                                                p.Value)}'")));
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
    private IDataParameter CreateParameter(IDbCommand cmd, string name, object? value)
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
    /// <returns>
    /// A list of parameter names found in the query string.<br/>
    /// The list may include duplicates if the class is set to operate in <see cref="DataBaseType.SQLServer"/>.<br/>
    /// If no parameters are found, an empty list is returned.
    /// </returns>
    /// <remarks>
    /// This method removes SQL comments (both block comments and single-line comments)
    /// before extracting parameter names. Parameters within string literals are ignored.
    /// Parameter names are case-insensitive and must start with '@', followed by a letter and 
    /// then alphanumeric characters or underscores.
    /// <para>
    /// For SQL Server databases, this method recognizes named parameter assignment syntax 
    /// (e.g., '@paramName = @valueParam') and only includes the value parameters (@valueParam) 
    /// in the result, treating the parameter names (@paramName) as literal SQL text.
    /// </para>
    /// </remarks>
    private List<string> ParseParameters(string query)
    {
        var allowRepetition = Options.DatabaseType == DataBaseType.ODBC;
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parameters = new List<string>();

        // Handle null or empty queries
        if (string.IsNullOrWhiteSpace(query))
            return parameters;

        // Remove /* */ style comments
        var noBlockComments = Regex.Replace(query, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Remove -- style comments
        var noComments = Regex.Replace(noBlockComments, @"--.+?$", "", RegexOptions.Multiline);

        if (Options.DatabaseType == DataBaseType.ODBC)
        {
            // Original logic for non-SQL Server databases
            var matches = Regex.Matches(noComments, @"(?<![a-zA-Z0-9_.])@[A-Za-z]\w*(?=(?:[^']*'[^']*')*[^']*$)");

            foreach (Match match in matches)
                if (set.Add(match.Value) || allowRepetition)
                    parameters.Add(match.Value);

            return parameters;
        }

        // For SQL Server, handle named parameter assignment syntax (@paramName = @valueParam)

        // Find named parameter assignments and extract only the value parameters (right side of =)
        // Exclude assignments that are preceded by comparison keywords (these are comparisons, not parameter assignments)
        var namedAssignments = Regex.Matches(noComments, @"(?<!(WHERE|HAVING|AND|OR|WHEN|ON|CASE|THEN|ELSE|NOT|EXISTS|IN|ANY|ALL|SOME)\s+)@[A-Za-z]\w*\s*=\s*(@[A-Za-z]\w*)(?=(?:[^']*'[^']*')*[^']*$)", RegexOptions.IgnoreCase);
        var assignedParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in namedAssignments)
        {
            var valueParam = match.Groups[2].Value; // Now group 2 because of the lookbehind
            assignedParams.Add(valueParam);
            if (set.Add(valueParam) || allowRepetition)
                parameters.Add(valueParam);
        }

        // Find standalone parameters (not part of named assignments)
        var allMatches = Regex.Matches(noComments, @"(?<![a-zA-Z0-9_.])@[A-Za-z]\w*(?=(?:[^']*'[^']*')*[^']*$)");

        foreach (Match match in allMatches)
        {
            var param = match.Value;
            // Skip if this parameter is already handled as a value parameter in named assignment
            if (assignedParams.Contains(param))
                continue;

            // Skip if this parameter is the left side of an assignment that is NOT a comparison
            // (i.e., skip assignments that are NOT preceded by comparison keywords)
            var isAssignmentNotComparison = Regex.IsMatch(noComments, @"(?<!(WHERE|HAVING|AND|OR|WHEN|ON|CASE|THEN|ELSE|NOT|EXISTS|IN|ANY|ALL|SOME)\s+)" +
                Regex.Escape(param) + @"\s*=\s*(?:@[A-Za-z]\w*|'[^']*'|""[^""]*""|[^,\s)]+)(?=(?:[^']*'[^']*')*[^']*$)", RegexOptions.IgnoreCase);
            if (isAssignmentNotComparison)
                continue;

            // This is a standalone parameter
            if (set.Add(param) || allowRepetition)
                parameters.Add(param);
        }

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
            sqlParam.TypeName = dt.TableName;
            return;
        }

        /*
         * Now we deal with the rest of the types
         */
        var type = value.GetType();

        DbType? dbType;
        dbType = type.Name switch
        {
            "String" => System.Data.DbType.String,
            "Char" => System.Data.DbType.StringFixedLength,
            "Char[]" => System.Data.DbType.String,
            "Byte[]" => System.Data.DbType.Binary,
            "Byte" => System.Data.DbType.Byte,
            "SByte" => System.Data.DbType.SByte,
            "Int16" => System.Data.DbType.Int16,
            "UInt16" => System.Data.DbType.UInt16,
            "Int32" => System.Data.DbType.Int32,
            "UInt32" => System.Data.DbType.UInt32,
            "Single" => System.Data.DbType.Single,
            "Double" => System.Data.DbType.Double,
            "Boolean" => System.Data.DbType.Boolean,
            "DateTime" => System.Data.DbType.DateTime,
            "DateTimeOffset" => System.Data.DbType.DateTimeOffset,
            "TimeSpan" => System.Data.DbType.Time,
            _ => null,
        };

        if (dbType.HasValue)
        {
            prm.DbType = dbType.Value;
            return;
        }

        /*
         * String is omitted because it's the default type
         * Special handling for ODBC which has more restrictive type mappings
         */
        if (Options.DatabaseType == DataBaseType.ODBC)
        {
            prm.DbType = type.Name switch
            {
                "Guid" => System.Data.DbType.String,    // GUID as string for ODBC
                "Int64" => System.Data.DbType.Int32,    // Access doesn't have BigInt, use Int32
                "UInt64" => System.Data.DbType.UInt32,
                "Decimal" => System.Data.DbType.Double, // Use Double for ODBC compatibility with Access Currency
                _ => System.Data.DbType.String,
            };
        }
        else
        {
            prm.DbType = type.Name switch
            {
                "Guid" => System.Data.DbType.Guid,
                "Int64" => System.Data.DbType.Int64,
                "UInt64" => System.Data.DbType.UInt64,
                "Decimal" => System.Data.DbType.Decimal,
                _ => System.Data.DbType.String,
            };
        }
    }
}
