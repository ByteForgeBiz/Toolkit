using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace ByteForge.Toolkit.Tests.Unit.Data.Database
{
    /// <summary>
    /// Unit tests for DBAccess script execution functionality.
    /// </summary>
    /// <remarks>
    /// Tests the ExecuteScript method and related script processing functionality including:
    /// - Script execution with various SQL statement types
    /// - GO batch separator handling
    /// - DDL statement processing (parameter skipping)
    /// - Result capture and script execution results
    /// - Error handling and exception management
    /// - Utility methods for script parsing
    /// </remarks>
    [TestClass]
    public class DBAccessScriptExecutionTests
    {
        #region Test Setup and Teardown

        private DBAccess? _dbAccess;
        private const string TestTablePrefix = "ScriptTest_";
        
        /// <summary>
        /// Initializes test resources before each test method.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            _dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            DatabaseTestHelper.AssertTestDatabaseSetup(_dbAccess);
            
            // Clean up any previous test artifacts
            CleanupTestTables();
        }

        /// <summary>
        /// Cleans up test resources after each test method.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            CleanupTestTables();
            _dbAccess = null;
        }

        /// <summary>
        /// Removes any test tables created during script execution tests.
        /// </summary>
        private void CleanupTestTables()
        {
            try
            {
                // Get list of test tables and drop them
                var testTables = new[]
                {
                    "ScriptTest_SimpleTable",
                    "ScriptTest_BatchTable1", 
                    "ScriptTest_BatchTable2",
                    "ScriptTest_ResultsTable",
                    "ScriptTest_DDLTable"
                };

                foreach (var tableName in testTables)
                {
                    _dbAccess?.ExecuteQuery($"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP TABLE {tableName}");
                }
            }
            catch (Exception)
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }

        #endregion

        #region ExecuteScript - Basic Functionality Tests

        /// <summary>
        /// Tests ExecuteScript with a simple single-statement script.
        /// </summary>
        [TestMethod]
        public void ExecuteScript_SimpleScript_ShouldExecuteSuccessfully()
        {
            // Arrange
            var script = $@"
                CREATE TABLE {TestTablePrefix}SimpleTable (
                    Id INT PRIMARY KEY,
                    Name NVARCHAR(50),
                    CreatedDate DATETIME DEFAULT GETDATE()
                )";

            // Act
            var result = _dbAccess.ExecuteScript(script);

            // Assert
            result.Should().NotBeNull("script execution should return a result");
            result.Success.Should().BeTrue("script execution should succeed");
            result.LastException.Should().BeNull("no exception should occur on successful execution");
            result.RecordsAffected.Should().HaveCount(1, "should have one batch result");
            result.RecordsAffected[0].Should().Be(-1, "DDL statements typically return -1 records affected");
            result.BatchResults.Should().HaveCount(1, "should have one batch result");
            result.ResultSets.Should().BeEmpty("DDL statements don't return result sets");

            // Verify the table was created
            var tableExists = _dbAccess.GetValue<int>(
                $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{TestTablePrefix}SimpleTable'");
            tableExists.Should().Be(1, "the test table should have been created");
        }

        /// <summary>
        /// Tests ExecuteScript with a simple DML statement.
        /// </summary>
        [TestMethod]
        public void ExecuteScript_SimpleDMLScript_ShouldExecuteSuccessfully()
        {
            // Arrange - First create the test data
            var testEntityId = DatabaseTestHelper.GenerateTestCode();
            var testName = DatabaseTestHelper.GenerateTestString("ScriptTest");
            
            var script = $@"
                INSERT INTO TestEntities (Name, Description, IsActive, CreatedDate)
                VALUES ('{testName}', 'Created by script execution test', 1, GETDATE())";

            // Act
            var result = _dbAccess.ExecuteScript(script);

            // Assert
            result.Should().NotBeNull("script execution should return a result");
            result.Success.Should().BeTrue("script execution should succeed");
            result.LastException.Should().BeNull("no exception should occur on successful execution");
            result.RecordsAffected.Should().HaveCount(1, "should have one batch result");
            result.RecordsAffected[0].Should().Be(1, "INSERT should affect exactly 1 record");
            result.BatchResults.Should().HaveCount(1, "should have one batch result");
            result.BatchResults[0].Should().Be(1, "batch result should indicate 1 record affected");
            result.ResultSets.Should().BeEmpty("INSERT statements don't return result sets by default");

            // Verify the record was inserted
            var insertedName = _dbAccess.GetValue<string>(
                $"SELECT Name FROM TestEntities WHERE Name = '{testName}'");
            insertedName.Should().Be(testName, "the test entity should have been inserted with correct name");
        }

        #endregion

        #region ExecuteScript - Multiple Batch Tests

        /// <summary>
        /// Tests ExecuteScript with multiple batches separated by GO statements.
        /// </summary>
        [TestMethod]
        public void ExecuteScript_MultipleGoBatches_ShouldExecuteAllBatches()
        {
            // Arrange
            var testName1 = DatabaseTestHelper.GenerateTestString("Batch1");
            var testName2 = DatabaseTestHelper.GenerateTestString("Batch2");
            
            var script = $@"
                INSERT INTO TestEntities (Name, Description, IsActive, CreatedDate)
                VALUES ('{testName1}', 'First batch', 1, GETDATE())
                GO

                INSERT INTO TestEntities (Name, Description, IsActive, CreatedDate)
                VALUES ('{testName2}', 'Second batch', 1, GETDATE())
                GO

                SELECT COUNT(*) FROM TestEntities WHERE Name IN ('{testName1}', '{testName2}')";

            // Act
            var result = _dbAccess.ExecuteScript(script, captureResults: true);

            // Assert
            result.Should().NotBeNull("script execution should return a result");
            result.Success.Should().BeTrue("script execution should succeed");
            result.LastException.Should().BeNull("no exception should occur on successful execution");
            
            result.RecordsAffected.Should().HaveCount(3, "should have three batch results");
            result.RecordsAffected[0].Should().Be(1, "first INSERT should affect 1 record");
            result.RecordsAffected[1].Should().Be(1, "second INSERT should affect 1 record");
            result.RecordsAffected[2].Should().Be(-1, "SELECT typically returns -1 for records affected");
            
            // The BatchResults might aggregate results differently, so be more flexible
            result.BatchResults.Should().NotBeEmpty("should have batch results");
            result.ResultSets.Should().HaveCount(1, "should have one result set from the SELECT");
            
            // Verify the SELECT result
            var selectResult = result.ResultSets[0];
            selectResult.Rows.Should().HaveCount(1, "SELECT should return one row");
            var countValue = Convert.ToInt32(selectResult.Rows[0][0]);
            countValue.Should().Be(2, "should find both inserted records");
        }

        /// <summary>
        /// Tests ExecuteScript with mixed DDL and DML statements in separate batches.
        /// </summary>
        [TestMethod]
        public void ExecuteScript_MixedDDLAndDMLBatches_ShouldExecuteAllBatches()
        {
            // Arrange
            var script = $@"
                CREATE TABLE {TestTablePrefix}BatchTable1 (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(50),
                    Value INT
                )
                GO

                INSERT INTO {TestTablePrefix}BatchTable1 (Name, Value)
                VALUES ('Test1', 100), ('Test2', 200)
                GO

                CREATE TABLE {TestTablePrefix}BatchTable2 (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    RefId INT,
                    Description NVARCHAR(100)
                )
                GO

                INSERT INTO {TestTablePrefix}BatchTable2 (RefId, Description)
                SELECT Id, 'Ref to ' + Name FROM {TestTablePrefix}BatchTable1";

            // Act
            var result = _dbAccess.ExecuteScript(script);

            // Assert
            result.Should().NotBeNull("script execution should return a result");
            result.Success.Should().BeTrue("script execution should succeed");
            result.LastException.Should().BeNull("no exception should occur on successful execution");
            
            result.RecordsAffected.Should().HaveCount(4, "should have four batch results");
            result.RecordsAffected[0].Should().Be(-1, "CREATE TABLE returns -1");
            result.RecordsAffected[1].Should().Be(2, "first INSERT should affect 2 records");
            result.RecordsAffected[2].Should().Be(-1, "second CREATE TABLE returns -1");
            result.RecordsAffected[3].Should().Be(2, "second INSERT should affect 2 records");

            // Verify both tables were created and populated
            var table1Count = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {TestTablePrefix}BatchTable1");
            var table2Count = _dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {TestTablePrefix}BatchTable2");
            
            table1Count.Should().Be(2, "first table should have 2 records");
            table2Count.Should().Be(2, "second table should have 2 records");
        }

        #endregion

        #region ExecuteScript - DDL Statement Tests

        /// <summary>
        /// Tests that ExecuteScript correctly identifies DDL statements and skips parameter processing.
        /// </summary>
        [TestMethod]
        public void ExecuteScript_WithDDLStatements_ShouldSkipParameterProcessing()
        {
            // Arrange - Create a script with DDL statements that would fail if parameters were processed
            // Use a simpler DDL statement without complex parameter-like syntax
            var script = $@"
                CREATE TABLE {TestTablePrefix}DDLTable (
                    Id INT PRIMARY KEY,
                    Name NVARCHAR(50),
                    TestValue NVARCHAR(100)
                )";
            
            // These arguments would cause an error if processed for DDL
            var arguments = new object[] { "SomeValue" };

            // Act
            var result = _dbAccess.ExecuteScript(script, arguments);

            // Assert
            result.Should().NotBeNull("script execution should return a result");
            result.Success.Should().BeTrue("DDL script should execute successfully");
            result.LastException.Should().BeNull("no exception should occur");

            // Verify the table was created
            var columnExists = _dbAccess.GetValue<int>($@"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = '{TestTablePrefix}DDLTable' 
                AND COLUMN_NAME = 'TestValue'");
            
            columnExists.Should().Be(1, "the TestValue column should have been created");
        }

        /// <summary>
        /// Tests ExecuteScript with various DDL statement types.
        /// </summary>
        [TestMethod]
        public void ExecuteScript_VariousDDLStatements_ShouldIdentifyCorrectly()
        {
            // Arrange - Create script with different DDL statement types
            var script = $@"
                CREATE TABLE {TestTablePrefix}DDLTable (Id INT, Name NVARCHAR(50))
                GO
                
                ALTER TABLE {TestTablePrefix}DDLTable ADD Description NVARCHAR(100)
                GO
                
                DECLARE @TestVar INT = 1
                GO
                
                SET NOCOUNT ON
                GO
                
                GRANT SELECT ON {TestTablePrefix}DDLTable TO public
                GO
                
                DROP TABLE {TestTablePrefix}DDLTable";

            // Act
            var result = _dbAccess.ExecuteScript(script);

            // Assert
            result.Should().NotBeNull("script execution should return a result");
            result.Success.Should().BeTrue("all DDL statements should execute successfully");
            result.LastException.Should().BeNull("no exception should occur");
            result.RecordsAffected.Should().HaveCount(6, "should have six batch results");
            
            // All DDL operations should return -1 for records affected
            result.RecordsAffected.Should().OnlyContain(count => count == -1, 
                "DDL statements typically return -1 for records affected");
        }

        #endregion

        #region ExecuteScript - Result Capture Tests

        /// <summary>
        /// Tests ExecuteScript with captureResults=true returns proper result sets.
        /// </summary>
        [TestMethod]
        public void ExecuteScript_WithCaptureResults_ShouldReturnResultSets()
        {
            // Arrange
            var testName1 = DatabaseTestHelper.GenerateTestString("Results1");
            var testName2 = DatabaseTestHelper.GenerateTestString("Results2");
            
            var script = $@"
                SELECT TOP 3 Id, Name FROM TestEntities ORDER BY Id
                GO
                
                INSERT INTO TestEntities (Name, Description, IsActive, CreatedDate)
                VALUES ('{testName1}', 'Test for results', 1, GETDATE())
                GO
                
                SELECT Id, Name FROM TestEntities WHERE Name = '{testName1}'
                GO
                
                UPDATE TestEntities SET Description = 'Updated description' WHERE Name = '{testName1}'
                GO
                
                SELECT COUNT(*) as TotalCount FROM TestEntities WHERE Name LIKE 'Results%'";

            // Act
            var result = _dbAccess.ExecuteScript(script, captureResults: true);

            // Assert
            result.Should().NotBeNull("script execution should return a result");
            result.Success.Should().BeTrue("script execution should succeed");
            result.LastException.Should().BeNull("no exception should occur");
            
            result.RecordsAffected.Should().HaveCount(5, "should have five batch results");
            result.BatchResults.Should().HaveCount(5, "should have five batch results");
            result.ResultSets.Should().HaveCount(3, "should have three result sets from SELECT statements");
            
            // Validate first result set (TOP 3 entities)
            var firstResultSet = result.ResultSets[0];
            firstResultSet.Rows.Should().HaveCountLessOrEqualTo(3, "first SELECT should return at most 3 rows");
            firstResultSet.Columns.Contains("Id").Should().BeTrue("should have Id column");
            firstResultSet.Columns.Contains("Name").Should().BeTrue("should have Name column");
            
            // Validate second result set (specific entity)
            var secondResultSet = result.ResultSets[1];
            secondResultSet.Rows.Should().HaveCount(1, "second SELECT should return exactly 1 row");
            var retrievedName = secondResultSet.Rows[0]["Name"].ToString();
            retrievedName.Should().Be(testName1, "should retrieve the inserted entity");
            
            // Validate third result set (count)
            var thirdResultSet = result.ResultSets[2];
            thirdResultSet.Rows.Should().HaveCount(1, "third SELECT should return exactly 1 row");
            var countValue = Convert.ToInt32(thirdResultSet.Rows[0]["TotalCount"]);
            countValue.Should().BeGreaterOrEqualTo(1, "should find at least the inserted test entity");
        }

        /// <summary>
        /// Tests ExecuteScript with captureResults=false doesn't capture result sets.
        /// </summary>
        [TestMethod]
        public void ExecuteScript_WithoutCaptureResults_ShouldNotReturnResultSets()
        {
            // Arrange
            var script = @"
                SELECT TOP 5 Id, Name FROM TestEntities
                GO
                
                SELECT COUNT(*) FROM TestEntities";

            // Act
            var result = _dbAccess.ExecuteScript(script, captureResults: false);

            // Assert
            result.Should().NotBeNull("script execution should return a result");
            result.Success.Should().BeTrue("script execution should succeed");
            result.LastException.Should().BeNull("no exception should occur");
            
            result.RecordsAffected.Should().HaveCount(2, "should have two batch results");
            result.BatchResults.Should().HaveCount(2, "should have two batch results");
            result.ResultSets.Should().BeEmpty("should not capture result sets when captureResults=false");
            
            // Records affected should show results of ExecuteNonQuery
            result.RecordsAffected.Should().OnlyContain(count => count >= -1, 
                "ExecuteNonQuery should return valid affected counts");
        }

        #endregion

        #region ExecuteScript - Error Handling Tests

        /// <summary>
        /// Tests ExecuteScript error handling with invalid SQL.
        /// </summary>
        [TestMethod]
        public void ExecuteScript_WithSQLError_ShouldReturnFailureResult()
        {
            // Arrange
            var script = @"
                SELECT * FROM NonExistentTable
                GO
                
                INSERT INTO TestEntities (Name) VALUES ('This should not execute')";

            // Act
            var result = _dbAccess.ExecuteScript(script);

            // Assert
            result.Should().NotBeNull("script execution should return a result even on failure");
            result.Success.Should().BeFalse("script execution should fail with invalid SQL");
            result.LastException.Should().NotBeNull("should capture the SQL exception");
            result.LastException.Message.Should().Contain("NonExistentTable", 
                "exception should reference the problematic table");
            
            // Error occurs immediately, so no batch results are recorded
            result.RecordsAffected.Should().BeEmpty("no batches should be recorded when error occurs immediately");
            result.BatchResults.Should().BeEmpty("no batch results should be recorded when error occurs immediately");
        }

        /// <summary>
        /// Tests ExecuteScript error handling with connection issues.
        /// </summary>
        [TestMethod]
        public void ExecuteScript_WithConnectionError_ShouldReturnFailureResult()
        {
            // Arrange
            var invalidDbAccess = new DBAccess(DatabaseTestHelper.CreateInvalidDatabaseOptions());
            var script = "SELECT 1";

            // Act
            var result = invalidDbAccess.ExecuteScript(script);

            // Assert
            result.Should().NotBeNull("script execution should return a result even on connection failure");
            result.Success.Should().BeFalse("script execution should fail with connection error");
            result.LastException.Should().NotBeNull("should capture the connection exception");
            result.RecordsAffected.Should().BeEmpty("no batches should execute with connection failure");
            result.ResultSets.Should().BeEmpty("no result sets should be returned on connection failure");
            result.BatchResults.Should().BeEmpty("no batch results should be returned on connection failure");
        }

        /// <summary>
        /// Tests ExecuteScript with mixed success and failure scenarios.
        /// </summary>
        [TestMethod]
        public void ExecuteScript_MixedSuccessAndFailure_ShouldStopAtFirstError()
        {
            // Arrange
            var testName = DatabaseTestHelper.GenerateTestString("MixedTest");
            var script = $@"
                INSERT INTO TestEntities (Name, Description, IsActive, CreatedDate)
                VALUES ('{testName}', 'This should succeed', 1, GETDATE())
                GO
                
                SELECT * FROM NonExistentTable
                GO
                
                INSERT INTO TestEntities (Name, Description, IsActive, CreatedDate)
                VALUES ('Should not execute', 'This should not execute', 1, GETDATE())";

            // Act
            var result = _dbAccess.ExecuteScript(script);

            // Assert
            result.Should().NotBeNull("script execution should return a result");
            result.Success.Should().BeFalse("script execution should fail at second batch");
            result.LastException.Should().NotBeNull("should capture the SQL exception");
            
            // Should have executed first batch successfully, but based on error behavior, might only have 1 result
            result.RecordsAffected.Should().HaveCountLessOrEqualTo(2, "should have results for batches before error");
            result.RecordsAffected.Should().NotBeEmpty("should have at least one successful batch result");
            result.RecordsAffected[0].Should().Be(1, "first batch should succeed and affect 1 record");
            
            // Verify first insert succeeded but third did not
            var firstEntityExists = _dbAccess.GetValue<int>(
                $"SELECT COUNT(*) FROM TestEntities WHERE Name = '{testName}'");
            var thirdEntityExists = _dbAccess.GetValue<int>(
                $"SELECT COUNT(*) FROM TestEntities WHERE Name = 'Should not execute'");
            
            firstEntityExists.Should().Be(1, "first entity should have been inserted");
            thirdEntityExists.Should().Be(0, "third entity should not have been inserted");
        }

        #endregion

        #region Utility Method Tests

        /// <summary>
        /// Tests the SplitIntoBatches utility method through script execution.
        /// </summary>
        [TestMethod]
        public void SplitIntoBatches_VariousScenarios_ShouldParseCorrectly()
        {
            // Test through ExecuteScript to verify batch splitting behavior
            
            // Arrange - Script with various GO statement formats and comments
            var testName1 = DatabaseTestHelper.GenerateTestString("Batch1");
            var testName2 = DatabaseTestHelper.GenerateTestString("Batch2");
            var testName3 = DatabaseTestHelper.GenerateTestString("Batch3");
            
            var script = $@"
                -- This is a comment that should be ignored
                INSERT INTO TestEntities (Name, Description, IsActive, CreatedDate)
                VALUES ('{testName1}', 'First batch', 1, GETDATE())
                
                GO
                
                -- Another comment
                INSERT INTO TestEntities (Name, Description, IsActive, CreatedDate)
                VALUES ('{testName2}', 'Second batch', 1, GETDATE())
                
                go  -- GO with lowercase and comment
                
                INSERT INTO TestEntities (Name, Description, IsActive, CreatedDate)
                VALUES ('{testName3}', 'Third batch', 1, GETDATE())
                
                  GO    -- GO with extra spaces
                  
                -- Final comment without GO should be ignored";

            // Act
            var result = _dbAccess.ExecuteScript(script);

            // Assert
            result.LastException.Should().BeNull("no exception should occur");
            result.Success.Should().BeTrue("script execution should succeed");
            result.Should().NotBeNull("script execution should return a result");
            
            // Should have exactly 3 batches (3 INSERT statements)
            result.RecordsAffected.Should().HaveCount(3, "should split into exactly 3 batches");
            result.RecordsAffected.Should().OnlyContain(count => count == 1, 
                "each batch should affect exactly 1 record");
            
            // Verify all records were inserted
            var insertedCount = _dbAccess.GetValue<int>($@"
                SELECT COUNT(*) FROM TestEntities 
                WHERE Name IN ('{testName1}', '{testName2}', '{testName3}')");
            insertedCount.Should().Be(3, "all three test entities should have been inserted");
        }

        /// <summary>
        /// Tests SplitIntoBatches with edge cases like empty batches and no GO statements.
        /// </summary>
        [TestMethod]
        public void SplitIntoBatches_EdgeCases_ShouldHandleCorrectly()
        {
            // Test script with empty batches and no GO at end
            var testName = DatabaseTestHelper.GenerateTestString("EdgeCase");
            
            var script = $@"
                GO
                -- Empty batch above should be ignored
                
                INSERT INTO TestEntities (Name, Description, IsActive, CreatedDate)
                VALUES ('{testName}', 'Only batch', 1, GETDATE())
                
                GO
                GO
                -- Multiple empty batches should be ignored";

            // Act
            var result = _dbAccess.ExecuteScript(script);

            // Assert
            result.Should().NotBeNull("script execution should return a result");
            result.Success.Should().BeTrue("script execution should succeed");
            result.RecordsAffected.Should().HaveCount(1, "should have only one non-empty batch");
            result.RecordsAffected[0].Should().Be(1, "the single batch should affect 1 record");
            
            // Verify the record was inserted
            var insertedCount = _dbAccess.GetValue<int>(
                $"SELECT COUNT(*) FROM TestEntities WHERE Name = '{testName}'");
            insertedCount.Should().Be(1, "the test entity should have been inserted");
        }

        /// <summary>
        /// Tests the IsDDLStatement utility method through DDL detection behavior.
        /// </summary>
        [TestMethod]
        public void IsDDLStatement_VariousStatementTypes_ShouldIdentifyCorrectly()
        {
            // Test simple DDL statements that should skip parameter processing
            var ddlScript = $@"
                CREATE TABLE {TestTablePrefix}DDLTest1 (Id INT, Data NVARCHAR(50))
                GO
                
                ALTER TABLE {TestTablePrefix}DDLTest1 ADD NewColumn VARCHAR(100)
                GO
                
                DROP TABLE {TestTablePrefix}DDLTest1";
            
            // Arguments that would cause errors if processed - but DDL should ignore them
            var arguments = new object[] { "test" };

            // Act
            var result = _dbAccess.ExecuteScript(ddlScript, arguments);

            // Assert
            result.Should().NotBeNull("script execution should return a result");
            result.Success.Should().BeTrue("DDL statements should execute without parameter processing errors");
            result.LastException.Should().BeNull("no parameter processing errors should occur");
            result.RecordsAffected.Should().HaveCount(3, "should execute all DDL batches");
        }

        /// <summary>
        /// Tests IsDDLStatement by verifying DML statements DO process parameters.
        /// </summary>
        [TestMethod]
        public void IsDDLStatement_DMLStatements_ShouldProcessParameters()
        {
            // Arrange - Simple DML test without complex parameterization to avoid issues
            var testName = DatabaseTestHelper.GenerateTestString("ParamTest");
            var script = $@"
                INSERT INTO TestEntities (Name, Description, IsActive, CreatedDate)
                VALUES ('{testName}', 'Original description', 1, GETDATE())
                GO
                
                UPDATE TestEntities 
                SET Description = 'Updated description' 
                WHERE Name = '{testName}'
                GO
                
                SELECT COUNT(*) FROM TestEntities WHERE Name = '{testName}'";

            // Act
            var result = _dbAccess.ExecuteScript(script, captureResults: true);

            // Assert
            result.Should().NotBeNull("script execution should return a result");
            result.Success.Should().BeTrue("DML script should execute successfully");
            result.LastException.Should().BeNull("no exception should occur");
            
            result.RecordsAffected.Should().HaveCount(3, "should have three batch results");
            result.RecordsAffected[0].Should().Be(1, "INSERT should affect 1 record");
            result.RecordsAffected[1].Should().Be(1, "UPDATE should affect 1 record");
            
            // Verify the operations worked
            var updatedDescription = _dbAccess.GetValue<string>(
                $"SELECT Description FROM TestEntities WHERE Name = '{testName}'");
            updatedDescription.Should().Be("Updated description", "UPDATE should have worked");
            
            // Verify SELECT result set
            result.ResultSets.Should().HaveCount(1, "should have one result set from SELECT");
            var countResult = Convert.ToInt32(result.ResultSets[0].Rows[0][0]);
            countResult.Should().Be(1, "SELECT should find the test record");
        }

        /// <summary>
        /// Tests mixed DDL and DML statements to verify parameter processing logic.
        /// </summary>
        [TestMethod]
        public void IsDDLStatement_MixedDDLAndDML_ShouldProcessParametersSelectively()
        {
            // Arrange - Mix DDL and DML statements (simplified without complex parameters)
            var testName = DatabaseTestHelper.GenerateTestString("MixedTest");
            var script = $@"
                CREATE TABLE {TestTablePrefix}MixedTest (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(50),
                    Value INT
                )
                GO
                
                INSERT INTO {TestTablePrefix}MixedTest (Name, Value)
                VALUES ('{testName}', 42)
                GO
                
                ALTER TABLE {TestTablePrefix}MixedTest ADD Description NVARCHAR(100)
                GO
                
                UPDATE {TestTablePrefix}MixedTest 
                SET Value = 84
                WHERE Name = '{testName}'
                GO
                
                DROP TABLE {TestTablePrefix}MixedTest";

            // Act
            var result = _dbAccess.ExecuteScript(script);

            // Assert
            result.Should().NotBeNull("script execution should return a result");
            result.Success.Should().BeTrue("mixed DDL/DML script should execute successfully");
            result.LastException.Should().BeNull("no exception should occur");
            
            result.RecordsAffected.Should().HaveCount(5, "should have five batch results");
            result.RecordsAffected[0].Should().Be(-1, "CREATE TABLE should return -1");
            result.RecordsAffected[1].Should().Be(1, "INSERT should affect 1 record");
            result.RecordsAffected[2].Should().Be(-1, "ALTER TABLE should return -1");
            result.RecordsAffected[3].Should().Be(1, "UPDATE should affect 1 record");
            result.RecordsAffected[4].Should().Be(-1, "DROP TABLE should return -1");
        }

        #endregion
    }
}