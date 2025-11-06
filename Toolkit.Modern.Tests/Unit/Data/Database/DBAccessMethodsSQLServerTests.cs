using ByteForge.Toolkit.Tests.Helpers;
using AwesomeAssertions;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace ByteForge.Toolkit.Tests.Unit.Data.Database
{
    /// <summary>
    /// Unit tests for the query and data retrieval methods of the <see cref="DBAccess"/> class using SQL Server.
    /// </summary>
    /// <remarks>
    /// These tests focus on the core database operation methods including GetValue, GetRecord,
    /// GetRecords, ExecuteQuery, and their async variants. Tests use the TestUnitDB SQL Server database
    /// with real data to ensure proper integration and data handling for SQL Server connections.
    /// </remarks>
    [TestClass]
    public class DBAccessMethodsSQLServerTests
    {
        #region Test Fields and Setup

        private DBAccess _dbAccess;

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
        }

        /// <summary>
        /// Cleans up the DBAccess instance after each test.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            // _dbAccess?.Dispose();
        }

        #endregion

        #region TestConnection Tests

        /// <summary>
        /// Tests that TestConnection returns true for a valid database connection.
        /// </summary>
        /// <remarks>
        /// This test verifies the fundamental connectivity functionality of DBAccess with SQL Server.
        /// It ensures that the database configuration is valid and that the connection can be established
        /// successfully, which is critical for all other database operations.
        /// </remarks>
        [TestMethod]
        public void TestConnection_ValidDatabase_ShouldReturnTrue()
        {
            // Act
            var result = _dbAccess.TestConnection();

            // Assert
            result.Should().BeTrue("TestConnection should return true for valid database");
            _dbAccess.LastException.Should().BeNull("no exception should occur for valid connection");
        }

        /// <summary>
        /// Tests that TestConnection returns false for an invalid database connection.
        /// </summary>
        /// <remarks>
        /// This test validates proper error handling when connection parameters are invalid.
        /// It ensures that DBAccess gracefully handles connection failures and sets appropriate
        /// exception information without throwing unhandled exceptions.
        /// </remarks>
        [TestMethod]
        public void TestConnection_InvalidDatabase_ShouldReturnFalse()
        {
            // Arrange
            var invalidOptions = DatabaseTestHelper.CreateInvalidDatabaseOptions();
            var invalidDbAccess = new DBAccess(invalidOptions);

            // Act
            var result = invalidDbAccess.TestConnection();

            // Assert
            result.Should().BeFalse("TestConnection should return false for invalid database");
            invalidDbAccess.LastException.Should().NotBeNull("exception should be set for invalid connection");
        }

        /// <summary>
        /// Tests the async version of TestConnection.
        /// </summary>
        /// <remarks>
        /// This test ensures that the asynchronous connection testing functionality works correctly
        /// with SQL Server, allowing for non-blocking database connectivity checks in async/await
        /// patterns commonly used in modern applications.
        /// </remarks>
        [TestMethod]
        public async Task TestConnectionAsync_ValidDatabase_ShouldReturnTrue()
        {
            // Act
            var result = await _dbAccess.TestConnectionAsync();

            // Assert
            result.Should().BeTrue("TestConnectionAsync should return true for valid database");
        }

        #endregion

        #region GetValue Tests

        /// <summary>
        /// Tests that GetValue returns the correct scalar value from a simple query.
        /// </summary>
        /// <remarks>
        /// This test validates the core scalar value retrieval functionality with SQL Server.
        /// It demonstrates proper handling of COUNT queries and type conversion, which are
        /// fundamental operations for data validation and business logic in applications.
        /// </remarks>
        [TestMethod]
        public void GetValue_SimpleScalarQuery_ShouldReturnCorrectValue()
        {
            // Act
            var count = _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterThan(0, "should return a positive count of test entities");
        }

        /// <summary>
        /// Tests that GetValue with parameters works correctly.
        /// </summary>
        /// <remarks>
        /// This test ensures that parameterized queries work properly with GetValue, validating
        /// SQL injection prevention mechanisms and proper parameter binding with SQL Server.
        /// Parameterized queries are essential for security and performance in production systems.
        /// </remarks>
        [TestMethod]
        public void GetValue_WithParameters_ShouldReturnCorrectValue()
        {
            // Act
            var activeCount = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE IsActive = @active", true);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            activeCount.Should().BeGreaterThan(0, "should return active entities count");
        }

        /// <summary>
        /// Tests that GetValue returns null for queries that return no results.
        /// </summary>
        /// <remarks>
        /// This test validates proper handling of empty result sets in scalar queries.
        /// It ensures that the method returns appropriate null values rather than throwing
        /// exceptions, which is crucial for robust application behavior when data is not found.
        /// </remarks>
        [TestMethod]
        public void GetValue_NoResults_ShouldReturnDefault()
        {
            // Act
            var name = _dbAccess.GetValue<string>(
                "SELECT Name FROM TestEntities WHERE Id = @id", -9999);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            name.Should().BeNull("should return null for non-existent record");
        }

        /// <summary>
        /// Tests the generic TryGetValue method for successful operations.
        /// </summary>
        /// <remarks>
        /// This test validates the Try-pattern implementation for scalar value retrieval,
        /// which provides a safer alternative to GetValue by returning success/failure status
        /// without throwing exceptions. This pattern is valuable for performance-critical scenarios.
        /// </remarks>
        [TestMethod]
        public void TryGetValue_ValidQuery_ShouldReturnTrueAndValue()
        {
            // Act
            var success = _dbAccess.TryGetValue<int>(out var count, "SELECT COUNT(*) FROM TestEntities");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            success.Should().BeTrue("TryGetValue should return true for valid query");
            count.Should().BeGreaterThan(0, "should return a positive count");
        }

        /// <summary>
        /// Tests TryGetValue with an invalid query.
        /// </summary>
        /// <remarks>
        /// This test ensures that TryGetValue properly handles SQL syntax errors and invalid
        /// table references by returning false and setting appropriate error information.
        /// This behavior is crucial for applications that need to handle database errors gracefully.
        /// </remarks>
        [TestMethod]
        public void TryGetValue_InvalidQuery_ShouldReturnFalseAndDefault()
        {
            // Act
            var success = _dbAccess.TryGetValue<int>(out var count, "SELECT * FROM NonExistentTable");

            // Assert
            _dbAccess.LastException.Should().NotBeNull("should have thrown exception for invalid query");
            success.Should().BeFalse("TryGetValue should return false for invalid query");
            count.Should().Be(0, "should return default value");
        }

        /// <summary>
        /// Tests the async version of GetValue.
        /// </summary>
        /// <remarks>
        /// This test validates asynchronous scalar value retrieval with SQL Server,
        /// ensuring proper async/await patterns and non-blocking database operations.
        /// Async methods are essential for scalable web applications and services.
        /// </remarks>
        [TestMethod]
        public async Task GetValueAsync_ValidQuery_ShouldReturnCorrectValue()
        {
            // Act
            var count = await _dbAccess.GetValueAsync<int>("SELECT COUNT(*) FROM TestEntities");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterThan(0, "async GetValue should return positive count");
        }

        /// <summary>
        /// Tests the async version of TryGetValue.
        /// </summary>
        /// <remarks>
        /// This test combines the benefits of the Try-pattern with asynchronous execution,
        /// providing both performance benefits and graceful error handling for scalar queries.
        /// The tuple return type enables clean destructuring in modern C# applications.
        /// </remarks>
        [TestMethod]
        public async Task TryGetValueAsync_ValidQuery_ShouldReturnSuccessAndValue()
        {
            // Act
            var (success, count) = await _dbAccess.TryGetValueAsync<int>("SELECT COUNT(*) FROM TestEntities");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            success.Should().BeTrue("async TryGetValue should return true for valid query");
            count.Should().BeGreaterThan(0, "should return positive count");
        }

        #endregion

        #region GetRecord Tests

        /// <summary>
        /// Tests that GetRecord returns a single DataRow for a valid query.
        /// </summary>
        /// <remarks>
        /// This test validates single record retrieval functionality with SQL Server.
        /// It ensures proper DataRow population with column metadata and data values,
        /// which is essential for applications that need structured access to database records.
        /// </remarks>
        [TestMethod]
        public void GetRecord_ValidQuery_ShouldReturnDataRow()
        {
            // Act
            var record = _dbAccess.GetRecord("SELECT TOP 1 * FROM TestEntities WHERE IsActive = @active", true);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            record.Should().NotBeNull("should return a DataRow");
            record.Table.Columns.Count.Should().BeGreaterThan(0, "DataRow should have columns");
            record["Name"].Should().NotBeNull("should have Name column");
        }

        /// <summary>
        /// Tests that GetRecord returns null when no records match the query.
        /// </summary>
        /// <remarks>
        /// This test ensures proper handling of empty result sets in single record queries.
        /// Returning null for non-existent records allows applications to distinguish between
        /// 'no data found' and actual data retrieval errors, enabling proper business logic flow.
        /// </remarks>
        [TestMethod]
        public void GetRecord_NoResults_ShouldReturnNull()
        {
            // Act
            var record = _dbAccess.GetRecord("SELECT * FROM TestEntities WHERE Id = @id", -9999);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            record.Should().BeNull("should return null for non-existent record");
        }

        /// <summary>
        /// Tests the async version of GetRecord.
        /// </summary>
        /// <remarks>
        /// This test validates asynchronous single record retrieval with SQL Server,
        /// ensuring that DataRow objects are properly constructed in async scenarios.
        /// Async record retrieval is crucial for responsive UI applications and web services.
        /// </remarks>
        [TestMethod]
        public async Task GetRecordAsync_ValidQuery_ShouldReturnDataRow()
        {
            // Act
            var record = await _dbAccess.GetRecordAsync("SELECT TOP 1 * FROM TestEntities WHERE IsActive = 1");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            record.Should().NotBeNull("async GetRecord should return a DataRow");
            record.Table.Columns.Count.Should().BeGreaterThan(0, "DataRow should have columns");
        }

        #endregion

        #region GetRecords Tests

        /// <summary>
        /// Tests that GetRecords returns multiple records as DataRowCollection.
        /// </summary>
        /// <remarks>
        /// This test validates bulk record retrieval functionality with SQL Server.
        /// It ensures proper population of DataRowCollection with multiple records,
        /// which is fundamental for reporting, data analysis, and bulk data processing scenarios.
        /// </remarks>
        [TestMethod]
        public void GetRecords_ValidQuery_ShouldReturnDataRowCollection()
        {
            // Act
            var records = _dbAccess.GetRecords("SELECT * FROM TestEntities WHERE IsActive = @active", true);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            records.Should().NotBeNull("should return DataRowCollection");
            records.Count.Should().BeGreaterThan(0, "should return multiple records");
        }

        /// <summary>
        /// Tests that GetRecords returns null when no records match the query.
        /// </summary>
        /// <remarks>
        /// This test validates handling of empty result sets in multi-record queries.
        /// The behavior returns an empty DataRowCollection rather than null, which provides
        /// consistent behavior for iteration and count operations in application code.
        /// </remarks>
        [TestMethod]
        public void GetRecords_NoResults_ShouldReturnNull()
        {
            // Act
            var records = _dbAccess.GetRecords("SELECT * FROM TestEntities WHERE Name = @name", "NonExistentEntity");

            // Assert
            _dbAccess.LastException.Should().BeNull("No exception should occur for no results");
            records.Should().NotBeNull("Should return an empty DataRowCollection instead of null");
        }

        /// <summary>
        /// Tests the async version of GetRecords.
        /// </summary>
        /// <remarks>
        /// This test ensures asynchronous bulk record retrieval works correctly with SQL Server.
        /// Async multi-record queries are essential for scalable applications that process
        /// large datasets without blocking the calling thread.
        /// </remarks>
        [TestMethod]
        public async Task GetRecordsAsync_ValidQuery_ShouldReturnDataRowCollection()
        {
            // Act
            var records = await _dbAccess.GetRecordsAsync("SELECT * FROM TestEntities WHERE IsActive = 1");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            records.Should().NotBeNull("async GetRecords should return DataRowCollection");
            records.Count.Should().BeGreaterThan(0, "should return multiple records");
        }

        #endregion

        #region ExecuteQuery Tests

        /// <summary>
        /// Tests that ExecuteQuery successfully executes an INSERT statement.
        /// </summary>
        /// <remarks>
        /// This test validates data insertion capabilities with parameterized queries on SQL Server.
        /// It demonstrates proper parameter binding, transaction handling, and RecordsAffected tracking,
        /// which are essential for data modification operations and audit trails in business applications.
        /// </remarks>
        [TestMethod]
        public void ExecuteQuery_InsertStatement_ShouldReturnTrueAndUpdateRecordsAffected()
        {
            // Arrange
            var testName = DatabaseTestHelper.GenerateTestString("ExecuteQuery");
            var testDesc = "Test record for ExecuteQuery method";

            try
            {
                // Act
                var result = _dbAccess.ExecuteQuery(
                    "INSERT INTO TestEntities (Name, Description, IsActive, TestValue) VALUES (@name, @desc, @active, @value)",
                    testName, testDesc, true, 123.45m);

                // Assert
                _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
                result.Should().BeTrue("ExecuteQuery should return true for successful INSERT");
                _dbAccess.RecordsAffected.Should().Be(1, "should affect exactly 1 record");

                // Verify the record was inserted
                var insertedName = _dbAccess.GetValue<string>("SELECT Name FROM TestEntities WHERE Name = @name", testName);
                insertedName.Should().Be(testName, "record should be inserted correctly");
            }
            finally
            {
                // Cleanup
                DatabaseTestHelper.CleanupTestData(_dbAccess, "TestEntities", $"Name = '{testName}'");
            }
        }

        /// <summary>
        /// Tests that ExecuteQuery successfully executes an UPDATE statement.
        /// </summary>
        /// <remarks>
        /// This test ensures data modification capabilities work correctly with SQL Server.
        /// It validates parameter handling for WHERE clauses and SET operations, demonstrating
        /// the safety and reliability of data updates in transactional business scenarios.
        /// </remarks>
        [TestMethod]
        public void ExecuteQuery_UpdateStatement_ShouldReturnTrueAndUpdateRecordsAffected()
        {
            // Arrange - Insert a test record first
            var testName = DatabaseTestHelper.GenerateTestString("UpdateTest");
            var originalDesc = "Original description";
            var updatedDesc = "Updated description";

            _dbAccess.ExecuteQuery(
                "INSERT INTO TestEntities (Name, Description, IsActive) VALUES (@name, @desc, @active)",
                testName, originalDesc, true);

            try
            {
                // Act - Update the record
                var result = _dbAccess.ExecuteQuery(
                    "UPDATE TestEntities SET Description = @newDesc WHERE Name = @name",
                    updatedDesc, testName);

                // Assert
                _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
                result.Should().BeTrue("ExecuteQuery should return true for successful UPDATE");
                _dbAccess.RecordsAffected.Should().Be(1, "should affect exactly 1 record");

                // Verify the record was updated
                var actualDesc = _dbAccess.GetValue<string>("SELECT Description FROM TestEntities WHERE Name = @name", testName);
                actualDesc.Should().Be(updatedDesc, "record should be updated correctly");
            }
            finally
            {
                // Cleanup
                DatabaseTestHelper.CleanupTestData(_dbAccess, "TestEntities", $"Name = '{testName}'");
            }
        }

        /// <summary>
        /// Tests that ExecuteQuery successfully executes a DELETE statement.
        /// </summary>
        /// <remarks>
        /// This test validates data deletion functionality with proper parameter binding.
        /// It ensures that DELETE operations are executed safely with accurate RecordsAffected
        /// tracking, which is crucial for data integrity and audit logging in enterprise applications.
        /// </remarks>
        [TestMethod]
        public void ExecuteQuery_DeleteStatement_ShouldReturnTrueAndUpdateRecordsAffected()
        {
            // Arrange - Insert a test record first
            var testName = DatabaseTestHelper.GenerateTestString("DeleteTest");
            _dbAccess.ExecuteQuery(
                "INSERT INTO TestEntities (Name, Description, IsActive) VALUES (@name, @desc, @active)",
                testName, "Record to be deleted", true);

            // Act - Delete the record
            var result = _dbAccess.ExecuteQuery("DELETE FROM TestEntities WHERE Name = @name", testName);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            result.Should().BeTrue("ExecuteQuery should return true for successful DELETE");
            _dbAccess.RecordsAffected.Should().Be(1, "should affect exactly 1 record");

            // Verify the record was deleted
            var count = _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities WHERE Name = @name", testName);
            count.Should().Be(0, "record should be deleted");
        }

        /// <summary>
        /// Tests ExecuteQuery with an invalid SQL statement.
        /// </summary>
        /// <remarks>
        /// This test ensures proper error handling for malformed SQL statements.
        /// It validates that the method returns false and sets appropriate exception information
        /// rather than allowing SQL errors to propagate, providing robust error handling for applications.
        /// </remarks>
        [TestMethod]
        public void ExecuteQuery_InvalidSQL_ShouldReturnFalseAndSetException()
        {
            // Act
            var result = _dbAccess.ExecuteQuery("INVALID SQL STATEMENT");

            // Assert
            _dbAccess.LastException.Should().NotBeNull("exception should be set for invalid SQL");
            result.Should().BeFalse("ExecuteQuery should return false for invalid SQL");
        }

        /// <summary>
        /// Tests the async version of ExecuteQuery.
        /// </summary>
        /// <remarks>
        /// This test validates asynchronous data modification operations with SQL Server.
        /// It ensures that INSERT statements work correctly in async scenarios, which is
        /// essential for scalable web applications and services that require non-blocking database operations.
        /// </remarks>
        [TestMethod]
        public async Task ExecuteQueryAsync_ValidStatement_ShouldReturnTrue()
        {
            // Arrange
            var testName = DatabaseTestHelper.GenerateTestString("AsyncExecute");

            try
            {
                // Act
                var result = await _dbAccess.ExecuteQueryAsync(
                    "INSERT INTO TestEntities (Name, Description, IsActive) VALUES (@name, @desc, @active)",
                    testName, "Async test record", true);

                // Assert
                _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
                result.Should().BeTrue("async ExecuteQuery should return true for valid statement");
                _dbAccess.RecordsAffected.Should().Be(1, "should affect exactly 1 record");
            }
            finally
            {
                // Cleanup
                DatabaseTestHelper.CleanupTestData(_dbAccess, "TestEntities", $"Name = '{testName}'");
            }
        }

        #endregion

        #region Parameter Handling Tests

        /// <summary>
        /// Tests that queries with multiple parameters work correctly.
        /// </summary>
        /// <remarks>
        /// This test validates complex parameter handling with multiple conditions in SQL Server queries.
        /// It ensures proper parameter binding for range queries and complex WHERE clauses, which are
        /// common in business applications for filtering and data analysis operations.
        /// </remarks>
        [TestMethod]
        public void GetValue_MultipleParameters_ShouldHandleCorrectly()
        {
            // Act
            var count = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE IsActive = @active AND TestValue >= @minValue AND TestValue <= @maxValue",
                true, 0m, 1000m);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterThanOrEqualTo(0, "should return a valid count");
        }

        /// <summary>
        /// Tests that null parameters are handled correctly.
        /// </summary>
        /// <remarks>
        /// This test ensures proper handling of NULL values in parameterized queries with SQL Server.
        /// Null parameter handling is crucial for optional search criteria and nullable database columns,
        /// preventing SQL errors and ensuring correct business logic execution.
        /// </remarks>
        [TestMethod]
        public void GetValue_NullParameter_ShouldHandleCorrectly()
        {
            // Act
            var count = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE Description = @desc OR @desc IS NULL", [null]);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not be thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterThan(0, "should handle null parameters correctly");
        }

        /// <summary>
        /// Tests that various data types as parameters work correctly.
        /// </summary>
        /// <remarks>
        /// This test validates type safety and proper conversion for different .NET data types
        /// when used as SQL Server parameters. It ensures that GUID, DateTime, Decimal, Boolean,
        /// and String types are correctly handled, which is essential for robust data persistence.
        /// </remarks>
        [TestMethod]
        public void ExecuteQuery_VariousDataTypes_ShouldHandleCorrectly()
        {
            // Arrange
            var testName = DatabaseTestHelper.GenerateTestString("DataTypes");
            var testGuid = Guid.NewGuid();
            var testDate = DateTime.Now.Date;
            var testDecimal = 999.99m;
            var testBool = true;

            try
            {
                // Act
                var result = _dbAccess.ExecuteQuery(@"
                    INSERT INTO TestEntities (Name, Description, IsActive, TestValue, TestGuid, CreatedDate) 
                    VALUES (@name, @desc, @active, @value, @guid, @date)",
                    testName, "Data type test", testBool, testDecimal, testGuid, testDate);

                // Assert
                _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
                result.Should().BeTrue("should handle various data types as parameters");
                _dbAccess.RecordsAffected.Should().Be(1);

                // Verify values were stored correctly
                var record = _dbAccess.GetRecord("SELECT * FROM TestEntities WHERE Name = @name", testName);
                record.Should().NotBeNull();
                record["Name"].ToString().Should().Be(testName);
                record["IsActive"].Should().Be(testBool);
            }
            finally
            {
                // Cleanup
                DatabaseTestHelper.CleanupTestData(_dbAccess, "TestEntities", $"Name = '{testName}'");
            }
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Tests that simple queries execute within reasonable time limits.
        /// </summary>
        /// <remarks>
        /// This performance test ensures that basic SQL Server operations meet expected response times.
        /// Performance validation is crucial for applications with SLA requirements and helps identify
        /// potential database configuration or connectivity issues early in development.
        /// </remarks>
        [TestMethod]
        public void GetValue_SimpleQuery_ShouldExecuteQuickly()
        {
            // Act & Assert
            DatabaseTestHelper.AssertExecutionTime(
                () => _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities"),
                maxMilliseconds: 1000,
                operationName: "Simple count query");
        }

        /// <summary>
        /// Tests that queries with parameters don't significantly impact performance.
        /// </summary>
        /// <remarks>
        /// This test validates that parameterized queries maintain acceptable performance with SQL Server.
        /// While parameters add security benefits, this test ensures they don't introduce significant
        /// overhead that could impact application responsiveness in production environments.
        /// </remarks>
        [TestMethod]
        public void GetValue_ParameterizedQuery_ShouldExecuteQuickly()
        {
            // Act & Assert
            DatabaseTestHelper.AssertExecutionTime(
                () => _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities WHERE IsActive = @active", true),
                maxMilliseconds: 1000,
                operationName: "Parameterized count query");
        }

        /// <summary>
        /// Tests that retrieving multiple records doesn't take excessive time.
        /// </summary>
        /// <remarks>
        /// This performance test ensures bulk data retrieval operations complete within acceptable timeframes.
        /// Multi-record queries are common in reporting and data analysis scenarios, so performance
        /// validation helps ensure scalability for large datasets.
        /// </remarks>
        [TestMethod]
        public void GetRecords_MultipleRecords_ShouldExecuteQuickly()
        {
            // Act & Assert
            DatabaseTestHelper.AssertExecutionTime(
                () => _dbAccess.GetRecords("SELECT * FROM TestEntities"),
                maxMilliseconds: 2000,
                operationName: "Retrieve all test entities");
        }

        #endregion

        #region Edge Cases and Error Handling

        /// <summary>
        /// Tests behavior with empty string parameters.
        /// </summary>
        /// <remarks>
        /// This edge case test validates handling of empty string parameters in SQL Server queries.
        /// Empty strings are common in user input scenarios and need to be handled correctly to
        /// prevent unexpected query results or application errors.
        /// </remarks>
        [TestMethod]
        public void GetValue_EmptyStringParameter_ShouldHandleCorrectly()
        {
            // Act
            var count = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE Name = @name OR @name = ''", "");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterThanOrEqualTo(0, "should handle empty string parameters");
        }

        /// <summary>
        /// Tests behavior when query returns DBNull values.
        /// </summary>
        /// <remarks>
        /// This test ensures proper conversion of SQL NULL values to .NET null values.
        /// DBNull handling is critical for applications working with nullable database columns,
        /// preventing InvalidCastException and ensuring proper null propagation in business logic.
        /// </remarks>
        [TestMethod]
        public void GetValue_DBNullResult_ShouldReturnNull()
        {
            // Act - Query that may return NULL
            var result = _dbAccess.GetValue<string>(
                "SELECT TOP 1 Description FROM TestEntities WHERE Description IS NULL");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            result.Should().BeNull("should return null for DBNull values");
        }

        /// <summary>
        /// Tests that very long query strings are handled properly.
        /// </summary>
        /// <remarks>
        /// This stress test validates that SQL Server can handle complex queries with many conditions.
        /// Long query strings can occur in dynamic query building scenarios, search filters,
        /// and reporting applications, so proper handling ensures application stability.
        /// </remarks>
        [TestMethod]
        public void GetValue_LongQueryString_ShouldHandleCorrectly()
        {
            // Arrange - Create a query with many OR conditions
            var queryParts = Enumerable.Range(1, 50)
                .Select(i => $"Name = 'NonExistent{i}'")
                .ToArray();
            var longQuery = "SELECT COUNT(*) FROM TestEntities WHERE " + string.Join(" OR ", queryParts);

            // Act
            var count = _dbAccess.GetValue<int>(longQuery);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().Be(0, "should handle long query strings");
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleans up any test data that might have been created during testing.
        /// </summary>
        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
                DatabaseTestHelper.CleanupTestEntities(dbAccess);
            }
            catch (Exception)
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }

        #endregion
    }
}