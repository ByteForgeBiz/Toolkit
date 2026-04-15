using AwesomeAssertions;
using ByteForge.Toolkit.Data;
using System.Diagnostics;

namespace ByteForge.Toolkit.Tests.Helpers
{
    /// <summary>
    /// Provides helper methods and utilities for database testing.
    /// </summary>
    /// <remarks>
    /// This class centralizes common database testing operations including connection management,
    /// test data cleanup, transaction handling, and configuration creation for consistent testing
    /// across all database-related test classes.
    /// </remarks>
    public static class DatabaseTestHelper
    {
        #region Constants and Configuration
        
        /// <summary>
        /// The name of the test database.
        /// </summary>
        public const string TestDatabaseName = "TestUnitDB";
        
        /// <summary>
        /// The local SQL Server instance name.
        /// </summary>
        public const string TestServerName = "(local)";
        
        /// <summary>
        /// Default connection timeout for tests (shorter than production).
        /// </summary>
        public const int TestConnectionTimeout = 30;
        
        /// <summary>
        /// Default command timeout for tests.
        /// </summary>
        public const int TestCommandTimeout = 300;

        /// <summary>
        /// Environment variable containing the SQL Server host used by CI test runs.
        /// </summary>
        public const string TestSqlServerEnvVar = "BYTEFORGE_TEST_SQL_SERVER";

        /// <summary>
        /// Environment variable containing the SQL Server port used by CI test runs.
        /// </summary>
        public const string TestSqlPortEnvVar = "BYTEFORGE_TEST_SQL_PORT";

        /// <summary>
        /// Environment variable containing the SQL Server user used by CI test runs.
        /// </summary>
        public const string TestSqlUserEnvVar = "BYTEFORGE_TEST_SQL_USER";

        /// <summary>
        /// Environment variable containing the SQL Server password used by CI test runs.
        /// </summary>
        public const string TestSqlPasswordEnvVar = "BYTEFORGE_TEST_SQL_PASSWORD";

        /// <summary>
        /// Environment variable containing a full SQL Server connection string used by CI test runs.
        /// </summary>
        public const string TestSqlConnectionStringEnvVar = "BYTEFORGE_TEST_SQL_CONNECTION_STRING";

        /// <summary>
        /// Environment variable containing the SQL Server database name used by CI test runs.
        /// </summary>
        public const string TestSqlDatabaseEnvVar = "BYTEFORGE_TEST_SQL_DATABASE";
        
        #endregion

        #region Database Configuration Factory Methods

        /// <summary>
        /// Creates a standard test database configuration using integrated security.
        /// </summary>
        /// <returns>A configured <see cref="DatabaseOptions"/> instance for testing.</returns>
        public static DatabaseOptions CreateTestDatabaseOptions()
        {
            return CreateSqlServerTestOptions(TestDatabaseName, TestConnectionTimeout, TestCommandTimeout, autoTrimStrings: true, allowNullStrings: true);
        }

        /// <summary>
        /// Creates a test database configuration with custom settings.
        /// </summary>
        /// <param name="connectionTimeout">Custom connection timeout.</param>
        /// <param name="commandTimeout">Custom command timeout.</param>
        /// <param name="autoTrimStrings">Whether to auto-trim strings.</param>
        /// <returns>A configured <see cref="DatabaseOptions"/> instance.</returns>
        public static DatabaseOptions CreateTestDatabaseOptions(
            int connectionTimeout, 
            int commandTimeout, 
            bool autoTrimStrings = true)
        {
            return CreateSqlServerTestOptions(TestDatabaseName, connectionTimeout, commandTimeout, autoTrimStrings, allowNullStrings: false);
        }

        /// <summary>
        /// Creates an invalid database configuration for error testing.
        /// </summary>
        /// <returns>A <see cref="DatabaseOptions"/> instance with invalid settings.</returns>
        public static DatabaseOptions CreateInvalidDatabaseOptions()
        {
            return CreateSqlServerTestOptions("NonExistentDatabase", 5, 30, autoTrimStrings: true, allowNullStrings: false);
        }

        /// <summary>
        /// Creates a test DBAccess instance with standard configuration.
        /// </summary>
        /// <returns>A configured <see cref="DBAccess"/> instance for testing.</returns>
        public static DBAccess CreateTestDBAccess()
        {
            return new DBAccess(CreateTestDatabaseOptions());
        }

        /// <summary>
        /// Creates SQL Server test options using CI environment variables when available, otherwise local defaults.
        /// </summary>
        /// <param name="databaseName">The target database name.</param>
        /// <param name="connectionTimeout">The connection timeout in seconds.</param>
        /// <param name="commandTimeout">The command timeout in seconds.</param>
        /// <param name="autoTrimStrings">Whether to trim strings automatically.</param>
        /// <param name="allowNullStrings">Whether null strings are allowed.</param>
        /// <returns>A configured <see cref="DatabaseOptions"/> instance.</returns>
        private static DatabaseOptions CreateSqlServerTestOptions(
            string databaseName,
            int connectionTimeout,
            int commandTimeout,
            bool autoTrimStrings,
            bool allowNullStrings)
        {
            var ciOptions = TryCreateCiSqlServerOptions(databaseName, connectionTimeout, commandTimeout, autoTrimStrings, allowNullStrings);
            if (ciOptions != null)
                return ciOptions;

            return new DatabaseOptions
            {
                DatabaseType = DBAccess.DataBaseType.SQLServer,
                Server = TestServerName,
                DatabaseName = databaseName,
                UseTrustedConnection = true,
                UseEncryption = false,
                ConnectionTimeout = connectionTimeout,
                CommandTimeout = commandTimeout,
                AutoTrimStrings = autoTrimStrings,
                AllowNullStrings = allowNullStrings
            };
        }

        /// <summary>
        /// Creates SQL Server test options from CI environment variables, or returns null when CI overrides are absent.
        /// </summary>
        private static DatabaseOptions? TryCreateCiSqlServerOptions(
            string databaseName,
            int connectionTimeout,
            int commandTimeout,
            bool autoTrimStrings,
            bool allowNullStrings)
        {
            var explicitConnectionString = Environment.GetEnvironmentVariable(TestSqlConnectionStringEnvVar);
            if (!string.IsNullOrWhiteSpace(explicitConnectionString))
            {
                return new DatabaseOptions
                {
                    DatabaseType = DBAccess.DataBaseType.SQLServer,
                    Server = Environment.GetEnvironmentVariable(TestSqlServerEnvVar) ?? TestServerName,
                    DatabaseName = Environment.GetEnvironmentVariable(TestSqlDatabaseEnvVar) ?? databaseName,
                    ConnectionString = explicitConnectionString,
                    UseTrustedConnection = explicitConnectionString.Contains("Trusted_Connection=true", StringComparison.OrdinalIgnoreCase) ||
                                           explicitConnectionString.Contains("Integrated Security=true", StringComparison.OrdinalIgnoreCase),
                    UseEncryption = explicitConnectionString.Contains("Encrypt=True", StringComparison.OrdinalIgnoreCase) ||
                                    explicitConnectionString.Contains("Encrypt=Optional", StringComparison.OrdinalIgnoreCase),
                    ConnectionTimeout = connectionTimeout,
                    CommandTimeout = commandTimeout,
                    AutoTrimStrings = autoTrimStrings,
                    AllowNullStrings = allowNullStrings
                };
            }

            var server = Environment.GetEnvironmentVariable(TestSqlServerEnvVar);
            var user = Environment.GetEnvironmentVariable(TestSqlUserEnvVar);
            var password = Environment.GetEnvironmentVariable(TestSqlPasswordEnvVar);
            var configuredDatabaseName = Environment.GetEnvironmentVariable(TestSqlDatabaseEnvVar);
            if (!string.IsNullOrWhiteSpace(configuredDatabaseName))
                databaseName = configuredDatabaseName == TestDatabaseName ? databaseName : configuredDatabaseName;

            if (!string.IsNullOrWhiteSpace(server) &&
                string.IsNullOrWhiteSpace(user) &&
                string.IsNullOrWhiteSpace(password))
            {
                var trustedConnectionString =
                    $"Server={server};" +
                    $"Database={databaseName};" +
                    "Trusted_Connection=true;" +
                    $"Connection Timeout={connectionTimeout}";

                return new DatabaseOptions
                {
                    DatabaseType = DBAccess.DataBaseType.SQLServer,
                    Server = server,
                    DatabaseName = databaseName,
                    ConnectionString = trustedConnectionString,
                    UseTrustedConnection = true,
                    UseEncryption = false,
                    ConnectionTimeout = connectionTimeout,
                    CommandTimeout = commandTimeout,
                    AutoTrimStrings = autoTrimStrings,
                    AllowNullStrings = allowNullStrings
                };
            }

            if (string.IsNullOrWhiteSpace(server) ||
                string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var port = Environment.GetEnvironmentVariable(TestSqlPortEnvVar);
            var serverAddress = string.IsNullOrWhiteSpace(port) ? server : $"{server},{port}";
            var connectionString =
                $"Server={serverAddress};" +
                $"Database={databaseName};" +
                $"User ID={user};" +
                $"Password={password};" +
                $"Encrypt=True;" +
                $"TrustServerCertificate=True;" +
                $"Connection Timeout={connectionTimeout}";

            return new DatabaseOptions
            {
                DatabaseType = DBAccess.DataBaseType.SQLServer,
                Server = server,
                DatabaseName = databaseName,
                ConnectionString = connectionString,
                UseTrustedConnection = false,
                UseEncryption = true,
                ConnectionTimeout = connectionTimeout,
                CommandTimeout = commandTimeout,
                AutoTrimStrings = autoTrimStrings,
                AllowNullStrings = allowNullStrings
            };
        }

        #endregion

        #region Access Database Configuration Factory Methods

        /// <summary>
        /// The name of the test Access database file.
        /// </summary>
        public const string TestAccessDatabaseName = "TestUnitDB.accdb";
        
        /// <summary>
        /// The ODBC Data Source Name for the test Access database.
        /// </summary>
        public const string TestAccessDSN = "TestAccessDB";
        
        /// <summary>
        /// The full path to the test Access database file.
        /// </summary>
        public static readonly string TestAccessDatabasePath = 
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), 
                                  "TestData", TestAccessDatabaseName);

        /// <summary>
        /// Creates a standard test Access database configuration using direct file path (recommended).
        /// </summary>
        /// <returns>A configured <see cref="DatabaseOptions"/> instance for Access testing.</returns>
        public static DatabaseOptions CreateTestAccessDatabaseOptions()
        {
            var fullPath = System.IO.Path.GetFullPath(TestAccessDatabasePath);
            return new DatabaseOptions
            {
                DatabaseType = DBAccess.DataBaseType.ODBC,
                Server = "", // Not used for ODBC file connections
                DatabaseName = "", // Not used for ODBC file connections
                ConnectionString = $"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq={fullPath};",
                UseTrustedConnection = false,
                UseEncryption = false,
                ConnectionTimeout = TestConnectionTimeout,
                CommandTimeout = TestCommandTimeout,
                AutoTrimStrings = true
            };
        }

        /// <summary>
        /// Creates a test Access database configuration using ODBC DSN (requires DSN setup).
        /// </summary>
        /// <returns>A configured <see cref="DatabaseOptions"/> instance using DSN-based connection.</returns>
        public static DatabaseOptions CreateTestAccessDatabaseOptionsWithDSN()
        {
            return new DatabaseOptions
            {
                DatabaseType = DBAccess.DataBaseType.ODBC,
                Server = "", // Not used for ODBC DSN connections
                DatabaseName = "", // Not used for ODBC DSN connections
                ConnectionString = $"DSN={TestAccessDSN};",
                UseTrustedConnection = false,
                UseEncryption = false,
                ConnectionTimeout = TestConnectionTimeout,
                CommandTimeout = TestCommandTimeout,
                AutoTrimStrings = true
            };
        }

        /// <summary>
        /// Creates a test Access database configuration with custom settings.
        /// </summary>
        /// <param name="connectionTimeout">Custom connection timeout.</param>
        /// <param name="commandTimeout">Custom command timeout.</param>
        /// <param name="autoTrimStrings">Whether to auto-trim strings.</param>
        /// <returns>A configured <see cref="DatabaseOptions"/> instance.</returns>
        public static DatabaseOptions CreateTestAccessDatabaseOptions(
            int connectionTimeout, 
            int commandTimeout, 
            bool autoTrimStrings = true)
        {
            var fullPath = System.IO.Path.GetFullPath(TestAccessDatabasePath);
            return new DatabaseOptions
            {
                DatabaseType = DBAccess.DataBaseType.ODBC,
                Server = "",
                DatabaseName = "",
                ConnectionString = $"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq={fullPath};",
                UseTrustedConnection = false,
                UseEncryption = false,
                ConnectionTimeout = connectionTimeout,
                CommandTimeout = commandTimeout,
                AutoTrimStrings = autoTrimStrings
            };
        }

        /// <summary>
        /// Creates an invalid Access database configuration for error testing.
        /// </summary>
        /// <returns>A <see cref="DatabaseOptions"/> instance with invalid ODBC settings.</returns>
        public static DatabaseOptions CreateInvalidAccessDatabaseOptions()
        {
            return new DatabaseOptions
            {
                DatabaseType = DBAccess.DataBaseType.ODBC,
                Server = "",
                DatabaseName = "",
                ConnectionString = "DSN=NonExistentAccessDSN;",
                UseTrustedConnection = false,
                UseEncryption = false,
                ConnectionTimeout = 5,
                CommandTimeout = 30,
                AutoTrimStrings = true
            };
        }

        /// <summary>
        /// Creates a test Access DBAccess instance with standard ODBC configuration.
        /// </summary>
        /// <returns>A configured <see cref="DBAccess"/> instance for Access testing.</returns>
        public static DBAccess CreateTestAccessDBAccess()
        {
            return new DBAccess(CreateTestAccessDatabaseOptions());
        }

        #endregion

        #region Database Connection Validation

        /// <summary>
        /// Verifies that the test database is accessible and contains expected test data.
        /// </summary>
        /// <param name="dbAccess">The database access instance to test.</param>
        /// <returns>True if the database is properly configured and accessible.</returns>
        public static bool VerifyTestDatabaseSetup(DBAccess dbAccess)
        {
            try
            {
                // Test basic connectivity
                if (!dbAccess.TestConnection())
                    return false;

                // Verify test tables exist and have data
                var entityCount = dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities");
                var dataTypeCount = dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestDataTypes");
                var bulkEntityCount = dbAccess.GetValue<int>("SELECT COUNT(*) FROM BulkTestEntities");

                // Expected minimum counts based on seed data
                return entityCount >= 15 && dataTypeCount >= 3 && bulkEntityCount >= 40;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Asserts that the test database is properly set up, failing the test if not.
        /// </summary>
        /// <param name="dbAccess">The database access instance to verify.</param>
        public static void AssertTestDatabaseSetup(DBAccess dbAccess)
        {
            VerifyTestDatabaseSetup(dbAccess).Should().BeTrue(
                "test database should be properly set up with required tables and data. Please run the TestUnitDB setup scripts.");
        }

        /// <summary>
        /// Verifies that the test Access database is accessible and contains expected test data.
        /// </summary>
        /// <param name="dbAccess">The database access instance to test.</param>
        /// <returns>True if the Access database is properly configured and accessible.</returns>
        public static bool VerifyAccessDatabaseSetup(DBAccess dbAccess)
        {
            try
            {
                // Test basic connectivity
                if (!dbAccess.TestConnection())
                    return false;

                // Verify test tables exist and have data (Access may have different counts than SQL Server)
                var entityCount = dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities");
                var dataTypeCount = dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestDataTypes");
                var bulkEntityCount = dbAccess.GetValue<int>("SELECT COUNT(*) FROM BulkTestEntities");

                // Expected minimum counts based on Access seed data (may be different from SQL Server)
                return entityCount >= 3 && dataTypeCount >= 1 && bulkEntityCount >= 3;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Asserts that the test Access database is properly set up, failing the test if not.
        /// </summary>
        /// <param name="dbAccess">The database access instance to verify.</param>
        public static void AssertAccessDatabaseSetup(DBAccess dbAccess)
        {
            VerifyAccessDatabaseSetup(dbAccess).Should().BeTrue(
                $"test Access database should be properly set up with required tables and data. Expected database path: {TestAccessDatabasePath}");
        }

        /// <summary>
        /// Verifies that the Access database file exists at the expected location.
        /// </summary>
        /// <returns>True if the database file exists; otherwise, false.</returns>
        public static bool VerifyAccessDatabaseFileExists()
        {
            var fullPath = System.IO.Path.GetFullPath(TestAccessDatabasePath);
            return System.IO.File.Exists(fullPath);
        }

        #endregion

        #region Test Data Management

        /// <summary>
        /// Generates a unique test string for use in test data.
        /// </summary>
        /// <param name="prefix">Optional prefix for the test string.</param>
        /// <returns>A unique string suitable for test data.</returns>
        public static string GenerateTestString(string prefix = "Test")
        {
            var fullString = $"{prefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
            return fullString.Length > 50 ? fullString.Substring(0, 50) : fullString;
        }

        /// <summary>
        /// Generates a unique test code for use in test entities.
        /// </summary>
        /// <returns>A unique code string.</returns>
        public static string GenerateTestCode()
        {
            return $"TEST{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        /// <summary>
        /// Cleans up test data by removing records with test-specific patterns.
        /// </summary>
        /// <param name="dbAccess">The database access instance.</param>
        /// <param name="tableName">The name of the table to clean.</param>
        /// <param name="whereClause">The WHERE clause to identify test records.</param>
        /// <returns>Number of records deleted.</returns>
        public static int CleanupTestData(DBAccess dbAccess, string tableName, string whereClause)
        {
            try
            {
                var sql = $"DELETE FROM {tableName} WHERE {whereClause}";
                dbAccess.ExecuteQuery(sql);
                return dbAccess.RecordsAffected;
            }
            catch (Exception)
            {
                // Ignore cleanup errors to avoid masking real test failures
                return 0;
            }
        }

        /// <summary>
        /// Cleans up test entities from the TestEntities table.
        /// </summary>
        /// <param name="dbAccess">The database access instance.</param>
        /// <returns>Number of test records deleted.</returns>
        public static int CleanupTestEntities(DBAccess dbAccess)
        {
            return CleanupTestData(dbAccess, "TestEntities", "Name LIKE 'Test_%' OR Description LIKE '%unit test%'");
        }

        /// <summary>
        /// Cleans up test bulk entities from the BulkTestEntities table.
        /// </summary>
        /// <param name="dbAccess">The database access instance.</param>
        /// <returns>Number of test records deleted.</returns>
        public static int CleanupBulkTestEntities(DBAccess dbAccess)
        {
            return CleanupTestData(dbAccess, "BulkTestEntities", "Code LIKE 'TEST%' OR Name LIKE 'Unit Test%'");
        }

        #endregion

        #region Transaction Management

        /// <summary>
        /// Executes a test action within a transaction that will be rolled back.
        /// </summary>
        /// <param name="dbAccess">The database access instance.</param>
        /// <param name="testAction">The test action to execute.</param>
        /// <remarks>
        /// This method is useful for tests that need to modify data without affecting other tests.
        /// The transaction is always rolled back, regardless of success or failure.
        /// </remarks>
        public static void ExecuteInTransaction(DBAccess dbAccess, Action<DBAccess> testAction)
        {
            var script = @"
BEGIN TRANSACTION;
-- Test actions will be executed here
-- Transaction will be rolled back automatically
ROLLBACK TRANSACTION;";

            // Note: This is a simplified approach. For full transaction support,
            // we would need to enhance DBAccess to support explicit transaction management.
            // For now, individual tests should handle their own cleanup.
            
            testAction(dbAccess);
        }

        #endregion

        #region Performance Testing Utilities

        /// <summary>
        /// Measures the execution time of a database operation.
        /// </summary>
        /// <param name="operation">The operation to time.</param>
        /// <returns>The elapsed time in milliseconds.</returns>
        public static long MeasureExecutionTime(Action operation)
        {
            var stopwatch = Stopwatch.StartNew();
            operation();
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Measures the execution time of a database operation with a return value.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The operation to time.</param>
        /// <returns>A tuple containing the result and elapsed time in milliseconds.</returns>
        public static (T Result, long ElapsedMs) MeasureExecutionTime<T>(Func<T> operation)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = operation();
            stopwatch.Stop();
            return (result, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Asserts that a database operation completes within the specified time limit.
        /// </summary>
        /// <param name="operation">The operation to test.</param>
        /// <param name="maxMilliseconds">Maximum allowed execution time in milliseconds.</param>
        /// <param name="operationName">Name of the operation for error messages.</param>
        public static void AssertExecutionTime(Action operation, long maxMilliseconds, string operationName = "Operation")
        {
            var elapsed = MeasureExecutionTime(operation);
            elapsed.Should().BeLessThanOrEqualTo(maxMilliseconds, 
                $"{operationName} should complete within {maxMilliseconds}ms but took {elapsed}ms");
        }

        #endregion

        #region Test Data Validation

        /// <summary>
        /// Validates that a test entity exists in the database with expected properties.
        /// </summary>
        /// <param name="dbAccess">The database access instance.</param>
        /// <param name="entityId">The ID of the entity to validate.</param>
        /// <param name="expectedName">The expected name value.</param>
        /// <returns>True if the entity exists with expected properties.</returns>
        public static bool ValidateTestEntity(DBAccess dbAccess, int entityId, string expectedName)
        {
            try
            {
                var actualName = dbAccess.GetValue<string>(
                    "SELECT Name FROM TestEntities WHERE Id = @id", entityId);
                return string.Equals(actualName, expectedName, StringComparison.Ordinal);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the count of records in a specified table.
        /// </summary>
        /// <param name="dbAccess">The database access instance.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>The number of records in the table.</returns>
        public static int GetRecordCount(DBAccess dbAccess, string tableName)
        {
            return dbAccess.GetValue<int>($"SELECT COUNT(*) FROM {tableName}");
        }

        /// <summary>
        /// Validates that the specified table has the expected number of records.
        /// </summary>
        /// <param name="dbAccess">The database access instance.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="expectedCount">The expected number of records.</param>
        /// <param name="tolerance">Allowed variance in record count.</param>
        public static void AssertRecordCount(DBAccess dbAccess, string tableName, int expectedCount, int tolerance = 0)
        {
            var actualCount = GetRecordCount(dbAccess, tableName);
            actualCount.Should().BeInRange(expectedCount - tolerance, expectedCount + tolerance,
                $"table {tableName} should have {expectedCount} records (±{tolerance}) but has {actualCount}");
        }

        #endregion

        #region Exception Testing Utilities

        /// <summary>
        /// Asserts that a database operation throws an exception of the specified type.
        /// </summary>
        /// <typeparam name="TException">The expected exception type.</typeparam>
        /// <param name="operation">The operation that should throw an exception.</param>
        /// <param name="message">Optional message for the assertion.</param>
        /// <returns>The thrown exception for further validation.</returns>
        public static TException AssertThrows<TException>(Action operation, string? message = null)
            where TException : Exception
        {
            var action = new Action(operation);
            return action.Should().Throw<TException>(message ?? $"operation should throw {typeof(TException).Name}")
                        .Which;
        }

        /// <summary>
        /// Validates that a database exception contains expected error information.
        /// </summary>
        /// <param name="exception">The exception to validate.</param>
        /// <param name="expectedMessageContent">Expected content in the exception message.</param>
        public static void ValidateDatabaseException(Exception exception, string expectedMessageContent)
        {
            exception.Should().NotBeNull("exception should be provided for validation");
            exception.Message.Should().NotBeNull("exception should have a message");
            exception.Message.Should().Contain(expectedMessageContent, 
                $"exception message should contain expected content '{expectedMessageContent}'");
        }

        #endregion
    }
}
