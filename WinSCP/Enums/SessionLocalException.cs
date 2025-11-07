using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents a session exception that occurred locally (not on the remote server).
/// </summary>
[Guid("59B362D6-7FD3-4EF0-A3B6-E3244F793778")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class SessionLocalException : SessionException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SessionLocalException"/> class.
	/// </summary>
	/// <param name="session">The session associated with this exception.</param>
	/// <param name="message">The error message.</param>
	internal SessionLocalException(Session session, string message)
		: base(session, message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SessionLocalException"/> class with an inner exception.
	/// </summary>
	/// <param name="session">The session associated with this exception.</param>
	/// <param name="message">The error message.</param>
	/// <param name="innerException">The inner exception.</param>
	internal SessionLocalException(Session session, string message, Exception innerException)
		: base(session, message, innerException)
	{
	}

	/// <summary>
	/// Creates a new instance for a missing element in the log file.
	/// </summary>
	/// <param name="session">The session associated with this exception.</param>
	/// <param name="localName">The name of the missing element.</param>
	/// <returns>A new instance of <see cref="SessionLocalException"/>.</returns>
	internal static SessionLocalException CreateElementNotFound(Session session, string localName)
	{
		return new SessionLocalException(session, string.Format(CultureInfo.CurrentCulture, "Element \"{0}\" not found in the log file", new object[1] { localName }));
	}
}
