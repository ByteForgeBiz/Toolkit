using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents a base exception for session operations.
/// </summary>
[Guid("91109A4F-D81A-4326-BEC5-1AB26EBF89A6")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public class SessionException : Exception
{
	/// <summary>
	/// Gets the session associated with this exception.
	/// </summary>
	public Session Session { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SessionException"/> class.
	/// </summary>
	/// <param name="session">The session associated with this exception.</param>
	/// <param name="message">The error message.</param>
	internal SessionException(Session session, string message)
		: this(session, message, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SessionException"/> class with an inner exception.
	/// </summary>
	/// <param name="session">The session associated with this exception.</param>
	/// <param name="message">The error message.</param>
	/// <param name="innerException">The inner exception.</param>
	internal SessionException(Session session, string message, Exception innerException)
		: base(message, innerException)
	{
		Session = session;
	}
}
