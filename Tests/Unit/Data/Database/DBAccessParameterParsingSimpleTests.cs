using AwesomeAssertions;
using System.Reflection;

#pragma warning disable CS8603 // Possible null reference return.

namespace ByteForge.Toolkit.Tests.Unit.Data.Database
{
    /// <summary>
    /// Simple unit tests for parameter parsing functionality that don't require database connectivity.
    /// </summary>
    /// <remarks>
    /// These tests focus specifically on the ParseParameters method logic for both SQL Server
    /// and ODBC scenarios without requiring actual database connections.
    /// </remarks>
    [TestClass]
    public class DBAccessParameterParsingSimpleTests
    {
        #region Helper Methods

        /// <summary>
        /// Creates a simple DBAccess instance with basic options for testing.
        /// </summary>
        /// <param name="databaseType">The database type to configure.</param>
        /// <returns>A configured DBAccess instance.</returns>
        private DBAccess CreateSimpleDBAccess(DBAccess.DataBaseType databaseType)
        {
            var options = new DatabaseOptions
            {
                DatabaseType = databaseType,
                Server = "localhost",
                DatabaseName = "test",
                UseTrustedConnection = true,
                ConnectionTimeout = 30,
                CommandTimeout = 300
            };
            return new DBAccess(options);
        }

        /// <summary>
        /// Uses reflection to access the private ParseParameters method for testing.
        /// </summary>
        /// <param name="dbAccess">The DBAccess instance.</param>
        /// <param name="query">The SQL query to parse.</param>
        /// <returns>List of parameter names found in the query.</returns>
        private List<string> InvokeParseParameters(DBAccess dbAccess, string query)
        {
            var method = typeof(DBAccess).GetMethod("ParseParameters", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Should().NotBeNull("ParseParameters method should exist");
            
            var result = method.Invoke(dbAccess, new object[] { query });
            return result as List<string>;
        }

        #endregion

        #region Basic SQL Server Tests

        /// <summary>
        /// Tests that simple named parameter assignments work for SQL Server.
        /// </summary>
        /// <remarks>
        /// This test validates the fundamental named parameter parsing logic for SQL Server stored procedures.
        /// It ensures that only value parameters (@value1, @value2) are extracted for parameter binding,
        /// while named parameters (@param1, @param2) are recognized as stored procedure parameter names.
        /// </remarks>
        [TestMethod]
        public void ParseParameters_SQLServer_BasicNamedAssignment_ShouldExtractValueParameters()
        {
            // Arrange
            var dbAccess = CreateSimpleDBAccess(DBAccess.DataBaseType.SQLServer);
            var query = "EXEC MyProc @param1 = @value1, @param2 = @value2";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(2);
            parameters.Should().Contain("@value1");
            parameters.Should().Contain("@value2");
            parameters.Should().NotContain("@param1");
            parameters.Should().NotContain("@param2");
        }

        /// <summary>
        /// Tests that standalone parameters work for SQL Server.
        /// </summary>
        /// <remarks>
        /// This test validates traditional parameter parsing for standard SQL Server queries.
        /// Standalone parameters in WHERE clauses and other SQL constructs should be detected
        /// and returned for parameter binding without special named assignment processing.
        /// </remarks>
        [TestMethod]
        public void ParseParameters_SQLServer_StandaloneParameters_ShouldExtractAllParameters()
        {
            // Arrange
            var dbAccess = CreateSimpleDBAccess(DBAccess.DataBaseType.SQLServer);
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
        /// Tests mixed scenarios with both named assignments and standalone parameters.
        /// </summary>
        /// <remarks>
        /// This test validates complex parsing scenarios common in enterprise applications.
        /// Queries may combine stored procedure calls with named parameters and traditional
        /// parameterized SQL, requiring intelligent parsing to handle both patterns correctly.
        /// </remarks>
        [TestMethod]
        public void ParseParameters_SQLServer_MixedScenario_ShouldHandleBothTypes()
        {
            // Arrange
            var dbAccess = CreateSimpleDBAccess(DBAccess.DataBaseType.SQLServer);
            var query = "SELECT * FROM Users WHERE Name = @userName AND EXISTS (EXEC MyProc @input = @procValue)";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(2);
            parameters.Should().Contain("@userName");
            parameters.Should().Contain("@procValue");
            parameters.Should().NotContain("@input");
        }

        #endregion

        #region Basic ODBC Tests

        /// <summary>
        /// Tests that ODBC treats all @ symbols as parameters (no named assignment logic).
        /// </summary>
        /// <remarks>
        /// This test ensures backward compatibility and database-specific behavior for ODBC connections.
        /// ODBC databases don't support SQL Server's named parameter syntax, so all @ symbols
        /// must be treated as positional parameters for proper parameter binding.
        /// </remarks>
        [TestMethod]
        public void ParseParameters_ODBC_NamedAssignmentSyntax_ShouldTreatAllAsParameters()
        {
            // Arrange
            var dbAccess = CreateSimpleDBAccess(DBAccess.DataBaseType.ODBC);
            var query = "EXEC MyProc @param1 = @value1, @param2 = @value2";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(4);
            parameters.Should().Contain("@param1");
            parameters.Should().Contain("@value1");
            parameters.Should().Contain("@param2");
            parameters.Should().Contain("@value2");
        }

        /// <summary>
        /// Tests that ODBC allows parameter repetition.
        /// </summary>
        /// <remarks>
        /// This test validates ODBC-specific parameter handling that allows the same parameter
        /// to appear multiple times in a query. This behavior is important for ODBC drivers
        /// that expect positional parameter binding rather than named parameter binding.
        /// </remarks>
        [TestMethod]
        public void ParseParameters_ODBC_RepeatedParameters_ShouldIncludeAllOccurrences()
        {
            // Arrange
            var dbAccess = CreateSimpleDBAccess(DBAccess.DataBaseType.ODBC);
            var query = "SELECT * FROM Users WHERE Name = @name OR Alias = @name";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(2); // ODBC allows repetition
            parameters.Count(p => p == "@name").Should().Be(2);
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Tests that empty queries return empty parameter lists.
        /// </summary>
        /// <remarks>
        /// This edge case test ensures robust handling of empty or whitespace-only queries.
        /// Parameter parsing should gracefully handle edge cases without throwing exceptions,
        /// maintaining application stability in dynamic query generation scenarios.
        /// </remarks>
        [TestMethod]
        public void ParseParameters_EmptyQuery_ShouldReturnEmptyList()
        {
            // Arrange
            var dbAccess = CreateSimpleDBAccess(DBAccess.DataBaseType.SQLServer);

            // Act & Assert
            var emptyResult = InvokeParseParameters(dbAccess, "");
            emptyResult.Should().NotBeNull().And.BeEmpty();

            var whitespaceResult = InvokeParseParameters(dbAccess, "   ");
            whitespaceResult.Should().NotBeNull().And.BeEmpty();
        }

        /// <summary>
        /// Tests that queries without parameters return empty lists.
        /// </summary>
        /// <remarks>
        /// This test validates parameter parsing behavior for static, non-parameterized queries.
        /// Applications may mix parameterized and static SQL, so parameter parsing must
        /// efficiently handle queries without parameters without unnecessary processing.
        /// </remarks>
        [TestMethod]
        public void ParseParameters_NoParameters_ShouldReturnEmptyList()
        {
            // Arrange
            var dbAccess = CreateSimpleDBAccess(DBAccess.DataBaseType.SQLServer);
            var query = "SELECT * FROM Users WHERE Name = 'John'";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull().And.BeEmpty();
        }

        /// <summary>
        /// Tests that parameters in string literals are ignored.
        /// </summary>
        /// <remarks>
        /// This test validates context-aware parsing that distinguishes between actual parameters
        /// and parameter-like text within string literals. Proper string context handling prevents
        /// false parameter detection and ensures accurate parameter extraction.
        /// </remarks>
        [TestMethod]
        public void ParseParameters_ParametersInStrings_ShouldIgnoreStringContent()
        {
            // Arrange
            var dbAccess = CreateSimpleDBAccess(DBAccess.DataBaseType.SQLServer);
            var query = "SELECT '@param1 = @value1' AS Test, @realParam";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(1);
            parameters.Should().Contain("@realParam");
            parameters.Should().NotContain("@param1");
            parameters.Should().NotContain("@value1");
        }

        /// <summary>
        /// Tests that parameters in comments are ignored.
        /// </summary>
        /// <remarks>
        /// This test ensures that both single-line and multi-line comments are properly excluded
        /// from parameter parsing. Comment awareness prevents extraction of commented-out
        /// parameters and supports well-documented SQL scripts.
        /// </remarks>
        [TestMethod]
        public void ParseParameters_ParametersInComments_ShouldIgnoreComments()
        {
            // Arrange
            var dbAccess = CreateSimpleDBAccess(DBAccess.DataBaseType.SQLServer);
            var query = @"-- Comment with @commentParam = @commentValue
                         SELECT @realParam
                         /* Block comment @blockParam = @blockValue */";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(1);
            parameters.Should().Contain("@realParam");
            parameters.Should().NotContain("@commentParam");
            parameters.Should().NotContain("@commentValue");
            parameters.Should().NotContain("@blockParam");
            parameters.Should().NotContain("@blockValue");
        }

        #endregion

        #region Parameter Name Validation

        /// <summary>
        /// Tests that parameter names with underscores and numbers are handled correctly.
        /// </summary>
        /// <remarks>
        /// This test validates identifier parsing for parameters following common naming conventions.
        /// Enterprise applications often use underscores and numbers in parameter names,
        /// so robust identifier recognition is essential for broad compatibility.
        /// </remarks>
        [TestMethod]
        public void ParseParameters_ComplexParameterNames_ShouldParseCorrectly()
        {
            // Arrange
            var dbAccess = CreateSimpleDBAccess(DBAccess.DataBaseType.SQLServer);
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
        /// Tests case insensitive parameter handling.
        /// </summary>
        /// <remarks>
        /// This test validates case-insensitive parameter deduplication consistent with SQL Server behavior.
        /// Parameter names should be treated case-insensitively to prevent duplicate parameter
        /// creation and ensure consistency with database parameter handling semantics.
        /// </remarks>
        [TestMethod]
        public void ParseParameters_CaseInsensitive_ShouldDeduplicateCorrectly()
        {
            // Arrange
            var dbAccess = CreateSimpleDBAccess(DBAccess.DataBaseType.SQLServer);
            var query = "SELECT * FROM Users WHERE Name = @UserName AND Age = @USERNAME";

            // Act
            var parameters = InvokeParseParameters(dbAccess, query);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(1); // Should deduplicate case-insensitive matches
        }

        #endregion
    }
}