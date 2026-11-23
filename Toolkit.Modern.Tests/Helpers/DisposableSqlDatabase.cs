using ByteForge.Toolkit.Data;
#if NET48
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif

namespace ByteForge.Toolkit.Tests.Helpers;

/// <summary>Owns a uniquely named database; never resets the configured test database.</summary>
internal sealed class DisposableSqlDatabase : IDisposable
{
    private readonly string name = "ByteForgeRegression_" + Guid.NewGuid().ToString("N");
    private readonly string masterConnectionString;
    public string ConnectionString { get; }

    public DisposableSqlDatabase()
    {
        var configured = DatabaseTestHelper.CreateTestDatabaseOptions();
        var master = new SqlConnectionStringBuilder(configured.GetConnectionString())
        {
            InitialCatalog = "master", Pooling = false, Enlist = false, ConnectTimeout = 5,
            TrustServerCertificate = true
        };
        masterConnectionString = master.ConnectionString;
        ConnectionString = new SqlConnectionStringBuilder(master.ConnectionString)
        {
            InitialCatalog = name, Pooling = true, Enlist = true
        }.ConnectionString;
        ExecuteOn(masterConnectionString, "CREATE DATABASE [" + name + "]");
    }

    public DBAccess CreateAccess() => new(new DatabaseOptions
    {
        DatabaseType = DBAccess.DataBaseType.SQLServer,
        ConnectionString = ConnectionString,
        RetryEnabled = false
    });

    public void Execute(string sql) => ExecuteOn(ConnectionString, sql);

    private static void ExecuteOn(string connectionString, string sql)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection) { CommandTimeout = 15 };
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        // The name is generated here, never supplied by a connection string or test caller.
        ExecuteOn(masterConnectionString,
            "ALTER DATABASE [" + name + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + name + "]");
    }
}
