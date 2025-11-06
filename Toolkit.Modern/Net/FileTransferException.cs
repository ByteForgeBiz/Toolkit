namespace ByteForge.Toolkit.Net;
/// <summary>
/// Exception thrown when file transfer operations fail.
/// </summary>
public class FileTransferException : Exception
{
    /// <summary>
    /// Initializes a new instance of the FileTransferException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public FileTransferException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the FileTransferException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public FileTransferException(string message, Exception innerException) : base(message, innerException) { }
}
