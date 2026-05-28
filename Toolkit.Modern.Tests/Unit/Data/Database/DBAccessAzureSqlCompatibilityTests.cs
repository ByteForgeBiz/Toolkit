using AwesomeAssertions;
using ByteForge.Toolkit.Data;
using ByteForge.Toolkit.Tests.Helpers;
using System.Reflection;

namespace ByteForge.Toolkit.Tests.Unit.Data.Database
{
    /// <summary>
    /// Unit tests for Azure SQL compatibility behavior in <see cref="DBAccess"/>.
    /// </summary>
    /// <remarks>
    /// These tests do not require a live database. They guard source and provider compatibility
    /// for callers that use the legacy <see cref="DBAccess.DataBaseType.AzureSQL"/> enum value.
    /// </remarks>
    [TestClass]
    public class DBAccessAzureSqlCompatibilityTests
    {
        /// <summary>
        /// Tests that the database type enumeration includes Azure SQL.
        /// </summary>
        [TestMethod]
        public void DataBaseType_Enumeration_ShouldContainAzureSql()
        {
            Enum.IsDefined(typeof(DBAccess.DataBaseType), DBAccess.DataBaseType.AzureSQL).Should().BeTrue();
        }

        /// <summary>
        /// Tests that Azure SQL can be assigned to a DBAccess instance.
        /// </summary>
        [TestMethod]
        public void Constructor_WithAzureSqlDatabaseType_ShouldSetDbType()
        {
            var options = DatabaseTestHelper.CreateTestDatabaseOptions();
            options.DatabaseType = DBAccess.DataBaseType.AzureSQL;

            var dbAccess = new DBAccess(options);

            dbAccess.DbType.Should().Be(DBAccess.DataBaseType.AzureSQL);
        }

        /// <summary>
        /// Tests that Azure SQL uses the SQL client provider path.
        /// </summary>
        /// <remarks>
        /// This guards legacy compatibility: Azure SQL is a distinct configuration value, but it
        /// should use the same ADO.NET provider family as SQL Server rather than falling into ODBC
        /// or unsupported-type handling.
        /// </remarks>
        [TestMethod]
        public void CreateConnection_WithAzureSql_ShouldUseSqlClientConnection()
        {
            var options = DatabaseTestHelper.CreateTestDatabaseOptions();
            options.DatabaseType = DBAccess.DataBaseType.AzureSQL;
            var dbAccess = new DBAccess(options);
            var method = typeof(DBAccess).GetMethod("CreateConnection", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Should().NotBeNull("CreateConnection should exist for provider selection");

            var connection = method.Invoke(dbAccess, null);

            connection.Should().NotBeNull();
            connection!.GetType().Name.Should().Be("SqlConnection");
        }

        /// <summary>
        /// Tests that Azure SQL generates an Azure SQL connection string.
        /// </summary>
        /// <remarks>
        /// Azure SQL remains source-compatible with the legacy enum value while using the
        /// generated connection-string shape from the older toolkit.
        /// </remarks>
        [TestMethod]
        public void GetConnectionString_WithAzureSql_ShouldUseAzureSqlShape()
        {
            var options = DatabaseTestHelper.CreateTestDatabaseOptions();
            options.DatabaseType = DBAccess.DataBaseType.AzureSQL;
            options.ConnectionString = "";
            options.Server = "example.database.windows.net";
            options.DatabaseName = "Amch";
            options.UseTrustedConnection = false;

            var connectionString = options.GetConnectionString();

            connectionString.Should().Contain("Data Source=example.database.windows.net")
                            .And.Contain("Initial Catalog=Amch")
                            .And.Contain("TrustServerCertificate=False")
                            .And.NotContain("DSN=");
        }

        /// <summary>
        /// Tests that Azure SQL uses SQL Server parameter parsing rules.
        /// </summary>
        /// <remarks>
        /// Azure SQL should not be treated as ODBC: named stored procedure assignment syntax must
        /// extract only the value-side parameters, matching SQL Server behavior.
        /// </remarks>
        [TestMethod]
        public void ParseParameters_AzureSqlNamedAssignment_ShouldReturnValueParameters()
        {
            var options = DatabaseTestHelper.CreateTestDatabaseOptions();
            options.DatabaseType = DBAccess.DataBaseType.AzureSQL;
            var dbAccess = new DBAccess(options);
            var method = typeof(DBAccess).GetMethod("ParseParameters", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Should().NotBeNull("ParseParameters should exist for provider-specific parsing");
            var query = "EXEC MyStoredProc @InputParam = @value1, @OutputParam = @value2";

            var parameters = method.Invoke(dbAccess, [query]) as List<string>;

            parameters.Should().NotBeNull();
            parameters.Should().HaveCount(2);
            parameters.Should().Contain("@value1");
            parameters.Should().Contain("@value2");
            parameters.Should().NotContain("@InputParam");
            parameters.Should().NotContain("@OutputParam");
        }
    }
}
