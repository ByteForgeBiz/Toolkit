using ByteForge.Toolkit.Logging;
using System;
using System.Data;
using System.Globalization;
using System.Text;
#if NET48
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif

namespace ByteForge.Toolkit.Data;

public partial class DBAccess
{
    /// <summary>
    /// Logs a tabular estimated plan without executing the command. SHOWPLAN_ALL supports
    /// SQL Server 2000 and modern servers; the server enforces its version-specific permissions.
    /// Uses the original session so existing temporary tables and transaction state are visible.
    /// </summary>
    /// <param name="command">The configured command that will subsequently execute normally.</param>
    /// <param name="reference">The short query reference.</param>
    /// <param name="operationId">The full operation identifier.</param>
    /// <param name="attempt">The execution attempt number.</param>
    private void LogEstimatedPlan(IDbCommand command, string reference, string operationId, int attempt)
    {
        var context = $"reference={reference}; operationId={operationId}; attempt={attempt}";
        if (!(command is SqlCommand sqlCommand))
        {
            Log.Warning($"Estimated execution plan skipped; {context}; provider does not support SQL Server SHOWPLAN_ALL.");
            return;
        }

        // Clone parameters as well as command metadata: plan capture must not overwrite
        // the real command's output or return-value parameters.
        using var planCommand = sqlCommand.Clone();
        using var settingCommand = sqlCommand.Connection.CreateCommand();
        settingCommand.Transaction = sqlCommand.Transaction;
        settingCommand.CommandTimeout = sqlCommand.CommandTimeout;
        try
        {
            settingCommand.CommandText = "SET SHOWPLAN_ALL ON";
            settingCommand.ExecuteNonQuery();
            var plan = new StringBuilder();
            using (var reader = planCommand.ExecuteReader())
            {
                do
                {
                    for (var field = 0; field < reader.FieldCount; field++)
                        plan.Append(field == 0 ? "" : "\t").Append(reader.GetName(field));
                    plan.AppendLine();
                    while (reader.Read())
                    {
                        for (var field = 0; field < reader.FieldCount; field++)
                            plan.Append(field == 0 ? "" : "\t")
                                .Append(Convert.ToString(reader.GetValue(field), CultureInfo.InvariantCulture));
                        plan.AppendLine();
                    }
                } while (reader.NextResult());
            }
            Log.Verbose($"Estimated execution plan; {context}:{Environment.NewLine}{plan}");
        }
        catch (Exception ex)
        {
            // Includes missing SHOWPLAN permissions and batches whose temporary tables
            // cannot exist until execution. Neither should prevent normal execution.
            Log.Warning($"Estimated execution plan unavailable; {context}; {ex.Message}");
        }
        finally
        {
            try
            {
                // Reset even when enabling/capturing timed out: the server may have
                // accepted the setting before the client received the failure.
                settingCommand.CommandText = "SET SHOWPLAN_ALL OFF";
                settingCommand.ExecuteNonQuery();
            }
            catch
            {
                sqlCommand.Connection.Close();
                throw new InvalidOperationException(
                    $"Cannot restore SHOWPLAN_ALL OFF; connection closed to prevent execution with SHOWPLAN enabled; {context}.");
            }
        }
    }
}
