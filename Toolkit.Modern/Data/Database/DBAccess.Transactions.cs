namespace ByteForge.Toolkit;
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
