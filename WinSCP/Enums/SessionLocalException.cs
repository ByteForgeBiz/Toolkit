using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("59B362D6-7FD3-4EF0-A3B6-E3244F793778")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class SessionLocalException : SessionException
{
	internal SessionLocalException(Session session, string message)
		: base(session, message)
	{
	}

	internal SessionLocalException(Session session, string message, Exception innerException)
		: base(session, message, innerException)
	{
	}

	internal static SessionLocalException CreateElementNotFound(Session session, string localName)
	{
		return new SessionLocalException(session, string.Format(CultureInfo.CurrentCulture, "Element \"{0}\" not found in the log file", new object[1] { localName }));
	}
}
