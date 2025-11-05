using ByteForge.Toolkit.Tests.Helpers;
using AwesomeAssertions;

namespace ByteForge.Toolkit.Tests.Unit.Data.Database
{
    /// <summary>
    /// Automated tests for transaction support in the <see cref="DBAccess"/> class.
    /// </summary>
    /// <remarks>
    /// These tests verify transaction commit, rollback, exception handling, and read consistency.
    /// </remarks>
    [TestClass]
    public class DBAccessTransactionTests
    {
        private DBAccess _dbAccess;

        [TestInitialize]
        public void TestInitialize()
        {
            _dbAccess = DatabaseTestHelper.CreateTestDBAccess();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DatabaseTestHelper.CleanupTestEntities(_dbAccess);
        }

        /// <summary>
        /// Verifies that committing a transaction persists the data.
        /// </summary>
        [TestMethod]
        public void Transaction_Commit_ShouldPersistData()
        {
            var testName = DatabaseTestHelper.GenerateTestString("TxnCommit");
            using (var txn = _dbAccess.BeginTransaction())
            {
                var result = _dbAccess.ExecuteQuery(
                    "INSERT INTO TestEntities (Name, Description, IsActive) VALUES (@name, @desc, @active)",
                    testName, "Transaction commit test", true);
                result.Should().BeTrue();
                txn.Commit();
            }
            // After commit, record should exist
            var count = _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities WHERE Name = @name", testName);
            count.Should().Be(1, "Record should be persisted after commit");
        }

        /// <summary>
        /// Verifies that rolling back a transaction does not persist the data.
        /// </summary>
        [TestMethod]
        public void Transaction_Rollback_ShouldNotPersistData()
        {
            var testName = DatabaseTestHelper.GenerateTestString("TxnRollback");
            using (var txn = _dbAccess.BeginTransaction())
            {
                var result = _dbAccess.ExecuteQuery(
                    "INSERT INTO TestEntities (Name, Description, IsActive) VALUES (@name, @desc, @active)",
                    testName, "Transaction rollback test", true);
                result.Should().BeTrue();
                txn.Rollback();
            }
            // After rollback, record should not exist
            var count = _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities WHERE Name = @name", testName);
            count.Should().Be(0, "Record should not be persisted after rollback");
        }

        /// <summary>
        /// Verifies that an error during a transaction causes a rollback (no exception thrown).
        /// </summary>
        [TestMethod]
        public void Transaction_Exception_ShouldRollback()
        {
            var testName = DatabaseTestHelper.GenerateTestString("TxnException");
            using (var txn = _dbAccess.BeginTransaction())
            {
                var result = _dbAccess.ExecuteQuery(
                    "INSERT INTO TestEntities (Name, Description, IsActive) VALUES (@name, @desc, @active)",
                    testName, "Transaction exception test", true);
                result.Should().BeTrue();
                // Cause an error (invalid SQL)
                var errorResult = _dbAccess.ExecuteQuery("INVALID SQL");
                errorResult.Should().BeFalse("ExecuteQuery should fail for invalid SQL");
                _dbAccess.LastException.Should().NotBeNull("LastException should be set after error");
                // Do not commit, transaction should be rolled back on dispose
            }
            // After transaction scope, record should not exist
            var count = _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities WHERE Name = @name", testName);
            count.Should().Be(0, "Record should not be persisted after error and rollback");
        }

        /// <summary>
        /// Verifies that data is visible within the transaction before commit.
        /// </summary>
        [TestMethod]
        public void Transaction_ReadConsistency_ShouldSeeUncommittedData()
        {
            var testName = DatabaseTestHelper.GenerateTestString("TxnRead");
            using (var txn = _dbAccess.BeginTransaction())
            {
                var result = _dbAccess.ExecuteQuery(
                    "INSERT INTO TestEntities (Name, Description, IsActive) VALUES (@name, @desc, @active)",
                    testName, "Transaction read test", true);
                result.Should().BeTrue();
                // Should be able to read the data within the transaction
                var count = _dbAccess.GetValue<int>("SELECT COUNT(*) FROM TestEntities WHERE Name = @name", testName);
                count.Should().Be(1, "Should see uncommitted data within transaction");
                txn.Rollback();
            }
        }

        // Add more tests for nested transactions if supported by DBAccess
    }
}
