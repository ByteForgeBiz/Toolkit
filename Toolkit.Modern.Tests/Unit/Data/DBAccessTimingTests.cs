using ByteForge.Toolkit.Data;
using ByteForge.Toolkit.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Threading;

namespace ByteForge.Toolkit.Tests.Unit.Data
{
    [TestClass]
[TestCategory("Unit")]
    public class DBAccessTimingTests
    {
        [TestMethod]
        public void TimeFunc_MeasuresCallbackAndPreservesFailure()
        {
            var logger = new CaptureLogger();
            var previousThreading = Log.Instance.EnableMultiThreading;
            Log.Instance.EnableMultiThreading = false;
            Log.Instance.AddLogger(logger);
            try
            {
                var db = new DBAccess(new DatabaseOptions());
                var method = typeof(DBAccess).GetMethod("TimeFunc", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(typeof(int));
                Assert.AreEqual(42, method.Invoke(db, new object[] { new Func<int>(() => { Thread.Sleep(30); return 42; }), "TEST" }));
                Assert.IsTrue(TimeSpan.Parse(logger.Message.Substring("Query (TEST) executed in ".Length).TrimEnd('.')).TotalMilliseconds >= 20);
                var expected = new InvalidOperationException("test failure");
                try { method.Invoke(db, new object[] { new Func<int>(() => { Thread.Sleep(30); throw expected; }), "TEST" }); Assert.Fail(); }
                catch (TargetInvocationException ex) { Assert.AreSame(expected, ex.InnerException); }
                Assert.IsTrue(TimeSpan.Parse(logger.Message.Substring("Query (TEST) failed after ".Length).TrimEnd('.')).TotalMilliseconds >= 20);
            }
            finally { Log.Instance.RemoveLogger(logger); Log.Instance.EnableMultiThreading = previousThreading; }
        }

        private sealed class CaptureLogger : BaseLogger
        {
            public CaptureLogger() : base("Timing regression") { MinLogLevel = LogLevel.Trace; }
            public string Message { get; private set; }
            protected internal override void RecordLogEntry(LogEntry entry) { Message = entry.Message; }
        }
    }
}
