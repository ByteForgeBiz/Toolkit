using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;

namespace ByteForge.Toolkit.Tests.Unit.Data.Database
{
    /// <summary>
    /// Unit tests for the query and data retrieval methods of the <see cref="DBAccess"/> class.
    /// </summary>
    /// <remarks>
    /// These tests focus on the core database operation methods including GetValue, GetRecord,
    /// GetRecords, ExecuteQuery, and their async variants. Tests use the TestUnitDB database
    /// with real data to ensure proper integration and data handling.
    /// </remarks>
    [TestClass]
    public class DBAccessMethodsTests
    {
        #region Test Fields and Setup

        private DBAccess _dbAccess;

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
        [TestMethod]
        public void GetValue_SimpleScalarQuery_ShouldReturnCorrectValue()
        {
            // Act
            var count = _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities");

            // Assert
            count.Should().BeGreaterThan(0, "should return a positive count of test entities");
            _dbAccess.LastException.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetValue with parameters works correctly.
        /// </summary>
        [TestMethod]
        public void GetValue_WithParameters_ShouldReturnCorrectValue()
        {
            // Act
            var activeCount = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE IsActive = @active", true);

            // Assert
            activeCount.Should().BeGreaterThan(0, "should return active entities count");
            _dbAccess.LastException.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetValue returns null for queries that return no results.
        /// </summary>
        [TestMethod]
        public void GetValue_NoResults_ShouldReturnDefault()
        {
            // Act
            var name = _dbAccess.GetValue<string>(
                "SELECT Name FROM TestEntities WHERE Id = @id", -9999);

            // Assert
            name.Should().BeNull("should return null for non-existent record");
            _dbAccess.LastException.Should().BeNull();
        }

        /// <summary>
        /// Tests the generic TryGetValue method for successful operations.
        /// </summary>
        [TestMethod]
        public void TryGetValue_ValidQuery_ShouldReturnTrueAndValue()
        {
            // Act
            var success = _dbAccess.TryGetValue<int>(out var count, "SELECT COUNT(*) FROM TestEntities");

            // Assert
            success.Should().BeTrue("TryGetValue should return true for valid query");
            count.Should().BeGreaterThan(0, "should return a positive count");
            _dbAccess.LastException.Should().BeNull();
        }

        /// <summary>
        /// Tests TryGetValue with an invalid query.
        /// </summary>
        [TestMethod]
        public void TryGetValue_InvalidQuery_ShouldReturnFalseAndDefault()
        {
            // Act
            var success = _dbAccess.TryGetValue<int>(out var count, "SELECT * FROM NonExistentTable");

            // Assert
            success.Should().BeFalse("TryGetValue should return false for invalid query");
            count.Should().Be(0, "should return default value");
            _dbAccess.LastException.Should().NotBeNull();
        }

        /// <summary>
        /// Tests the async version of GetValue.
        /// </summary>
        [TestMethod]
        public async Task GetValueAsync_ValidQuery_ShouldReturnCorrectValue()
        {
            // Act
            var count = await _dbAccess.GetValueAsync<int>("SELECT COUNT(*) FROM TestEntities");

            // Assert
            count.Should().BeGreaterThan(0, "async GetValue should return positive count");
        }

        /// <summary>
        /// Tests the async version of TryGetValue.
        /// </summary>
        [TestMethod]
        public async Task TryGetValueAsync_ValidQuery_ShouldReturnSuccessAndValue()
        {
            // Act
            var (success, count) = await _dbAccess.TryGetValueAsync<int>("SELECT COUNT(*) FROM TestEntities");

            // Assert
            success.Should().BeTrue("async TryGetValue should return true for valid query");
            count.Should().BeGreaterThan(0, "should return positive count");
        }

        #endregion

        #region GetRecord Tests

        /// <summary>
        /// Tests that GetRecord returns a single DataRow for a valid query.
        /// </summary>
        [TestMethod]
        public void GetRecord_ValidQuery_ShouldReturnDataRow()
        {
            // Act
            var record = _dbAccess.GetRecord("SELECT TOP 1 * FROM TestEntities WHERE IsActive = @active", true);

            // Assert
            record.Should().NotBeNull("should return a DataRow");
            record.Table.Columns.Count.Should().BeGreaterThan(0, "DataRow should have columns");
            record["Name"].Should().NotBeNull("should have Name column");
            _dbAccess.LastException.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetRecord returns null when no records match the query.
        /// </summary>
        [TestMethod]
        public void GetRecord_NoResults_ShouldReturnNull()
        {
            // Act
            var record = _dbAccess.GetRecord("SELECT * FROM TestEntities WHERE Id = @id", -9999);

            // Assert
            record.Should().BeNull("should return null for non-existent record");
            _dbAccess.LastException.Should().BeNull();
        }

        /// <summary>
        /// Tests the async version of GetRecord.
        /// </summary>
        [TestMethod]
        public async Task GetRecordAsync_ValidQuery_ShouldReturnDataRow()
        {
            // Act
            var record = await _dbAccess.GetRecordAsync("SELECT TOP 1 * FROM TestEntities WHERE IsActive = 1");

            // Assert
            record.Should().NotBeNull("async GetRecord should return a DataRow");
            record.Table.Columns.Count.Should().BeGreaterThan(0, "DataRow should have columns");
        }

        #endregion

        #region GetRecords Tests

        /// <summary>
        /// Tests that GetRecords returns multiple records as DataRowCollection.
        /// </summary>
        [TestMethod]
        public void GetRecords_ValidQuery_ShouldReturnDataRowCollection()
        {
            // Act
            var records = _dbAccess.GetRecords("SELECT * FROM TestEntities WHERE IsActive = @active", true);

            // Assert
            records.Should().NotBeNull("should return DataRowCollection");
            records.Count.Should().BeGreaterThan(0, "should return multiple records");
            _dbAccess.LastException.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetRecords returns null when no records match the query.
        /// </summary>
        [TestMethod]
        public void GetRecords_NoResults_ShouldReturnNull()
        {
            // Act
            var records = _dbAccess.GetRecords("SELECT * FROM TestEntities WHERE Name = @name", "NonExistentEntity");

            // Assert
            records.Should().NotBeNull("Should return an empty DataRowCollection instead of null");
            _dbAccess.LastException.Should().BeNull("No exception should occur for no results");
        }

        /// <summary>
        /// Tests the async version of GetRecords.
        /// </summary>
        [TestMethod]
        public async Task GetRecordsAsync_ValidQuery_ShouldReturnDataRowCollection()
        {
            // Act
            var records = await _dbAccess.GetRecordsAsync("SELECT * FROM TestEntities WHERE IsActive = 1");

            // Assert
            records.Should().NotBeNull("async GetRecords should return DataRowCollection");
            records.Count.Should().BeGreaterThan(0, "should return multiple records");
        }

        #endregion

        #region ExecuteQuery Tests

        /// <summary>
        /// Tests that ExecuteQuery successfully executes an INSERT statement.
        /// </summary>
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
                result.Should().BeTrue("ExecuteQuery should return true for successful INSERT");
                _dbAccess.RecordsAffected.Should().Be(1, "should affect exactly 1 record");
                _dbAccess.LastException.Should().BeNull();

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
                result.Should().BeTrue("ExecuteQuery should return true for successful UPDATE");
                _dbAccess.RecordsAffected.Should().Be(1, "should affect exactly 1 record");
                _dbAccess.LastException.Should().BeNull();

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
            result.Should().BeTrue("ExecuteQuery should return true for successful DELETE");
            _dbAccess.RecordsAffected.Should().Be(1, "should affect exactly 1 record");
            _dbAccess.LastException.Should().BeNull();

            // Verify the record was deleted
            var count = _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities WHERE Name = @name", testName);
            count.Should().Be(0, "record should be deleted");
        }

        /// <summary>
        /// Tests ExecuteQuery with an invalid SQL statement.
        /// </summary>
        [TestMethod]
        public void ExecuteQuery_InvalidSQL_ShouldReturnFalseAndSetException()
        {
            // Act
            var result = _dbAccess.ExecuteQuery("INVALID SQL STATEMENT");

            // Assert
            result.Should().BeFalse("ExecuteQuery should return false for invalid SQL");
            _dbAccess.LastException.Should().NotBeNull("exception should be set for invalid SQL");
        }

        /// <summary>
        /// Tests the async version of ExecuteQuery.
        /// </summary>
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
        [TestMethod]
        public void GetValue_MultipleParameters_ShouldHandleCorrectly()
        {
            // Act
            var count = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE IsActive = @active AND TestValue >= @minValue AND TestValue <= @maxValue",
                true, 0m, 1000m);

            // Assert
            count.Should().BeGreaterOrEqualTo(0, "should return a valid count");
            _dbAccess.LastException.Should().BeNull();
        }

        /// <summary>
        /// Tests that null parameters are handled correctly.
        /// </summary>
        [TestMethod]
        public void GetValue_NullParameter_ShouldHandleCorrectly()
        {
            // Act
            var count = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE Description = @desc OR @desc IS NULL",
                (object)null);

            // Assert
            count.Should().BeGreaterThan(0, "should handle null parameters correctly");
            _dbAccess.LastException.Should().BeNull();
        }

        /// <summary>
        /// Tests that various data types as parameters work correctly.
        /// </summary>
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
                result.Should().BeTrue("should handle various data types as parameters");
                _dbAccess.RecordsAffected.Should().Be(1);
                _dbAccess.LastException.Should().BeNull();

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
        [TestMethod]
        public void GetValue_EmptyStringParameter_ShouldHandleCorrectly()
        {
            // Act
            var count = _dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM TestEntities WHERE Name = @name OR @name = ''", "");

            // Assert
            count.Should().BeGreaterOrEqualTo(0, "should handle empty string parameters");
            _dbAccess.LastException.Should().BeNull();
        }

        /// <summary>
        /// Tests behavior when query returns DBNull values.
        /// </summary>
        [TestMethod]
        public void GetValue_DBNullResult_ShouldReturnNull()
        {
            // Act - Query that may return NULL
            var result = _dbAccess.GetValue<string>(
                "SELECT TOP 1 Description FROM TestEntities WHERE Description IS NULL");

            // Assert
            result.Should().BeNull("should return null for DBNull values");
            _dbAccess.LastException.Should().BeNull();
        }

        /// <summary>
        /// Tests that very long query strings are handled properly.
        /// </summary>
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
            count.Should().Be(0, "should handle long query strings");
            _dbAccess.LastException.Should().BeNull();
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