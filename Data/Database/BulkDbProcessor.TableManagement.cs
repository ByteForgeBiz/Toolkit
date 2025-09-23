using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace ByteForge.Toolkit
{
    public partial class BulkDbProcessor<T>
    {
        /// <summary>
        /// Adds a record to the specified <see cref="DataTable"/> by mapping property values.
        /// </summary>
        /// <param name="record">The record to add to the DataTable.</param>
        /// <param name="dt">The DataTable to which the record will be added.</param>
        /// <remarks>
        /// This method maps the properties of the record to columns in the DataTable.
        /// Special handling is provided for null values, DateTime.MinValue, enums, and string length adjustments.
        /// </remarks>
        protected virtual void AddRecordToDataTable(T record, DataTable dt)
        {
            object colValue;
            var values = new List<object>();

            foreach (var prop in Properties)
            {
                var columnAttr = prop.GetCustomAttribute<DBColumnAttribute>();
                var col = ColumnMap[prop];
                if (!dt.Columns.Contains(col))
                    continue;

                var propType = TypeHelper.ResolveType(prop);
                var propValue = prop.GetValue(record);

                if (propValue == null)
                    colValue = (DBNull.Value);
                else if (propType == typeof(DateTime) && (DateTime)propValue == DateTime.MinValue)
                    colValue = (DBNull.Value);
                else if (propType.IsEnum)
                    colValue = ((int)propValue);
                else
                    colValue = (propValue);

                /*
                 * Apply any custom converter function if specified in the attribute.
                 * Also, enforce non-nullability by setting default values for non-nullable columns.
                 */
                if (columnAttr?.Converter != null)
                    colValue = columnAttr.Converter(colValue);

                // Handle dynamic string length adjustment only if MaxLength is not explicitly set
                if (propType == typeof(string))
                {
                    if (columnAttr?.MaxLength <= 0)
                    {
                        var column = dt.Columns[col];
                        var newMaxLength = Math.Max(column.MaxLength, (propValue?.ToString() ?? "").Length);
                        column.MaxLength = newMaxLength;
                    }
                    else if (colValue is string strValue && strValue.Length > columnAttr.MaxLength)
                    {
                        // Truncate strings that exceed the defined MaxLength
                        colValue = strValue.Substring(0, columnAttr.MaxLength);
                    }

                    /*
                     * Convert null strings to empty strings if the option is enabled.
                     */
                    if (NullStringsAreEmpty && colValue == DBNull.Value)
                        colValue = string.Empty;
                }

                /*
                 * If the column is marked as non-nullable and the value is DBNull, 
                 * set it to the default value for the property type.
                 */
                if (columnAttr?.IsNullable == false && colValue == DBNull.Value)
                    colValue = TypeHelper.GetDefault(propType);

                /*
                 * Identity columns should be set to DBNull so that the database can auto-generate the value.  
                 */
                if (columnAttr?.IsIdentity == true)
                    colValue = null;

                values.Add(colValue);
            }

            dt.Rows.Add(values.ToArray());
        }

        /// <summary>
        /// Initializes the property mapping for the type <typeparamref name="T"/>.<br/>
        /// Can be called to refresh mappings if needed.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no properties with DBColumnAttribute are found in the type, or when duplicate column names are found in property mappings.
        /// </exception>
        /// <remarks>
        /// This method scans the type for properties marked with <see cref="DBColumnAttribute"/> and creates mappings for primary keys,
        /// unique indexes, and regular indexes based on the attribute settings.
        /// </remarks>
        protected void InitializePropertyMapping()
        {
            Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(p => p.CanRead && p.GetCustomAttributes(typeof(DBColumnAttribute), true).Any())
                .ToArray();

            if (Properties.Length == 0)
                throw new InvalidOperationException($"No properties with DBColumnAttribute found in type {typeof(T).Name}");

            ColumnMap = Properties.ToDictionary(
                p => p,
                p => p.GetCustomAttribute<DBColumnAttribute>()?.Name ?? p.Name
            );

            if (ColumnMap.Values.Distinct().Count() != ColumnMap.Values.Count)
                throw new InvalidOperationException("Duplicate column names found in property mappings. Ensure each DBColumnAttribute has a unique Name.");

            PrimaryKeys = Properties
                .Where(p => p.GetCustomAttribute<DBColumnAttribute>()?.IsPrimaryKey == true)
                .Select(p => ColumnMap[p])
                .ToArray();

            UniqueIndexes = Properties
                .Where(p => p.GetCustomAttribute<DBColumnAttribute>()?.IsUnique == true)
                .Select(p => ColumnMap[p])
                .Except(PrimaryKeys)
                .ToArray();

            Indexes = Properties
                .Where(p => p.GetCustomAttribute<DBColumnAttribute>()?.HasIndex == true)
                .Select(p => ColumnMap[p])
                .Except(PrimaryKeys)
                .Except(UniqueIndexes)
                .ToArray();
        }

        /// <summary>
        /// Creates and configures the <see cref="DataTable"/> structure for the bulk insert operation.<br/>
        /// Can be overridden to provide custom <see cref="DataTable"/> creation logic.
        /// </summary>
        /// <returns>A configured <see cref="DataTable"/> with the appropriate schema.</returns>
        /// <remarks>
        /// The DataTable is configured based on the properties decorated with <see cref="DBColumnAttribute"/>,
        /// respecting column types, maximum lengths, auto-increment settings, and constraints.
        /// </remarks>
        protected virtual DataTable CreateDataTable()
        {
            var dt = new DataTable($"type_{typeof(T).Name}");

            foreach (var prop in Properties)
            {
                var columnAttr = prop.GetCustomAttribute<DBColumnAttribute>();

                var columnType = GetDataTableColumnType(prop, columnAttr);
                var column = dt.Columns.Add(ColumnMap[prop], columnType);

                // Set MaxLength if specified in the attribute and it's a string column
                if (columnType == typeof(string) && columnAttr?.MaxLength > 0)
                    column.MaxLength = columnAttr.MaxLength;

                if (columnAttr != null)
                {
                    if (columnAttr.IsIdentity)
                    {
                        column.AutoIncrement = true;
                        column.AutoIncrementSeed = columnAttr.IdentitySeed;
                        column.AutoIncrementStep = columnAttr.IdentityStep;
                    }

                    if (columnAttr.IsPrimaryKey)
                        column.AllowDBNull = false;

                    if (columnAttr.IsUnique)
                        column.Unique = true;
                }
            }
            OnDebug($"Created DataTable for type {typeof(T).Name} with {dt.Columns.Count} columns.");

            return dt;
        }

        /// <summary>
        /// Creates the destination table in the database based on the provided <see cref="DataTable"/> schema.
        /// </summary>
        /// <param name="db">The database connection to use.</param>
        /// <param name="dt">The DataTable containing the schema to use for creating the table.</param>
        /// <param name="tableName">The name of the table to create.</param>
        /// <param name="dropTable">If true, will drop the table if it already exists before creating it.</param>
        /// <exception cref="Exception">Rethrows any exception that occurs during table creation with additional context.</exception>
        /// <remarks>
        /// Executes a SQL script to create the table with appropriate primary keys, unique constraints, and indexes based on the
        /// DataTable schema and attribute settings.
        /// </remarks>
        private void CreateTable(DBAccess db, DataTable dt, string tableName, bool dropTable)
        {
            var sqlScript = GenerateCreateTableSql(dt, tableName, dropTable);

            if (dropTable)
                OnMessage($"Creating table {tableName} (dropping if exists)...");
            else
                OnMessage($"Creating table {tableName}...");

            try
            {
                var result = db.ExecuteScript(sqlScript);
                if (!result.Success)
                    throw result.LastException;
            }
            catch (Exception ex)
            {
                OnError($"Failed to create table {tableName}", ex);
                throw;
            }
        }

        /// <summary>
        /// Adds only the key column values from a record to the DataTable.
        /// </summary>
        /// <param name="record">The record containing the key values.</param>
        /// <param name="dt">The DataTable to which the key values will be added.</param>
        /// <param name="keyColumns">The array of key column names to extract from the record.</param>
        /// <remarks>
        /// This method is optimized to extract only the key columns needed for operations like bulk delete,
        /// avoiding the overhead of processing all columns.
        /// </remarks>
        private void AddKeyOnlyRecordToDataTable(T record, DataTable dt, string[] keyColumns)
        {
            var values = new object[keyColumns.Length];

            for (var i = 0; i < keyColumns.Length; i++)
            {
                var keyColumn = keyColumns[i];
                var prop = Properties.FirstOrDefault(p => ColumnMap[p] == keyColumn);

                if (prop == null)
                {
                    values[i] = DBNull.Value;
                    continue;
                }

                var propType = TypeHelper.ResolveType(prop);
                var value = prop.GetValue(record);

                if (value == null)
                    values[i] = DBNull.Value;
                else if (propType == typeof(DateTime) && (DateTime)value == DateTime.MinValue)
                    values[i] = DBNull.Value;
                else if (propType.IsEnum)
                    values[i] = (int)value;
                else
                    values[i] = value;

                // Handle dynamic string length adjustment only if MaxLength is not explicitly set
                if (propType == typeof(string))
                {
                    var columnAttr = prop.GetCustomAttribute<DBColumnAttribute>();
                    if (columnAttr?.MaxLength == null)
                    {
                        var column = dt.Columns[keyColumn];
                        column.MaxLength = Math.Max(column.MaxLength, (value?.ToString() ?? "").Length);
                    }
                }
            }

            dt.Rows.Add(values);
        }

        /// <summary>
        /// Creates a DataTable containing only the key columns needed for delete operations.
        /// </summary>
        /// <param name="keyColumns">The array of key column names to include in the DataTable.</param>
        /// <returns>A DataTable with schema containing only the specified key columns.</returns>
        /// <remarks>
        /// The created DataTable is optimized for bulk delete operations, containing only the
        /// columns needed to identify records to delete.
        /// </remarks>
        private DataTable CreateKeyOnlyDataTable(string[] keyColumns)
        {
            var dt = new DataTable($"type_{typeof(T).Name}_keys");

            foreach (var keyColumn in keyColumns)
            {
                var prop = Properties.FirstOrDefault(p => ColumnMap[p] == keyColumn);
                if (prop == null)
                    continue;

                var columnAttr = prop.GetCustomAttribute<DBColumnAttribute>();
                var columnType = GetDataTableColumnType(prop, columnAttr);
                var column = dt.Columns.Add(keyColumn, columnType);

                // Set MaxLength if specified in the attribute and it's a string column
                if (columnType == typeof(string) && columnAttr?.MaxLength > 0)
                    column.MaxLength = columnAttr.MaxLength;

                column.AllowDBNull = false; // Key columns shouldn't be null
            }

            return dt;
        }

        /// <summary>
        /// Gets the CLR type that corresponds to the specified DbType.
        /// </summary>
        /// <param name="dbType">The DbType for which to get the corresponding CLR type.</param>
        /// <returns>The .NET type that corresponds to the specified DbType.</returns>
        /// <remarks>
        /// This method is used to map database column types to .NET types when creating DataTable schemas.
        /// </remarks>
        private Type GetClrTypeFromDbType(DbType dbType)
        {
            switch (dbType)
            {
                case DbType.AnsiString:
                case DbType.AnsiStringFixedLength:
                case DbType.String:
                case DbType.StringFixedLength:
                    return typeof(string);
                case DbType.Binary:
                    return typeof(byte[]);
                case DbType.Boolean:
                    return typeof(bool);
                case DbType.Byte:
                    return typeof(byte);
                case DbType.Currency:
                case DbType.Decimal:
                case DbType.VarNumeric:
                    return typeof(decimal);
                case DbType.Date:
                case DbType.DateTime:
                case DbType.DateTime2:
                    return typeof(DateTime);
                case DbType.DateTimeOffset:
                    return typeof(DateTimeOffset);
                case DbType.Double:
                    return typeof(double);
                case DbType.Guid:
                    return typeof(Guid);
                case DbType.Int16:
                    return typeof(short);
                case DbType.Int32:
                    return typeof(int);
                case DbType.Int64:
                    return typeof(long);
                case DbType.Object:
                    return typeof(object);
                case DbType.SByte:
                    return typeof(sbyte);
                case DbType.Single:
                    return typeof(float);
                case DbType.Time:
                    return typeof(TimeSpan);
                case DbType.UInt16:
                    return typeof(ushort);
                case DbType.UInt32:
                    return typeof(uint);
                case DbType.UInt64:
                    return typeof(ulong);
                case DbType.Xml:
                    return typeof(string);
                default:
                    return typeof(string);
            }
        }

        /// <summary>
        /// Gets the column type for the specified property type.
        /// </summary>
        /// <param name="propertyType">The property type for which to get the column type.</param>
        /// <returns>The appropriate .NET type to use for a DataTable column based on the property type.</returns>
        /// <remarks>
        /// Handles mapping of nullable types, enums, and various .NET types to their appropriate
        /// DataTable column types.
        /// </remarks>
        private Type GetColumnType(Type propertyType)
        {
            // Handle nullable types
            var underlyingType = TypeHelper.ResolveType(propertyType);

            // Map common .NET types to their SQL Server equivalents
            if (underlyingType.IsEnum)
                return typeof(int);

            return underlyingType.Name switch
            {
                nameof(Boolean) => typeof(Boolean),
                nameof(Byte) => typeof(Byte),
                nameof(SByte) => typeof(SByte),
                nameof(DateTime) => typeof(DateTime),
                nameof(DateTimeOffset) => typeof(DateTimeOffset),
                nameof(TimeSpan) => typeof(TimeSpan),
                nameof(Decimal) => typeof(Decimal),
                nameof(Double) => typeof(Double),
                nameof(Single) => typeof(Single),
                nameof(Guid) => typeof(Guid),
                nameof(Int16) => typeof(Int16),
                nameof(UInt16) => typeof(UInt16),
                nameof(Int32) => typeof(Int32),
                nameof(UInt32) => typeof(UInt32),
                nameof(Int64) => typeof(Int64),
                nameof(UInt64) => typeof(UInt64),
                "Byte[]" => typeof(byte[]),
                _ => typeof(String),
            };
        }

        /// <summary>
        /// Gets the appropriate .NET type for the DataTable column based on the property and attribute.
        /// </summary>
        /// <param name="prop">The PropertyInfo representing the property.</param>
        /// <param name="columnAttr">The DBColumnAttribute applied to the property, if any.</param>
        /// <returns>The .NET type to use for the DataTable column.</returns>
        /// <remarks>
        /// This method determines the column type based on the DBColumnAttribute's DbType if specified,
        /// otherwise falls back to inferring the type from the property's type.
        /// </remarks>
        private Type GetDataTableColumnType(PropertyInfo prop, DBColumnAttribute columnAttr)
        {
            if (columnAttr?.DbType != null)
                return GetClrTypeFromDbType(columnAttr.DbType.Value);

            return GetColumnType(prop.PropertyType);
        }
    }
}