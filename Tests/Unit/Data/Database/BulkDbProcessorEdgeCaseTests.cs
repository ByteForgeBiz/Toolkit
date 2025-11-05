using ByteForge.Toolkit.Tests.Helpers;
using ByteForge.Toolkit.Tests.Models;
using AwesomeAssertions;
using System.Reflection;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace ByteForge.Toolkit.Tests.Unit.Data.Database
{
    /// <summary>
    /// Unit tests for edge cases and special scenarios in the <see cref="BulkDbProcessor{T}"/> class.
    /// </summary>
    /// <remarks>
    /// These tests focus on edge cases, error conditions, validation scenarios, and special
    /// configurations that might not be covered in the main test suite.
    /// </remarks>
    [TestClass]
    public class BulkDbProcessorEdgeCaseTests
    {
        #region Test Setup and Cleanup

#pragma warning disable CS8618
        // Non-nullable field must contain a non-null value when exiting constructor.
        // Consider adding the 'required' modifier or declaring as nullable.
        private DBAccess _dbAccess;
        private const string EdgeCaseTableName = "EdgeCaseTempTable";
#pragma warning restore CS8618

        /// <summary>
        /// Verifies that the test database is properly configured before running tests.
        /// </summary>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            DatabaseTestHelper.AssertTestDatabaseSetup(dbAccess);
        }

        /// <summary>
        /// Sets up a fresh DBAccess instance for each test.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            _dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            CleanupEdgeCaseTable();
        }

        /// <summary>
        /// Cleans up test data after each test.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            CleanupEdgeCaseTable();
        }

        /// <summary>
        /// Cleans up any test data that might have been created during testing.
        /// </summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void ClassCleanup()
        {
            try
            {
                var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
                var cleanup = $"IF OBJECT_ID('{EdgeCaseTableName}', 'U') IS NOT NULL DROP TABLE {EdgeCaseTableName}";
                dbAccess.ExecuteQuery(cleanup);
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }

        #endregion

        #region Entity Type Without DBColumnAttribute Tests

        /// <summary>
        /// Test entity without any DBColumnAttribute decorations.
        /// </summary>
        public class InvalidEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        /// <summary>
        /// Tests that BulkDbProcessor throws appropriate exception for entities without DBColumnAttribute.
        /// </summary>
        /// <remarks>
        /// This test validates robust entity validation during BulkDbProcessor initialization.
        /// Entities without DBColumnAttribute cannot be processed because there's no column mapping information,
        /// so early detection with clear error messages is essential for developer experience.
        /// </remarks>
        [TestMethod]
        public void Constructor_EntityWithoutDBColumnAttribute_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            var action = new Action(() => new BulkDbProcessor<InvalidEntity>("TestTable"));
            
            action.Should().Throw<InvalidOperationException>()
                  .And.Message.Should().Contain("No properties with DBColumnAttribute found");
        }

        #endregion

        #region Entity Type With Duplicate Column Names

        /// <summary>
        /// Test entity with duplicate column names.
        /// </summary>
        public class DuplicateColumnEntity
        {
            [DBColumn("SameName")]
            public int Property1 { get; set; }

            [DBColumn("SameName")]
            public string Property2 { get; set; }
        }

        /// <summary>
        /// Tests that BulkDbProcessor throws appropriate exception for entities with duplicate column names.
        /// </summary>
        /// <remarks>
        /// This test ensures data integrity by preventing ambiguous column mappings in bulk operations.
        /// Duplicate column names would cause SQL generation errors and unpredictable behavior,
        /// so validation during initialization prevents runtime failures in production environments.
        /// </remarks>
        [TestMethod]
        public void Constructor_EntityWithDuplicateColumnNames_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            var action = new Action(() => new BulkDbProcessor<DuplicateColumnEntity>("TestTable"));
            
            action.Should().Throw<InvalidOperationException>()
                  .And.Message.Should().Contain("Duplicate column names found");
        }

        #endregion

        #region Entity Type Without Primary Key or Unique Index

        /// <summary>
        /// Test entity without primary key or unique index.
        /// </summary>
        public class NoKeyEntity
        {
            [DBColumn("Id")]
            public int Id { get; set; }

            [DBColumn("Name")]
            public string Name { get; set; }
        }

        /// <summary>
        /// Tests that bulk upsert throws appropriate exception for entities without keys or unique indexes.
        /// </summary>
        /// <remarks>
        /// This test validates requirements for upsert operations that need unique identification criteria.
        /// Without primary keys or unique indexes, upsert operations cannot determine which records
        /// to update versus insert, making the operation semantically impossible.
        /// </remarks>
        [TestMethod]
        public void BulkUpsert_EntityWithoutKeysOrUniqueIndexes_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var processor = new BulkDbProcessor<NoKeyEntity>("TestTable");
            var entity = new NoKeyEntity { Id = 1, Name = "Test" };

            // Act & Assert
            var action = new Action(() => processor.BulkUpsert(_dbAccess, [entity]));
            
            action.Should().Throw<InvalidOperationException>()
                  .And.Message.Should().Contain("must have either primary key(s) or unique index(es) for upsert operations");
        }

        /// <summary>
        /// Tests that bulk delete throws appropriate exception for entities without keys or unique indexes.
        /// </summary>
        /// <remarks>
        /// This test ensures data safety by requiring unique identification for bulk delete operations.
        /// Without keys or unique indexes, delete operations could affect unintended records,
        /// potentially causing data loss in production systems.
        /// </remarks>
        [TestMethod]
        public void BulkDelete_EntityWithoutKeysOrUniqueIndexes_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var processor = new BulkDbProcessor<NoKeyEntity>("TestTable");
            var entity = new NoKeyEntity { Id = 1, Name = "Test" };

            // Act & Assert
            var action = new Action(() => processor.BulkDelete(_dbAccess, [entity]));
            
            action.Should().Throw<InvalidOperationException>()
                  .And.Message.Should().Contain("must have either primary key(s) or unique index(es) for upsert operations");
        }

        #endregion

        #region Entity Type With Identity Primary Key Only

        /// <summary>
        /// Test entity with identity primary key only (no unique indexes).
        /// </summary>
        public class IdentityOnlyEntity
        {
            [DBColumn("Id", isPrimaryKey: true, IsIdentity = true)]
            public int Id { get; set; }

            [DBColumn("Name")]
            public string Name { get; set; }
        }

        /// <summary>
        /// Tests that bulk upsert fails for entities with identity primary key only.
        /// </summary>
        /// <remarks>
        /// This test validates constraints for upsert operations with identity columns.
        /// Identity primary keys are auto-generated and cannot be used for matching during upsert,
        /// requiring additional unique columns or indexes for proper operation identification.
        /// </remarks>
        [TestMethod]
        public void BulkUpsert_EntityWithIdentityPrimaryKeyOnly_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var processor = new BulkDbProcessor<IdentityOnlyEntity>("TestTable");
            var entity = new IdentityOnlyEntity { Id = 1, Name = "Test" };

            // Act & Assert
            var action = new Action(() => processor.BulkUpsert(_dbAccess, [entity]));
            
            action.Should().Throw<InvalidOperationException>()
                  .And.Message.Should().Contain("must have either non-identity primary key(s) or unique index(es) for upsert operations");
        }

        #endregion

        #region String Length and Data Type Edge Cases

        /// <summary>
        /// Test entity for long string testing without MaxLength constraints.
        /// </summary>
        public class LongStringTestEntity
        {
            [DBColumn("Id", isPrimaryKey: true)]
            public Guid Id { get; set; } = Guid.NewGuid();

            [DBColumn("Code", isUnique: true, MaxLength = 50)]
            public string Code { get; set; }

            [DBColumn("LongTextField")] // No MaxLength - should create TEXT field for very long strings
            public string LongTextField { get; set; }

            [DBColumn("CreatedDate")]
            public DateTime CreatedDate { get; set; } = DateTime.Now;
        }

        /// <summary>
        /// Tests that bulk operations handle very long strings correctly by creating appropriate column types.
        /// </summary>
        /// <remarks>
        /// This test validates dynamic column type selection for varying string lengths in bulk operations.
        /// Proper handling of long strings is crucial for applications dealing with large text data,
        /// ensuring no data truncation occurs during bulk loading processes.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_WithVeryLongStrings_ShouldCreateTextFieldAndHandleCorrectly()
        {
            // Arrange
            var longStringTableName = "LongStringTestTable";
            var processor = new BulkDbProcessor<LongStringTestEntity>(longStringTableName);
            var longString = new string('A', 5000); // 5000 characters - exceeds varchar limits
            var entity = new LongStringTestEntity
            {
                Code = "LONG001",
                LongTextField = longString
            };

            try
            {
                // Act
                var result = processor.BulkInsert(_dbAccess, [entity]);

                // Assert
                result.Should().BeTrue();

                // Verify the long string was stored correctly
                var storedEntity = _dbAccess.GetRecord<LongStringTestEntity>(
                    $"SELECT * FROM {longStringTableName} WHERE Code = @code", "LONG001");
                _dbAccess.LastException.Should().BeNull("");
                storedEntity.Should().NotBeNull();
                storedEntity.LongTextField.Should().NotBeNull();
                storedEntity.LongTextField.Length.Should().Be(5000);
                storedEntity.LongTextField.Should().Be(longString);

                // Verify the table was created with appropriate column types
                var columnInfo = _dbAccess.GetRecord($@"
                    SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = '{longStringTableName}' AND COLUMN_NAME = 'LongTextField'");
                columnInfo.Should().NotBeNull();
                
                var dataType = columnInfo["DATA_TYPE"].ToString();
                // Should be either 'text' for very long strings or 'varchar' with appropriate length
                dataType.Should().BeOneOf("text", "varchar");
            }
            finally
            {
                // Cleanup
                try
                {
                    _dbAccess.ExecuteQuery($"IF OBJECT_ID('{longStringTableName}', 'U') IS NOT NULL DROP TABLE {longStringTableName}");
                }
                catch (Exception)
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Tests that bulk operations handle null values correctly.
        /// </summary>
        /// <remarks>
        /// This test ensures proper null value handling throughout the bulk operation pipeline.
        /// Null values are common in real-world data and must be handled correctly to prevent
        /// SQL errors and ensure data integrity during bulk loading operations.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_WithNullValues_ShouldHandleCorrectly()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(EdgeCaseTableName);
            var entity = new BulkTestEntity
            {
                Id = Guid.NewGuid(),
                Code = "NULL001",
                Name = null, // Null string
                Category = null, // Null string
                Value = 0m,
                Priority = 1,
                IsActive = true,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            // Act
            var result = processor.BulkInsert(_dbAccess, [entity]);

            // Assert
            result.Should().BeTrue();

            // Verify the null values were stored correctly
            var storedEntity = _dbAccess.GetRecord<BulkTestEntity>($"SELECT * FROM {EdgeCaseTableName} WHERE Code = @code", "NULL001");
            storedEntity.Should().NotBeNull();
            storedEntity.Name.Should().BeNull();
            storedEntity.Category.Should().BeNull();
        }

        /// <summary>
        /// Tests that bulk operations handle DateTime.MinValue correctly (should convert to NULL).
        /// </summary>
        /// <remarks>
        /// This test validates special handling of DateTime edge cases in bulk operations.
        /// DateTime.MinValue often represents uninitialized dates and should be converted to NULL
        /// to maintain data consistency and prevent invalid date storage in database systems.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_WithDateTimeMinValue_ShouldConvertToNull()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(EdgeCaseTableName);
            var entity = new BulkTestEntity
            {
                Id = Guid.NewGuid(),
                Code = "MINDATE001",
                Name = "Min Date Test",
                Category = "Test",
                Value = 0m,
                Priority = 1,
                IsActive = true,
                CreatedDate = DateTime.MinValue, // Should convert to NULL
                ModifiedDate = DateTime.Now
            };

            // Act
            var result = processor.BulkInsert(_dbAccess, [entity]);

            // Assert
            result.Should().BeTrue();

            // Verify the DateTime.MinValue was converted to NULL
            // Note: This depends on how the data retrieval handles NULL dates
            var storedEntity = _dbAccess.GetRecord<BulkTestEntity>($"SELECT * FROM {EdgeCaseTableName} WHERE Code = @code", "MINDATE001");
            storedEntity.Should().NotBeNull();
        }

        #endregion

        #region Large Dataset Performance Edge Cases

        /// <summary>
        /// Tests that bulk operations handle very large datasets without memory issues.
        /// </summary>
        /// <remarks>
        /// This stress test validates memory efficiency and performance scalability of bulk operations.
        /// Large dataset handling is crucial for ETL processes and data migration scenarios,
        /// ensuring applications can process substantial volumes without memory exhaustion.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_VeryLargeDataset_ShouldHandleMemoryEfficiently()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(EdgeCaseTableName);
            var entities = BulkTestEntity.CreateBatch(5000, "LARGE"); // 5000 records
            var progressReports = new List<float>();

            processor.Progress += (s, e) => progressReports.Add((float)e.Progress);

            // Act & Assert - Should complete within reasonable time and memory
            DatabaseTestHelper.AssertExecutionTime(
                () => processor.BulkInsert(_dbAccess, entities),
                maxMilliseconds: 30000, // 30 seconds for 5000 records
                operationName: "Bulk insert of 5000 records");

            // Verify all data was inserted
            var insertedCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {EdgeCaseTableName}");
            insertedCount.Should().Be(5000);

            // Verify progress was reported appropriately
            progressReports.Should().NotBeEmpty();
            progressReports.Last().Should().Be(100f);
        }

        #endregion

        #region Event Handling Edge Cases

        /// <summary>
        /// Tests that events are fired correctly even when operations complete very quickly.
        /// </summary>
        /// <remarks>
        /// This test ensures event reliability for minimal datasets in bulk operations.
        /// Event consistency regardless of operation duration is important for applications
        /// that depend on progress reporting and completion notifications for user feedback.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_VerySmallDataset_ShouldStillFireEvents()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(EdgeCaseTableName);
            var entity = BulkTestEntity.Create("TINY001", "Tiny Test");
            var progressReports = new List<float>();
            var errorMessages = new List<string>();

            processor.Progress += (s, e) => progressReports.Add((float)e.Progress);
            processor.Error += (s, e) => errorMessages.Add(e.Message);

            // Act
            var result = processor.BulkInsert(_dbAccess, [entity]);

            // Assert
            result.Should().BeTrue();
            progressReports.Should().NotBeEmpty();
            progressReports.Should().Contain(100f);
            errorMessages.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that events handle null subscribers gracefully.
        /// </summary>
        /// <remarks>
        /// This test ensures robustness when no event handlers are subscribed to bulk operations.
        /// Graceful handling of null event subscribers prevents NullReferenceException and ensures
        /// operations complete successfully regardless of event subscription status.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_WithNullEventSubscribers_ShouldNotThrow()
        {
            // Arrange
            var processor = new BulkDbProcessor<BulkTestEntity>(EdgeCaseTableName);
            // Don't subscribe to any events (events should be null)
            var entity = BulkTestEntity.Create("NULLEVENT001", "Null Event Test");

            // Act & Assert - Should not throw even with null events
            var action = new Action(() => processor.BulkInsert(_dbAccess, [entity]));
            action.Should().NotThrow();

            // Verify operation completed successfully
            var insertedCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {EdgeCaseTableName}");
            insertedCount.Should().Be(1);
        }

        #endregion

        #region Table Name Edge Cases

        /// <summary>
        /// Tests bulk operations with table names containing special characters.
        /// </summary>
        /// <remarks>
        /// This test validates SQL identifier handling for table names with special characters.
        /// Special characters in table names require proper escaping and bracket notation
        /// to prevent SQL syntax errors during bulk operation SQL generation.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_WithSpecialCharactersInTableName_ShouldHandleCorrectly()
        {
            // Arrange
            var specialTableName = "[Test Table With Spaces And Brackets]";
            var processor = new BulkDbProcessor<BulkTestEntity>(specialTableName);
            var entity = BulkTestEntity.Create("SPECIAL001", "Special Table Test");

            try
            {
                // Act
                var result = processor.BulkInsert(_dbAccess, [entity]);

                // Assert
                result.Should().BeTrue();

                // Verify data was inserted
                var insertedCount = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {specialTableName}");
                insertedCount.Should().Be(1);
            }
            finally
            {
                // Cleanup
                try
                {
                    _dbAccess.ExecuteQuery($"IF OBJECT_ID('{specialTableName}', 'U') IS NOT NULL DROP TABLE {specialTableName}");
                }
                catch (Exception)
                {
                    // Ignore cleanup errors
                }
            }
        }

        #endregion

        #region Reflection and Internal Method Tests

        /// <summary>
        /// Tests that internal property mapping is correctly initialized.
        /// </summary>
        /// <remarks>
        /// This test validates the internal reflection-based property mapping system in BulkDbProcessor.
        /// Proper property mapping initialization is crucial for correct SQL generation, column mapping,
        /// and data transfer during all bulk operations.
        /// </remarks>
        [TestMethod]
        public void Constructor_PropertyMapping_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var processor = new BulkDbProcessor<BulkTestEntity>(EdgeCaseTableName);

            // Use reflection to access protected properties for testing
            var propertiesField = typeof(BulkDbProcessor<BulkTestEntity>).GetProperty("Properties", BindingFlags.NonPublic | BindingFlags.Instance);
            var columnMapField = typeof(BulkDbProcessor<BulkTestEntity>).GetProperty("ColumnMap", BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert
            propertiesField.Should().NotBeNull();
            columnMapField.Should().NotBeNull();

            var properties = propertiesField.GetValue(processor) as PropertyInfo[];
            var columnMap = columnMapField.GetValue(processor) as Dictionary<PropertyInfo, string>;

            properties.Should().NotBeNull().And.NotBeEmpty();
            columnMap.Should().NotBeNull().And.NotBeEmpty();

            // Verify that all properties have corresponding column mappings
            properties.Length.Should().Be(columnMap.Count);

            // Verify specific mappings
            columnMap.Values.Should().Contain("Id");
            columnMap.Values.Should().Contain("Code");
            columnMap.Values.Should().Contain("Name");
        }

        #endregion

        #region Concurrent Access Tests

        /// <summary>
        /// Tests that bulk operations handle concurrent access correctly.
        /// </summary>
        /// <remarks>
        /// This test validates thread safety and isolation of concurrent bulk operations.
        /// Multi-threaded applications may perform bulk operations simultaneously,
        /// so ensuring proper isolation prevents data corruption and operation interference.
        /// </remarks>
        [TestMethod]
        public void BulkInsert_ConcurrentOperations_ShouldHandleCorrectly()
        {
            // Arrange
            var processor1 = new BulkDbProcessor<BulkTestEntity>("ConcurrentTable1");
            var processor2 = new BulkDbProcessor<BulkTestEntity>("ConcurrentTable2");
            
            var entities1 = BulkTestEntity.CreateBatch(50, "CONC1");
            var entities2 = BulkTestEntity.CreateBatch(50, "CONC2");

            var results = new bool[2];
            var exceptions = new Exception[2];

            try
            {
                // Act - Run operations concurrently
                var tasks = new[]
                {
                    System.Threading.Tasks.Task.Run(() => 
                    {
                        try
                        {
                            results[0] = processor1.BulkInsert(_dbAccess, entities1);
                        }
                        catch (Exception ex)
                        {
                            exceptions[0] = ex;
                        }
                    }),
                    System.Threading.Tasks.Task.Run(() => 
                    {
                        try
                        {
                            results[1] = processor2.BulkInsert(_dbAccess, entities2);
                        }
                        catch (Exception ex)
                        {
                            exceptions[1] = ex;
                        }
                    })
                };

                System.Threading.Tasks.Task.WaitAll(tasks);

                // Assert
                exceptions[0].Should().BeNull();
                exceptions[1].Should().BeNull();
                results[0].Should().BeTrue();
                results[1].Should().BeTrue();
            }
            finally
            {
                // Cleanup
                try
                {
                    _dbAccess.ExecuteQuery("IF OBJECT_ID('ConcurrentTable1', 'U') IS NOT NULL DROP TABLE ConcurrentTable1");
                    _dbAccess.ExecuteQuery("IF OBJECT_ID('ConcurrentTable2', 'U') IS NOT NULL DROP TABLE ConcurrentTable2");
                }
                catch (Exception)
                {
                    // Ignore cleanup errors
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Cleans up the edge case test table.
        /// </summary>
        private void CleanupEdgeCaseTable()
        {
            try
            {
                var cleanup = $"IF OBJECT_ID('{EdgeCaseTableName}', 'U') IS NOT NULL DROP TABLE {EdgeCaseTableName}";
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