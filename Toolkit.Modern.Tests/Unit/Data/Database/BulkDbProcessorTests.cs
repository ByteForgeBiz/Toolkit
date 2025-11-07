using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;
using AwesomeAssertions;
using ByteForge.Toolkit.Data;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace ByteForge.Toolkit.Tests.Unit.Data.Database
{
    /// <summary>
    /// Unit tests for the <see cref="BulkDbProcessor{T}"/> class.
    /// </summary>
    /// <remarks>
    /// These tests focus on bulk database operations including insert, upsert, and delete operations.
    /// Tests use the BulkTestEntity model and TestUnitDB SQL Server database for integration scenarios.
    /// </remarks>
    [TestClass]
    public class BulkDbProcessorTests
    {
        #region Test Setup and Cleanup

        private DBAccess _dbAccess;
        private const string BulkTestTableName = "BulkTestTempTable";

#pragma warning disable IDE0060 // Called by test framework

        /// <summary>
        /// Verifies that the test database is properly configured before running tests.
        /// </summary>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Verify test database is accessible
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            DatabaseTestHelper.AssertTestDatabaseSetup(dbAccess);
        }

#pragma warning restore IDE0060


        /// <summary>
        /// Sets up a fresh DBAccess instance for each test.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            _dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            CleanupBulkTestTable();
        }

        /// <summary>
        /// Cleans up test data after each test.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            CleanupBulkTestTable();
        }

        /// <summary>
        /// Cleans up any test data that might have been created during testing.
        /// </summary>
        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
                var cleanup = $"IF OBJECT_ID('{BulkTestTableName}', 'U') IS NOT NULL DROP TABLE {BulkTestTableName}";
                dbAccess.ExecuteQuery(cleanup);
            }
            catch (Exception)
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }

        #endregion

        #region Constructor and Initialization Tests

        /// <summary>
        /// Tests that the BulkDbProcessor constructor with table name works correctly.
        /// </summary>
        /// <remarks>
        /// This test validates the core initialization of BulkDbProcessor for high-performance bulk operations.
        /// It ensures proper detection of primary keys and unique indexes from entity attributes,
        /// which is essential for generating efficient bulk SQL operations and maintaining data integrity.
        /// </remarks>
        [TestMethod]
        public void Constructor_WithValidTableName_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);

            // Assert
            processor.Should().NotBeNull();
            processor.DestinationTableName.Should().Be(BulkTestTableName);
            processor.CreateDestinationTable.Should().BeTrue();
            processor.DropDestinationTableIfExists.Should().BeTrue();
            processor.PrimaryKeys.Should().NotBeNull().And.Contain("Id");
            processor.UniqueIndexes.Should().NotBeNull().And.Contain("Code");
        }

        /// <summary>
        /// Tests that the BulkDbProcessor constructor throws for null or empty table name.
        /// </summary>
        /// <remarks>
        /// This test ensures robust parameter validation for BulkDbProcessor construction.
        /// Proper validation prevents runtime errors in bulk operations and provides clear
        /// error messages for developers, which is crucial for debugging complex data processing scenarios.
        /// </remarks>
        [TestMethod]
        public void Constructor_WithNullOrEmptyTableName_ShouldThrowArgumentException()
        {
            // Act & Assert - Null table name
            var nullAction = new Action(() => new BulkDbProcessor<BulkTestEntity>(null));
            nullAction.Should().Throw<ArgumentException>()
                     .WithParameterName("destinationTableName");

            // Act & Assert - Empty table name
            var emptyAction = new Action(() => new BulkDbProcessor<BulkTestEntity>(""));
            emptyAction.Should().Throw<ArgumentException>()
                      .WithParameterName("destinationTableName");

            // Act & Assert - Whitespace table name
            var whitespaceAction = new Action(() => new BulkDbProcessor<BulkTestEntity>("   "));
            whitespaceAction.Should().Throw<ArgumentException>()
                           .WithParameterName("destinationTableName");
        }

        /// <summary>
        /// Tests that property mapping is initialized correctly for BulkTestEntity.
        /// </summary>
        /// <remarks>
        /// This test validates the attribute-based mapping system that drives bulk operation efficiency.
        /// Correct property mapping is essential for generating optimized SQL operations,
        /// implementing upsert logic, and ensuring data integrity constraints are properly handled.
        /// </remarks>
        [TestMethod]
        public void Initialization_PropertyMapping_ShouldBeConfiguredCorrectly()
        {
            // Arrange & Act
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);

            // Assert
            processor.PrimaryKeys.Should().NotBeNull().And.HaveCount(1);
            processor.PrimaryKeys[0].Should().Be("Id");

            processor.UniqueIndexes.Should().NotBeNull().And.HaveCount(1);
            processor.UniqueIndexes[0].Should().Be("Code");

            // All other columns should not be in indexes or primary keys
            processor.Indexes.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Bulk Insert Tests

        /// <summary>
        /// Tests that bulk insert operation works correctly with a single record.
        /// </summary>
        /// <remarks>
        /// This test validates the fundamental bulk insert functionality with minimal data.
        /// Single record insertion tests the core table creation, SQL generation, and progress reporting
        /// mechanisms that form the foundation for larger bulk operations.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_SingleRecord_ShouldInsertSuccessfully()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            var entity = BulkTestEntity.Create("SINGLE001", "Single Test Entity");
            var progressReports = new List<float>();
            
            processor.Progress += (s, e) => progressReports.Add((float)e.Progress);

            // Act
            var result = processor.BulkInsert(_dbAccess, [entity]);

            // Assert
            result.Should().BeTrue();
            progressReports.Should().NotBeEmpty();
            progressReports.Last().Should().Be(100f);

            // Verify data was inserted
            var insertedEntity = _dbAccess.GetRecord<BulkTestEntity>($"SELECT * FROM {BulkTestTableName} WHERE Code = @code", entity.Code);
            insertedEntity.Should().NotBeNull();
            insertedEntity.Code.Should().Be(entity.Code);
            insertedEntity.Name.Should().Be(entity.Name);
        }

        /// <summary>
        /// Tests that bulk insert operation works correctly with multiple records.
        /// </summary>
        /// <remarks>
        /// This test validates high-performance bulk insertion capabilities with significant data volumes.
        /// Multi-record operations test batch processing efficiency, transaction management,
        /// and progress reporting for real-world data loading scenarios.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_MultipleRecords_ShouldInsertAllSuccessfully()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            var entities = BulkTestEntity.CreateBatch(50, "MULTI");
            var progressReports = new List<float>();
            var errorMessages = new List<string>();

            processor.Progress += (s, e) => progressReports.Add((float)e.Progress);
            processor.Error += (s, e) => errorMessages.Add(e.Message);

            // Act
            var result = processor.BulkInsert(_dbAccess, entities);

            // Assert
            result.Should().BeTrue();
            errorMessages.Should().BeEmpty();
            progressReports.Should().NotBeEmpty();
            progressReports.Last().Should().Be(100f);

            // Verify all data was inserted
            var insertedCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName}");
            insertedCount.Should().Be(50);

            // Verify a sample of the data
            var sampleEntity = _dbAccess.GetRecord<BulkTestEntity>($"SELECT * FROM {BulkTestTableName} WHERE Code = @code", "MULTI0001");
            sampleEntity.Should().NotBeNull();
            sampleEntity.Name.Should().Be("MULTI Test Entity 1");
        }

        /// <summary>
        /// Tests that bulk insert with empty collection completes successfully.
        /// </summary>
        /// <remarks>
        /// This edge case test ensures robust handling of empty data collections in bulk operations.
        /// Empty collection handling is important for dynamic data processing scenarios where
        /// data availability may vary, preventing application errors in production environments.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_EmptyCollection_ShouldCompleteSuccessfully()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            var progressReports = new List<float>();

            processor.Progress += (s, e) => progressReports.Add((float)e.Progress);

            // Act
            var result = processor.BulkInsert(_dbAccess, []);

            // Assert
            result.Should().BeTrue();
            progressReports.Should().Contain(100f);

            // Verify no data was inserted but table was created
            var insertedCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName}");
            insertedCount.Should().Be(0);
        }

        /// <summary>
        /// Tests that bulk insert creates the destination table when configured to do so.
        /// </summary>
        /// <remarks>
        /// This test validates dynamic table creation functionality for bulk operations.
        /// Automatic table creation is essential for ETL processes, data migration scenarios,
        /// and applications that need to create destination tables based on source data structures.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_WithCreateDestinationTable_ShouldCreateTable()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName)
            {
                CreateDestinationTable = true,
                DropDestinationTableIfExists = true
            };
            var entity = BulkTestEntity.Create("CREATE001", "Create Test Entity");

            // Act
            var result = processor.BulkInsert(_dbAccess, [entity]);

            // Assert
            result.Should().BeTrue();

            // Verify table exists and has correct structure
            var tableExists = _dbAccess.GetValue<int>("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName", BulkTestTableName);
            tableExists.Should().Be(1);

            // Verify primary key constraint exists
            var pkExists = _dbAccess.GetValue<int>(@"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                WHERE TABLE_NAME = @tableName AND CONSTRAINT_NAME LIKE 'PK_%'", BulkTestTableName);
            pkExists.Should().BeGreaterThan(0);
        }

        #endregion

        #region Bulk Upsert Tests

        /// <summary>
        /// Tests that bulk upsert operation inserts new records correctly.
        /// </summary>
        /// <remarks>
        /// This test validates upsert functionality for new record insertion scenarios.
        /// Upsert operations are crucial for data synchronization, incremental loading,
        /// and maintaining data consistency in systems with multiple data sources.
        /// </remarks>
        [TestMethod]
        public void BulkUpsert_NewRecords_ShouldInsertSuccessfully()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            var entities = BulkTestEntity.CreateBatch(25, "UPSERT");
            var progressReports = new List<float>();

            processor.Progress += (s, e) => progressReports.Add((float)e.Progress);

            // Act
            var result = processor.BulkUpsert(_dbAccess, entities);

            // Assert
            result.Should().BeTrue();
            progressReports.Should().NotBeEmpty();
            progressReports.Last().Should().Be(100f);

            // Verify all data was inserted
            var insertedCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName}");
            insertedCount.Should().Be(25);
        }

        /// <summary>
        /// Tests that bulk upsert operation updates existing records correctly.
        /// </summary>
        /// <remarks>
        /// This test validates upsert functionality for existing record modification scenarios.
        /// Update capabilities in upsert operations are essential for maintaining current data
        /// in synchronization processes and handling duplicate key scenarios gracefully.
        /// </remarks>
        [TestMethod]
        public void BulkUpsert_ExistingRecords_ShouldUpdateSuccessfully()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            
            // First, insert some initial data
            var initialEntities = BulkTestEntity.CreateBatch(10, "UPDT");
            processor.BulkInsert(_dbAccess, initialEntities);

            // Create updated versions of the same entities (same ID and Code, different values)
            var updatedEntities = initialEntities.Select(e => 
                e.CreateUpdatedCopy($"Updated {e.Name}", e.Value + 100m)).ToArray();

            // Act
            var result = processor.BulkUpsert(_dbAccess, updatedEntities);

            // Assert
            result.Should().BeTrue();

            // Verify record count is still the same (updates, not inserts)
            var recordCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName}");
            recordCount.Should().Be(10);

            // Verify that records were actually updated
            var sampleUpdated = _dbAccess.GetRecord<BulkTestEntity>($"SELECT * FROM {BulkTestTableName} WHERE Code = @code", "UPDT0001");
            sampleUpdated.Should().NotBeNull();
            sampleUpdated.Name.Should().StartWith("Updated");
            sampleUpdated.Value.Should().BeGreaterThan(100m);
        }

        /// <summary>
        /// Tests that bulk upsert operation handles mixed insert and update scenarios.
        /// </summary>
        /// <remarks>
        /// This test validates the sophisticated logic required for mixed upsert operations.
        /// Mixed scenarios are common in real-world data synchronization where some records exist
        /// and others are new, requiring intelligent decision-making for optimal performance.
        /// </remarks>
        [TestMethod]
        public void BulkUpsert_MixedInsertAndUpdate_ShouldHandleBothCorrectly()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            
            // Insert initial data (5 records)
            var existingEntities = BulkTestEntity.CreateBatch(5, "MIX");
            processor.BulkInsert(_dbAccess, existingEntities);

            // Create a mix: 3 updates + 5 new inserts
            var mixedEntities = new List<BulkTestEntity>();
            
            // Add 3 updated versions of existing records
            for (var i = 0; i < 3; i++)
            {
                mixedEntities.Add(existingEntities[i].CreateUpdatedCopy($"Updated {existingEntities[i].Name}", 999m));
            }
            
            // Add 5 new records
            mixedEntities.AddRange(BulkTestEntity.CreateBatch(5, "NEW"));

            // Act
            var result = processor.BulkUpsert(_dbAccess, mixedEntities);

            // Assert
            result.Should().BeTrue();

            // Verify total record count (5 original + 5 new = 10)
            var totalCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName}");
            totalCount.Should().Be(10);

            // Verify updates occurred
            var updatedCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName} WHERE Value = 999");
            updatedCount.Should().Be(3);

            // Verify new inserts occurred
            var newCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName} WHERE Code LIKE 'NEW%'");
            newCount.Should().Be(5);
        }

        #endregion

        #region Bulk Delete Tests

        /// <summary>
        /// Tests that bulk delete operation removes records correctly.
        /// </summary>
        /// <remarks>
        /// This test validates bulk deletion functionality for data cleanup and maintenance operations.
        /// Bulk delete operations are essential for data lifecycle management, archival processes,
        /// and maintaining database performance by removing obsolete records efficiently.
        /// </remarks>
        [TestMethod]
        public void BulkDelete_ExistingRecords_ShouldDeleteSuccessfully()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            var errorMessages = new List<string>();
            var errorExceptions = new List<Exception>();

            processor.Error += (s, e) =>
            {
                errorMessages.Add(e.Message);
                errorExceptions.Add(e.Exception);
            };
            
            // Debug: Check what keys the processor found
            Console.WriteLine($"Primary keys: [{string.Join(", ", processor.PrimaryKeys)}]");
            Console.WriteLine($"Unique indexes: [{string.Join(", ", processor.UniqueIndexes)}]");
            
            // Insert test data
            var entities = BulkTestEntity.CreateBatch(20, "DEL");
            var insertResult = processor.BulkInsert(_dbAccess, entities);
            insertResult.Should().BeTrue("Insert should succeed before testing delete");
            
            if (errorMessages.Any())
            {
                Assert.Fail($"Insert operation had errors: {string.Join(", ", errorMessages)}");
            }

            // Select 10 records to delete
            var toDelete = entities.Take(10).ToArray();

            // Act
            var result = processor.BulkDelete(_dbAccess, toDelete);

            // Assert
            if (!result && errorMessages.Any())
            {
                Assert.Fail($"BulkDelete failed with errors: {string.Join(", ", errorMessages)}. Exceptions: {string.Join(", ", errorExceptions.Select(e => e.Message))}");
            }
            if (!result && !errorMessages.Any())
            {
                Assert.Fail($"BulkDelete failed but no error messages were captured. Primary keys: [{string.Join(", ", processor.PrimaryKeys)}], Unique indexes: [{string.Join(", ", processor.UniqueIndexes)}]");
            }
            result.Should().BeTrue();

            // Verify records were deleted
            var remainingCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName}");
            remainingCount.Should().Be(10);

            // Verify specific records were deleted
            var deletedCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName} WHERE Code IN ('DEL0001', 'DEL0002', 'DEL0003')");
            deletedCount.Should().Be(0);

            // Verify remaining records still exist
            var remainingSpecific = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName} WHERE Code IN ('DEL0015', 'DEL0020')");
            remainingSpecific.Should().Be(2);
        }

        /// <summary>
        /// Tests that bulk delete with non-existent records completes without error.
        /// </summary>
        /// <remarks>
        /// This test ensures graceful handling of deletion attempts for non-existent records.
        /// Robust error handling for missing records is crucial in distributed systems where
        /// data may be deleted by other processes or in multi-step data processing scenarios.
        /// </remarks>
        [TestMethod]
        public void BulkDelete_NonExistentRecords_ShouldCompleteWithoutError()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            
            // Create table with some data
            var existingEntities = BulkTestEntity.CreateBatch(5, "EXIST");
            processor.BulkInsert(_dbAccess, existingEntities);

            // Create entities that don't exist in the database
            var nonExistentEntities = BulkTestEntity.CreateBatch(3, "NOEXIST");

            // Act
            var result = processor.BulkDelete(_dbAccess, nonExistentEntities);

            // Assert
            result.Should().BeTrue();

            // Verify original data is still there
            var remainingCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName}");
            remainingCount.Should().Be(5);
        }

        /// <summary>
        /// Tests that bulk delete with empty collection completes successfully.
        /// </summary>
        /// <remarks>
        /// This edge case test validates empty collection handling in bulk delete operations.
        /// Empty collection scenarios are common in conditional data processing and filtered
        /// operations, requiring graceful handling to prevent application failures.
        /// </remarks>
        [TestMethod]
        public void BulkDelete_EmptyCollection_ShouldCompleteSuccessfully()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            
            // Create table with some data
            var entities = BulkTestEntity.CreateBatch(5, "KEEP");
            processor.BulkInsert(_dbAccess, entities);

            // Act
            var result = processor.BulkDelete(_dbAccess, []);

            // Assert
            result.Should().BeTrue();

            // Verify no data was deleted
            var remainingCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName}");
            remainingCount.Should().Be(5);
        }

        #endregion

        #region Async Operation Tests

        /// <summary>
        /// Tests that async bulk insert operation works correctly.
        /// </summary>
        /// <remarks>
        /// This test validates asynchronous bulk operations with cancellation token support.
        /// Async operations are essential for responsive applications and enable proper
        /// cancellation handling for long-running bulk data processing operations.
        /// </remarks>
        [TestMethod]
        public async Task BulkInsertAsync_WithCancellationToken_ShouldInsertSuccessfully()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            var entities = BulkTestEntity.CreateBatch(25, "ASYNC");
            var cancellationToken = new CancellationTokenSource().Token;

            // Act
            var result = await processor.BulkInsertAsync(_dbAccess, entities, cancellationToken);

            // Assert
            result.Should().BeTrue();

            // Verify data was inserted
            var insertedCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName}");
            insertedCount.Should().Be(25);
        }

        /// <summary>
        /// Tests that async bulk upsert operation works correctly.
        /// </summary>
        /// <remarks>
        /// This test validates asynchronous upsert operations for non-blocking data synchronization.
        /// Async upserts are crucial for maintaining application responsiveness during large-scale
        /// data synchronization and ETL processes in production environments.
        /// </remarks>
        [TestMethod]
        public async Task BulkUpsertAsync_WithCancellationToken_ShouldUpsertSuccessfully()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            var entities = BulkTestEntity.CreateBatch(15, "ASYNCUP");
            var cancellationToken = new CancellationTokenSource().Token;

            // Act
            var result = await processor.BulkUpsertAsync(_dbAccess, entities, cancellationToken);

            // Assert
            result.Should().BeTrue();

            // Verify data was inserted
            var insertedCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName}");
            insertedCount.Should().Be(15);
        }

        /// <summary>
        /// Tests that async bulk delete operation works correctly.
        /// </summary>
        /// <remarks>
        /// This test validates asynchronous bulk deletion with proper cancellation support.
        /// Async delete operations are important for data lifecycle management without blocking
        /// application threads during large-scale cleanup and archival operations.
        /// </remarks>
        [TestMethod]
        public async Task BulkDeleteAsync_WithCancellationToken_ShouldDeleteSuccessfully()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            var entities = BulkTestEntity.CreateBatch(20, "ASYNCDEL");
            
            // Insert data first
            processor.BulkInsert(_dbAccess, entities);
            
            var toDelete = entities.Take(10).ToArray();
            var cancellationToken = new CancellationTokenSource().Token;

            // Act
            var result = await processor.BulkDeleteAsync(_dbAccess, toDelete, cancellationToken);

            // Assert
            result.Should().BeTrue();

            // Verify records were deleted
            var remainingCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName}");
            remainingCount.Should().Be(10);
        }

        #endregion

        #region Error Handling Tests

        /// <summary>
        /// Tests that bulk operations fail gracefully with ODBC database type.
        /// </summary>
        /// <remarks>
        /// This test ensures proper error handling for unsupported database types in bulk operations.
        /// ODBC databases don't support SQL Server bulk copy operations, so graceful failure handling
        /// is essential for providing clear error messages and preventing application crashes.
        /// </remarks>
        [TestMethod]
        public void BulkOperations_WithODBCDatabase_ShouldFailGracefully()
        {
            // Arrange
            var odbcOptions = DatabaseTestHelper.CreateTestDatabaseOptions();
            odbcOptions.DatabaseType = DBAccess.DataBaseType.ODBC;
            var odbcDbAccess = new DBAccess(odbcOptions);

            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            var entities = BulkTestEntity.CreateBatch(5, "ODBC");
            var errorMessages = new List<string>();
            var errorExceptions = new List<Exception>();

            processor.Error += (s, e) =>
            {
                errorMessages.Add(e.Message);
                errorExceptions.Add(e.Exception);
            };

            // Act & Assert - Bulk Insert
            var insertResult = processor.BulkInsert(odbcDbAccess, entities);
            insertResult.Should().BeFalse();
            errorMessages.Should().Contain("Batch insert failed");
            errorExceptions.Should().NotBeEmpty();

            // Act & Assert - Bulk Upsert
            errorMessages.Clear();
            errorExceptions.Clear();
            var upsertResult = processor.BulkUpsert(odbcDbAccess, entities);
            upsertResult.Should().BeFalse();
            errorMessages.Should().Contain("Bulk upsert failed");

            // Act & Assert - Bulk Delete
            errorMessages.Clear();
            errorExceptions.Clear();
            var deleteResult = processor.BulkDelete(odbcDbAccess, entities);
            deleteResult.Should().BeFalse();
            errorMessages.Should().Contain("Bulk delete failed");
        }

        /// <summary>
        /// Tests that bulk operations handle database connection errors gracefully.
        /// </summary>
        /// <remarks>
        /// This test validates error handling for connectivity issues during bulk operations.
        /// Connection error handling is crucial for robust applications that must handle
        /// network issues, database outages, and configuration problems gracefully.
        /// </remarks>
        [TestMethod]
        public void BulkOperations_WithInvalidDatabase_ShouldHandleErrorsGracefully()
        {
            // Arrange
            var invalidOptions = DatabaseTestHelper.CreateInvalidDatabaseOptions();
            var invalidDbAccess = new DBAccess(invalidOptions);

            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            var entities = BulkTestEntity.CreateBatch(5, "ERROR");
            var errorMessages = new List<string>();

            processor.Error += (s, e) => errorMessages.Add(e.Message);

            // Act
            var result = processor.BulkInsert(invalidDbAccess, entities);

            // Assert
            result.Should().BeFalse();
            errorMessages.Should().NotBeEmpty();
        }

        #endregion

        #region Property Configuration Tests

        /// <summary>
        /// Tests that CreateDestinationTable property affects table creation behavior.
        /// </summary>
        /// <remarks>
        /// This test validates configurable table creation behavior for bulk operations.
        /// The ability to disable table creation is important for production environments where
        /// table structures are pre-defined and automatic creation could cause security or schema issues.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_WithCreateDestinationTableFalse_ShouldRequireExistingTable()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName)
            {
                CreateDestinationTable = false
            };
            var entity = BulkTestEntity.Create("NOCREATE001", "No Create Test");
            var errorMessages = new List<string>();

            processor.Error += (s, e) => errorMessages.Add(e.Message);

            // Act
            var result = processor.BulkInsert(_dbAccess, [entity]);

            // Assert - Should fail because table doesn't exist and won't be created
            result.Should().BeFalse();
            errorMessages.Should().NotBeEmpty();
        }

        /// <summary>
        /// Tests that DropDestinationTableIfExists property affects table dropping behavior.
        /// </summary>
        /// <remarks>
        /// This test validates configurable table preservation behavior for incremental data loading.
        /// Preserving existing tables is essential for incremental ETL processes and scenarios where
        /// data should be appended rather than replaced during bulk operations.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_WithDropDestinationTableIfExistsFalse_ShouldPreserveExistingTable()
        {
            // Arrange
            // First create the table with some data
            var initialProcessor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            var initialEntity = BulkTestEntity.Create("PRESERVE001", "Preserve Test");
            initialProcessor.BulkInsert(_dbAccess, [initialEntity]);

            // Create new processor that won't drop the table
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName)
            {
                DropDestinationTableIfExists = false
            };
            var newEntity = BulkTestEntity.Create("PRESERVE002", "Preserve Test 2");

            // Act
            var result = processor.BulkInsert(_dbAccess, [newEntity]);

            // Assert
            result.Should().BeTrue();

            // Verify both records exist (table wasn't dropped)
            var totalCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName}");
            totalCount.Should().Be(2);
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Tests that bulk insert performs well with a large dataset.
        /// </summary>
        /// <remarks>
        /// This performance test validates scalability and efficiency of bulk operations with large datasets.
        /// Performance validation is crucial for ensuring bulk operations meet enterprise requirements
        /// for data loading speed and resource utilization in production environments.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_LargeDataset_ShouldPerformEfficiently()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(BulkTestTableName);
            var entities = BulkTestEntity.CreateBatch(1000, "PERF");
            var progressReports = new List<float>();

            processor.Progress += (s, e) => progressReports.Add((float)e.Progress);

            // Act & Assert - Should complete within reasonable time
            DatabaseTestHelper.AssertExecutionTime(
                () => processor.BulkInsert(_dbAccess, entities),
                maxMilliseconds: 10000,
                operationName: "Bulk insert of 1000 records");

            // Verify all data was inserted
            var insertedCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {BulkTestTableName}");
            insertedCount.Should().Be(1000);

            // Verify progress was reported
            progressReports.Should().NotBeEmpty();
            progressReports.Last().Should().Be(100f);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Cleans up the bulk test table.
        /// </summary>
        private void CleanupBulkTestTable()
        {
            try
            {
                var cleanup = $"IF OBJECT_ID('{BulkTestTableName}', 'U') IS NOT NULL DROP TABLE {BulkTestTableName}";
                _dbAccess?.ExecuteQuery(cleanup);
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }

        #endregion
    }
}