using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static ByteForge.Toolkit.DBAccess;
using System.Threading;

namespace ByteForge.Toolkit
{
    /*
     *  ___      _ _   ___  _    ___                                 _________  
     * | _ )_  _| | |_|   \| |__| _ \_ _ ___  __ ___ ______ ___ _ _ / /_   _\ \ 
     * | _ \ || | | / / |) | '_ \  _/ '_/ _ \/ _/ -_)_-<_-</ _ \ '_< <  | |  > >
     * |___/\_,_|_|_\_\___/|_.__/_| |_| \___/\__\___/__/__/\___/_|  \_\ |_| /_/ 
     *                                                                          
     */
    /// <summary>
    /// Provides generic batch database operations for bulk inserting, upserting, and deleting records of type <typeparamref name="T"/>.<br/>
    /// Supports SQL Server bulk operations and dynamic table creation based on property mappings.
    /// </summary>
    /// <typeparam name="T">The type of records to be processed.</typeparam>
    public class BulkDbProcessor<T>
    {
        // Fields (sorted)
        private const int DEFAULT_VARCHAR_LENGTH = 255;
        private const int DefaultBatchSize = 1000;
        private const int SQL2000_TEXT_THRESHOLD = 8000;
        private bool _dropTableIfExists = true;

        // Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkDbProcessor{T}"/> class.
        /// </summary>
        protected BulkDbProcessor()
        {
            InitializePropertyMapping();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkDbProcessor{T}"/> class with the specified destination table
        /// name.
        /// </summary>
        /// <param name="destinationTableName">The name of the destination table where data will be processed.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="destinationTableName"/> is null, empty, or consists only of whitespace.</exception>
        public BulkDbProcessor(string destinationTableName) : this()
        {
            if (string.IsNullOrWhiteSpace(destinationTableName))
                throw new ArgumentException("Destination table name cannot be null or empty", nameof(destinationTableName));
            DestinationTableName = destinationTableName;
        }

        // Properties (sorted)
        /// <summary>
        /// Gets the batch size for bulk insert operations. Default is 1000 records.
        /// </summary>
        protected virtual int BatchSize => DefaultBatchSize;

        /// <summary>
        /// Gets or sets a value indicating whether the destination table should be created.
        /// </summary>
        public bool CreateDestinationTable { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the destination table should be dropped if it already exists.
        /// </summary>
        public bool DropDestinationTableIfExists
        {
            get => _dropTableIfExists;
            set
            {
                _dropTableIfExists = value;
                CreateDestinationTable = value || CreateDestinationTable;
            }
        }

        /// <summary>
        /// Gets the timeout in seconds for bulk copy operations. Default is 600 seconds (10 minutes).
        /// </summary>
        protected virtual int BulkCopyTimeout => 600;

        /// <summary>
        /// Gets the cached property to column mapping.
        /// </summary>
        protected Dictionary<PropertyInfo, string> ColumnMap { get; private set; }

        /// <summary>
        /// Gets the name of the destination table for bulk insert operations.
        /// </summary>
        public string DestinationTableName { get; }

        /// <summary>
        /// Gets the names of the index columns.
        /// </summary>
        public string[] Indexes { get; private set; }

        /// <summary>
        /// Gets the cached properties for the current mapping.
        /// </summary>
        protected PropertyInfo[] Properties { get; private set; }

        /// <summary>
        /// Gets the names of the primary key columns.
        /// </summary>
        public string[] PrimaryKeys { get; private set; }

        /// <summary>
        /// Gets the names of the unique index columns.
        /// </summary>
        public string[] UniqueIndexes { get; private set; }

        // Events (sorted)
        /// <summary>
        /// Event that fires when a batch operation encounters an error.
        /// </summary>
        public event Action<string, Exception> ErrorOccurred;

        /// <summary>
        /// Event that fires when batch processing progress is made.
        /// </summary>
        public event Action<float> ProgressChanged;

        // Public Methods (sorted)
        /// <summary>
        /// Inserts multiple records into the database in a single operation.
        /// </summary>
        public bool BulkInsert(DBAccess db, IEnumerable<T> records) => Utils.RunSync((c) => BulkInsertAsync(db, records, c));

        /// <summary>
        /// Inserts a collection of records into the database in a single bulk operation.
        /// </summary>
        public async Task<bool> BulkInsertAsync(DBAccess db, IEnumerable<T> records, CancellationToken  cancellationToken)
        {
            if (db.DbType != DataBaseType.SQLServer)
            {
                ErrorOccurred?.Invoke("Batch insert failed", new Exception("Bulk insert is only supported for SQL Server databases."));
                return false;
            }

            try
            {
                var dt = CreateDataTable();
                var totalRecords = 0;

                foreach (var record in records)
                {
                    AddRecordToDataTable(record, dt);
                    totalRecords++;
                }

                if (totalRecords == 0)
                {
                    ProgressChanged?.Invoke(100);
                    return true;
                }

                if (CreateDestinationTable)
                    CreateTable(db, dt, DestinationTableName, DropDestinationTableIfExists);

                using (var bulkCopy = new SqlBulkCopy(db.ConnectionString, SqlBulkCopyOptions.Default)
                {
                    DestinationTableName = DestinationTableName,
                    BatchSize = BatchSize,
                    BulkCopyTimeout = BulkCopyTimeout
                })
                {
                    foreach (DataColumn column in dt.Columns)
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

                    bulkCopy.SqlRowsCopied += (sender, e) =>
                        ProgressChanged?.Invoke((float)e.RowsCopied / totalRecords * 100);

                    try
                    {
                        await bulkCopy.WriteToServerAsync(dt, cancellationToken);
                    }
                    finally
                    {
                        bulkCopy.Close();
                    }
                }

                ProgressChanged?.Invoke(100);
                dt.Clear();

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke("Batch insert failed", ex);
                Log.Error("Batch insert failed", ex);
                return false;
            }
        }

        /// <summary>
        /// Performs a bulk upsert operation (update existing records, insert new ones) for a collection of records.
        /// </summary>
        public bool BulkUpsert(DBAccess db, IEnumerable<T> records) => Utils.RunSync((c) => BulkUpsertAsync(db, records, c));

        /// <summary>
        /// Performs a bulk upsert operation (update existing records, insert new ones) for a collection of records.
        /// </summary>
        public async Task<bool> BulkUpsertAsync(DBAccess db, IEnumerable<T> records, CancellationToken cancellationToken)
        {
            if (db.DbType != DataBaseType.SQLServer)
            {
                ErrorOccurred?.Invoke("Bulk upsert failed", new Exception("Bulk upsert is only supported for SQL Server databases."));
                return false;
            }

            // Validate that type supports upsert operations
            ValidateUpsertSupport();

            string tempTableName = null;
            try
            {
                var dt = CreateDataTable();
                var totalRecords = 0;

                // Populate DataTable with records
                foreach (var record in records)
                {
                    AddRecordToDataTable(record, dt);
                    totalRecords++;
                }

                if (totalRecords == 0)
                {
                    ProgressChanged?.Invoke(100);
                    return true;
                }

                // Create global temp table with GUID name
                tempTableName = $"BulkUpsert_{Guid.NewGuid():N}";

                // Create the temp table (never drop, it's a new GUID-named table)
                CreateTable(db, dt, tempTableName, false);

                // Bulk insert data into temp table
                using (var bulkCopy = new SqlBulkCopy(db.ConnectionString, SqlBulkCopyOptions.Default)
                {
                    DestinationTableName = tempTableName,
                    BatchSize = BatchSize,
                    BulkCopyTimeout = BulkCopyTimeout
                })
                {
                    foreach (DataColumn column in dt.Columns)
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

                    bulkCopy.SqlRowsCopied += (sender, e) =>
                        ProgressChanged?.Invoke((float)e.RowsCopied / totalRecords * 50); // 50% progress for bulk insert

                    await bulkCopy.WriteToServerAsync(dt, cancellationToken);
                    bulkCopy.Close();
                }

                // Generate and execute upsert SQL

                /*
                 * The script generated will:
                 * 
                 * 1. Create the destination table if it doesn't exist (based on the DataTable schema).
                 * 2. Start a transaction to ensure atomicity.
                 * 3. Update existing records in the destination table that match on primary key or unique index.
                 * 4. Insert new records that do not exist in the destination table.
                 * 5. Commit the transaction if all operations succeed, or roll back if any operation fails.
                 */

                var createTableSql = GenerateCreateTableSql(dt, DestinationTableName, dropTable: false);
                var upsertSql = GenerateUpsertSql(tempTableName, dt);
                var result = db.ExecuteScript($"{createTableSql}{Environment.NewLine}{upsertSql}");

                if (!result.Success)
                    throw result.LastException ?? new Exception("Upsert operation failed");

                ProgressChanged?.Invoke(100);
                dt.Clear();

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke("Bulk upsert failed", ex);
                Log.Error("Bulk upsert failed", ex);
                return false;
            }
            finally
            {
                // Clean up global temp table
                if (!string.IsNullOrEmpty(tempTableName))
                {
                    try
                    {
                        var dropSql = $"IF OBJECT_ID('{tempTableName}') IS NOT NULL DROP TABLE {tempTableName}";
                        db.ExecuteQuery(dropSql);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Failed to cleanup temp table {tempTableName}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Performs a bulk delete operation for a collection of records based on their key values.
        /// </summary>
        public bool BulkDelete(DBAccess db, IEnumerable<T> records) => Utils.RunSync((c) => BulkDeleteAsync(db, records, c));

        /// <summary>
        /// Performs a bulk delete operation for a collection of records based on their key values.
        /// </summary>
        public async Task<bool> BulkDeleteAsync(DBAccess db, IEnumerable<T> records, CancellationToken cancellationToken)
        {
            if (db.DbType != DataBaseType.SQLServer)
            {
                ErrorOccurred?.Invoke("Bulk delete failed", new Exception("Bulk delete is only supported for SQL Server databases."));
                return false;
            }

            // Validate that type supports delete operations (same validation as upsert)
            ValidateUpsertSupport();

            string tempTableName = null;
            try
            {
                // Determine which keys to use for matching (primary key takes precedence)
                var matchKeys = PrimaryKeys.Length > 0 ? PrimaryKeys : UniqueIndexes;

                // Create a minimal DataTable with only the key columns
                var dt = CreateKeyOnlyDataTable(matchKeys);
                var totalRecords = 0;

                // Populate DataTable with only key values from records
                foreach (var record in records)
                {
                    AddKeyOnlyRecordToDataTable(record, dt, matchKeys);
                    totalRecords++;
                }

                if (totalRecords == 0)
                {
                    ProgressChanged?.Invoke(100);
                    return true;
                }

                // Create global temp table with GUID name
                tempTableName = $"BulkDelete_{Guid.NewGuid():N}";

                // Create the temp table (never drop, it's a new GUID-named table)
                CreateTable(db, dt, tempTableName, false);

                // Bulk insert key data into temp table
                using (var bulkCopy = new SqlBulkCopy(db.ConnectionString, SqlBulkCopyOptions.Default)
                {
                    DestinationTableName = tempTableName,
                    BatchSize = BatchSize,
                    BulkCopyTimeout = BulkCopyTimeout
                })
                {
                    foreach (DataColumn column in dt.Columns)
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

                    bulkCopy.SqlRowsCopied += (sender, e) =>
                        ProgressChanged?.Invoke((float)e.RowsCopied / totalRecords * 50); // 50% progress for bulk insert

                    await bulkCopy.WriteToServerAsync(dt, cancellationToken);
                    bulkCopy.Close();
                }

                // Generate and execute delete SQL
                var deleteSql = GenerateDeleteSql(tempTableName, matchKeys);
                var result = db.ExecuteScript(deleteSql);

                if (!result.Success)
                    throw result.LastException ?? new Exception("Delete operation failed");

                ProgressChanged?.Invoke(100);
                dt.Clear();

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke("Bulk delete failed", ex);
                Log.Error("Bulk delete failed", ex);
                return false;
            }
            finally
            {
                // Clean up global temp table
                if (!string.IsNullOrEmpty(tempTableName))
                {
                    try
                    {
                        var dropSql = $"IF OBJECT_ID('{tempTableName}', 'U') IS NOT NULL DROP TABLE {tempTableName}";
                        db.ExecuteQuery(dropSql);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Failed to cleanup temp table {tempTableName}", ex);
                    }
                }
            }
        }

        // Protected Methods (sorted)
        /// <summary>
        /// Adds a record to the specified <see cref="DataTable"/> by mapping property values.
        /// </summary>
        protected virtual void AddRecordToDataTable(T record, DataTable dt)
        {
            var values = new List<object>();

            foreach(var prop in Properties)
            {
                var mappedColumn = ColumnMap[prop];
                if (!dt.Columns.Contains(mappedColumn))
                    continue;

                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                var propValue = prop.GetValue(record);

                if (propValue == null)
                    values.Add(DBNull.Value);
                else if (propType == typeof(DateTime) && (DateTime)propValue == DateTime.MinValue)
                    values.Add(DBNull.Value);
                else if (propType.IsEnum)
                    values.Add((int)propValue);
                else
                    values.Add(propValue);

                // Handle dynamic string length adjustment only if MaxLength is not explicitly set
                if (propType == typeof(string))
                {
                    var columnAttr = prop.GetCustomAttribute<DBColumnAttribute>();
                    if (columnAttr?.MaxLength == null)
                    {
                        var column = dt.Columns[mappedColumn];
                        column.MaxLength = Math.Max(column.MaxLength, (propValue?.ToString() ?? "").Length);
                    }
                }
            }

            dt.Rows.Add(values.ToArray());
        }

        /// <summary>
        /// Initializes the property mapping for the type <typeparamref name="T"/>.<br/>
        /// Can be called to refresh mappings if needed.
        /// </summary>
        protected void InitializePropertyMapping()
        {
            Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttributes(typeof(DBColumnAttribute), true).Any())
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
        protected virtual DataTable CreateDataTable()
        {
            var dt = new DataTable($"type_{typeof(T).Name}");

            foreach (var prop in Properties)
            {
                var columnAttr = prop.GetCustomAttribute<DBColumnAttribute>();

                var columnType = GetDataTableColumnType(prop, columnAttr);
                var column = dt.Columns.Add(ColumnMap[prop], columnType);

                // Set MaxLength if specified in the attribute and it's a string column
                if (columnType == typeof(string) && columnAttr?.MaxLength != 0)
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

            return dt;
        }

        // Private Methods (sorted)
        /// <summary>
        /// Adds only the key column values from a record to the DataTable.
        /// </summary>
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

                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
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
                if (columnType == typeof(string) && columnAttr?.MaxLength != 0)
                    column.MaxLength = columnAttr.MaxLength;

                column.AllowDBNull = false; // Key columns shouldn't be null
            }

            return dt;
        }

        /// <summary>
        /// Creates the destination table in the database based on the provided <see cref="DataTable"/> schema.
        /// </summary>
        private void CreateTable(DBAccess db, DataTable dt, string tableName, bool dropTable)
        {
            var sqlScript = GenerateCreateTableSql(dt, tableName, dropTable);

            try
            {
                var result = db.ExecuteScript(sqlScript);
                if (!result.Success)
                    throw result.LastException;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Failed to create table {tableName}", ex);
                throw;
            }
        }

        private string GenerateCreateTableSql(DataTable dt, string tableName, bool dropTable)
        {
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
            tableName = tableName.Replace("[", "").Replace("]", "").Substring(tableName.LastIndexOf('.') + 1);

            if (PrimaryKeys.Length > 0)
                sql.AppendLine($"ALTER TABLE {tableName} ADD CONSTRAINT PK_{tableName} PRIMARY KEY ({string.Join(", ", PrimaryKeys)})");

            // Add unique constraints if defined
            foreach (var uniqueCol in UniqueIndexes)
                sql.AppendLine($"CREATE UNIQUE INDEX IX_{tableName}_{uniqueCol} ON {tableName} ({uniqueCol})");

            // Add indexes if defined
            foreach (var indexCol in Indexes)
                sql.AppendLine($"CREATE INDEX IX_{tableName}_{indexCol} ON {tableName} ({indexCol})");

            sql.AppendLine("END");
            sql.AppendLine("GO");
            var sqlScript = sql.ToString();
            return sqlScript;
        }

        /// <summary>
        /// Gets the CLR type that corresponds to the specified DbType.
        /// </summary>
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
        private Type GetColumnType(Type propertyType)
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

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
        private Type GetDataTableColumnType(PropertyInfo prop, DBColumnAttribute columnAttr)
        {
            if (columnAttr?.DbType != null)
                return GetClrTypeFromDbType(columnAttr.DbType.Value);

            return GetColumnType(prop.PropertyType);
        }

        /// <summary>
        /// Gets the SQL data type for the specified <see cref="DataColumn"/>.
        /// </summary>
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
            var columnType = Nullable.GetUnderlyingType(column.DataType) ?? column.DataType;

            if (columnType == typeof(string))
            {
                // Check if MaxLength is explicitly set in the attribute first
                var explicitMaxLength = columnAttr?.MaxLength;

                var maxLength = explicitMaxLength ?? column.MaxLength;
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
        /// Generates the SQL script for performing the bulk delete operation using the staging table.
        /// </summary>
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
        /// Generates the SQL script for performing the upsert operation using the staging table.
        /// </summary>
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
        /// Validates that the entity type supports upsert operations by checking for required keys or unique indexes.
        /// </summary>
        private void ValidateUpsertSupport()
        {
            // Check for non-identity primary key
            var hasValidPrimaryKey = false;
            if (PrimaryKeys.Length > 0)
            {
                // Check if any primary key column is an identity column
                var identityPkColumns = Properties
                    .Where(p => PrimaryKeys.Contains(ColumnMap[p]))
                    .Where(p => p.GetCustomAttribute<DBColumnAttribute>()?.IsIdentity == true)
                    .Select(p => ColumnMap[p])
                    .ToArray();

                hasValidPrimaryKey = identityPkColumns.Length == 0;
            }

            // Check for unique indexes
            var hasUniqueIndex = UniqueIndexes.Length > 0;

            if (!hasValidPrimaryKey && !hasUniqueIndex)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(T).Name} must have either non-identity primary key(s) or unique index(es) for upsert operations. " +
                    "Ensure properties are decorated with appropriate DBColumnAttribute settings.");
            }
        }
    }
}