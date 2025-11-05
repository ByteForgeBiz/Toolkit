#if NETFRAMEWORK

using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
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
        private TransactionScope _scope;
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
            _scope = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Suppress);
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        public void Commit()
        {
            _scope.Complete();
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

    /// <summary>
    /// Partial class for DBAccess to support transaction management.
    /// </summary>
    public partial class DBAccess
    {
        private Stack<DBTransaction> _transactionStack = new Stack<DBTransaction>();

        /// <summary>
        /// Begins a new transaction and pushes it onto the transaction stack.
        /// </summary>
        /// <param name="autoCommit">If true, changes are automatically committed when the transaction is disposed, unless an exception occurred.</param>
        /// <returns>A <see cref="DBTransaction"/> instance.</returns>
        public DBTransaction BeginTransaction(bool autoCommit = true)
        {
            var transaction = new DBTransaction(this, autoCommit);
            _transactionStack.Push(transaction);
            Log.Trace($"Began transaction {transaction.Id}. Transaction depth: {_transactionStack.Count}");
            return transaction;
        }

        /// <summary>
        /// Gets a value indicating whether a transaction is currently active.
        /// </summary>
        public bool IsInTransaction => _transactionStack.Count > 0;

        /// <summary>
        /// Gets the current depth of the transaction stack.
        /// </summary>
        public int TransactionDepth => _transactionStack.Count;

        /// <summary>
        /// Commits the current transaction if one is active.
        /// </summary>
        /// <remarks>
        /// This method finalizes the current transaction by applying all changes made within it.
        /// If no transaction is active, the method performs no operation.
        /// </remarks>
        public void CommitTransaction()
        {
            if (IsInTransaction)
            {
                var currentTransaction = _transactionStack.Peek();
                currentTransaction.Commit();
            }
        }

        /// <summary>
        /// Rolls back the current transaction if one is active.
        /// </summary>
        /// <remarks>
        /// This method reverts all changes made during the current transaction.  
        /// It has no effect if there is no active transaction.
        /// </remarks>
        public void RollbackTransaction()
        {
            if (IsInTransaction)
            {
                var currentTransaction = _transactionStack.Peek();
                currentTransaction.Rollback();
            }
        }

        /// <summary>
        /// Pops the top transaction from the stack (called on commit).
        /// </summary>
        internal void PopTransaction()
        {
            if (_transactionStack.Count > 0)
                _transactionStack.Pop();
        }

        /// <summary>
        /// Clears the transaction stack, rolling back all nested transactions.
        /// </summary>
        internal void ClearTransactionStack()
        {
            while (_transactionStack.Count > 0)
                _transactionStack.Pop();
        }
    }
}
#endif
