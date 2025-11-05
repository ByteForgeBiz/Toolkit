using System;
using System.Xml;

namespace ByteForge.WinSCP;

internal abstract class CustomLogReader : IDisposable
{
	public Session Session { get; private set; }

	public XmlNodeType NodeType => Reader.NodeType;

	public string NamespaceURI => Reader.NamespaceURI;

	public string LocalName => Reader.LocalName;

	public bool IsEmptyElement => Reader.IsEmptyElement;

	public int Depth => Reader.Depth;

	public string Value => Reader.Value;

	internal abstract XmlReader Reader { get; }

	public abstract bool Read(LogReadFlags flags);

	protected CustomLogReader(Session session)
	{
		Session = session;
	}

	public virtual void Dispose()
	{
	}

	public bool IsElement()
	{
		if (NodeType == XmlNodeType.Element)
		{
			return NamespaceURI == "http://winscp.net/schema/session/1.0";
		}
		return false;
	}

	public bool IsElement(string localName)
	{
		if (IsElement())
		{
			return LocalName == localName;
		}
		return false;
	}

	public bool IsNonEmptyElement(string localName)
	{
		if (IsElement(localName))
		{
			return !IsEmptyElement;
		}
		return false;
	}

	public bool GetEmptyElementValue(string localName, out string value)
	{
		bool flag = IsElement(localName) && IsEmptyElement;
		if (flag)
		{
			value = GetAttribute("value");
			flag = value != null;
		}
		else
		{
			value = null;
		}
		return flag;
	}

	public bool IsEndElement(string localName)
	{
		if (NodeType == XmlNodeType.EndElement && NamespaceURI == "http://winscp.net/schema/session/1.0")
		{
			return LocalName == localName;
		}
		return false;
	}

	public bool TryWaitForNonEmptyElement(string localName, LogReadFlags flags)
	{
		bool flag = false;
		while (!flag && Read(flags))
		{
			if (IsNonEmptyElement(localName))
			{
				flag = true;
			}
		}
		return flag;
	}

	public void WaitForNonEmptyElement(string localName, LogReadFlags flags)
	{
		if (!TryWaitForNonEmptyElement(localName, flags))
		{
			throw Session.Logger.WriteException(SessionLocalException.CreateElementNotFound(Session, localName));
		}
	}

	public bool TryWaitForEmptyElement(string localName, LogReadFlags flags)
	{
		bool flag = false;
		while (!flag && Read(flags))
		{
			if (IsElement(localName) && IsEmptyElement)
			{
				flag = true;
			}
		}
		return flag;
	}

	public ElementLogReader CreateLogReader()
	{
		return new ElementLogReader(this);
	}

	public ElementLogReader WaitForNonEmptyElementAndCreateLogReader(string localName, LogReadFlags flags)
	{
		WaitForNonEmptyElement(localName, flags);
		return CreateLogReader();
	}

	public ElementLogReader WaitForGroupAndCreateLogReader()
	{
		return WaitForNonEmptyElementAndCreateLogReader("group", LogReadFlags.ThrowFailures);
	}

	public string GetAttribute(string name)
	{
		return Reader.GetAttribute(name);
	}
}
