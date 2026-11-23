using ByteForge.Toolkit.Data;
using ByteForge.Toolkit.Tests.Helpers;
using System.Data;

namespace ByteForge.Toolkit.Tests.Unit.Data.Database;

[TestClass]
[TestCategory("SQLServer")]
[DoNotParallelize]
public class DBAccessConnectionTransactionTests
{
    private DisposableSqlDatabase fixture = null!;
    private DBAccess db = null!;

    [TestInitialize]
    public void Initialize()
    {
        fixture = new DisposableSqlDatabase();
        fixture.Execute("CREATE TABLE dbo.Changes (Id INT PRIMARY KEY, Value INT NOT NULL)");
        db = fixture.CreateAccess();
    }

    [TestCleanup]
    public void Cleanup() => fixture?.Dispose();

    [TestMethod]
    public void CommandsShareSessionAndCommitTogether()
    {
        using (var transaction = db.BeginConnectionTransaction())
        {
            var spid = db.GetValue<int>("SELECT @@SPID");
            Assert.IsTrue(db.ExecuteQuery("CREATE TABLE #SessionValues (Value INT); INSERT INTO #SessionValues VALUES (7)"));
            Assert.AreEqual(spid, db.GetValue<int>("SELECT @@SPID"));
            Assert.AreEqual(7, db.GetValue<int>("SELECT Value FROM #SessionValues"));
            Assert.AreEqual(1, db.GetValue<int>("SELECT @@TRANCOUNT"));
            Assert.IsTrue(db.ExecuteQuery("INSERT INTO dbo.Changes VALUES (1, 7)"));
            Assert.IsTrue(db.ExecuteQuery("UPDATE dbo.Changes SET Value=8 WHERE Id=1"));
            transaction.Complete();
        }
        Assert.AreEqual(8, fixture.CreateAccess().GetValue<int>("SELECT Value FROM dbo.Changes WHERE Id=1"));
        Assert.AreEqual(0, db.GetValue<int>("SELECT @@TRANCOUNT"));
    }

    [TestMethod]
    public void DisposalWithoutCompleteRollsBackAndReleasesOwner()
    {
        var abandoned = db.BeginConnectionTransaction();
        Assert.IsTrue(db.ExecuteQuery("INSERT INTO dbo.Changes VALUES (1, 7)"));
        abandoned.Dispose();
        abandoned.Dispose();
        Assert.ThrowsExactly<ObjectDisposedException>(() => abandoned.Complete());
        Assert.AreEqual(0, db.GetValue<int>("SELECT COUNT(*) FROM dbo.Changes"));
        using (var next = db.BeginConnectionTransaction())
        {
            Assert.IsTrue(db.ExecuteQuery("INSERT INTO dbo.Changes VALUES (2, 9)"));
            next.Complete();
        }
        Assert.AreEqual(9, db.GetValue<int>("SELECT Value FROM dbo.Changes WHERE Id=2"));
    }

    [TestMethod]
    public void ExceptionUnwindingRollsBackCompletedWrites()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            using var transaction = db.BeginConnectionTransaction();
            Assert.IsTrue(db.ExecuteQuery("INSERT INTO dbo.Changes VALUES (1, 7)"));
            throw new InvalidOperationException("application failure");
        });
        Assert.AreEqual(0, db.GetValue<int>("SELECT COUNT(*) FROM dbo.Changes"));
    }

    [TestMethod]
    public void FailedCommandPreventsCommitEvenAfterSuccessfulRead()
    {
        using (var transaction = db.BeginConnectionTransaction())
        {
            Assert.IsTrue(db.ExecuteQuery("INSERT INTO dbo.Changes VALUES (1, 7)"));
            Assert.IsFalse(db.ExecuteQuery("INSERT INTO dbo.Changes VALUES (1, 99)"));
            Assert.IsNotNull(db.LastException);
            // A successful diagnostic read must not make the partial write committable again.
            Assert.AreEqual(7, db.GetValue<int>("SELECT Value FROM dbo.Changes WHERE Id=1"));
            transaction.Complete();
        }
        Assert.AreEqual(0, db.GetValue<int>("SELECT COUNT(*) FROM dbo.Changes"));
        using (var next = db.BeginConnectionTransaction())
        {
            Assert.IsTrue(db.ExecuteQuery("INSERT INTO dbo.Changes VALUES (2, 10)"));
            next.Complete();
        }
        Assert.AreEqual(10, db.GetValue<int>("SELECT Value FROM dbo.Changes WHERE Id=2"));
    }

    [TestMethod]
    public void TimeoutIsAttemptedOnceInsideConnectionTransaction()
    {
        fixture.Execute("CREATE SEQUENCE dbo.Attempts AS INT START WITH 1 INCREMENT BY 1");
        db.Options.RetryEnabled = true;
        db.Options.RetryMaxAttempts = 3;
        db.Options.RetryDelayMs = 1;
        db.Options.CommandTimeout = 1;
        using (var transaction = db.BeginConnectionTransaction())
        {
            Assert.IsFalse(db.ExecuteQuery("SELECT NEXT VALUE FOR dbo.Attempts; WAITFOR DELAY '00:00:02'"));
            Assert.IsNotNull(db.LastException);
            transaction.Complete();
        }
        // Sequence values survive rollbacks, so this detects repeated execution rather than elapsed time.
        Assert.AreEqual(1, fixture.CreateAccess().GetValue<int>(
            "SELECT CONVERT(INT,current_value) FROM sys.sequences WHERE name='Attempts'"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task BulkInsertParticipatesInOwningTransaction(bool complete)
    {
        using (var transaction = db.BeginConnectionTransaction())
        {
            var bulk = new BulkDbProcessor<ChangeRow>("dbo.Changes") { CreateDestinationTable = false };
            Assert.IsTrue(await bulk.BulkInsertAsync(db,
                new[] { new ChangeRow { Id = 1, Value = 7 }, new ChangeRow { Id = 2, Value = 8 } },
                System.Threading.CancellationToken.None));
            Assert.AreEqual(2, db.GetValue<int>("SELECT COUNT(*) FROM dbo.Changes"));
            if (complete) transaction.Complete();
        }
        Assert.AreEqual(complete ? 2 : 0, fixture.CreateAccess().GetValue<int>("SELECT COUNT(*) FROM dbo.Changes"));
    }

    [TestMethod]
    public void NestedAndMixedTransactionModelsAreRejected()
    {
        using (db.BeginConnectionTransaction(IsolationLevel.ReadCommitted))
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => db.BeginConnectionTransaction());
            Assert.ThrowsExactly<InvalidOperationException>(() => db.BeginTransaction());
        }
        using (db.BeginTransaction(autoCommit: false))
            Assert.ThrowsExactly<InvalidOperationException>(() => db.BeginConnectionTransaction());
        using (var scope = new System.Transactions.TransactionScope())
            Assert.ThrowsExactly<InvalidOperationException>(() => db.BeginConnectionTransaction());
    }

    public sealed class ChangeRow
    {
        [DBColumn("Id", isPrimaryKey: true)]
        public int Id { get; set; }
        [DBColumn("Value")]
        public int Value { get; set; }
    }
}
