namespace ByteForge.Toolkit.Net;
/// <summary>
/// Supported file transfer protocols.
/// </summary>
public enum TransferProtocol
{
    /// <summary>
    /// Standard FTP protocol (no encryption).
    /// </summary>
    FTP,

    /// <summary>
    /// FTP with explicit TLS/SSL encryption.
    /// </summary>
    FTPS_Explicit,

    /// <summary>
    /// FTP with implicit TLS/SSL encryption.
    /// </summary>
    FTPS_Implicit,

    /// <summary>
    /// SSH File Transfer Protocol.
    /// </summary>
    SFTP
}
