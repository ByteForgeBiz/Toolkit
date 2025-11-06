using ByteForge.Toolkit.Tests.Helpers;
using AwesomeAssertions;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace ByteForge.Toolkit.Tests.Unit.Data.Database
{
    /// <summary>
    /// Unit tests for the query and data retrieval methods of the <see cref="DBAccess"/> class using ODBC.
    /// </summary>
    /// <remarks>
    /// These tests focus on the core database operation methods including GetValue, GetRecord,
    /// GetRecords, ExecuteQuery, and their async variants. Tests use the TestAccess.accdb Access database
    /// via ODBC to ensure proper integration and data handling for ODBC connections.
    /// </remarks>
    [TestClass]
    public class DBAccessMethodsODBCTests
    {
        #region Test Fields and Setup

        private DBAccess _dbAccess;

        /// <summary>
        /// Verifies that the test Access database is properly configured before running tests.
        /// </summary>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Verify test Access database is accessible via ODBC
            var dbAccess = DatabaseTestHelper.CreateTestAccessDBAccess();
            DatabaseTestHelper.AssertAccessDatabaseSetup(dbAccess);
        }

        /// <summary>
        /// Sets up a fresh DBAccess instance for each test using ODBC connection to Access.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            _dbAccess = DatabaseTestHelper.CreateTestAccessDBAccess();
        }

        /// <summary>
        /// Cleans up the DBAccess instance after each test.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            // Note: DBAccess is not IDisposable, no cleanup needed
        }

        #endregion

        #region TestConnection Tests

        /// <summary>
        /// Tests that TestConnection returns true for a valid Access database connection.
        /// </summary>
        /// <remarks>
        /// This test validates ODBC connectivity to Access databases, ensuring proper driver configuration
        /// and database file accessibility. ODBC connections to Access require proper driver installation
        /// and file permissions, making this test crucial for environment validation.
        /// </remarks>
        [TestMethod]
        public void TestConnection_ValidAccessDatabase_ShouldReturnTrue()
        {
            // Act
            var result = _dbAccess.TestConnection();

            // Assert
            _dbAccess.LastException.Should().BeNull("no exception should occur for valid ODBC connection");
            result.Should().BeTrue("TestConnection should return true for valid Access database");
        }

        /// <summary>
        /// Tests that TestConnection returns false for an invalid ODBC connection.
        /// </summary>
        /// <remarks>
        /// This test ensures graceful handling of ODBC connection failures, which can occur due to
        /// missing drivers, invalid file paths, or access permission issues. Proper error handling
        /// is critical for ODBC connections as they often involve external file dependencies.
        /// </remarks>
        [TestMethod]
        public void TestConnection_InvalidODBCDatabase_ShouldReturnFalse()
        {
            // Arrange
            var invalidOptions = DatabaseTestHelper.CreateInvalidAccessDatabaseOptions();
            var invalidDbAccess = new DBAccess(invalidOptions);

            // Act
            var result = invalidDbAccess.TestConnection();

            // Assert
            invalidDbAccess.LastException.Should().NotBeNull("exception should be set for invalid ODBC connection");
            result.Should().BeFalse("TestConnection should return false for invalid ODBC database");
        }

        /// <summary>
        /// Tests the async version of TestConnection with Access database.
        /// </summary>
        /// <remarks>
        /// This test validates asynchronous ODBC connection testing with Access databases.
        /// Async ODBC operations are important for preventing UI freezing when testing
        /// file-based database connections that may involve disk I/O delays.
        /// </remarks>
        [TestMethod]
        public async Task TestConnectionAsync_ValidAccessDatabase_ShouldReturnTrue()
        {
            // Act
            var result = await _dbAccess.TestConnectionAsync();

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            result.Should().BeTrue("TestConnectionAsync should return true for valid Access database");
        }

        #endregion

        #region GetValue Tests

        /// <summary>
        /// Tests that GetValue returns the correct scalar value from Access database.
        /// </summary>
        /// <remarks>
        /// This test validates scalar value retrieval through ODBC with Access databases.
        /// Access databases have specific data type mappings and query syntax differences
        /// compared to SQL Server, making this test important for cross-database compatibility.
        /// </remarks>
        [TestMethod]
        public void GetValue_SimpleScalarQuery_ShouldReturnCorrectValue()
        {
            // Act
            var count = _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterThan(0, "should return a positive count of test entities from Access");
        }

        /// <summary>
        /// Tests that GetValue with parameters works correctly with Access database.
        /// </summary>
        /// <remarks>
        /// This test ensures parameterized queries work correctly with ODBC and Access databases.
        /// Parameter binding with ODBC can behave differently than with native SQL Server connections,
        /// particularly with data type conversions and null value handling.
        /// </remarks>
        [TestMethod]
        public void GetValue_WithParameters_ShouldReturnCorrectValue()
        {
            // Act
            var activeCount = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE IsActive = @active", true);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            activeCount.Should().BeGreaterThan(0, "should return active entities count from Access");
        }

        /// <summary>
        /// Tests that GetValue returns null for queries that return no results in Access.
        /// </summary>
        /// <remarks>
        /// This test validates empty result handling with ODBC connections to Access databases.
        /// Access databases may handle null values and empty results differently than SQL Server,
        /// requiring specific validation to ensure consistent behavior across database types.
        /// </remarks>
        [TestMethod]
        public void GetValue_NoResults_ShouldReturnDefault()
        {
            // Act
            var name = _dbAccess.GetValue<string>(
                "SELECT Name FROM TestEntities WHERE ID = @id", -9999);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            name.Should().BeNull("should return null for non-existent record in Access");
        }

        /// <summary>
        /// Tests the generic TryGetValue method for successful operations with Access.
        /// </summary>
        /// <remarks>
        /// This test validates the Try-pattern implementation with ODBC and Access databases.
        /// The Try-pattern provides safer error handling for ODBC connections, which can be
        /// more prone to connectivity issues due to file-based nature of Access databases.
        /// </remarks>
        [TestMethod]
        public void TryGetValue_ValidQuery_ShouldReturnTrueAndValue()
        {
            // Act
            var success = _dbAccess.TryGetValue<int>(out var count, "SELECT COUNT(*) FROM TestEntities");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            success.Should().BeTrue("TryGetValue should return true for valid Access query");
            count.Should().BeGreaterThan(0, "should return a positive count from Access");
        }

        /// <summary>
        /// Tests TryGetValue with an invalid query in Access.
        /// </summary>
        /// <remarks>
        /// This test ensures proper error handling for invalid SQL with ODBC connections.
        /// Access databases have different SQL syntax rules and error messages compared to SQL Server,
        /// making robust error handling crucial for applications supporting multiple database types.
        /// </remarks>
        [TestMethod]
        public void TryGetValue_InvalidQuery_ShouldReturnFalseAndDefault()
        {
            // Act
            var success = _dbAccess.TryGetValue<int>(out var count, "SELECT * FROM NonExistentTable");

            // Assert
            _dbAccess.LastException.Should().NotBeNull("should have thrown exception for invalid query");
            success.Should().BeFalse("TryGetValue should return false for invalid Access query");
            count.Should().Be(0, "should return default value");
        }

        /// <summary>
        /// Tests the async version of GetValue with Access database.
        /// </summary>
        /// <remarks>
        /// This test validates asynchronous scalar value retrieval with ODBC and Access databases.
        /// Async operations are particularly beneficial for file-based databases like Access,
        /// where disk I/O operations can introduce latency in data retrieval operations.
        /// </remarks>
        [TestMethod]
        public async Task GetValueAsync_ValidQuery_ShouldReturnCorrectValue()
        {
            // Act
            var count = await _dbAccess.GetValueAsync<int>("SELECT COUNT(*) FROM TestEntities");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterThan(0, "async GetValue should return positive count from Access");
        }

        /// <summary>
        /// Tests the async version of TryGetValue with Access database.
        /// </summary>
        /// <remarks>
        /// This test combines async patterns with the Try-pattern for ODBC Access operations.
        /// This combination provides both performance benefits and robust error handling,
        /// essential for reliable data access in applications using file-based databases.
        /// </remarks>
        [TestMethod]
        public async Task TryGetValueAsync_ValidQuery_ShouldReturnSuccessAndValue()
        {
            // Act
            var (success, count) = await _dbAccess.TryGetValueAsync<int>("SELECT COUNT(*) FROM TestEntities");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            success.Should().BeTrue("async TryGetValue should return true for valid Access query");
            count.Should().BeGreaterThan(0, "should return positive count from Access");
        }

        #endregion

        #region GetRecord Tests

        /// <summary>
        /// Tests that GetRecord returns a single DataRow for a valid query in Access.
        /// </summary>
        /// <remarks>
        /// This test validates single record retrieval through ODBC with Access databases.
        /// Access databases may have different column metadata and data type representations
        /// compared to SQL Server, requiring specific validation of DataRow population.
        /// </remarks>
        [TestMethod]
        public void GetRecord_ValidQuery_ShouldReturnDataRow()
        {
            // Act
            var record = _dbAccess.GetRecord("SELECT TOP 1 * FROM TestEntities WHERE IsActive = @active", true);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            record.Should().NotBeNull("should return a DataRow from Access");
            record.Table.Columns.Count.Should().BeGreaterThan(0, "DataRow should have columns");
            record["Name"].Should().NotBeNull("should have Name column");
        }

        /// <summary>
        /// Tests that GetRecord returns null when no records match the query in Access.
        /// </summary>
        /// <remarks>
        /// This test ensures consistent null-handling behavior for single record queries with ODBC.
        /// Access databases may handle empty result sets differently, making this validation
        /// important for maintaining consistent API behavior across database types.
        /// </remarks>
        [TestMethod]
        public void GetRecord_NoResults_ShouldReturnNull()
        {
            // Act
            var record = _dbAccess.GetRecord("SELECT * FROM TestEntities WHERE ID = @id", -9999);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            record.Should().BeNull("should return null for non-existent record in Access");
        }

        /// <summary>
        /// Tests the async version of GetRecord with Access database.
        /// </summary>
        /// <remarks>
        /// This test validates asynchronous single record retrieval with ODBC and Access databases.
        /// File-based databases like Access can benefit significantly from async operations
        /// to prevent blocking during disk I/O operations for record retrieval.
        /// </remarks>
        [TestMethod]
        public async Task GetRecordAsync_ValidQuery_ShouldReturnDataRow()
        {
            // Act
            var record = await _dbAccess.GetRecordAsync("SELECT TOP 1 * FROM TestEntities WHERE IsActive = True");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            record.Should().NotBeNull("async GetRecord should return a DataRow from Access");
            record.Table.Columns.Count.Should().BeGreaterThan(0, "DataRow should have columns");
        }

        #endregion

        #region GetRecords Tests

        /// <summary>
        /// Tests that GetRecords returns multiple records as DataRowCollection from Access.
        /// </summary>
        /// <remarks>
        /// This test validates bulk record retrieval through ODBC with Access databases.
        /// Multi-record queries with Access databases can have performance implications
        /// and different memory usage patterns compared to server-based databases.
        /// </remarks>
        [TestMethod]
        public void GetRecords_ValidQuery_ShouldReturnDataRowCollection()
        {
            // Act
            var records = _dbAccess.GetRecords("SELECT * FROM TestEntities WHERE IsActive = @active", true);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            records.Should().NotBeNull("should return DataRowCollection from Access");
            records.Count.Should().BeGreaterThan(0, "should return multiple records from Access");
        }

        /// <summary>
        /// Tests that GetRecords returns null when no records match the query in Access.
        /// </summary>
        /// <remarks>
        /// This test ensures consistent empty result handling for multi-record ODBC queries.
        /// The behavior returns an empty DataRowCollection rather than null, providing
        /// consistent iteration patterns regardless of the underlying database type.
        /// </remarks>
        [TestMethod]
        public void GetRecords_NoResults_ShouldReturnNull()
        {
            // Act
            var records = _dbAccess.GetRecords("SELECT * FROM TestEntities WHERE Name = @name", "NonExistentEntity");

            // Assert
            _dbAccess.LastException.Should().BeNull("No exception should occur for no results in Access");
            records.Should().NotBeNull("Should return an empty DataRowCollection instead of null for Access");
        }

        /// <summary>
        /// Tests the async version of GetRecords with Access database.
        /// </summary>
        /// <remarks>
        /// This test validates asynchronous bulk record retrieval with ODBC and Access databases.
        /// Async bulk operations are particularly important for Access databases where
        /// file I/O operations can introduce significant latency for large result sets.
        /// </remarks>
        [TestMethod]
        public async Task GetRecordsAsync_ValidQuery_ShouldReturnDataRowCollection()
        {
            // Act
            var records = await _dbAccess.GetRecordsAsync("SELECT * FROM TestEntities WHERE IsActive = True");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            records.Should().NotBeNull("async GetRecords should return DataRowCollection from Access");
            records.Count.Should().BeGreaterThan(0, "should return multiple records from Access");
        }

        #endregion

        #region ExecuteQuery Tests

        /// <summary>
        /// Tests that ExecuteQuery successfully executes an INSERT statement in Access.
        /// </summary>
        /// <remarks>
        /// This test validates data insertion through ODBC with Access databases.
        /// Access databases have specific constraints on data types, field lengths, and
        /// concurrent access that require careful validation for reliable data insertion.
        /// </remarks>
        [TestMethod]
        public void ExecuteQuery_InsertStatement_ShouldReturnTrueAndUpdateRecordsAffected()
        {
            // Arrange
            var testName = DatabaseTestHelper.GenerateTestString("AccessInsert");
            var testDesc = "Test record for ExecuteQuery method in Access";

            try
            {
                // Act
                var result = _dbAccess.ExecuteQuery(
                    "INSERT INTO TestEntities (Name, Description, IsActive, TestValue) VALUES (@name, @desc, @active, @value)",
                    testName, testDesc, true, 123.45m);

                // Assert
                _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
                result.Should().BeTrue("ExecuteQuery should return true for successful INSERT in Access");
                _dbAccess.RecordsAffected.Should().Be(1, "should affect exactly 1 record in Access");

                // Verify the record was inserted
                var insertedName = _dbAccess.GetValue<string>("SELECT Name FROM TestEntities WHERE Name = @name", testName);
                insertedName.Should().Be(testName, "record should be inserted correctly in Access");
            }
            finally
            {
                // Cleanup
                DatabaseTestHelper.CleanupTestData(_dbAccess, "TestEntities", $"Name = '{testName}'");
            }
        }

        /// <summary>
        /// Tests that ExecuteQuery successfully executes an UPDATE statement in Access.
        /// </summary>
        /// <remarks>
        /// This test ensures data modification works correctly through ODBC with Access databases.
        /// UPDATE operations in Access databases require careful handling of record locking
        /// and file-based transaction management to ensure data integrity.
        /// </remarks>
        [TestMethod]
        public void ExecuteQuery_UpdateStatement_ShouldReturnTrueAndUpdateRecordsAffected()
        {
            // Arrange - Insert a test record first
            var testName = DatabaseTestHelper.GenerateTestString("AccessUpdate");
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
                result.Should().BeTrue("ExecuteQuery should return true for successful UPDATE in Access");
                _dbAccess.RecordsAffected.Should().Be(1, "should affect exactly 1 record in Access");

                // Verify the record was updated
                var actualDesc = _dbAccess.GetValue<string>("SELECT Description FROM TestEntities WHERE Name = @name", testName);
                actualDesc.Should().Be(updatedDesc, "record should be updated correctly in Access");
            }
            finally
            {
                // Cleanup
                DatabaseTestHelper.CleanupTestData(_dbAccess, "TestEntities", $"Name = '{testName}'");
            }
        }

        /// <summary>
        /// Tests that ExecuteQuery successfully executes a DELETE statement in Access.
        /// </summary>
        /// <remarks>
        /// This test validates data deletion through ODBC with Access databases.
        /// DELETE operations in file-based databases like Access require proper handling
        /// of record locking and database compaction considerations for optimal performance.
        /// </remarks>
        [TestMethod]
        public void ExecuteQuery_DeleteStatement_ShouldReturnTrueAndUpdateRecordsAffected()
        {
            // Arrange - Insert a test record first
            var testName = DatabaseTestHelper.GenerateTestString("AccessDelete");
            _dbAccess.ExecuteQuery(
                "INSERT INTO TestEntities (Name, Description, IsActive) VALUES (@name, @desc, @active)",
                testName, "Record to be deleted", true);

            // Act - Delete the record
            var result = _dbAccess.ExecuteQuery("DELETE FROM TestEntities WHERE Name = @name", testName);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            result.Should().BeTrue("ExecuteQuery should return true for successful DELETE in Access");
            _dbAccess.RecordsAffected.Should().Be(1, "should affect exactly 1 record in Access");

            // Verify the record was deleted
            var count = _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities WHERE Name = @name", testName);
            count.Should().Be(0, "record should be deleted from Access");
        }

        /// <summary>
        /// Tests ExecuteQuery with an invalid SQL statement in Access.
        /// </summary>
        /// <remarks>
        /// This test ensures robust error handling for malformed SQL with ODBC connections.
        /// Access databases have different SQL syntax support and error reporting compared
        /// to SQL Server, requiring specific validation of error handling mechanisms.
        /// </remarks>
        [TestMethod]
        public void ExecuteQuery_InvalidSQL_ShouldReturnFalseAndSetException()
        {
            // Act
            var result = _dbAccess.ExecuteQuery("INVALID SQL STATEMENT");

            // Assert
            _dbAccess.LastException.Should().NotBeNull("exception should be set for invalid SQL in Access");
            result.Should().BeFalse("ExecuteQuery should return false for invalid SQL in Access");
        }

        /// <summary>
        /// Tests the async version of ExecuteQuery with Access database.
        /// </summary>
        /// <remarks>
        /// This test validates asynchronous data modification through ODBC with Access databases.
        /// Async INSERT operations are particularly beneficial for Access databases where
        /// file I/O and locking mechanisms can introduce latency in data modification operations.
        /// </remarks>
        [TestMethod]
        public async Task ExecuteQueryAsync_ValidStatement_ShouldReturnTrue()
        {
            // Arrange
            var testName = DatabaseTestHelper.GenerateTestString("AccessAsync");

            try
            {
                // Act
                var result = await _dbAccess.ExecuteQueryAsync(
                    "INSERT INTO TestEntities (Name, Description, IsActive) VALUES (@name, @desc, @active)",
                    testName, "Async test record", true);

                // Assert
                _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
                result.Should().BeTrue("async ExecuteQuery should return true for valid statement in Access");
                _dbAccess.RecordsAffected.Should().Be(1, "should affect exactly 1 record in Access");
            }
            finally
            {
                // Cleanup
                DatabaseTestHelper.CleanupTestData(_dbAccess, "TestEntities", $"Name = '{testName}'");
            }
        }

        #endregion

        #region ODBC-Specific Parameter Handling Tests

        /// <summary>
        /// Tests that queries with multiple ODBC parameters work correctly.
        /// </summary>
        /// <remarks>
        /// This test validates complex parameter binding with ODBC drivers for Access databases.
        /// ODBC parameter handling can differ from native database connections, particularly
        /// with data type conversions and parameter ordering for complex queries.
        /// </remarks>
        [TestMethod]
        public void GetValue_MultipleODBCParameters_ShouldHandleCorrectly()
        {
            // Act
            var count = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE IsActive = @active AND TestValue >= @minValue AND TestValue <= @maxValue",
                true, 0m, 1000m);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterThanOrEqualTo(0, "should return a valid count with ODBC parameters");
        }

        /// <summary>
        /// Tests that null parameters are handled correctly in Access via ODBC.
        /// </summary>
        /// <remarks>
        /// This test ensures proper null value handling through ODBC drivers with Access databases.
        /// ODBC connections may handle null values differently than native connections,
        /// requiring specific validation to ensure consistent null value processing.
        /// </remarks>
        [TestMethod]
        public void GetValue_NullODBCParameter_ShouldHandleCorrectly()
        {
            // Act
            var count = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE Description IS NULL OR @desc IS NULL",
                (object)null);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterThan(0, "should handle null ODBC parameters correctly");
        }

        /// <summary>
        /// Tests that various data types as ODBC parameters work correctly.
        /// </summary>
        /// <remarks>
        /// This test validates data type conversion and storage through ODBC with Access databases.
        /// Access databases have specific data type limitations and conversion rules that differ
        /// from SQL Server, particularly for GUID, DateTime, and Decimal types.
        /// </remarks>
        [TestMethod]
        public void ExecuteQuery_VariousDataTypesODBC_ShouldHandleCorrectly()
        {
            // Arrange
            var testName = DatabaseTestHelper.GenerateTestString("AccessDataTypes");
            var testGuid = Guid.NewGuid().ToString(); // Access stores GUIDs as strings
            var testDate = DateTime.Now.Date;
            var testDecimal = 999.99m;
            var testBool = true;

            try
            {
                // Act
                var result = _dbAccess.ExecuteQuery(
                    "INSERT INTO TestEntities (Name, Description, IsActive, TestValue, TestGuid, CreatedDate) VALUES (@name, @desc, @active, @value, @guid, @date)",
                    testName, "Data type test", testBool, testDecimal, testGuid, testDate);

                // Assert
                _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
                result.Should().BeTrue("should handle various data types as ODBC parameters");
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
        /// Tests that simple queries execute within reasonable time limits in Access.
        /// </summary>
        /// <remarks>
        /// This performance test validates that ODBC operations with Access databases meet
        /// acceptable response times. File-based databases typically have slower performance
        /// than server-based databases, requiring adjusted performance expectations.
        /// </remarks>
        [TestMethod]
        public void GetValue_SimpleAccessQuery_ShouldExecuteQuickly()
        {
            // Act & Assert
            DatabaseTestHelper.AssertExecutionTime(
                () => _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities"),
                maxMilliseconds: 2000, // Access may be slower than SQL Server
                operationName: "Simple count query in Access");
        }

        /// <summary>
        /// Tests that parameterized queries don't significantly impact performance in Access.
        /// </summary>
        /// <remarks>
        /// This test ensures that ODBC parameter binding doesn't introduce excessive overhead
        /// with Access databases. Parameterized queries through ODBC can have different
        /// performance characteristics compared to native database connections.
        /// </remarks>
        [TestMethod]
        public void GetValue_ParameterizedAccessQuery_ShouldExecuteQuickly()
        {
            // Act & Assert
            DatabaseTestHelper.AssertExecutionTime(
                () => _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities WHERE IsActive = @active", true),
                maxMilliseconds: 2000, // Access may be slower than SQL Server
                operationName: "Parameterized count query in Access");
        }

        #endregion

        #region Edge Cases and Access-Specific Tests

        /// <summary>
        /// Tests behavior with empty string parameters in Access.
        /// </summary>
        /// <remarks>
        /// This edge case test validates empty string handling through ODBC with Access databases.
        /// Access databases may treat empty strings differently than other database systems,
        /// particularly in comparison operations and null coalescing scenarios.
        /// </remarks>
        [TestMethod]
        public void GetValue_EmptyStringParameterAccess_ShouldHandleCorrectly()
        {
            // Act
            var count = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE Name = @name OR @name = ''", "");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterThanOrEqualTo(0, "should handle empty string parameters in Access");
        }

        /// <summary>
        /// Tests behavior when Access query returns null values.
        /// </summary>
        /// <remarks>
        /// This test ensures proper null value conversion from Access databases through ODBC.
        /// ODBC drivers may handle null value representation differently, requiring validation
        /// to ensure consistent null value processing in .NET applications.
        /// </remarks>
        [TestMethod]
        public void GetValue_AccessNullResult_ShouldReturnNull()
        {
            // Act - Query that may return NULL in Access
            var result = _dbAccess.GetValue<string>(
                "SELECT TOP 1 Description FROM TestEntities WHERE Description IS NULL");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            result.Should().BeNull("should return null for NULL values in Access");
        }

        /// <summary>
        /// Tests that Access-specific date format handling works correctly.
        /// </summary>
        /// <remarks>
        /// This test validates date/time handling through ODBC with Access databases.
        /// Access databases have specific date/time storage formats and range limitations
        /// that require careful validation to ensure proper date parameter handling.
        /// </remarks>
        [TestMethod]
        public void GetValue_AccessDateHandling_ShouldWorkCorrectly()
        {
            // Act
            var count = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE CreatedDate >= @date", DateTime.Today);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterThanOrEqualTo(0, "should handle date parameters correctly in Access");
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
                var dbAccess = DatabaseTestHelper.CreateTestAccessDBAccess();
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