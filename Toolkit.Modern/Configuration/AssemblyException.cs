using System;
using System.Runtime.Serialization;

namespace ByteForge.Toolkit;
/// <summary>
/// Represents errors that occur during assembly operations within the ByteForge.Toolkit.
/// </summary>
[Serializable]
internal class AssemblyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyException"/> class.
    /// </summary>
    public AssemblyException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AssemblyException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyException"/> class with a specified error message 
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a <see langword="null"/> reference if no inner exception is specified.</param>
    public AssemblyException(string message, Exception innerException) : base(message, innerException) { }
}
