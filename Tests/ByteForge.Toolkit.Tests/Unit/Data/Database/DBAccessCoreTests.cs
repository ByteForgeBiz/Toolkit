using ByteForge.Toolkit.Tests.Helpers;
using AwesomeAssertions;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace ByteForge.Toolkit.Tests.Unit.Data.Database
{
    /// <summary>
    /// Unit tests for the core functionality of the <see cref="DBAccess"/> class.
    /// </summary>
    /// <remarks>
    /// These tests focus on the fundamental aspects of the DBAccess class including
    /// constructors, configuration handling, database type detection, and basic properties.
    /// Tests use the TestUnitDB database for integration scenarios.
    /// </remarks>
    [TestClass]
    public class DBAccessCoreTests
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

        #region Constructor Tests

        /// <summary>
        /// Tests that the DBAccess constructor with DatabaseOptions parameter works correctly.
        /// </summary>
        [TestMethod]
        public void Constructor_WithValidDatabaseOptions_ShouldInitializeCorrectly()
        {
            // Arrange
            var options = DatabaseTestHelper.CreateTestDatabaseOptions();

            // Act
            var dbAccess = new DBAccess(options);

            // Assert
            dbAccess.Should().NotBeNull();
            dbAccess.Options.Should().BeSameAs(options);
            dbAccess.DbType.Should().Be(DBAccess.DataBaseType.SQLServer);
            dbAccess.ConnectionString.Should().Be(options.GetConnectionString());
        }

        /// <summary>
        /// Tests that the DBAccess constructor throws ArgumentNullException for null options.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullDatabaseOptions_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var action = new Action(() => new DBAccess((DatabaseOptions)null));
            action.Should().Throw<ArgumentNullException>()
                  .WithParameterName("options");
        }

        /// <summary>
        /// Tests that DBAccess can be constructed with different database types.
        /// </summary>
        [TestMethod]
        public void Constructor_WithDifferentDatabaseTypes_ShouldSetCorrectDbType()
        {
            // Test SQL Server
            var sqlOptions = DatabaseTestHelper.CreateTestDatabaseOptions();
            sqlOptions.DatabaseType = DBAccess.DataBaseType.SQLServer;
            var sqlDbAccess = new DBAccess(sqlOptions);
            sqlDbAccess.DbType.Should().Be(DBAccess.DataBaseType.SQLServer);

            // Test ODBC
            var odbcOptions = DatabaseTestHelper.CreateTestDatabaseOptions();
            odbcOptions.DatabaseType = DBAccess.DataBaseType.ODBC;
            var odbcDbAccess = new DBAccess(odbcOptions);
            odbcDbAccess.DbType.Should().Be(DBAccess.DataBaseType.ODBC);
        }

        #endregion

        #region Properties Tests

        /// <summary>
        /// Tests that the Options property returns the correct DatabaseOptions instance.
        /// </summary>
        [TestMethod]
        public void Options_PropertyAccess_ShouldReturnCorrectInstance()
        {
            // Arrange
            var expectedOptions = DatabaseTestHelper.CreateTestDatabaseOptions();
            var dbAccess = new DBAccess(expectedOptions);

            // Act
            var actualOptions = dbAccess.Options;

            // Assert
            actualOptions.Should().BeSameAs(expectedOptions);
            actualOptions.Server.Should().Be(expectedOptions.Server);
            actualOptions.DatabaseName.Should().Be(expectedOptions.DatabaseName);
            actualOptions.ConnectionTimeout.Should().Be(expectedOptions.ConnectionTimeout);
        }

        /// <summary>
        /// Tests that the DbType property returns the correct database type.
        /// </summary>
        [TestMethod]
        public void DbType_PropertyAccess_ShouldReturnCorrectDatabaseType()
        {
            // Arrange
            var options = DatabaseTestHelper.CreateTestDatabaseOptions();
            options.DatabaseType = DBAccess.DataBaseType.SQLServer;
            var dbAccess = new DBAccess(options);

            // Act
            var dbType = dbAccess.DbType;

            // Assert
            dbType.Should().Be(DBAccess.DataBaseType.SQLServer);
        }

        /// <summary>
        /// Tests that the ConnectionString property returns a properly formatted connection string.
        /// </summary>
        [TestMethod]
        public void ConnectionString_PropertyAccess_ShouldReturnFormattedConnectionString()
        {
            // Arrange
            var options = DatabaseTestHelper.CreateTestDatabaseOptions();
            var dbAccess = new DBAccess(options);

            // Act
            var connectionString = dbAccess.ConnectionString;

            // Assert
            connectionString.Should().NotBeNull()
                           .And.Contain(DatabaseTestHelper.TestServerName, "connection string should contain server name")
                           .And.Contain(DatabaseTestHelper.TestDatabaseName, "connection string should contain database name")
                           .And.SatisfyAny(
                               because: "connection string should contain integrated security setting",
                               cs => cs.Contains("Integrated Security=true"),
                               cs => cs.Contains("Trusted_Connection=true"));
        }

        #endregion

        #region RecordsAffected and LastException Tests

        /// <summary>
        /// Tests that RecordsAffected property initializes correctly and updates after operations.
        /// </summary>
        [TestMethod]
        public void RecordsAffected_InitialValue_ShouldBeZero()
        {
            // Arrange & Act
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();

            // Assert
            dbAccess.RecordsAffected.Should().Be(0);
        }

        /// <summary>
        /// Tests that LastException property initializes as null.
        /// </summary>
        [TestMethod]
        public void LastException_InitialValue_ShouldBeNull()
        {
            // Arrange & Act
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();

            // Assert
            dbAccess.LastException.Should().BeNull();
        }

        /// <summary>
        /// Tests that RecordsAffected is updated after a successful query operation.
        /// </summary>
        [TestMethod]
        public void RecordsAffected_AfterSuccessfulQuery_ShouldBeUpdated()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var testName = DatabaseTestHelper.GenerateTestString("DBAccessCore");

            try
            {
                // Act - Insert a test record
                var success = dbAccess.ExecuteQuery(
                    "INSERT INTO TestEntities (Name, Description, IsActive, TestValue) VALUES (@name, @desc, @active, @value)",
                    testName, "Core test record", true, 42.0m);

                // Assert
                success.Should().BeTrue();
                dbAccess.RecordsAffected.Should().Be(1);
                dbAccess.LastException.Should().BeNull();
            }
            finally
            {
                // Cleanup
                DatabaseTestHelper.CleanupTestData(dbAccess, "TestEntities", $"Name = '{testName}'");
            }
        }

        /// <summary>
        /// Tests that LastException is set when a query fails.
        /// </summary>
        [TestMethod]
        public void LastException_AfterFailedQuery_ShouldContainExceptionDetails()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();

            // Act - Execute an invalid query
            var success = dbAccess.ExecuteQuery("SELECT * FROM NonExistentTable");

            // Assert
            success.Should().BeFalse();
            dbAccess.LastException.Should().NotBeNull()
                                  .And.Subject.As<Exception>().Message.Should().SatisfyAny(
                                      because: "error message should indicate table doesn't exist",
                                      msg => msg.Contains("Invalid object name"),
                                      msg => msg.Contains("doesn't exist"));
        }

        #endregion

        #region Database Connection Tests

        /// <summary>
        /// Tests that a valid database configuration can successfully connect to the test database.
        /// </summary>
        [TestMethod]
        public void TestConnection_WithValidConfiguration_ShouldReturnTrue()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();

            // Act
            var canConnect = dbAccess.TestConnection();

            // Assert
            canConnect.Should().BeTrue("should be able to connect to test database");
            dbAccess.LastException.Should().BeNull("no exception should occur for valid connection");
        }

        /// <summary>
        /// Tests that an invalid database configuration fails to connect.
        /// </summary>
        [TestMethod]
        public void TestConnection_WithInvalidConfiguration_ShouldReturnFalse()
        {
            // Arrange
            var invalidOptions = DatabaseTestHelper.CreateInvalidDatabaseOptions();
            var dbAccess = new DBAccess(invalidOptions);

            // Act
            var canConnect = dbAccess.TestConnection();

            // Assert
            canConnect.Should().BeFalse("should not be able to connect to invalid database");
            dbAccess.LastException.Should().NotBeNull("exception should be set for failed connection");
        }

        #endregion

        #region Database Type Enumeration Tests

        /// <summary>
        /// Tests that the DataBaseType enumeration contains expected values.
        /// </summary>
        [TestMethod]
        public void DataBaseType_Enumeration_ShouldContainExpectedValues()
        {
            // Assert that enumeration has expected values
            var values = Enum.GetValues(typeof(DBAccess.DataBaseType));
            values.Length.Should().BeGreaterThanOrEqualTo(2, "should have at least SQLServer and ODBC types");

            // Check for specific expected values
            Enum.IsDefined(typeof(DBAccess.DataBaseType), DBAccess.DataBaseType.SQLServer).Should().BeTrue();
            Enum.IsDefined(typeof(DBAccess.DataBaseType), DBAccess.DataBaseType.ODBC).Should().BeTrue();
        }

        #endregion

        #region Configuration Integration Tests

        /// <summary>
        /// Tests that different timeout configurations are properly applied.
        /// </summary>
        [TestMethod]
        public void DatabaseOptions_DifferentTimeouts_ShouldBeAppliedCorrectly()
        {
            // Arrange - Create options with custom timeouts
            var customOptions = DatabaseTestHelper.CreateTestDatabaseOptions(
                connectionTimeout: 45, 
                commandTimeout: 600);

            // Act
            var dbAccess = new DBAccess(customOptions);

            // Assert
            dbAccess.Options.ConnectionTimeout.Should().Be(45);
            dbAccess.Options.CommandTimeout.Should().Be(600);
        }

        /// <summary>
        /// Tests that AutoTrimStrings configuration is properly applied.
        /// </summary>
        [TestMethod]
        public void DatabaseOptions_AutoTrimStrings_ShouldBeConfigurable()
        {
            // Test with AutoTrimStrings enabled
            var trimOptions = DatabaseTestHelper.CreateTestDatabaseOptions(30, 300, autoTrimStrings: true);
            var dbAccessWithTrim = new DBAccess(trimOptions);
            dbAccessWithTrim.Options.AutoTrimStrings.Should().BeTrue();

            // Test with AutoTrimStrings disabled
            var noTrimOptions = DatabaseTestHelper.CreateTestDatabaseOptions(30, 300, autoTrimStrings: false);
            var dbAccessNoTrim = new DBAccess(noTrimOptions);
            dbAccessNoTrim.Options.AutoTrimStrings.Should().BeFalse();
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Tests that database connection establishment is reasonably fast.
        /// </summary>
        [TestMethod]
        public void TestConnection_Performance_ShouldConnectWithinReasonableTime()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();

            // Act & Assert - Connection should be established within 5 seconds
            DatabaseTestHelper.AssertExecutionTime(
                () => dbAccess.TestConnection(),
                maxMilliseconds: 5000,
                operationName: "Database connection test");
        }

        /// <summary>
        /// Tests that multiple DBAccess instances can be created without performance degradation.
        /// </summary>
        [TestMethod]
        public void Constructor_MultipleInstances_ShouldNotDegradePerformance()
        {
            // Arrange
            const int instanceCount = 10;
            var options = DatabaseTestHelper.CreateTestDatabaseOptions();

            // Act & Assert - Creating multiple instances should be fast
            DatabaseTestHelper.AssertExecutionTime(
                () =>
                {
                    for (var i = 0; i < instanceCount; i++)
                    {
                        var dbAccess = new DBAccess(options);
                        // Verify instance is properly created
                        Assert.IsNotNull(dbAccess.Options);
                    }
                },
                maxMilliseconds: 1000,
                operationName: $"Creating {instanceCount} DBAccess instances");
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests behavior with extremely short timeout values.
        /// </summary>
        [TestMethod]
        public void DatabaseOptions_ExtremelyShortTimeouts_ShouldBeAccepted()
        {
            // Arrange
            var options = DatabaseTestHelper.CreateTestDatabaseOptions(connectionTimeout: 1, commandTimeout: 1);

            // Act
            var dbAccess = new DBAccess(options);

            // Assert - Constructor should succeed even with very short timeouts
            dbAccess.Should().NotBeNull();
            dbAccess.Options.ConnectionTimeout.Should().Be(1);
            dbAccess.Options.CommandTimeout.Should().Be(1);
        }

        /// <summary>
        /// Tests behavior with very long timeout values.
        /// </summary>
        [TestMethod]
        public void DatabaseOptions_VeryLongTimeouts_ShouldBeAccepted()
        {
            // Arrange
            var options = DatabaseTestHelper.CreateTestDatabaseOptions(connectionTimeout: 3600, commandTimeout: 7200);

            // Act
            var dbAccess = new DBAccess(options);

            // Assert - Constructor should succeed with long timeouts
            dbAccess.Should().NotBeNull();
            dbAccess.Options.ConnectionTimeout.Should().Be(3600);
            dbAccess.Options.CommandTimeout.Should().Be(7200);
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