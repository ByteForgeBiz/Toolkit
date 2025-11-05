using System;
using System.Globalization;
using System.Xml;

namespace ByteForge.WinSCP;

internal class ElementLogReader : CustomLogReader
{
	private readonly CustomLogReader _parentReader;

	private readonly string _localName;

	private readonly int _depth;

	protected bool _read;

	private readonly string _token;

	internal override XmlReader Reader => _parentReader.Reader;

	public ElementLogReader(CustomLogReader parentReader)
		: base(parentReader.Session)
	{
		_parentReader = parentReader;
		if (base.NodeType != XmlNodeType.Element || base.IsEmptyElement)
		{
			throw base.Session.Logger.WriteException(new InvalidOperationException("Cannot use ElementLogReader with non-element node or empty element"));
		}
		_localName = _parentReader.Reader.LocalName;
		_depth = _parentReader.Reader.Depth;
		_token = _localName + "@" + _depth;
		_read = false;
	}

	public override void Dispose()
	{
		using (base.Session.Logger.CreateCallstack(_token))
		{
			try
			{
				ReadToEnd((LogReadFlags)0);
			}
			catch (Exception)
			{
				base.Session.Logger.WriteLine("Swallowing exception");
			}
		}
		base.Dispose();
	}

	public override bool Read(LogReadFlags flags)
	{
		if (_read)
		{
			throw base.Session.Logger.WriteException(new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Element {0} already read to the end", new object[1] { _token })));
		}
		bool flag = _parentReader.Read(flags);
		if (flag && IsEndElement(_localName) && base.Depth == _depth)
		{
			flag = false;
			base.Session.Logger.WriteLineLevel(1, "Element {0} read to the end", _token);
			_read = true;
		}
		return flag;
	}

	public void ReadToEnd(LogReadFlags flags)
	{
		using (base.Session.Logger.CreateCallstack(_token))
		{
			if (!_read)
			{
				while (Read(flags))
				{
				}
			}
		}
	}
}
