using System;
using System.Transactions;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Represents a database transaction context for use with <see cref="DBAccess"/>.
    /// Supports nested transactions via a stack model.
    /// </summary>
    public class DBTransaction : IDisposable
    {
        private bool _completed;
        private bool _rollingBack;
        private TransactionScope? _scope;
        private readonly DBAccess _dbAccess;

        /// <summary>
        /// Gets a value indicating whether changes are automatically committed to the data source.
        /// </summary>
        public bool AutoCommit { get; }

        /// <summary>
        /// Gets the unique identifier for this transaction instance.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString().Substring(0, 8);

        internal DBTransaction(DBAccess dbAccess, bool autoCommit)
        {
            _dbAccess = dbAccess;
            AutoCommit = autoCommit;
            _scope = new TransactionScope(TransactionScopeOption.Required);
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        public void Commit()
        {
            _scope?.Complete();
            _completed = true;
            _dbAccess.PopTransaction();
            Log.Trace($"Committed transaction {Id}. Remaining transaction depth: {_dbAccess.TransactionDepth}");
        }

        /// <summary>
        /// Rolls back the current transaction, discarding any changes made within it.
        /// </summary>
        /// <remarks>
        /// This method marks the transaction as completed to prevent it from being automatically
        /// committed  when the object is disposed. 
        /// After calling this method, the transaction is removed from the transaction stack.
        /// </remarks>
        public void Rollback()
        {
            _completed = true;      // Mark as completed to prevent auto-commit on dispose
            _rollingBack = true;    // Indicate that a rollback is in progress
            Dispose();              // Dispose the scope without completing it to roll back
        }

        /// <summary>
        /// Disposes the transaction, rolling back if not committed and rollback is required.
        /// </summary>
        public void Dispose()
        {
            if (AutoCommit &&                      // Only commit if auto-commit is enabled
                _dbAccess.IsInTransaction &&       // we are still in a transaction
                _dbAccess.LastException == null && // if there were no exceptions
                !_completed                        // and the transaction hasn't already been completed.
                )
                Commit();

            // If not completed, clear the entire transaction stack to roll back all nested transactions
            if (!_completed || _rollingBack)
            {
                _dbAccess.ClearTransactionStack();
                Log.Trace($"Rolled back transaction {Id}. All nested transactions cleared.");
            }

            _scope?.Dispose();
            _scope = null;
        }
    }
}