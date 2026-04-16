namespace ByteForge.WinSCP;

// Fields mirror the Win32 IO_COUNTERS struct layout for P/Invoke marshalling.
// All fields exist to preserve the correct struct size embedded in JOBOBJECT_EXTENDED_LIMIT_INFORMATION;
// none are read back because SetInformationJobObject is used only to write limits, not query them.
#pragma warning disable CS0649 // Field is never assigned to and will always have its default value
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
#pragma warning restore CS0649
