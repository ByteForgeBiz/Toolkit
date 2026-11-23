using ByteForge.Toolkit.Logging;
using System.Data;
#if NET48
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif

namespace ByteForge.Toolkit.Data;
/// <summary>
/// Partial class for DBAccess to support transaction management.
/// </summary>
public partial class DBAccess
{
    private Stack<DBTransaction> _transactionStack = new Stack<DBTransaction>();
    private IDbConnection? _connectionTransactionConnection;
    private IDbTransaction? _connectionTransaction;
    private DBConnectionTransaction? _connectionTransactionOwner;
    private bool _connectionTransactionFailed;

    /// <summary>
    /// Begins a database-local transaction that reuses one open connection for every command.
    /// </summary>
    /// <param name="isolationLevel">The isolation level used by the database transaction.</param>
    /// <returns>A transaction that commits only after <see cref="DBConnectionTransaction.Complete"/> is called.</returns>
    /// <remarks>
    /// Use this transaction for multi-command operations that must remain atomic without promoting an
    /// ambient <see cref="System.Transactions.TransactionScope"/> to a distributed transaction.
    /// A <see cref="DBAccess"/> instance supports one connection transaction at a time.
    /// </remarks>
    public DBConnectionTransaction BeginConnectionTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        if (_connectionTransaction != null)
            throw new InvalidOperationException("This database access instance already has an active connection transaction.");
        if (IsInTransaction || System.Transactions.Transaction.Current != null)
            throw new InvalidOperationException("A connection transaction cannot start inside an ambient transaction.");

        var connection = CreateConnection();
        IDbTransaction? transaction = null;
        try
        {
            connection.Open();
            InitializeDeadlockTrace(connection);
            transaction = connection.BeginTransaction(isolationLevel);
            var owner = new DBConnectionTransaction(this);
            _connectionTransactionConnection = connection;
            _connectionTransaction = transaction;
            _connectionTransactionOwner = owner;
            _connectionTransactionFailed = false;
            LastException = null;
            TryLogConnectionTransaction(() => Log.Trace($"Began connection transaction {owner.Id}."));
            return owner;
        }
        catch
        {
            transaction?.Dispose();
            connection.Dispose();
            _connectionTransaction = null;
            _connectionTransactionConnection = null;
            _connectionTransactionOwner = null;
            throw;
        }
    }

    /// <summary>Gets the open connection used by the active database-local transaction.</summary>
    private IDbConnection? ConnectionTransactionConnection => _connectionTransactionConnection;

    /// <summary>Gets the transaction assigned to commands on the shared connection.</summary>
    private IDbTransaction? ConnectionTransaction => _connectionTransaction;

    /// <summary>Returns the active SQL Server connection and local transaction for transaction-aware bulk operations.</summary>
    internal bool TryGetActiveSqlTransaction(out SqlConnection? connection, out SqlTransaction? transaction)
    {
        connection = _connectionTransactionConnection as SqlConnection;
        transaction = _connectionTransaction as SqlTransaction;
        return connection != null && transaction != null;
    }

    /// <summary>Commits or rolls back and releases a database-local transaction.</summary>
    internal void EndConnectionTransaction(DBConnectionTransaction owner, bool complete)
    {
        if (!ReferenceEquals(owner, _connectionTransactionOwner))
            return;

        try
        {
            if (complete && !_connectionTransactionFailed && LastException == null)
            {
                _connectionTransaction?.Commit();
                TryLogConnectionTransaction(() => Log.Trace($"Committed connection transaction {owner.Id}."));
            }
            else
            {
                try
                {
                    _connectionTransaction?.Rollback();
                }
                catch (Exception ex)
                {
                    // A server-aborted transaction may no longer accept an explicit rollback.
                    // Preserve the command failure that caused disposal instead of replacing it.
                    TryLogConnectionTransaction(() => Log.Warning($"Connection transaction {owner.Id} could not be explicitly rolled back: {ex.Message}"));
                }
                TryLogConnectionTransaction(() => Log.Trace($"Rolled back connection transaction {owner.Id}."));
            }
        }
        finally
        {
            _connectionTransaction?.Dispose();
            _connectionTransactionConnection?.Dispose();
            _connectionTransaction = null;
            _connectionTransactionConnection = null;
            _connectionTransactionOwner = null;
            _connectionTransactionFailed = false;
        }
    }

    /// <summary>Keeps transaction cleanup independent from optional logging destinations.</summary>
    private static void TryLogConnectionTransaction(Action writeLog)
    {
        try
        {
            writeLog?.Invoke();
        }
        catch
        {
            // Transaction correctness must not depend on the availability of a log destination.
        }
    }

    /// <summary>
    /// Begins a new transaction and pushes it onto the transaction stack.
    /// </summary>
    /// <param name="autoCommit">If true, changes are automatically committed when the transaction is disposed, unless an exception occurred.</param>
    /// <returns>A <see cref="DBTransaction"/> instance.</returns>
    public DBTransaction BeginTransaction(bool autoCommit = true)
        => BeginTransaction(autoCommit, System.Transactions.Transaction.Current?.IsolationLevel ?? System.Transactions.IsolationLevel.Serializable);

    /// <summary>Begins an ambient transaction with an explicit isolation level; nested calls inherit that level.</summary>
    /// <param name="autoCommit">Whether successful disposal commits automatically.</param>
    /// <param name="isolationLevel">The isolation required by the caller's concurrency strategy.</param>
    /// <returns>The transaction context.</returns>
    public DBTransaction BeginTransaction(bool autoCommit, System.Transactions.IsolationLevel isolationLevel)
    {
        if (_connectionTransaction != null)
            throw new InvalidOperationException("An ambient transaction cannot start while a connection transaction is active.");
        var transaction = new DBTransaction(this, autoCommit, System.Transactions.Transaction.Current?.IsolationLevel ?? isolationLevel);
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
