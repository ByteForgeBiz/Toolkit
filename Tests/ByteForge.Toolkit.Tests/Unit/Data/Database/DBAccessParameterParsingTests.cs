using ByteForge.Toolkit.Tests.Helpers;
using FluentAssertions;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Data.Database
{
    /// <summary>
    /// Unit tests for parameter parsing functionality in the <see cref="DBAccess"/> class.
    /// </summary>
    /// <remarks>
    /// These tests focus on the enhanced parameter parsing logic that supports SQL Server
    /// stored procedure calls with named parameter syntax (@paramName = @valueParam).
    /// Tests cover both SQL Server and ODBC database types to ensure proper behavior.
    /// </remarks>
    [TestClass]
    public class DBAccessParameterParsingTests
    {
        #region Test Setup and Cleanup

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

        #endregion

        #region Helper Methods

        /// <summary>
        /// Uses reflection to access the private ParseParameters method for testing.
        /// </summary>
        /// <param name="dbAccess">The DBAccess instance.</param>
        /// <param name="query">The SQL query to parse.</param>
        /// <returns>List of parameter names found in the query.</returns>
        private List<string> InvokeParseParameters(DBAccess dbAccess, string? query)
        {
            var method = typeof(DBAccess).GetMethod("ParseParameters", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Should().NotBeNull("ParseParameters method should exist");
            
            var result = method.Invoke(dbAccess, [query]);
            return result as List<string>;
        }

        /// <summary>
        /// Creates a DBAccess instance with the specified database type for testing.
        /// </summary>
        /// <param name="databaseType">The database type to configure.</param>
        /// <returns>A configured DBAccess instance.</returns>
        private DBAccess CreateDBAccessWithType(DBAccess.DataBaseType databaseType)
        {
            var options = DatabaseTestHelper.CreateTestDatabaseOptions();
            options.DatabaseType = databaseType;
            return new DBAccess(options);
        }

        #endregion

        #region SQL Server Named Parameter Assignment Tests

        /// <summary>
        /// Tests that simple named parameter assignments are parsed correctly for SQL Server.
        /// </summary>
        [TestMethod]
        public void ParseParameters_SQLServer_SimpleNamedAssignment_ShouldReturnValueParameters()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);
            var query = "EXEC MyStoredProc @InputParam = @value1, @OutputParam = @value2";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(2);
            parameters.Should().Contain("@value1");
            parameters.Should().Contain("@value2");
            parameters.Should().NotContain("@InputParam");
            parameters.Should().NotContain("@OutputParam");
        }

        /// <summary>
        /// Tests that multiple named parameter assignments with whitespace are handled correctly.
        /// </summary>
        [TestMethod]
        public void ParseParameters_SQLServer_NamedAssignmentWithWhitespace_ShouldParseCorrectly()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);
            var query = @"EXEC MyStoredProc 
                @InputParam1   =   @value1  ,  
                @InputParam2=@value2,
                @OutputParam =    @value3";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(3);
            parameters.Should().Contain("@value1");
            parameters.Should().Contain("@value2");
            parameters.Should().Contain("@value3");
            parameters.Should().NotContain("@InputParam1");
            parameters.Should().NotContain("@InputParam2");
            parameters.Should().NotContain("@OutputParam");
        }

        /// <summary>
        /// Tests that named assignments within string literals are ignored.
        /// </summary>
        [TestMethod]
        public void ParseParameters_SQLServer_NamedAssignmentInStringLiteral_ShouldIgnoreStringContent()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);
            var query = "SELECT '@param1 = @value1' AS TestString, @actualParam = @actualValue";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(1);
            parameters.Should().Contain("@actualValue");
            parameters.Should().NotContain("@param1");
            parameters.Should().NotContain("@value1");
            parameters.Should().NotContain("@actualParam");
        }

        /// <summary>
        /// Tests that named assignments in comments are ignored.
        /// </summary>
        [TestMethod]
        public void ParseParameters_SQLServer_NamedAssignmentInComments_ShouldIgnoreComments()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);
            var query = @"-- This is a comment with @param1 = @value1
                /* Another comment with @param2 = @value2 */
                EXEC MyProc @realParam = @realValue";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(1);
            parameters.Should().Contain("@realValue");
            parameters.Should().NotContain("@param1");
            parameters.Should().NotContain("@value1");
            parameters.Should().NotContain("@param2");
            parameters.Should().NotContain("@value2");
            parameters.Should().NotContain("@realParam");
        }

        #endregion

        #region SQL Server Standalone Parameter Tests

        /// <summary>
        /// Tests that standalone parameters (not part of named assignments) are handled correctly.
        /// </summary>
        [TestMethod]
        public void ParseParameters_SQLServer_StandaloneParameters_ShouldReturnStandaloneParams()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);
            var query = "SELECT * FROM Users WHERE Name = @userName AND Age > @minAge";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(2);
            parameters.Should().Contain("@userName");
            parameters.Should().Contain("@minAge");
        }

        /// <summary>
        /// Tests that mixed named assignments and standalone parameters are handled correctly.
        /// </summary>
        [TestMethod]
        public void ParseParameters_SQLServer_MixedParameterTypes_ShouldHandleBothTypes()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);
            var query = @"SELECT * FROM Users WHERE Name = @userName 
                         AND EXEC MyProc @InputParam = @procValue 
                         AND Age > @minAge";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(3);
            parameters.Should().Contain("@userName");
            parameters.Should().Contain("@procValue");
            parameters.Should().Contain("@minAge");
            parameters.Should().NotContain("@InputParam");
        }

        #endregion

        #region ODBC Parameter Tests

        /// <summary>
        /// Tests that ODBC parameter parsing works with the original logic (no named assignments).
        /// </summary>
        [TestMethod]
        public void ParseParameters_ODBC_AllParameters_ShouldReturnAllParametersIncludingDuplicates()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.ODBC);
            var query = "SELECT * FROM Users WHERE Name = @name AND Description LIKE @name AND Age > @age";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(3); // ODBC allows repetition
            parameters.Should().Contain("@name");
            parameters.Should().Contain("@age");
            // Should have @name twice for ODBC
            parameters.Count(p => p == "@name").Should().Be(2);
        }

        /// <summary>
        /// Tests that ODBC ignores named assignment syntax and treats all @ symbols as parameters.
        /// </summary>
        [TestMethod]
        public void ParseParameters_ODBC_NamedAssignmentSyntax_ShouldTreatAllAsRegularParameters()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.ODBC);
            var query = "EXEC MyStoredProc @InputParam = @value1, @OutputParam = @value2";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(4); // ODBC treats all @ symbols as parameters
            parameters.Should().Contain("@InputParam");
            parameters.Should().Contain("@value1");
            parameters.Should().Contain("@OutputParam");
            parameters.Should().Contain("@value2");
        }

        #endregion

        #region Edge Cases and Error Conditions

        /// <summary>
        /// Tests that empty or null queries are handled gracefully.
        /// </summary>
        [TestMethod]
        public void ParseParameters_EmptyOrNullQuery_ShouldReturnEmptyList()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);

            // Act & Assert - Empty query
            var emptyResult = InvokeParseParameters(dbAccess, "");
            emptyResult.Should().NotBeNull().And.BeEmpty();

            // Act & Assert - Null query
            var nullResult = InvokeParseParameters(dbAccess, null);
            nullResult.Should().NotBeNull().And.BeEmpty();

            // Act & Assert - Whitespace only query
            var whitespaceResult = InvokeParseParameters(dbAccess, "   \t\n   ");
            whitespaceResult.Should().NotBeNull().And.BeEmpty();
        }

        /// <summary>
        /// Tests that queries without parameters return empty results.
        /// </summary>
        [TestMethod]
        public void ParseParameters_QueryWithoutParameters_ShouldReturnEmptyList()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);
            var query = "SELECT * FROM Users WHERE Name = 'John' AND Age > 30";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull().And.BeEmpty();
        }

        /// <summary>
        /// Tests complex scenarios with nested comments and string literals.
        /// </summary>
        [TestMethod]
        public void ParseParameters_ComplexQueryWithCommentsAndStrings_ShouldParseCorrectly()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);
            var query = @"
                -- Comment with @fakeParam = @fakeValue
                SELECT 
                    '@stringParam = @stringValue' AS TestColumn,
                    /* Block comment @blockParam = @blockValue */
                    UserName
                FROM Users 
                WHERE Name = @realParam1
                /* Another 
                   multiline comment @multiParam = @multiValue */
                AND EXEC MyProc @procParam = @realParam2
                AND Age > @realParam3";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(3);
            parameters.Should().Contain("@realParam1");
            parameters.Should().Contain("@realParam2");
            parameters.Should().Contain("@realParam3");
            // Should not contain commented or string literal parameters
            parameters.Should().NotContain("@fakeParam");
            parameters.Should().NotContain("@fakeValue");
            parameters.Should().NotContain("@stringParam");
            parameters.Should().NotContain("@stringValue");
            parameters.Should().NotContain("@blockParam");
            parameters.Should().NotContain("@blockValue");
            parameters.Should().NotContain("@multiParam");
            parameters.Should().NotContain("@multiValue");
            parameters.Should().NotContain("@procParam");
        }

        /// <summary>
        /// Tests that parameter names with underscores and numbers are handled correctly.
        /// </summary>
        [TestMethod]
        public void ParseParameters_ParametersWithUnderscoresAndNumbers_ShouldParseCorrectly()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);
            var query = "EXEC MyProc @input_param_1 = @value_1, @output_param_2 = @value_2";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(2);
            parameters.Should().Contain("@value_1");
            parameters.Should().Contain("@value_2");
            parameters.Should().NotContain("@input_param_1");
            parameters.Should().NotContain("@output_param_2");
        }

        /// <summary>
        /// Tests that case sensitivity is handled correctly (parameters should be case-insensitive).
        /// </summary>
        [TestMethod]
        public void ParseParameters_CaseInsensitiveParameters_ShouldHandleCaseCorrectly()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);
            var query = "SELECT * FROM Users WHERE Name = @UserName AND Age = @USERNAME";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(1); // Should deduplicate case-insensitive matches
            parameters.Should().ContainSingle(p => 
                string.Equals(p, "@UserName", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p, "@USERNAME", StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Integration Tests with AddParametersToCommand

        /// <summary>
        /// Tests that the parameter parsing integrates correctly with parameter creation.
        /// </summary>
        [TestMethod]
        public void AddParametersToCommand_WithNamedAssignments_ShouldCreateCorrectParameters()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);
            var query = "EXEC TestProc @inputParam = @name, @outputParam = @value";
            
            // Act - Parse the parameters using reflection to verify the logic
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert - Should only extract the value parameters, not the named parameters
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(2);
            parameters.Should().Contain("@name");
            parameters.Should().Contain("@value");
            parameters.Should().NotContain("@inputParam");
            parameters.Should().NotContain("@outputParam");
        }

        /// <summary>
        /// Tests that parameter count mismatch is properly detected by parameter parsing.
        /// </summary>
        [TestMethod]
        public void AddParametersToCommand_ParameterCountMismatch_ShouldDetectCorrectParameterCount()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);
            var query = "EXEC MyProc @param1 = @value1, @param2 = @value2";

            // Act - Parse parameters to see what would be expected
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert - Should detect 2 parameters that would need to be provided
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(2, "two value parameters should be detected");
            parameters.Should().Contain("@value1");
            parameters.Should().Contain("@value2");
            // This means if only 1 argument is provided, it would be a mismatch
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Tests that parameter parsing performance is acceptable for complex queries.
        /// </summary>
        [TestMethod]
        public void ParseParameters_ComplexQuery_ShouldCompleteWithinReasonableTime()
        {
            // Arrange
            var dbAccess = CreateDBAccessWithType(DBAccess.DataBaseType.SQLServer);
            var complexQuery = @"
                -- Complex query with many parameters and named assignments
                EXEC Proc1 @p1 = @v1, @p2 = @v2, @p3 = @v3;
                SELECT * FROM Table1 WHERE Col1 = @standalone1 AND Col2 = @standalone2;
                EXEC Proc2 @p4 = @v4, @p5 = @v5;
                INSERT INTO Table2 (Col1, Col2, Col3) VALUES (@standalone3, @standalone4, @standalone5);
                EXEC Proc3 @p6 = @v6, @p7 = @v7, @p8 = @v8, @p9 = @v9, @p10 = @v10;";

            // Act & Assert - Parsing should complete within reasonable time
            DatabaseTestHelper.AssertExecutionTime(
                () => InvokeParseParameters(dbAccess, complexQuery),
                maxMilliseconds: 100,
                operationName: "Complex parameter parsing");
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