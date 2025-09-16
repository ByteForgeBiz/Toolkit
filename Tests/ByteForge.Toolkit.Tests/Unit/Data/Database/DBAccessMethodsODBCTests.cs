using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;

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
        [TestMethod]
        public void GetValue_MultipleODBCParameters_ShouldHandleCorrectly()
        {
            // Act
            var count = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE IsActive = @active AND TestValue >= @minValue AND TestValue <= @maxValue",
                true, 0m, 1000m);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterOrEqualTo(0, "should return a valid count with ODBC parameters");
        }

        /// <summary>
        /// Tests that null parameters are handled correctly in Access via ODBC.
        /// </summary>
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
        [TestMethod]
        public void GetValue_EmptyStringParameterAccess_ShouldHandleCorrectly()
        {
            // Act
            var count = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE Name = @name OR @name = ''", "");

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterOrEqualTo(0, "should handle empty string parameters in Access");
        }

        /// <summary>
        /// Tests behavior when Access query returns null values.
        /// </summary>
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
        [TestMethod]
        public void GetValue_AccessDateHandling_ShouldWorkCorrectly()
        {
            // Act
            var count = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE CreatedDate >= @date", DateTime.Today);

            // Assert
            _dbAccess.LastException.Should().BeNull($"should not have thrown exception: {_dbAccess.LastException}");
            count.Should().BeGreaterOrEqualTo(0, "should handle date parameters correctly in Access");
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleans up any test data that might have been created during testing.
        /// </summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
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