using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static ByteForge.Toolkit.DBAccess;
using System.Reflection;

namespace ByteForge.Toolkit
{
    public partial class BulkDbProcessor<T>
    {
        // Public Methods (sorted)
        /// <summary>
        /// Inserts multiple records into the database in a single operation.
        /// </summary>
        /// <param name="db">The database connection to use.</param>
        /// <param name="records">The collection of records to insert.</param>
        /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This is a synchronous wrapper around <see cref="BulkInsertAsync"/>.
        /// </remarks>
        public bool BulkInsert(DBAccess db, IEnumerable<T> records) => Utils.RunSync((c) => BulkInsertAsync(db, records, c));

        /// <summary>
        /// Inserts a collection of records into the database in a single bulk operation.
        /// </summary>
        /// <param name="db">The database connection to use.</param>
        /// <param name="records">The collection of records to insert.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with a result of <see langword="true"/> if successful; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method creates a temporary table if needed, then uses SqlBulkCopy to efficiently
        /// insert the records in batches. Progress is reported through the <see cref="ProgressChanged"/> event.
        /// </remarks>
        public async Task<bool> BulkInsertAsync(DBAccess db, IEnumerable<T> records, CancellationToken cancellationToken)
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
        /// <param name="db">The database connection to use.</param>
        /// <param name="records">The collection of records to upsert.</param>
        /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This is a synchronous wrapper around <see cref="BulkUpsertAsync"/>.
        /// </remarks>
        public bool BulkUpsert(DBAccess db, IEnumerable<T> records) => Utils.RunSync((c) => BulkUpsertAsync(db, records, c));

        /// <summary>
        /// Performs a bulk upsert operation (update existing records, insert new ones) for a collection of records.
        /// </summary>
        /// <param name="db">The database connection to use.</param>
        /// <param name="records">The collection of records to upsert.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with a result of <see langword="true"/> if successful; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the entity type does not have appropriate keys for upsert operations.</exception>
        /// <remarks>
        /// This method creates a temporary staging table, loads the records into it, and then executes
        /// a SQL script to perform the upsert operation. Progress is reported through the <see cref="ProgressChanged"/> event.
        /// </remarks>
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
        /// <param name="db">The database connection to use.</param>
        /// <param name="records">The collection of records containing key values to delete.</param>
        /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This is a synchronous wrapper around <see cref="BulkDeleteAsync"/>.
        /// </remarks>
        public bool BulkDelete(DBAccess db, IEnumerable<T> records) => Utils.RunSync((c) => BulkDeleteAsync(db, records, c));

        /// <summary>
        /// Performs a bulk delete operation for a collection of records based on their key values.
        /// </summary>
        /// <param name="db">The database connection to use.</param>
        /// <param name="records">The collection of records containing key values to delete.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation, with a result of <see langword="true"/> if successful; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the entity type does not have appropriate keys for delete operations.</exception>
        /// <remarks>
        /// This method creates a temporary staging table with only the key columns needed for deletion,
        /// loads the key values into it, and then executes a SQL script to perform the delete operation.
        /// Progress is reported through the <see cref="ProgressChanged"/> event.
        /// </remarks>
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

        /// <summary>
        /// Validates that the entity type supports upsert operations by checking for required keys or unique indexes.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the type doesn't have either non-identity primary key(s) or unique index(es) for upsert operations.
        /// </exception>
        /// <remarks>
        /// For upsert operations to work properly, the entity type must have either:
        /// 1. Non-identity primary key(s) that can be used to match existing records, or
        /// 2. Unique index(es) that can serve as alternative matching criteria.
        /// </remarks>
        private void ValidateUpsertSupport()
        {
            // Check for non-identity primary key
            var hasNonIdentityPk = false;
            if (PrimaryKeys.Length > 0)
            {
                // Check if any primary key column is an identity column
                var identityPkColumns = Properties
                    .Where(p => PrimaryKeys.Contains(ColumnMap[p]))
                    .Where(p => p.GetCustomAttribute<DBColumnAttribute>()?.IsIdentity == true)
                    .Select(p => ColumnMap[p])
                    .ToArray();

                hasNonIdentityPk = identityPkColumns.Length == 0;
            }

            // Check for unique indexes
            var hasUniqueIndex = UniqueIndexes.Length > 0;

            if (!hasNonIdentityPk && !hasUniqueIndex)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(T).Name} must have either non-identity primary key(s) or unique index(es) for upsert operations. " +
                    "Ensure properties are decorated with appropriate DBColumnAttribute settings.");
            }
        }
    }
}