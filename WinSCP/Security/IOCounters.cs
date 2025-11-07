namespace ByteForge.WinSCP;

/// <summary>
/// Represents I/O operation and transfer counters.
/// </summary>
internal struct IOCounters
{
    /// <summary>
    /// Gets or sets the number of read operations.
    /// </summary>
    public ulong ReadOperationCount;

    /// <summary>
    /// Gets or sets the number of write operations.
    /// </summary>
    public ulong WriteOperationCount;

    /// <summary>
    /// Gets or sets the number of other operations.
    /// </summary>
    public ulong OtherOperationCount;

    /// <summary>
    /// Gets or sets the number of bytes read.
    /// </summary>
    public ulong ReadTransferCount;

    /// <summary>
    /// Gets or sets the number of bytes written.
    /// </summary>
    public ulong WriteTransferCount;

    /// <summary>
    /// Gets or sets the number of bytes transferred in other operations.
    /// </summary>
    public ulong OtherTransferCount;
}
