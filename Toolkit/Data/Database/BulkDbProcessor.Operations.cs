using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static ByteForge.Toolkit.DBAccess;

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
        public async Task<bool> BulkInsertAsync(DBAccess db, IEnumerable<T> records, CancellationToken cancellationToken)
        {
            OnStarted($"Bulk insert started for {typeof(T).Name} into {DestinationTableName}.");
            OnDebug($"Starting bulk insert of {typeof(T).Name} records into {DestinationTableName}. Number of records: {records?.Count() ?? 0}");
            if (db.DbType != DataBaseType.SQLServer)
            {
                OnWarning("Bulk insert aborted: operation only supported for SQL Server databases.");
                OnError("Batch insert failed", new InvalidOperationException("Bulk insert is only supported for SQL Server databases."));
                OnFinished(true, "Bulk insert aborted.");
                return false;
            }

            try
            {
                var dt = CreateDataTable();
                var totalRecords = 0;

                OnDebug("Populating DataTable with records...");
                foreach (var record in records)
                {
                    AddRecordToDataTable(record, dt);
                    totalRecords++;
                    if (totalRecords % 1000 == 0)
                        OnDebug($"Prepared record {totalRecords} for bulk insert.");
                }
                OnDebug($"Prepared all {totalRecords} records for bulk insert.");

                if (totalRecords == 0)
                {
                    OnDebug("No records to insert.");
                    OnProgress(100);
                    OnFinished(false, "No records to insert.");
                    return true;
                }

                if (CreateDestinationTable)
                    CreateTable(db, dt, DestinationTableName, DropDestinationTableIfExists);

                using (var bulkCopy = new SqlBulkCopy(db.ConnectionString, SqlBulkCopyOptions.Default)
                {
                    DestinationTableName = DestinationTableName,
                    NotifyAfter = BatchSize,
                    BatchSize = BatchSize,
                    BulkCopyTimeout = BulkCopyTimeout,
                })
                {
                    foreach (DataColumn column in dt.Columns)
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

                    bulkCopy.SqlRowsCopied += (sender, e) =>
                        OnProgress((float)e.RowsCopied / totalRecords * 100);

                    try
                    {
                        OnDebug("Starting bulk copy to database...");
                        await bulkCopy.WriteToServerAsync(dt, cancellationToken);
                        OnDebug("Bulk copy completed successfully.");
                    }
                    finally
                    {
                        bulkCopy.Close();
                    }
                }

                OnProgress(100);
                dt.Clear();
                OnFinished(false, $"Bulk insert completed. Records inserted: {totalRecords}.");
                return true;
            }
            catch (Exception ex)
            {
                OnError("Batch insert failed", ex);
                OnFinished(true, "Bulk insert failed.");
                LastException = ex;
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
            OnStarted($"Bulk upsert started for {typeof(T).Name} into {DestinationTableName}.");
            OnDebug($"Starting bulk upsert of {typeof(T).Name} records into {DestinationTableName}. Number of records: {records?.Count() ?? 0}");
            if (db.DbType != DataBaseType.SQLServer)
            {
                OnWarning("Bulk upsert aborted: operation only supported for SQL Server databases.");
                OnError("Bulk upsert failed", new Exception("Bulk upsert is only supported for SQL Server databases."));
                OnFinished(true, "Bulk upsert aborted.");
                return false;
            }

            // Validate that type supports upsert operations
            try
            {
                ValidateUpsertSupport();
            }
            catch (Exception ex)
            {
                OnWarning("Bulk upsert aborted: entity type does not support upsert (missing suitable keys).");
                OnError("Bulk upsert failed", ex);
                OnFinished(true, "Bulk upsert aborted due to configuration.");
                LastException = ex;
                throw;
            }

            string tempTableName = null;
            try
            {
                var dt = CreateDataTable();
                var totalRecords = 0;

                OnDebug("Populating DataTable with records for upsert...");
                foreach (var record in records)
                {
                    AddRecordToDataTable(record, dt);
                    totalRecords++;
                    if (totalRecords % 1000 == 0)
                        OnDebug($"Prepared record {totalRecords} for bulk upsert.");
                }
                OnDebug($"Prepared all {totalRecords} records for bulk upsert.");

                if (totalRecords == 0)
                {
                    OnDebug("No records to upsert.");
                    OnProgress(100);
                    OnFinished(false, "No records to upsert.");
                    return true;
                }

                tempTableName = $"BulkUpsert_{Guid.NewGuid():N}";
                OnDebug($"Creating temp staging table {tempTableName}...");
                CreateTable(db, dt, tempTableName, false);
                OnDebug("Temp table created.");

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
                        OnProgress((float)e.RowsCopied / totalRecords * 50); // 50% progress for bulk insert

                    OnDebug("Starting bulk copy into temp table for upsert...");
                    await bulkCopy.WriteToServerAsync(dt, cancellationToken);
                    OnDebug("Bulk copy to temp table completed.");
                    bulkCopy.Close();
                }

                OnDebug("Generating upsert SQL script...");
                var createTableSql = GenerateCreateTableSql(dt, DestinationTableName, dropTable: false);
                var upsertSql = GenerateUpsertSql(tempTableName, dt);
                OnDebug("Executing upsert SQL script...");
                var result = db.ExecuteScript($"{createTableSql}{Environment.NewLine}{upsertSql}");

                if (!result.Success)
                    throw result.LastException ?? new Exception("Upsert operation failed");

                OnDebug("Upsert script executed successfully.");
                OnProgress(100);
                dt.Clear();
                OnFinished(false, $"Bulk upsert completed. Records processed: {totalRecords}.");
                return true;
            }
            catch (Exception ex)
            {
                OnError("Bulk upsert failed", ex);
                OnFinished(true, "Bulk upsert failed.");
                LastException = ex;
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempTableName))
                {
                    try
                    {
                        OnDebug($"Dropping temp table {tempTableName}...");
                        var dropSql = $"IF OBJECT_ID('{tempTableName}') IS NOT NULL DROP TABLE {tempTableName}";
                        db.ExecuteQuery(dropSql);
                        OnDebug("Temp table dropped.");
                    }
                    catch (Exception ex)
                    {
                        OnWarning($"Failed to cleanup temp table {tempTableName}: {ex.Message}");
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
            OnStarted($"Bulk delete started for {typeof(T).Name} from {DestinationTableName}.");
            OnDebug($"Starting bulk delete of {typeof(T).Name} records from {DestinationTableName}. Number of records: {records?.Count() ?? 0}");
            if (db.DbType != DataBaseType.SQLServer)
            {
                OnWarning("Bulk delete aborted: operation only supported for SQL Server databases.");
                OnError("Bulk delete failed", new Exception("Bulk delete is only supported for SQL Server databases."));
                OnFinished(true, "Bulk delete aborted.");
                return false;
            }

            // Validate that type supports delete operations (same validation as upsert)
            try
            {
                ValidateUpsertSupport();
            }
            catch (Exception ex)
            {
                OnWarning("Bulk delete aborted: entity type does not support delete (missing suitable keys).");
                OnError("Bulk delete failed", ex);
                OnFinished(true, "Bulk delete aborted due to configuration.");
                LastException = ex;
                throw;
            }

            string tempTableName = null;
            try
            {
                var matchKeys = PrimaryKeys.Length > 0 ? PrimaryKeys : UniqueIndexes;

                var dt = CreateKeyOnlyDataTable(matchKeys);
                var totalRecords = 0;

                OnDebug("Populating key-only DataTable with records for delete...");
                foreach (var record in records)
                {
                    AddKeyOnlyRecordToDataTable(record, dt, matchKeys);
                    totalRecords++;
                    if (totalRecords % 1000 == 0)
                        OnDebug($"Prepared key set {totalRecords} for bulk delete.");
                }
                OnDebug($"Prepared all {totalRecords} key sets for bulk delete.");

                if (totalRecords == 0)
                {
                    OnDebug("No records to delete.");
                    OnProgress(100);
                    OnFinished(false, "No records to delete.");
                    return true;
                }

                tempTableName = $"BulkDelete_{Guid.NewGuid():N}";
                OnDebug($"Creating temp key staging table {tempTableName}...");
                CreateTable(db, dt, tempTableName, false);
                OnDebug("Temp key table created.");

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
                        OnProgress((float)e.RowsCopied / totalRecords * 50); // 50% progress for bulk insert

                    OnDebug("Starting bulk copy of key data into temp table for delete...");
                    await bulkCopy.WriteToServerAsync(dt, cancellationToken);
                    OnDebug("Bulk copy of key data completed.");
                    bulkCopy.Close();
                }

                OnDebug("Generating delete SQL script...");
                var deleteSql = GenerateDeleteSql(tempTableName, matchKeys);
                OnDebug("Executing delete script...");
                var result = db.ExecuteScript(deleteSql);

                if (!result.Success)
                    throw result.LastException ?? new Exception("Delete operation failed");

                OnDebug("Delete script executed successfully.");
                OnProgress(100);
                dt.Clear();
                OnFinished(false, $"Bulk delete completed. Records deleted: {totalRecords}.");
                return true;
            }
            catch (Exception ex)
            {
                OnError("Bulk delete failed", ex);
                OnFinished(true, "Bulk delete failed.");
                LastException = ex;
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempTableName))
                {
                    OnDebug($"Dropping temp table {tempTableName}...");
                    var dropSql = $"IF OBJECT_ID('{tempTableName}', 'U') IS NOT NULL DROP TABLE {tempTableName}";
                    if (!db.ExecuteQuery(dropSql))
                        OnWarning($"Failed to drop temp table {tempTableName}");
                    else
                        OnDebug("Temp table dropped.");
                }
            }
        }

        /// <summary>
        /// Validates that the entity type supports upsert operations by checking for required keys or unique indexes.
        /// </summary>
        private void ValidateUpsertSupport()
        {
            // First let's deal with the obvious case
            if (PrimaryKeys.Length == 0 && UniqueIndexes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(T).Name} must have either primary key(s) or unique index(es) for upsert operations. " +
                    "Ensure properties are decorated with appropriate DBColumnAttribute settings.");
            }

            // Now check for the more complex case where PKs exist but are all identity columns
            var hasNonIdentityPk = false;
            if (PrimaryKeys.Length > 0)
            {
                var identityPkColumns = Properties
                    .Where(p => PrimaryKeys.Contains(ColumnMap[p]))
                    .Where(p => p.GetCustomAttribute<DBColumnAttribute>()?.IsIdentity == true)
                    .Select(p => ColumnMap[p])
                    .ToArray();

                hasNonIdentityPk = identityPkColumns.Length == 0;
            }

            // If no non-identity PKs, check for unique indexes
            var hasUniqueIndex = UniqueIndexes.Length > 0;

            // If neither, throw
            if (!hasNonIdentityPk && !hasUniqueIndex)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(T).Name} must have either non-identity primary key(s) or unique index(es) for upsert operations. " +
                    "Ensure properties are decorated with appropriate DBColumnAttribute settings.");
            }
        }
    }
}