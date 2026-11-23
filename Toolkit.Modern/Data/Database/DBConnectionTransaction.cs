namespace ByteForge.Toolkit.Data;

/// <summary>
/// Represents a database-local transaction that keeps all commands on one connection.
/// </summary>
public sealed class DBConnectionTransaction : IDisposable
{
    private readonly DBAccess _dbAccess;
    private bool _complete;
    private bool _disposed;

    /// <summary>Gets the unique identifier used in transaction diagnostics.</summary>
    public string Id { get; } = Guid.NewGuid().ToString().Substring(0, 8);

    internal DBConnectionTransaction(DBAccess dbAccess)
    {
        _dbAccess = dbAccess ?? throw new ArgumentNullException(nameof(dbAccess));
    }

    /// <summary>Marks this transaction to be committed when it is disposed.</summary>
    public void Complete()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DBConnectionTransaction));
        _complete = true;
    }

    /// <summary>Commits a completed transaction or otherwise rolls it back.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _dbAccess.EndConnectionTransaction(this, _complete);
    }
}
