using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml;

namespace ByteForge.WinSCP;

[Guid("0E8BBC73-AF4D-4E7E-995C-EB89D0BFDE9A")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class SessionRemoteException : SessionException
{
	internal SessionRemoteException(Session session, string message)
		: base(session, message)
	{
	}

	internal static bool IsResult(CustomLogReader reader)
	{
		return reader.IsNonEmptyElement("result");
	}

	internal static SessionRemoteException ReadResult(CustomLogReader areader)
	{
		SessionRemoteException result = null;
		if (areader.GetAttribute("success") == "false")
		{
			result = ReadMessages(areader);
		}
		return result;
	}

	internal static SessionRemoteException ReadFailure(CustomLogReader reader)
	{
		return ReadMessages(reader);
	}

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
