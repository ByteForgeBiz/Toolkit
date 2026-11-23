using ByteForge.Toolkit.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
#if NET48
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif

namespace ByteForge.Toolkit.Tests.Unit.Data.Database;

/// <summary>Exercises permission fallback, report correlation and deadlock classification without modifying any SQL server.</summary>
[TestClass]
[TestCategory("Unit")]
public class DBAccessDeadlockTraceTests
{
    private static readonly DateTime FailureTime = new DateTime(2026, 11, 9, 11, 23, 20);

    [TestMethod]
    public void Configuration_DefaultsToDisabled()
    {
        Assert.IsFalse(new DatabaseOptions().EnableDeadlockTrace);
    }

    [TestMethod]
    public void Existing1204_AllowsCaptureWithoutCheckingSysadminOrEnabling()
    {
        Assert.IsTrue(DeadlockTraceDiagnostics.Configure(flag => flag == 1204,
            () => throw new Exception("Must not check sysadmin"), () => Assert.Fail("Must not enable"), message => Assert.Fail(message)));
    }

    [TestMethod]
    public void Existing1222_AllowsCaptureWithoutSysadmin()
    {
        Assert.IsTrue(DeadlockTraceDiagnostics.Configure(flag => flag == 1222,
            () => throw new Exception("Must not check sysadmin"), () => Assert.Fail("Must not enable"), message => Assert.Fail(message)));
    }

    [TestMethod]
    public void NoTracing_NonAdminDisablesAndWarnsOnce()
    {
        var warnings = new List<string>();
        Assert.IsFalse(DeadlockTraceDiagnostics.Configure(flag => false, () => false,
            () => Assert.Fail("Must not enable"), warnings.Add));
        Assert.AreEqual(1, warnings.Count);
        StringAssert.Contains(warnings[0], "not sysadmin");
    }

    [TestMethod]
    public void NoTracing_AdminEnablesAndVerifies1204()
    {
        var enabled = false;
        Assert.IsTrue(DeadlockTraceDiagnostics.Configure(flag => enabled && flag == 1204,
            () => true, () => enabled = true, message => Assert.Fail(message)));
        Assert.IsTrue(enabled);
    }

    [TestMethod]
    public void PermissionFailureAndFailedVerification_AreNonFatal()
    {
        var warnings = new List<string>();
        Assert.IsFalse(DeadlockTraceDiagnostics.Configure(flag => throw new UnauthorizedAccessException("denied"),
            () => true, () => { }, warnings.Add));
        Assert.IsFalse(DeadlockTraceDiagnostics.Configure(flag => false, () => true, () => { }, warnings.Add));
        Assert.AreEqual(2, warnings.Count);
    }

    [TestMethod]
    public void SessionOnlyTrace_DoesNotCountAsGlobal()
    {
        var table = new DataTable();
        table.Columns.Add("Status", typeof(int));
        table.Columns.Add("Global", typeof(int));
        table.Rows.Add(1, 0);
        Assert.IsFalse(DeadlockTraceDiagnostics.IsGloballyEnabled(table));
        table.Rows.Add(1, 1);
        Assert.IsTrue(DeadlockTraceDiagnostics.IsGloballyEnabled(table));
    }

    [TestMethod]
    public void LegacyReport_PreservesBothParticipantsAndContinuationButExcludesUnrelatedProcess()
    {
        var log = LegacyLog();
        Add(log, "Printing deadlock information");
        Add(log, "Wait-for graph");
        Add(log, "SPID: 101 ECID: 0 Statement Type: SELECT");
        log.Rows.Add("continued input buffer", 1);
        log.Rows.Add("2026-11-09 11:23:20.00 spid90 unrelated private query", 0);
        Add(log, "SPID: 129 ECID: 0 Statement Type: UPDATE");
        Add(log, "Victim Resource Owner: SPID: 101");
        var report = DeadlockTraceDiagnostics.Format(log, 101, FailureTime, "operation");
        StringAssert.Contains(report, "SPID: 129");
        StringAssert.Contains(report, "continued input buffer");
        StringAssert.Contains(report, "Candidate report");
        Assert.IsFalse(report.Contains("unrelated private query"));
    }

    [TestMethod]
    public void Correlation_RejectsWrongSpidAndOldReports()
    {
        var log = LegacyLog();
        Add(log, "Wait-for graph");
        Add(log, "SPID: 1010");
        StringAssert.Contains(DeadlockTraceDiagnostics.Format(log, 101, FailureTime, "op"), "No matching report");
        StringAssert.Contains(DeadlockTraceDiagnostics.Format(log, 1010, FailureTime.AddMinutes(1), "op"), "No matching report");
    }

    [TestMethod]
    public void Modern1222Rows_KeepMatchingReportAndExcludeNextDeadlock()
    {
        var log = new DataTable();
        log.Columns.Add("LogDate", typeof(DateTime));
        log.Columns.Add("ProcessInfo");
        log.Columns.Add("Text");
        log.Rows.Add(FailureTime, "spid4", "deadlock-list");
        log.Rows.Add(FailureTime, "spid4", "process spid=101 lockMode=S");
        log.Rows.Add(FailureTime, "spid4", "process spid=129 lockMode=X");
        log.Rows.Add(FailureTime, "spid4", "deadlock-list");
        log.Rows.Add(FailureTime, "spid4", "process spid=200 lockMode=X");
        var report = DeadlockTraceDiagnostics.Format(log, 101, FailureTime, "op");
        StringAssert.Contains(report, "spid=129");
        Assert.IsFalse(report.Contains("spid=200"));
    }

    [TestMethod]
    public void ReportSize_IsBoundedAndMarkedAsTruncated()
    {
        var log = LegacyLog();
        Add(log, "Wait-for graph");
        Add(log, "SPID: 101");
        Add(log, new string('x', 40000));
        var report = DeadlockTraceDiagnostics.Format(log, 101, FailureTime, "op");
        Assert.IsTrue(report.Length <= DeadlockTraceDiagnostics.MaximumCharacters + 40);
        StringAssert.Contains(report, "truncated");
    }

    [TestMethod]
    public void Classifier_RecognizesWrapped1205ButRejectsTimeoutAndMessageOnly()
    {
        Assert.IsTrue(DBAccess.IsDeadlockException(new InvalidOperationException("wrapper", SqlFailure(1205))));
        Assert.IsFalse(DBAccess.IsDeadlockException(SqlFailure(-2)));
        Assert.IsFalse(DBAccess.IsDeadlockException(new Exception("deadlock 1205")));
    }

    [TestMethod]
    public void CaptureFailureAndBrokenLogger_PreserveOriginalExceptionAndMaskDiagnosticFailure()
    {
        var original = new InvalidOperationException("Original SQL error");
        original.Data["existing"] = "preserve";
        var reads = 0;
        DeadlockTraceDiagnostics.Capture(original, "op", () => { reads++; throw new Exception("password=private-value"); },
            report => throw new Exception("broken log"), warning => throw new Exception("broken warning"));
        Assert.AreEqual(1, reads);
        Assert.AreEqual("Original SQL error", original.Message);
        Assert.AreEqual("preserve", original.Data["existing"]);
        var diagnostic = (string)original.Data[DBAccess.DeadlockTraceDataKey];
        StringAssert.Contains(diagnostic, "capture unavailable");
        Assert.IsFalse(diagnostic.Contains("private-value"));
    }

    [TestMethod]
    public void CapturedReport_RemainsAttachedEvenWhenLoggingFails()
    {
        var original = new Exception("Original SQL error");
        DeadlockTraceDiagnostics.Capture(original, "op", () => "Candidate password=private-value",
            report => throw new Exception("broken log"), warning => Assert.Fail(warning));
        var diagnostic = (string)original.Data[DBAccess.DeadlockTraceDataKey];
        StringAssert.Contains(diagnostic, "Candidate");
        Assert.IsFalse(diagnostic.Contains("private-value"));
    }

    /// <summary>Creates provider exceptions without relying on a live database or a real deadlock.</summary>
    private static SqlException SqlFailure(int number)
    {
        var constructor = typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];
        var args = new List<object>();
        foreach (var parameter in constructor.GetParameters())
            args.Add(parameter.ParameterType == typeof(string) ? "test" : parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null);
        args[0] = number;
        var error = (SqlError)constructor.Invoke(args.ToArray());
        var collection = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), true);
        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(collection, new object[] { error });
        var factory = typeof(SqlException).GetMethod("CreateException", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(SqlErrorCollection), typeof(string) }, null);
        return (SqlException)factory.Invoke(null, new object[] { collection, "test" });
    }

    private static DataTable LegacyLog()
    {
        var log = new DataTable();
        log.Columns.Add("ERRORLOG");
        log.Columns.Add("ContinuationRow", typeof(int));
        return log;
    }

    private static void Add(DataTable log, string text) => log.Rows.Add("2026-11-09 11:23:20.00 spid4 " + text, 0);
}
