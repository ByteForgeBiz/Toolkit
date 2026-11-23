using ByteForge.Toolkit.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Transactions;

namespace ByteForge.Toolkit.Tests.Unit.Data.Database;

/// <summary>Checks ambient transaction isolation without opening a database connection.</summary>
[TestClass]
[TestCategory("Unit")]
public class DBAccessIsolationTests
{
    /// <summary>Explicit read-committed scope and nested default calls use the same isolation.</summary>
    [TestMethod]
    public void ReadCommittedScope_NestedDefaultInheritsIsolation()
    {
        var db = new DBAccess(new DatabaseOptions { DatabaseType = DBAccess.DataBaseType.SQLServer,
            ConnectionString = "Server=(local);Database=master;Integrated Security=true;Encrypt=false" });
        using (var outer = db.BeginTransaction(false, IsolationLevel.ReadCommitted))
        {
            Assert.AreEqual(IsolationLevel.ReadCommitted, Transaction.Current.IsolationLevel);
            using (var inner = db.BeginTransaction(false))
            {
                Assert.AreEqual(IsolationLevel.ReadCommitted, Transaction.Current.IsolationLevel);
                inner.Commit();
            }
            outer.Commit();
        }
        Assert.IsNull(Transaction.Current);
    }
}
