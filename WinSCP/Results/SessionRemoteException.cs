using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents an exception that occurred on the remote server.
/// </summary>
[Guid("0E8BBC73-AF4D-4E7E-995C-EB89D0BFDE9A")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class SessionRemoteException : SessionException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SessionRemoteException"/> class.
	/// </summary>
	/// <param name="session">The session associated with this exception.</param>
	/// <param name="message">The error message.</param>
	internal SessionRemoteException(Session session, string message)
		: base(session, message)
	{
	}

	/// <summary>
	/// Determines whether the specified reader contains a result element.
	/// </summary>
	/// <param name="reader">The log reader.</param>
	/// <returns>True if the reader is positioned at a result element; otherwise, false.</returns>
	internal static bool IsResult(CustomLogReader reader)
	{
		return reader.IsNonEmptyElement("result");
	}

	/// <summary>
	/// Reads a result element from the log reader and returns an exception if the result indicates failure.
	/// </summary>
	/// <param name="areader">The log reader.</param>
	/// <returns>A new <see cref="SessionRemoteException"/> if the operation failed; otherwise, null.</returns>
	internal static SessionRemoteException ReadResult(CustomLogReader areader)
	{
		SessionRemoteException result = null;
		if (areader.GetAttribute("success") == "false")
		{
			result = ReadMessages(areader);
		}
		return result;
	}

	/// <summary>
	/// Reads a failure element from the log reader and returns an exception.
	/// </summary>
	/// <param name="reader">The log reader.</param>
	/// <returns>A new <see cref="SessionRemoteException"/> containing the failure message.</returns>
	internal static SessionRemoteException ReadFailure(CustomLogReader reader)
	{
		return ReadMessages(reader);
	}

	/// <summary>
	/// Reads all message elements from the log reader and constructs a <see cref="SessionRemoteException"/> with the combined messages.
	/// </summary>
	/// <param name="areader">The log reader positioned at a result or failure element.</param>
	/// <returns>A new <see cref="SessionRemoteException"/> containing all messages joined by newlines.</returns>
	private static SessionRemoteException ReadMessages(CustomLogReader areader)
	{
		using ElementLogReader elementLogReader = new ElementLogReader(areader);
		string text = null;
		List<string> list = new List<string>();
		bool flag = false;
		while (elementLogReader.Read((LogReadFlags)0))
		{
			if (elementLogReader.IsNonEmptyElement("message"))
			{
				flag = true;
				text = null;
			}
			else if (flag && elementLogReader.NodeType == XmlNodeType.Text)
			{
				text += elementLogReader.Value;
			}
			else if (flag && elementLogReader.IsEndElement("message"))
			{
				list.Add(text);
				text = null;
				flag = false;
			}
		}
		string message = string.Join(Environment.NewLine, list.ToArray());
		return new SessionRemoteException(elementLogReader.Session, message);
	}
}
