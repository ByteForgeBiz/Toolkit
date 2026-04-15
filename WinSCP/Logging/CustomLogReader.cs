using System;
using System.Xml;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides an abstract base for reading XML session log data produced by WinSCP.
/// Subclasses implement the specific reading strategy while this class provides
/// common element-matching and navigation helpers.
/// </summary>
internal abstract class CustomLogReader : IDisposable
{
	/// <summary>
	/// Gets the <see cref="WinSCP.Session"/> associated with this log reader.
	/// </summary>
	public Session Session { get; private set; }

	/// <summary>
	/// Gets the type of the current XML node.
	/// </summary>
	public XmlNodeType NodeType => Reader.NodeType;

	/// <summary>
	/// Gets the namespace URI of the current XML node.
	/// </summary>
	public string NamespaceURI => Reader.NamespaceURI;

	/// <summary>
	/// Gets the local name of the current XML node.
	/// </summary>
	public string LocalName => Reader.LocalName;

	/// <summary>
	/// Gets a value indicating whether the current element is an empty element (self-closing tag).
	/// </summary>
	public bool IsEmptyElement => Reader.IsEmptyElement;

	/// <summary>
	/// Gets the depth of the current node in the XML document.
	/// </summary>
	public int Depth => Reader.Depth;

	/// <summary>
	/// Gets the text value of the current XML node.
	/// </summary>
	public string Value => Reader.Value;

	/// <summary>
	/// Gets the underlying <see cref="XmlReader"/> used to read the log stream.
	/// </summary>
	internal abstract XmlReader Reader { get; }

	/// <summary>
	/// Reads the next node from the log, applying the specified read flags.
	/// </summary>
	/// <param name="flags">Flags that control the read behavior, such as whether to throw on failures.</param>
	/// <returns><see langword="true"/> if the next node was read successfully; otherwise, <see langword="false"/>.</returns>
	public abstract bool Read(LogReadFlags flags);

	/// <summary>
	/// Initializes a new instance of the <see cref="CustomLogReader"/> class for the given session.
	/// </summary>
	/// <param name="session">The session whose log is being read.</param>
	protected CustomLogReader(Session session)
	{
		Session = session;
	}

	/// <summary>
	/// Releases resources used by this log reader. Override in derived classes to perform cleanup.
	/// </summary>
	public virtual void Dispose()
	{
	}

	/// <summary>
	/// Determines whether the current node is an XML element in the WinSCP session namespace.
	/// </summary>
	/// <returns><see langword="true"/> if the current node is a WinSCP session element; otherwise, <see langword="false"/>.</returns>
	public bool IsElement()
	{
		if (NodeType == XmlNodeType.Element)
		{
			return NamespaceURI == "http://winscp.net/schema/session/1.0";
		}
		return false;
	}

	/// <summary>
	/// Determines whether the current node is a WinSCP session element with the specified local name.
	/// </summary>
	/// <param name="localName">The local name to match against the current element.</param>
	/// <returns><see langword="true"/> if the current node matches the specified element name; otherwise, <see langword="false"/>.</returns>
	public bool IsElement(string localName)
	{
		if (IsElement())
		{
			return LocalName == localName;
		}
		return false;
	}

	/// <summary>
	/// Determines whether the current node is a non-empty WinSCP session element with the specified local name.
	/// </summary>
	/// <param name="localName">The local name to match against the current element.</param>
	/// <returns><see langword="true"/> if the current node is a matching non-empty element; otherwise, <see langword="false"/>.</returns>
	public bool IsNonEmptyElement(string localName)
	{
		if (IsElement(localName))
		{
			return !IsEmptyElement;
		}
		return false;
	}

	/// <summary>
	/// Attempts to read the <c>value</c> attribute from the current element if it matches the
	/// specified local name and is an empty (self-closing) element.
	/// </summary>
	/// <param name="localName">The local name of the element to match.</param>
	/// <param name="value">
	/// When this method returns <see langword="true"/>, contains the value of the <c>value</c>
	/// attribute; otherwise, <see langword="null"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the current element matches and has a <c>value</c> attribute;
	/// otherwise, <see langword="false"/>.
	/// </returns>
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

	/// <summary>
	/// Determines whether the current node is an end element in the WinSCP session namespace
	/// with the specified local name.
	/// </summary>
	/// <param name="localName">The local name to match against the current end element.</param>
	/// <returns><see langword="true"/> if the current node is the matching end element; otherwise, <see langword="false"/>.</returns>
	public bool IsEndElement(string localName)
	{
		if (NodeType == XmlNodeType.EndElement && NamespaceURI == "http://winscp.net/schema/session/1.0")
		{
			return LocalName == localName;
		}
		return false;
	}

	/// <summary>
	/// Advances the reader until a non-empty element with the specified local name is found,
	/// or until there are no more nodes to read.
	/// </summary>
	/// <param name="localName">The local name of the element to wait for.</param>
	/// <param name="flags">Flags controlling the read behavior.</param>
	/// <returns><see langword="true"/> if the element was found; otherwise, <see langword="false"/>.</returns>
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

	/// <summary>
	/// Advances the reader until a non-empty element with the specified local name is found,
	/// throwing a <see cref="SessionLocalException"/> if the element is not found.
	/// </summary>
	/// <param name="localName">The local name of the element to wait for.</param>
	/// <param name="flags">Flags controlling the read behavior.</param>
	/// <exception cref="SessionLocalException">Thrown when the expected element is not found in the log.</exception>
	public void WaitForNonEmptyElement(string localName, LogReadFlags flags)
	{
		if (!TryWaitForNonEmptyElement(localName, flags))
		{
			throw Session.Logger.WriteException(SessionLocalException.CreateElementNotFound(Session, localName));
		}
	}

	/// <summary>
	/// Advances the reader until an empty element with the specified local name is found,
	/// or until there are no more nodes to read.
	/// </summary>
	/// <param name="localName">The local name of the empty element to wait for.</param>
	/// <param name="flags">Flags controlling the read behavior.</param>
	/// <returns><see langword="true"/> if the empty element was found; otherwise, <see langword="false"/>.</returns>
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

	/// <summary>
	/// Creates a new <see cref="ElementLogReader"/> scoped to the current element.
	/// </summary>
	/// <returns>An <see cref="ElementLogReader"/> positioned at the current element.</returns>
	public ElementLogReader CreateLogReader()
	{
		return new ElementLogReader(this);
	}

	/// <summary>
	/// Advances the reader to the next non-empty element with the specified local name
	/// and creates a scoped <see cref="ElementLogReader"/> for it.
	/// </summary>
	/// <param name="localName">The local name of the element to locate.</param>
	/// <param name="flags">Flags controlling the read behavior.</param>
	/// <returns>An <see cref="ElementLogReader"/> scoped to the located element.</returns>
	/// <exception cref="SessionLocalException">Thrown when the expected element is not found in the log.</exception>
	public ElementLogReader WaitForNonEmptyElementAndCreateLogReader(string localName, LogReadFlags flags)
	{
		WaitForNonEmptyElement(localName, flags);
		return CreateLogReader();
	}

	/// <summary>
	/// Advances the reader to the next non-empty <c>group</c> element and creates a scoped
	/// <see cref="ElementLogReader"/> for it. Failures during the read will throw.
	/// </summary>
	/// <returns>An <see cref="ElementLogReader"/> scoped to the located <c>group</c> element.</returns>
	/// <exception cref="SessionLocalException">Thrown when a <c>group</c> element is not found.</exception>
	public ElementLogReader WaitForGroupAndCreateLogReader()
	{
		return WaitForNonEmptyElementAndCreateLogReader("group", LogReadFlags.ThrowFailures);
	}

	/// <summary>
	/// Gets the value of the specified attribute on the current XML node.
	/// </summary>
	/// <param name="name">The name of the attribute to retrieve.</param>
	/// <returns>The attribute value, or <see langword="null"/> if the attribute is not present.</returns>
	public string GetAttribute(string name)
	{
		return Reader.GetAttribute(name);
	}
}
