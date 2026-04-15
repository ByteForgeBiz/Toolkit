using AwesomeAssertions;
using ByteForge.Toolkit.Data;
using ByteForge.Toolkit.Logging;
using ByteForge.Toolkit.Tests.Helpers;
using Config = ByteForge.Toolkit.Configuration.Configuration;
using StaticLog = ByteForge.Toolkit.Logging.Log;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Logging
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Logging")]
    [TestCategory("SQLServer")]
    public class DatabaseLoggerSqlServerLiveTests
    {
        private const string SqlTableName = "DatabaseLoggerTestEntries";
        private DBAccess _dbAccess = null!;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            DatabaseTestHelper.AssertTestDatabaseSetup(dbAccess);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            DatabaseLoggerLiveTestHelper.ResetLogAndConfiguration();
            DatabaseLoggerLiveTestHelper.InitializeDiagnosticLogging(nameof(DatabaseLoggerSqlServerLiveTests));
            _dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            DatabaseLoggerLiveTestHelper.DropSqlServerTable(_dbAccess, SqlTableName);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DatabaseLoggerLiveTestHelper.DropSqlServerTable(_dbAccess, SqlTableName);
            DatabaseLoggerLiveTestHelper.ResetLogAndConfiguration();
            TempFileHelper.CleanupTempFiles();
        }

        [TestMethod]
        public void DatabaseLogger_SQLServer_ShouldAutoCreateTableAndPersistInfoEntry()
        {
            var logger = new DatabaseLogger(
                DatabaseTestHelper.CreateTestDatabaseOptions(),
                new DatabaseLoggerOptions
                {
                    TableName = SqlTableName,
                    AutoCreateTable = true,
                });

            var message = DatabaseTestHelper.GenerateTestString("DbLogSqlInfo");

            logger.LogInfo(message);

            DatabaseLoggerLiveTestHelper.SqlServerTableExists(_dbAccess, SqlTableName).Should().BeTrue();
            DatabaseTestHelper.GetRecordCount(_dbAccess, SqlTableName).Should().Be(1);

            var row = DatabaseLoggerLiveTestHelper.GetLatestLogEntry(_dbAccess, SqlTableName, message);

            DatabaseLoggerLiveTestHelper.AssertStoredEntry(
                row,
                expectedLevel: LogLevel.Info,
                expectedMessage: message,
                expectedLoggerName: "Database",
                expectedSource: LogLevel.Info.ToString(),
                expectCorrelationId: false,
                expectedException: null);
        }

        [TestMethod]
        public void DatabaseLogger_SQLServer_ShouldPersistExceptionDetails()
        {
            var logger = new DatabaseLogger(
                DatabaseTestHelper.CreateTestDatabaseOptions(),
                new DatabaseLoggerOptions
                {
                    TableName = SqlTableName,
                    AutoCreateTable = true,
                });

            var message = DatabaseTestHelper.GenerateTestString("DbLogSqlErr");
            var exception = new InvalidOperationException("SQL live logger test exception");

            logger.LogError(message, exception);

            var row = DatabaseLoggerLiveTestHelper.GetLatestLogEntry(_dbAccess, SqlTableName, message);

            DatabaseLoggerLiveTestHelper.AssertStoredEntry(
                row,
                expectedLevel: LogLevel.Error,
                expectedMessage: message,
                expectedLoggerName: "Database",
                expectedSource: LogLevel.Error.ToString(),
                expectCorrelationId: false,
                expectedException: exception);
        }

        [TestMethod]
        public void StaticLog_SQLServerDatabaseLoggerConfig_ShouldWriteUsingNamedDatabaseSection()
        {
            var message = DatabaseTestHelper.GenerateTestString("DbLogStaticSql");
            var iniPath = DatabaseLoggerLiveTestHelper.CreateDatabaseLoggerConfig(
                tableName: SqlTableName,
                databaseSectionName: "LiveLoggingDb",
                databaseOptions: DatabaseTestHelper.CreateTestDatabaseOptions());

            Config.Initialize(iniPath);

            StaticLog.Info(message);

            var row = DatabaseLoggerLiveTestHelper.GetLatestLogEntry(_dbAccess, SqlTableName, message);

            DatabaseLoggerLiveTestHelper.AssertStoredEntry(
                row,
                expectedLevel: LogLevel.Info,
                expectedMessage: message,
                expectedLoggerName: "ByteForge.Toolkit->Static Logger",
                expectedSource: LogLevel.Info.ToString(),
                expectCorrelationId: true,
                expectedException: null);
        }
    }

    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Logging")]
    [TestCategory("ODBC")]
    public class DatabaseLoggerOdbcLiveTests
    {
        private const string OdbcTableName = "DatabaseLoggerAccessTestEntries";
        private DBAccess _dbAccess = null!;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var dbAccess = DatabaseTestHelper.CreateTestAccessDBAccess();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            DatabaseLoggerLiveTestHelper.ResetLogAndConfiguration();
            DatabaseLoggerLiveTestHelper.InitializeDiagnosticLogging(nameof(DatabaseLoggerOdbcLiveTests));
            _dbAccess = DatabaseTestHelper.CreateTestAccessDBAccess();
            DatabaseLoggerLiveTestHelper.DropAccessTable(_dbAccess, OdbcTableName);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DatabaseLoggerLiveTestHelper.DropAccessTable(_dbAccess, OdbcTableName);
            DatabaseLoggerLiveTestHelper.ResetLogAndConfiguration();
            TempFileHelper.CleanupTempFiles();
        }

        [TestMethod]
        public void DatabaseLogger_ODBC_ShouldAutoCreateTableAndPersistInfoEntry()
        {
            var logger = new DatabaseLogger(
                DatabaseTestHelper.CreateTestAccessDatabaseOptions(),
                new DatabaseLoggerOptions
                {
                    TableName = OdbcTableName,
                    AutoCreateTable = true,
                });

            var message = DatabaseTestHelper.GenerateTestString("DbLogOdbcInfo");

            logger.LogInfo(message);

            DatabaseLoggerLiveTestHelper.AccessTableExists(_dbAccess, OdbcTableName).Should().BeTrue();
            DatabaseTestHelper.GetRecordCount(_dbAccess, OdbcTableName).Should().Be(1);

            var row = DatabaseLoggerLiveTestHelper.GetLatestLogEntry(_dbAccess, OdbcTableName, message);

            DatabaseLoggerLiveTestHelper.AssertStoredEntry(
                row,
                expectedLevel: LogLevel.Info,
                expectedMessage: message,
                expectedLoggerName: "Database",
                expectedSource: LogLevel.Info.ToString(),
                expectCorrelationId: false,
                expectedException: null);
        }

        [TestMethod]
        public void DatabaseLogger_ODBC_ShouldPersistExceptionDetails()
        {
            var logger = new DatabaseLogger(
                DatabaseTestHelper.CreateTestAccessDatabaseOptions(),
                new DatabaseLoggerOptions
                {
                    TableName = OdbcTableName,
                    AutoCreateTable = true,
                });

            var message = DatabaseTestHelper.GenerateTestString("DbLogOdbcErr");
            var exception = new InvalidOperationException("ODBC live logger test exception");

            logger.LogError(message, exception);

            var row = DatabaseLoggerLiveTestHelper.GetLatestLogEntry(_dbAccess, OdbcTableName, message);

            DatabaseLoggerLiveTestHelper.AssertStoredEntry(
                row,
                expectedLevel: LogLevel.Error,
                expectedMessage: message,
                expectedLoggerName: "Database",
                expectedSource: LogLevel.Error.ToString(),
                expectCorrelationId: false,
                expectedException: exception);
        }
    }

    internal static class DatabaseLoggerLiveTestHelper
    {
        private const string DiagnosticLogDirectoryEnvVar = "BYTEFORGE_TEST_LOG_DIR";

        internal static void ResetLogAndConfiguration()
        {
            var configurationField = typeof(Config)
                .GetField("_defaultInstance", BindingFlags.NonPublic | BindingFlags.Static);
            configurationField?.SetValue(null, null);

            var logField = typeof(StaticLog).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            if (logField?.GetValue(null) is IDisposable disposableLog)
                disposableLog.Dispose();

            logField?.SetValue(null, null);
        }

        internal static void InitializeDiagnosticLogging(string scopeName)
        {
            var diagnosticLogDirectory = Environment.GetEnvironmentVariable(DiagnosticLogDirectoryEnvVar);
            if (string.IsNullOrWhiteSpace(diagnosticLogDirectory))
                return;

            Directory.CreateDirectory(diagnosticLogDirectory);

            var safeScopeName = string.Concat(scopeName.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
            var filePrefix = $"{safeScopeName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
            var iniPath = Path.Combine(diagnosticLogDirectory, $"{filePrefix}.ini");
            var logFilePath = Path.Combine(diagnosticLogDirectory, $"{filePrefix}.log");

            var iniContent = $"""
                [Logging]
                sLogFile={logFilePath}
                eLogLevel=All
                bUseDatabaseLogging=False
                """;

            File.WriteAllText(iniPath, iniContent);
            Config.Initialize(iniPath);
        }

        internal static string CreateDatabaseLoggerConfig(string tableName, string databaseSectionName, DatabaseOptions databaseOptions)
        {
            ResetLogAndConfiguration();
            var diagnosticLogDirectory = Environment.GetEnvironmentVariable(DiagnosticLogDirectoryEnvVar);
            var logFilePath = string.IsNullOrWhiteSpace(diagnosticLogDirectory)
                ? TempFileHelper.GetTempFilePath(".log")
                : CreateDiagnosticFilePath(diagnosticLogDirectory, $"{tableName}_StaticDatabaseLogger", ".log");
            var iniContent = $"""
                [Logging]
                sLogFile={logFilePath}
                eLogLevel=All
                bUseDatabaseLogging=True
                [DatabaseLogger]
                bEnabled=True
                bAutoCreateTable=True
                sTableName={tableName}
                eMinLogLevel=All
                sDatabaseSection={databaseSectionName}
                [{databaseSectionName}]
                sType={databaseOptions.DatabaseType}
                sConnectionString={databaseOptions.GetConnectionString()}
                """;

            if (string.IsNullOrWhiteSpace(diagnosticLogDirectory))
                return TempFileHelper.CreateTempIniFile(iniContent);

            var iniPath = CreateDiagnosticFilePath(diagnosticLogDirectory, $"{tableName}_StaticDatabaseLogger", ".ini");
            File.WriteAllText(iniPath, iniContent);
            return iniPath;
        }

        private static string CreateDiagnosticFilePath(string directory, string prefix, string extension)
        {
            Directory.CreateDirectory(directory);
            var safePrefix = string.Concat(prefix.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
            return Path.Combine(directory, $"{safePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}");
        }

        internal static void DropSqlServerTable(DBAccess dbAccess, string tableName)
        {
            dbAccess.ExecuteQuery($"""
                IF OBJECT_ID(N'{tableName}', N'U') IS NOT NULL
                    DROP TABLE [{tableName}]
                """);
        }

        internal static void DropAccessTable(DBAccess dbAccess, string tableName)
        {
            try
            {
                dbAccess.ExecuteQuery($"DROP TABLE [{tableName}]");
            }
            catch
            {
                // Ignore cleanup failures when the table does not exist.
            }
        }

        internal static bool SqlServerTableExists(DBAccess dbAccess, string tableName)
        {
            return dbAccess.GetValue<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName",
                tableName) > 0;
        }

        internal static bool AccessTableExists(DBAccess dbAccess, string tableName)
        {
            dbAccess.GetRecord($"SELECT TOP 1 * FROM [{tableName}]");
            return dbAccess.LastException == null;
        }

        internal static System.Data.DataRow GetLatestLogEntry(DBAccess dbAccess, string tableName, string message)
        {
            var row = dbAccess.GetRecord(
                $"SELECT TOP 1 * FROM [{tableName}] WHERE [Message] = @message ORDER BY [LogTimestamp] DESC",
                message);

            row.Should().NotBeNull($"expected a stored log entry in table {tableName} for message '{message}'");
            return row!;
        }

        internal static void AssertStoredEntry(
            System.Data.DataRow row,
            LogLevel expectedLevel,
            string expectedMessage,
            string expectedLoggerName,
            string expectedSource,
            bool expectCorrelationId,
            Exception? expectedException)
        {
            row["LogLevel"].ToString().Should().Be(expectedLevel.ToString());
            row["LoggerName"].ToString().Should().Be(expectedLoggerName);
            row["Source"].ToString().Should().Be(expectedSource);
            row["Message"].ToString().Should().Be(expectedMessage);
            row["ThreadId"].Should().NotBe(DBNull.Value);
            Convert.ToInt32(row["ThreadId"]).Should().BeGreaterThan(0);
            row["LogTimestamp"].Should().NotBe(DBNull.Value);
            Convert.ToDateTime(row["LogTimestamp"]).Should().BeOnOrBefore(DateTime.Now.AddSeconds(5));

            if (expectCorrelationId)
            {
                row["CorrelationId"].Should().NotBe(DBNull.Value);
                row["CorrelationId"].ToString().Should().NotBeNullOrWhiteSpace();
            }
            else
            {
                (row["CorrelationId"] == DBNull.Value || string.IsNullOrWhiteSpace(row["CorrelationId"].ToString())).Should().BeTrue();
            }

            if (expectedException == null)
            {
                (row["ExceptionType"] == DBNull.Value || string.IsNullOrWhiteSpace(row["ExceptionType"].ToString())).Should().BeTrue();
                (row["ExceptionMessage"] == DBNull.Value || string.IsNullOrWhiteSpace(row["ExceptionMessage"].ToString())).Should().BeTrue();
                (row["ExceptionStackTrace"] == DBNull.Value || string.IsNullOrWhiteSpace(row["ExceptionStackTrace"].ToString())).Should().BeTrue();
                return;
            }

            row["ExceptionType"].ToString().Should().Be(expectedException.GetType().FullName);
            row["ExceptionMessage"].ToString().Should().Be(expectedException.Message);
            row["ExceptionStackTrace"].ToString().Should().Contain(expectedException.Message);
        }
    }
}
