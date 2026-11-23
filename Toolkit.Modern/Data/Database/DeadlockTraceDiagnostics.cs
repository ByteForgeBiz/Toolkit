using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ByteForge.Toolkit.Logging;

namespace ByteForge.Toolkit.Data;

/// <summary>Permission negotiation and bounded SQL Server error-log formatting, independent of connection ownership.</summary>
internal static class DeadlockTraceDiagnostics
{
    internal const string ExceptionDataKey = "ByteForge.DeadlockTrace";
    internal const int MaximumCharacters = 32768;
    private static readonly Regex LegacyPrefix = new Regex(
        @"^(?<time>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d+)\s+(?<process>\S+)\s*(?<text>.*)$", RegexOptions.Compiled);

    /// <summary>Attaches masked diagnostics while preserving the original exception even if capture or logging fails.</summary>
    internal static void Capture(Exception original, string operationId, Func<string> read, Action<string> log, Action<string> warning)
    {
        try
        {
            var report = LogSecretMasker.Mask(read());
            original.Data[ExceptionDataKey] = report;
            try { log(report); } catch { }
        }
        catch (Exception ex)
        {
            var message = LogSecretMasker.Mask("Deadlock trace capture unavailable; operationId=" + operationId + "; " + ex.Message);
            try { original.Data[ExceptionDataKey] = message; } catch { }
            try { warning(message); } catch { }
        }
    }

    /// <summary>Reuses existing global tracing before requiring administrator privileges.</summary>
    internal static bool Configure(Func<int, bool> isEnabled, Func<bool> isSysadmin, Action enable, Action<string> warning)
    {
        try
        {
            if (isEnabled(1204) || isEnabled(1222)) return true;
            if (!isSysadmin())
            {
                warning("Deadlock trace diagnostics disabled in memory: global tracing is not enabled and the connected login is not sysadmin.");
                return false;
            }
            enable();
            if (isEnabled(1204)) return true;
            warning("Deadlock trace diagnostics disabled in memory: global trace 1204 could not be verified.");
        }
        catch (Exception ex)
        {
            warning("Deadlock trace diagnostics disabled in memory: " + ex.Message);
        }
        return false;
    }

    /// <summary>Reads global status; legacy two-column output requires a fresh, unpooled session with no session flags.</summary>
    internal static bool IsGloballyEnabled(DataTable status, bool sessionIsFresh = false) => status.Rows.Cast<DataRow>().Any(row =>
        status.Columns.Contains("Status") && row["Status"] != DBNull.Value &&
        Convert.ToInt32(row["Status"], CultureInfo.InvariantCulture) == 1 &&
        (status.Columns.Contains("Global")
            ? row["Global"] != DBNull.Value && Convert.ToInt32(row["Global"], CultureInfo.InvariantCulture) == 1
            : sessionIsFresh && status.Columns.Count == 2 && status.Columns.Contains("TraceFlag")));

    /// <summary>Formats candidate reports from legacy or modern error-log rows using server time and exact SPID tokens.</summary>
    internal static string Format(DataTable log, int spid, DateTime failureTime, string operationId)
    {
        var reports = new List<(DateTime Time, string Process, StringBuilder Text)>();
        var active = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        DateTime previousTime = DateTime.MinValue;
        string previousProcess = "";
        foreach (DataRow row in log.Rows)
        {
            string text;
            string process;
            DateTime time;
            if (log.Columns.Contains("ERRORLOG"))
            {
                var raw = Convert.ToString(row["ERRORLOG"], CultureInfo.InvariantCulture) ?? "";
                var match = LegacyPrefix.Match(raw);
                if (match.Success && DateTime.TryParse(match.Groups["time"].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                {
                    process = match.Groups["process"].Value;
                    text = match.Groups["text"].Value;
                    previousTime = time;
                    previousProcess = process;
                }
                else if (log.Columns.Contains("ContinuationRow") && Convert.ToInt32(row["ContinuationRow"], CultureInfo.InvariantCulture) == 1)
                {
                    time = previousTime;
                    process = previousProcess;
                    text = raw;
                }
                else continue;
            }
            else if (log.Columns.Contains("LogDate") && log.Columns.Contains("Text") && log.Columns.Contains("ProcessInfo"))
            {
                time = Convert.ToDateTime(row["LogDate"], CultureInfo.InvariantCulture);
                process = Convert.ToString(row["ProcessInfo"], CultureInfo.InvariantCulture) ?? "";
                text = Convert.ToString(row["Text"], CultureInfo.InvariantCulture) ?? "";
            }
            else continue;

            if (Math.Abs((time - failureTime).TotalSeconds) > 5) continue;
            if (Regex.IsMatch(text, @"Printing deadlock information|Wait-for graph|deadlock-list|deadlock encountered", RegexOptions.IgnoreCase))
            {
                // Some versions print both a preamble and a Wait-for graph header.
                if (!active.TryGetValue(process, out var existing) || reports[existing].Text.ToString().IndexOf("SPID", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    active[process] = reports.Count;
                    reports.Add((time, process, new StringBuilder()));
                }
            }
            if (active.TryGetValue(process, out var index))
            {
                var buffer = reports[index].Text;
                if (buffer.Length < MaximumCharacters) buffer.AppendLine(text.Substring(0, Math.Min(text.Length, MaximumCharacters - buffer.Length)));
            }
        }
        var spidPattern = @"\bSPID\s*[:=]\s*[""']?" + spid.ToString(CultureInfo.InvariantCulture) + @"\b";
        var candidates = reports.Where(report => Regex.IsMatch(report.Text.ToString(), spidPattern, RegexOptions.IgnoreCase)).ToList();
        var result = new StringBuilder($"SQL Server deadlock diagnostics; operationId={operationId}; victimSpid={spid}; serverTime={failureTime:yyyy-MM-dd HH:mm:ss.fff}\n");
        if (candidates.Count == 0)
            return result.Append("No matching report is available in the bounded current error-log snapshot. Logging may be delayed, the log may have rolled over, or capture may have been disabled externally.").ToString();
        result.AppendLine("Candidate report(s), matched by server time and SPID; not a unique correlation. Confirm the victim and lock cycle.");
        foreach (var report in candidates)
            result.AppendLine($"--- {report.Time:yyyy-MM-dd HH:mm:ss.fff} / {report.Process} ---").Append(report.Text);
        if (result.Length > MaximumCharacters)
            return result.ToString(0, MaximumCharacters) + "\n[Deadlock diagnostics truncated]";
        return result.ToString();
    }
}
