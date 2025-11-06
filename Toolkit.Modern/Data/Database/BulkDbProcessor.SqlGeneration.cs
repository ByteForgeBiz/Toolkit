using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;

namespace ByteForge.Toolkit
{
    public partial class BulkDbProcessor<T>
    {
        /// <summary>
        /// Generates the SQL script for creating a table based on the DataTable schema.
        /// </summary>
        /// <param name="dt">The DataTable containing the schema definition.</param>
        /// <param name="tableName">The name of the table to create.</param>
        /// <param name="dropTable">If true, adds a DROP TABLE statement before creating the table.</param>
        /// <returns>A SQL script string for creating the table with appropriate constraints.</returns>
        /// <exception cref="ArgumentException">Thrown when tableName is null or empty.</exception>
        private string GenerateCreateTableSql(DataTable dt, string tableName, bool dropTable)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty", nameof(tableName));

            tableName = EscapeObjectName(tableName);

            var sql = new StringBuilder();
            // Drop the table if it already exists
            if (dropTable)
            {
                sql.AppendLine($"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL");
                sql.AppendLine($"   DROP TABLE {tableName}");
                sql.AppendLine("GO");
            }

            // Create the table
            sql.AppendLine($"IF OBJECT_ID('{tableName}', 'U') IS NULL");
            sql.AppendLine("BEGIN");
            sql.AppendLine($"CREATE TABLE {tableName}");
            sql.AppendLine("(");

            var colList = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName);

            var columnDefinitions = new StringBuilder();
            foreach (DataColumn column in dt.Columns)
            {
                if (columnDefinitions.Length > 0)
                    columnDefinitions.AppendLine(",");

                var sqlType = GetSqlTypeFromDataColumn(column);
                columnDefinitions.Append($" {column.ColumnName} {sqlType}");
            }

            sql.Append(columnDefinitions);
            sql.AppendLine();
            sql.AppendLine(")");

            // Add primary key constraint if defined
            var tableSuffix = ConvertToSuffix(tableName);

            if (PrimaryKeys.Length > 0)
                sql.AppendLine($"ALTER TABLE {tableName} ADD CONSTRAINT PK_{tableSuffix} PRIMARY KEY ({string.Join(", ", PrimaryKeys)})");

            // Add unique constraints if defined
            foreach (var uniqueCol in UniqueIndexes.Intersect(colList))
                sql.AppendLine($"CREATE UNIQUE INDEX IX_{tableSuffix}_{uniqueCol} ON {tableName} ({uniqueCol})");

            // Add indexes if defined
            foreach (var indexCol in Indexes.Intersect(colList))
                sql.AppendLine($"CREATE INDEX IX_{tableSuffix}_{indexCol} ON {tableName} ({indexCol})");

            sql.AppendLine("END");
            sql.AppendLine("GO");
            var sqlScript = sql.ToString();
            return sqlScript;
        }

        /// <summary>
        /// Generates the SQL script for performing the upsert operation using the staging table.
        /// </summary>
        /// <param name="tempTableName">The name of the temporary staging table containing the data.</param>
        /// <param name="dt">The DataTable containing the schema information.</param>
        /// <returns>A SQL script string that performs the upsert operation.</returns>
        private string GenerateUpsertSql(string tempTableName, DataTable dt)
        {
            // Determine which keys to use for matching (primary key takes precedence)
            var matchKeys = PrimaryKeys.Length > 0 ? PrimaryKeys : UniqueIndexes;

            // Get all columns that should be updated (excluding match keys and identity columns)
            var identityColumns = Properties
                .Where(p => p.GetCustomAttribute<DBColumnAttribute>()?.IsIdentity == true)
                .Select(p => ColumnMap[p])
                .ToArray();

            var updateColumns = dt.Columns.Cast<DataColumn>()
                .Select(c => c.ColumnName)
                .Where(colName => !matchKeys.Contains(colName) && !identityColumns.Contains(colName))
                .ToArray();

            var sql = new StringBuilder();

            // Begin transaction for atomicity
            sql.AppendLine("BEGIN TRANSACTION");
            sql.AppendLine();

            // UPDATE existing records
            if (updateColumns.Length > 0)
            {
                sql.AppendLine($"UPDATE {DestinationTableName}");
                sql.Append("SET ");

                var updateAssignments = updateColumns.Select(col => $"{col} = staging.{col}");
                sql.Append(string.Join(", ", updateAssignments));
                sql.AppendLine();

                sql.AppendLine($"FROM {DestinationTableName} target");
                sql.AppendLine($"INNER JOIN {tempTableName} staging");
                sql.Append("ON ");

                var joinConditions = matchKeys.Select(key => $"target.{key} = staging.{key}");
                sql.Append(string.Join(" AND ", joinConditions));
                sql.AppendLine();
                sql.AppendLine();

                // Check for errors after update
                sql.AppendLine("IF @@ERROR <> 0");
                sql.AppendLine("BEGIN");
                sql.AppendLine("    ROLLBACK TRANSACTION");
                sql.AppendLine("    RETURN");
                sql.AppendLine("END");
            }

            // INSERT new records
            var allColumns = dt.Columns.Cast<DataColumn>()
                .Select(c => c.ColumnName)
                .Where(colName => !identityColumns.Contains(colName))
                .ToArray();

            sql.AppendLine($"INSERT INTO {DestinationTableName} ({string.Join(", ", allColumns)})");
            sql.AppendLine($"SELECT {string.Join(", ", allColumns.Select(col => $"staging.{col}"))}");
            sql.AppendLine($"FROM {tempTableName} staging");
            sql.AppendLine("WHERE NOT EXISTS (");
            sql.AppendLine($"    SELECT 1 FROM {DestinationTableName} target");
            sql.AppendLine("    WHERE ");

            var notExistsConditions = matchKeys.Select(key => $"target.{key} = staging.{key}");
            sql.Append(string.Join(" AND ", notExistsConditions));
            sql.AppendLine();
            sql.AppendLine(")");
            sql.AppendLine();

            // Check for errors after insert
            sql.AppendLine("IF @@ERROR <> 0");
            sql.AppendLine("    ROLLBACK TRANSACTION");
            sql.AppendLine("ELSE");
            sql.AppendLine("    COMMIT TRANSACTION");
            sql.AppendLine();
            sql.AppendLine("GO");

            return sql.ToString();
        }

        /// <summary>
        /// Generates the SQL script for performing the bulk delete operation using the staging table.
        /// </summary>
        /// <param name="tempTableName">The name of the temporary staging table containing the keys to delete.</param>
        /// <param name="keyColumns">The array of column names to use for matching records to delete.</param>
        /// <returns>A SQL script string that performs the delete operation.</returns>
        private string GenerateDeleteSql(string tempTableName, string[] keyColumns)
        {
            var sql = new StringBuilder();

            // Begin transaction for atomicity
            sql.AppendLine("BEGIN TRANSACTION");
            sql.AppendLine();

            // DELETE records that match the keys in the staging table
            sql.AppendLine($"DELETE target");
            sql.AppendLine($"FROM {DestinationTableName} target");
            sql.AppendLine($"INNER JOIN {tempTableName} staging");
            sql.Append("ON ");

            var joinConditions = keyColumns.Select(key => $"target.{key} = staging.{key}");
            sql.Append(string.Join(" AND ", joinConditions));

            sql.AppendLine();
            sql.AppendLine();

            // Commit transaction
            sql.AppendLine("COMMIT TRANSACTION");

            return sql.ToString();
        }

        /// <summary>
        /// Gets the SQL data type for the specified <see cref="DataColumn"/>.
        /// </summary>
        /// <param name="column">The DataColumn to convert to a SQL type.</param>
        /// <returns>A string representation of the SQL data type including nullability.</returns>
        private string GetSqlTypeFromDataColumn(DataColumn column)
        {
            var colSuffix = column.AllowDBNull ? "" : " NOT NULL";

            // First check if this column has a specific DbType specified in the attribute
            var property = Properties.FirstOrDefault(p => ColumnMap[p] == column.ColumnName);
            var columnAttr = property?.GetCustomAttribute<DBColumnAttribute>();
            if (columnAttr != null && columnAttr.IsIdentity)
                return $"int IDENTITY({columnAttr.IdentitySeed},{columnAttr.IdentityStep})" + colSuffix;

            if (property != null)
            {
                if (columnAttr?.DbType != null)
                    return GetSqlTypeFromDbType(columnAttr.DbType.Value, column) + colSuffix;
            }

            // Fallback to the original type mapping logic
            var columnType = TypeHelper.ResolveType(column.DataType) ?? throw new ArgumentException("Column data type cannot be null", nameof(column));

            if (columnType == typeof(string))
            {
                // Check if MaxLength is explicitly set in the attribute first
                var explicitMaxLength = columnAttr?.MaxLength;

                var maxLength = explicitMaxLength > 0 ? explicitMaxLength : column.MaxLength;
                if (maxLength <= 0)
                    return $"varchar({DEFAULT_VARCHAR_LENGTH})" + colSuffix;
                if (maxLength > SQL2000_TEXT_THRESHOLD)
                    return "text" + colSuffix;
                return $"varchar({maxLength})" + colSuffix;
            }

            // Map other types - now handles all types from GetClrTypeFromDbType
            return columnType.Name switch
            {
                nameof(Boolean) => "bit" + colSuffix,
                nameof(Byte) => "tinyint" + colSuffix,
                nameof(SByte) => "tinyint" + colSuffix,// SQL Server doesn't have signed tinyint
                nameof(DateTime) => "datetime" + colSuffix,
                nameof(DateTimeOffset) => "datetime" + colSuffix,// SQL 2000 doesn't have datetimeoffset
                nameof(TimeSpan) => "datetime" + colSuffix,// SQL 2000 doesn't have time type
                nameof(Decimal) => "decimal(18,4)" + colSuffix,
                nameof(Double) => "float" + colSuffix,
                nameof(Single) => "real" + colSuffix,
                nameof(Guid) => "uniqueidentifier" + colSuffix,
                nameof(Int16) => "smallint" + colSuffix,
                nameof(UInt16) => "int" + colSuffix,// No unsigned types in SQL 2000
                nameof(Int32) => "int" + colSuffix,
                nameof(UInt32) => "bigint" + colSuffix,// No unsigned types in SQL 2000
                nameof(Int64) => "bigint" + colSuffix,
                nameof(UInt64) => "decimal(20,0)" + colSuffix,// No unsigned types in SQL 2000
                "Byte[]" => "varbinary(8000)" + colSuffix,
                nameof(Object) => "sql_variant" + colSuffix,
                _ => $"varchar({DEFAULT_VARCHAR_LENGTH})" + colSuffix,
            };
        }

        /// <summary>
        /// Gets the SQL Server data type string for the specified DbType.
        /// </summary>
        /// <param name="dbType">The DbType to convert.</param>
        /// <param name="column">The DataColumn providing additional type information like MaxLength.</param>
        /// <returns>A string representation of the SQL data type.</returns>
        private string GetSqlTypeFromDbType(DbType dbType, DataColumn column)
        {
            switch (dbType)
            {
                case DbType.AnsiString:
                case DbType.String:
                    var maxLength = column.MaxLength;
                    if (maxLength <= 0)
                        return $"varchar({DEFAULT_VARCHAR_LENGTH})";
                    if (maxLength > SQL2000_TEXT_THRESHOLD)
                        return "text";
                    return $"varchar({maxLength})";

                case DbType.AnsiStringFixedLength:
                case DbType.StringFixedLength:
                    var fixedLength = column.MaxLength > 0 ? column.MaxLength : DEFAULT_VARCHAR_LENGTH;
                    if (fixedLength > SQL2000_TEXT_THRESHOLD)
                        return "text";
                    return $"char({fixedLength})";

                case DbType.Binary:
                    return "varbinary(8000)";

                case DbType.Boolean:
                    return "bit";

                case DbType.Byte:
                    return "tinyint";

                case DbType.Currency:
                    return "money";

                case DbType.Date:
                case DbType.DateTime:
                case DbType.DateTime2:
                    return "datetime";

                case DbType.Decimal:
                case DbType.VarNumeric:
                    return "decimal(18,4)";

                case DbType.Double:
                    return "float";

                case DbType.Guid:
                    return "uniqueidentifier";

                case DbType.Int16:
                    return "smallint";

                case DbType.Int32:
                    return "int";

                case DbType.Int64:
                    return "bigint";

                case DbType.Object:
                    return "sql_variant";

                case DbType.SByte:
                    return "tinyint";

                case DbType.Single:
                    return "real";

                case DbType.Time:
                    return "datetime";  // SQL 2000 doesn't have time type

                case DbType.UInt16:
                    return "int";  // No unsigned types in SQL 2000

                case DbType.UInt32:
                    return "bigint";  // No unsigned types in SQL 2000

                case DbType.UInt64:
                    return "decimal(20,0)";  // No unsigned types in SQL 2000

                case DbType.Xml:
                    return "text";  // SQL 2000 doesn't have xml type

                default:
                    return $"varchar({DEFAULT_VARCHAR_LENGTH})";
            }
        }

        /// <summary>
        /// Escapes SQL object names by properly handling brackets and multi-part names.
        /// </summary>
        /// <param name="objectName">The SQL object name to escape.</param>
        /// <returns>The properly escaped SQL object name.</returns>
        /// <exception cref="ArgumentException">Thrown when objectName is null or empty.</exception>
        public static string EscapeObjectName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                throw new ArgumentException("Object name cannot be null or empty", nameof(objectName));

            var parts = ParseObjectName(objectName);
            return string.Join(".", parts.Select(EscapeIdentifier));
        }

        /// <summary>
        /// Converts object name to a valid SQL identifier suffix for constraints and indexes.
        /// </summary>
        /// <param name="objectName">The SQL object name to convert to a suffix.</param>
        /// <returns>A valid SQL identifier that can be used as a suffix for constraints and indexes.</returns>
        /// <exception cref="ArgumentException">Thrown when objectName is null or empty.</exception>
        public static string ConvertToSuffix(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                throw new ArgumentException("Object name cannot be null or empty", nameof(objectName));

            var parts = ParseObjectName(objectName);
            // Use the last part (table name) for the suffix
            var tableName = parts.LastOrDefault() ?? "";

            if (string.IsNullOrEmpty(tableName))
                return "";

            var result = new StringBuilder();

            foreach (var c in tableName)
            {
                if (char.IsLetterOrDigit(c))
                {
                    result.Append(c);
                }
                else if (char.IsWhiteSpace(c) || c == '-' || c == '.' || c == '[' || c == ']')
                {
                    // Convert common separators to underscore
                    if (result.Length > 0 && result[result.Length - 1] != '_')
                        result.Append('_');
                }
                // Skip other special characters entirely
            }

            // Clean up the result
            var suffix = result.ToString().Trim('_');

            // Ensure it starts with a letter or underscore
            if (!string.IsNullOrEmpty(suffix) && char.IsDigit(suffix[0]))
                suffix = "_" + suffix;

            // If result is empty or invalid, provide a fallback
            if (string.IsNullOrEmpty(suffix) || !IsValidIdentifier(suffix))
                suffix = "Table";

            return suffix;
        }

        /// <summary>
        /// Checks if a string is a valid SQL identifier.
        /// </summary>
        /// <param name="identifier">The string to check.</param>
        /// <returns>True if the string is a valid SQL identifier; otherwise, false.</returns>
        private static bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;

            // First character must be letter or underscore
            if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
                return false;

            // Remaining characters must be letters, digits, or underscores
            return identifier.Skip(1).All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        /// <summary>
        /// Parses a multi-part SQL object name handling brackets correctly.
        /// </summary>
        /// <param name="objectName">The SQL object name to parse.</param>
        /// <returns>A list of individual parts of the object name.</returns>
        private static List<string> ParseObjectName(string objectName)
        {
            var parts = new List<string>();
            var currentPart = new StringBuilder();
            var inBrackets = false;

            for (var i = 0; i < objectName.Length; i++)
            {
                var c = objectName[i];

                if (c == '[' && !inBrackets)
                {
                    inBrackets = true;
                    // Don't include the opening bracket in the part
                }
                else if (c == ']' && inBrackets)
                {
                    // Check for escaped bracket ]]
                    if (i + 1 < objectName.Length && objectName[i + 1] == ']')
                    {
                        currentPart.Append(']'); // Add single bracket to result
                        i++; // Skip the second bracket
                    }
                    else
                    {
                        inBrackets = false;
                        // Don't include the closing bracket in the part
                    }
                }
                else if (c == '.' && !inBrackets)
                {
                    // This is a separator
                    parts.Add(currentPart.ToString());
                    currentPart.Clear();
                }
                else
                {
                    currentPart.Append(c);
                }
            }

            // Add the last part
            if (currentPart.Length > 0)
                parts.Add(currentPart.ToString());

            return parts;
        }

        /// <summary>
        /// Escapes a SQL identifier by wrapping it in brackets and escaping internal brackets.
        /// </summary>
        /// <param name="identifier">The SQL identifier to escape.</param>
        /// <returns>The escaped SQL identifier.</returns>
        private static string EscapeIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return identifier;

            // Escape any existing square brackets by doubling them
            var escaped = identifier.Replace("[", "[[").Replace("]", "]]");

            // Always wrap in square brackets for safety
            return $"[{escaped}]";
        }
    }
}