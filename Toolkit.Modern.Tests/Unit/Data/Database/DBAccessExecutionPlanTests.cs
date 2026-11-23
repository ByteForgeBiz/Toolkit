using ByteForge.Toolkit.Data;
using ByteForge.Toolkit.Tests.Helpers;

namespace ByteForge.Toolkit.Tests.Unit.Data.Database;

/// <summary>Exercises estimated-plan capture without modifying persistent application data.</summary>
[TestClass]
public class DBAccessExecutionPlanTests
{
    /// <summary>Plan capture is opt-in for existing configurations.</summary>
    [TestMethod]
    public void EstimatedPlan_DefaultsToDisabled()
    {
        Assert.IsFalse(new DatabaseOptions().LogEstimatedExecutionPlan);
    }

    /// <summary>Plan result sets must not replace the normal parameterized scalar result.</summary>
    [TestMethod]
    public void EstimatedPlan_PreservesParameterizedResult()
    {
        var options = DatabaseTestHelper.CreateTestDatabaseOptions();
        options.LogEstimatedExecutionPlan = true;
        var db = new DBAccess(options);

        Assert.AreEqual(42, db.GetValue<int>("SELECT @Value", 42));
        Assert.IsNull(db.LastException);
    }

    /// <summary>Existing session temporary tables remain visible and inserts execute only once.</summary>
    [TestMethod]
    public void EstimatedPlan_PreservesSessionAndExecutesOnce()
    {
        var options = DatabaseTestHelper.CreateTestDatabaseOptions();
        options.LogEstimatedExecutionPlan = true;
        var db = new DBAccess(options);
        using var transaction = db.BeginConnectionTransaction();

        Assert.IsTrue(db.ExecuteQuery("CREATE TABLE #EstimatedPlanTest (Value INT NOT NULL)"));
        Assert.IsTrue(db.ExecuteQuery("INSERT INTO #EstimatedPlanTest (Value) SELECT @Value", 42));
        Assert.AreEqual(1, db.GetValue<int>("SELECT COUNT(*) FROM #EstimatedPlanTest"));
        Assert.AreEqual(42, db.GetValue<int>("SELECT Value FROM #EstimatedPlanTest"));
        Assert.IsNull(db.LastException);
        // Disposal rolls back the test transaction, including its temporary table.
    }

    /// <summary>Unplannable create-and-reference batches must still execute normally.</summary>
    [TestMethod]
    public void EstimatedPlan_TemporaryTableBatchFallsBackToNormalExecution()
    {
        var options = DatabaseTestHelper.CreateTestDatabaseOptions();
        options.LogEstimatedExecutionPlan = true;
        var db = new DBAccess(options);

        Assert.AreEqual(42, db.GetValue<int>(
            "SET NOCOUNT ON; CREATE TABLE #PlanFallback (Value INT); " +
            "INSERT INTO #PlanFallback (Value) SELECT 42; SELECT Value FROM #PlanFallback"));
        Assert.IsNull(db.LastException);
    }
}
