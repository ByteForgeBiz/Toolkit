using AwesomeAssertions;
using Config = ByteForge.Toolkit.Configuration.Configuration;
using Logg = ByteForge.Toolkit.Logging.Log;
using ByteForge.Toolkit.Data;
using ByteForge.Toolkit.Logging;
using ByteForge.Toolkit.Tests.Helpers;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Logging
{
    [TestClass]
    [TestCategory("Unit")]
    [TestCategory("Logging")]
    public class DatabaseLoggerTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            ResetLogAndConfiguration();
            TempFileHelper.CleanupTempFiles();
        }

        [TestMethod]
        public void SuppressedScope_ShouldBlockStaticLogDispatch()
        {
            InitializeLoggingConfig();
            var captureLogger = new CaptureLogger();
            Log.Instance.AddLogger(captureLogger);

            try
            {
                using (Log.BeginSuppressedScope())
                {
                    Log.Info("suppressed message");
                }

                Log.Info("visible message");

                captureLogger.Messages.Should().ContainSingle()
                    .Which.Should().Be("visible message");
            }
            finally
            {
                Log.Instance.RemoveLogger(captureLogger);
            }
        }

        [TestMethod]
        public void SuppressedScope_ShouldSupportNestedScopes()
        {
            InitializeLoggingConfig();
            var captureLogger = new CaptureLogger();
            Log.Instance.AddLogger(captureLogger);

            try
            {
                using (Log.BeginSuppressedScope())
                {
                    using (Log.BeginSuppressedScope())
                    {
                        Log.Info("nested suppressed");
                    }

                    Log.Info("still suppressed");
                }

                Log.Info("after scope");

                captureLogger.Messages.Should().ContainSingle()
                    .Which.Should().Be("after scope");
            }
            finally
            {
                Log.Instance.RemoveLogger(captureLogger);
            }
        }

        [TestMethod]
        public void DatabaseLogger_RecordLogEntry_ShouldRunInitializationInsideSuppression()
        {
            var logger = new InspectableDatabaseLogger(CreateSqlServerOptions(), new DatabaseLoggerOptions());

            logger.LogInfo("db init message");

            logger.WasSuppressedDuringTableCheck.Should().BeTrue();
            logger.WasSuppressedDuringInsert.Should().BeTrue();
            logger.IsDisabled.Should().BeFalse();
        }

        [TestMethod]
        public void DatabaseLogger_WhenInitializationFails_ShouldDisableItselfWithoutThrowing()
        {
            var logger = new InspectableDatabaseLogger(CreateSqlServerOptions(), new DatabaseLoggerOptions())
            {
                TableExistsResult = false,
                CommandResult = false,
            };

            Action action = () => logger.LogInfo("should not throw");

            action.Should().NotThrow();
            logger.IsDisabled.Should().BeTrue();
        }

        [TestMethod]
        public void DatabaseLogger_WhenInsertFails_ShouldDisableItselfWithoutThrowing()
        {
            var logger = new InspectableDatabaseLogger(CreateSqlServerOptions(), new DatabaseLoggerOptions())
            {
                TableExistsResult = true,
                CommandResult = false,
            };

            Action action = () => logger.LogInfo("insert failure");

            action.Should().NotThrow();
            logger.IsDisabled.Should().BeTrue();
            logger.InsertAttemptCount.Should().Be(1);
        }

        [TestMethod]
        public void DatabaseLogger_ResolveDatabaseOptions_ShouldPreferNamedSectionAndFallbackToDefault()
        {
            var iniPath = CreateDatabaseLoggingIni(
                """
                [Logging]
                bUseDatabaseLogging=True
                sLogFile={LOG_FILE}
                [DatabaseLogger]
                bEnabled=True
                sDatabaseSection=LoggingDb
                bAutoCreateTable=False
                [Data Source]
                SelectedDB=PrimaryDb
                [PrimaryDb]
                sType=SQLServer
                sServer=PrimaryServer
                sDatabaseName=PrimaryDatabase
                sConnectionString=Server=PrimaryServer;Database=PrimaryDatabase;Trusted_Connection=true;
                [LoggingDb]
                sType=ODBC
                sConnectionString=Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=C:\Logs\logging.accdb;
                """
            );

            Config.Initialize(iniPath);

            var namedLogger = new DatabaseLogger(new DatabaseLoggerOptions { DatabaseSection = "LoggingDb" });
            var fallbackLogger = new DatabaseLogger(new DatabaseLoggerOptions { DatabaseSection = "MissingSection" });

            namedLogger.ResolveDatabaseOptions()!.DatabaseType.Should().Be(DBAccess.DataBaseType.ODBC);
            fallbackLogger.ResolveDatabaseOptions()!.DatabaseType.Should().Be(DBAccess.DataBaseType.SQLServer);
        }

        [TestMethod]
        public void DatabaseLogger_WithNoDatabaseConfiguration_ShouldBehaveAsNullLogger()
        {
            var logger = new DatabaseLogger(new DatabaseLoggerOptions());

            Action action = () => logger.LogInfo("no configuration");

            action.Should().NotThrow();
            logger.IsDisabled.Should().BeTrue();
        }

        [TestMethod]
        public void DatabaseLogger_CanAutoCreate_ShouldHandleSqlServerAndRecognizedOdbc()
        {
            var logger = new DatabaseLogger(new DatabaseLoggerOptions());

            logger.CanAutoCreate(CreateSqlServerOptions()).Should().BeTrue();
            logger.CanAutoCreate(CreateRecognizedOdbcOptions()).Should().BeTrue();
            logger.CanAutoCreate(CreateUnknownOdbcOptions()).Should().BeFalse();
        }

        [TestMethod]
        public void DatabaseLogger_BuildStatements_ShouldUseConfiguredTableName()
        {
            var logger = new DatabaseLogger(new DatabaseLoggerOptions { TableName = "ToolkitLogs" });

            var insertSql = logger.BuildInsertStatement();
            var createSql = logger.BuildCreateTableStatement(DBAccess.DataBaseType.ODBC);

            insertSql.Should().Contain("[ToolkitLogs]");
            insertSql.Should().Contain("@LogTimestamp");
            createSql.Should().Contain("CREATE TABLE [ToolkitLogs]");
        }

        [TestMethod]
        public void Log_WhenDatabaseLoggingEnabled_ShouldAddDatabaseLogger()
        {
            var iniPath = CreateDatabaseLoggingIni(
                """
                [Logging]
                bUseDatabaseLogging=True
                sLogFile={LOG_FILE}
                [DatabaseLogger]
                bEnabled=True
                bAutoCreateTable=False
                sDatabaseSection=LoggingDb
                [Data Source]
                SelectedDB=PrimaryDb
                [PrimaryDb]
                sType=SQLServer
                sServer=PrimaryServer
                sDatabaseName=PrimaryDatabase
                sConnectionString=Server=PrimaryServer;Database=PrimaryDatabase;Trusted_Connection=true;
                [LoggingDb]
                sType=ODBC
                sConnectionString=Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=C:\Logs\logging.accdb;
                """
            );

            Config.Initialize(iniPath);

            Log.Instance.Any(logger => logger is DatabaseLogger).Should().BeTrue();
        }

        private static string CreateDatabaseLoggingIni(string iniContent)
        {
            ResetLogAndConfiguration();
            var logFilePath = TempFileHelper.GetTempFilePath(".log");
            return TempFileHelper.CreateTempIniFile(iniContent.Replace("{LOG_FILE}", logFilePath));
        }

        private static void InitializeLoggingConfig()
        {
            var iniPath = CreateDatabaseLoggingIni(
                """
                [Logging]
                sLogFile={LOG_FILE}
                eLogLevel=All
                bUseDatabaseLogging=False
                """
            );

            Config.Initialize(iniPath);
        }

        private static DatabaseOptions CreateSqlServerOptions()
        {
            return new DatabaseOptions
            {
                DatabaseType = DBAccess.DataBaseType.SQLServer,
                Server = "Server01",
                DatabaseName = "Toolkit",
                ConnectionString = "Server=Server01;Database=Toolkit;Trusted_Connection=true;",
            };
        }

        private static DatabaseOptions CreateRecognizedOdbcOptions()
        {
            return new DatabaseOptions
            {
                DatabaseType = DBAccess.DataBaseType.ODBC,
                ConnectionString = "Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=C:\\Logs\\Toolkit.accdb;",
            };
        }

        private static DatabaseOptions CreateUnknownOdbcOptions()
        {
            return new DatabaseOptions
            {
                DatabaseType = DBAccess.DataBaseType.ODBC,
                ConnectionString = "Driver={Some Generic ODBC Driver};Server=localhost;",
            };
        }

        private static void ResetLogAndConfiguration()
        {
            var configurationField = typeof(Config)
                .GetField("_defaultInstance", BindingFlags.NonPublic | BindingFlags.Static);
            configurationField?.SetValue(null, null);

            var logField = typeof(Log).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            if (logField?.GetValue(null) is IDisposable disposableLog)
                disposableLog.Dispose();

            logField?.SetValue(null, null);
        }

        private sealed class CaptureLogger : BaseLogger
        {
            public CaptureLogger() : base("Capture") { }

            public List<string?> Messages { get; } = new List<string?>();

            protected internal override void RecordLogEntry(LogEntry entry)
            {
                Messages.Add(entry.Message);
            }
        }

        private sealed class InspectableDatabaseLogger : DatabaseLogger
        {
            public InspectableDatabaseLogger(DatabaseOptions databaseOptions, DatabaseLoggerOptions options)
                : base(databaseOptions, options)
            {
            }

            public bool TableExistsResult { get; set; } = true;

            public bool CommandResult { get; set; } = true;

            public bool WasSuppressedDuringTableCheck { get; private set; }

            public bool WasSuppressedDuringInsert { get; private set; }

            public int InsertAttemptCount { get; private set; }

            protected override DBAccess CreateDbAccess(DatabaseOptions databaseOptions)
            {
                return new DBAccess(databaseOptions);
            }

            protected override bool DoesTableExist(DBAccess db, DBAccess.DataBaseType databaseType)
            {
                WasSuppressedDuringTableCheck = Logg.IsSuppressed;
                return TableExistsResult;
            }

            protected override bool ExecuteNonQuery(DBAccess db, string query, params object?[]? arguments)
            {
                WasSuppressedDuringInsert = Logg.IsSuppressed;

                if (query.Contains("INSERT INTO", StringComparison.OrdinalIgnoreCase))
                    InsertAttemptCount++;

                return CommandResult;
            }
        }
    }
}
