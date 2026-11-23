using ByteForge.Toolkit.Logging;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Transactions;
#if NET48
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif

namespace ByteForge.Toolkit.Data;

public partial class DBAccess
{
    /// <summary>Exception-data key containing masked, formatted deadlock diagnostics for error reporters.</summary>
    public const string DeadlockTraceDataKey = DeadlockTraceDiagnostics.ExceptionDataKey;
    private readonly object _deadlockTraceLock = new object();
    private bool _deadlockTraceInitialized;
    private readonly ConditionalWeakTable<IDbConnection, DeadlockSession> _deadlockSessions = new ConditionalWeakTable<IDbConnection, DeadlockSession>();

    /// <summary>Captures the original session identity and its server clock without relying on the client timezone.</summary>
    private sealed class DeadlockSession
    {
        internal int Spid;
        internal DateTime ServerTime;
        internal Stopwatch Elapsed = Stopwatch.StartNew();
    }

    /// <summary>Initializes opt-in tracing after opening a connection and before beginning a local transaction.</summary>
    private void InitializeDeadlockTrace(IDbConnection connection)
    {
        if (!Options.EnableDeadlockTrace) return;
        try
        {
            lock (_deadlockTraceLock)
            {
                if (!(connection is SqlConnection))
                {
                    Options.EnableDeadlockTrace = false;
                    TraceWarning("Deadlock trace diagnostics disabled in memory: unsupported provider.");
                    return;
                }
                if (!_deadlockTraceInitialized)
                {
                    _deadlockTraceInitialized = true;
                    // SQL Server 2000 omits the Global column. A new, unpooled session
                    // has no session trace flags, so its legacy Status reflects global tracing.
                    using var suppression = new TransactionScope(TransactionScopeOption.Suppress);
                    var builder = new SqlConnectionStringBuilder(ConnectionString) {
                        Enlist = false, Pooling = false, ConnectTimeout = 3
                    };
                    using var diagnosticConnection = new SqlConnection(builder.ConnectionString);
                    diagnosticConnection.Open();
                    Options.EnableDeadlockTrace = DeadlockTraceDiagnostics.Configure(
                        flag => DeadlockTraceDiagnostics.IsGloballyEnabled(ReadDiagnosticTable(diagnosticConnection, "DBCC TRACESTATUS(" + flag + ")"), sessionIsFresh: true),
                        () => Convert.ToInt32(ReadDiagnosticTable(diagnosticConnection, "SELECT IS_SRVROLEMEMBER('sysadmin')").Rows[0][0]) == 1,
                        () => { ReadDiagnosticTable(diagnosticConnection, "DBCC TRACEON(1204, -1)"); }, TraceWarning);
                }
                if (!Options.EnableDeadlockTrace) return;
            }
            var clock = Stopwatch.StartNew();
            var identity = ReadDiagnosticTable(connection, "SELECT @@SPID AS Spid, GETDATE() AS ServerTime").Rows[0];
            _deadlockSessions.Add(connection, new DeadlockSession {
                Spid = Convert.ToInt32(identity["Spid"]), ServerTime = Convert.ToDateTime(identity["ServerTime"]), Elapsed = clock
            });
        }
        catch (Exception ex)
        {
            Options.EnableDeadlockTrace = false;
            TraceWarning("Deadlock trace diagnostics disabled in memory: " + ex.Message);
        }
    }

    /// <summary>Runs diagnostics directly, bypassing normal execution hooks and their retry/error state.</summary>
    private static DataTable ReadDiagnosticTable(IDbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 3;
        using var reader = command.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    /// <summary>Recognizes only SQL Server deadlock victim errors, including wrapped and multi-error exceptions.</summary>
    internal static bool IsDeadlockException(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
            if (current is SqlException sql && sql.Errors.Cast<SqlError>().Any(error => error.Number == 1205)) return true;
        return false;
    }

    /// <summary>Captures diagnostics outside the failed transaction without replacing or masking its original failure.</summary>
    private void CaptureDeadlockTrace(Exception exception, IDbConnection? failedConnection, string operationId)
    {
        if (!Options.EnableDeadlockTrace || !IsDeadlockException(exception)) return;
        DeadlockTraceDiagnostics.Capture(exception, operationId, () =>
        {
            if (failedConnection == null || !_deadlockSessions.TryGetValue(failedConnection, out var session))
                return "Deadlock trace capture unavailable: original SQL session identity was not recorded.";
            var failureTime = session.ServerTime + session.Elapsed.Elapsed;
            // A deadlock victim's transaction is already aborted. Never enlist the diagnostic
            // connection in the ambient transaction or reuse the failed local transaction.
            using var suppression = new TransactionScope(TransactionScopeOption.Suppress);
            var builder = new SqlConnectionStringBuilder(ConnectionString) { Enlist = false, ConnectTimeout = 3 };
            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            var log = ReadBoundedDeadlockLog(connection);
            return DeadlockTraceDiagnostics.Format(log, session.Spid, failureTime, operationId);
        }, report => Log.Error(report), TraceWarning);
    }

    /// <summary>Retains at most 512 KiB of the current log tail, with a short read budget and no recursive DBAccess calls.</summary>
    private static DataTable ReadBoundedDeadlockLog(SqlConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "EXEC master.dbo.xp_readerrorlog";
        command.CommandTimeout = 3;
        using var reader = command.ExecuteReader();
        var table = new DataTable();
        for (var i = 0; i < reader.FieldCount; i++) table.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
        var retained = new Queue<(object[] Values, int Size)>();
        var size = 0;
        var count = 0;
        var watch = Stopwatch.StartNew();
        while (reader.Read())
        {
            if (++count > 50000 || watch.ElapsedMilliseconds > 3000) { command.Cancel(); break; }
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            var rowSize = 0;
            for (var i = 0; i < values.Length; i++)
                if (values[i] is string value) { values[i] = value.Substring(0, Math.Min(value.Length, 32768)); rowSize += ((string)values[i]).Length; }
            retained.Enqueue((values, rowSize));
            size += rowSize;
            while (size > 262144 && retained.Count > 0) size -= retained.Dequeue().Size;
        }
        foreach (var row in retained) table.Rows.Add(row.Values);
        return table;
    }

    /// <summary>Logs diagnostic warnings without allowing a logging failure to affect application execution.</summary>
    private static void TraceWarning(string message)
    {
        try { Log.Warning(LogSecretMasker.Mask(message)); } catch { }
    }
}
