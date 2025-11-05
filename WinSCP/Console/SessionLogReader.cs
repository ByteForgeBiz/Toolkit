using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

namespace ByteForge.WinSCP;

internal class SessionLogReader : CustomLogReader
{
	private int _position;

	private XmlReader _reader;

	private PatientFileStream _stream;

	private bool _closed;

	private string _logged;

	private bool _timeouted;

	internal override XmlReader Reader
	{
		get
		{
			if (_reader == null)
			{
				throw base.Session.Logger.WriteException(new SessionLocalException(base.Session, "Reading has not commenced yet"));
			}
			return _reader;
		}
	}

	public SessionLogReader(Session session)
		: base(session)
	{
		_position = 0;
	}

	public void SetTimeouted()
	{
		_timeouted = true;
	}

	public override void Dispose()
	{
		using (base.Session.Logger.CreateCallstack())
		{
			LogContents();
			Cleanup();
		}
		base.Dispose();
	}

	private void Cleanup()
	{
		if (_stream != null)
		{
			base.Session.Logger.WriteLine("Closing log");
			_stream.Dispose();
			_stream = null;
		}
		if (_reader != null)
		{
			((IDisposable)_reader).Dispose();
			_reader = null;
		}
	}

	public override bool Read(LogReadFlags flags)
	{
		using (base.Session.Logger.CreateCallstack())
		{
			bool flag;
			if (_timeouted)
			{
				base.Session.Logger.WriteLine("Not reading, session has timed out");
				flag = false;
			}
			else
			{
				bool flag2;
				do
				{
					flag = DoRead();
					flag2 = false;
					if (flag && IsNonEmptyElement("failure"))
					{
						SessionRemoteException e = SessionRemoteException.ReadFailure(this);
						base.Session.RaiseFailed(e);
						if ((flags & LogReadFlags.ThrowFailures) != 0)
						{
							throw base.Session.Logger.WriteException(e);
						}
						flag2 = true;
					}
				}
				while (flag2);
			}
			return flag;
		}
	}

	private bool DoRead()
	{
		int num = 50;
		bool flag;
		do
		{
			if (_reader == null)
			{
				OpenLog();
			}
			try
			{
				flag = _reader.Read();
				if (flag)
				{
					_position++;
					base.Session.Logger.WriteLine("Read node {0}: {1} {2}{3}{4}", _position, _reader.NodeType, _reader.Name, (_reader.HasValue && !string.IsNullOrEmpty(_reader.Name) && !string.IsNullOrEmpty(_reader.Value)) ? "=" : string.Empty, _reader.Value);
					base.Session.GotOutput();
				}
				else
				{
					base.Session.Logger.WriteLine("Cannot read");
					if (!_closed)
					{
						_closed = true;
						Cleanup();
					}
					base.Session.CheckForTimeout();
				}
			}
			catch (XmlException innerException)
			{
				Cleanup();
				base.Session.CheckForTimeout();
				LogContents();
				string text = "Error parsing session log file";
				Thread.Sleep(200);
				string errorOutputMessage = base.Session.GetErrorOutputMessage();
				if (!string.IsNullOrEmpty(errorOutputMessage))
				{
					text = text + " - " + errorOutputMessage;
				}
				throw base.Session.Logger.WriteException(new SessionLocalException(base.Session, text, innerException));
			}
			if (!flag && !_closed)
			{
				base.Session.Logger.WriteLine("Waiting for log update and dispatching events for {0}", num);
				base.Session.DispatchEvents(num);
				if (num < 500)
				{
					num *= 2;
				}
			}
		}
		while (!flag && !_closed);
		if (flag)
		{
			LogContents();
		}
		return flag;
	}

	private void LogContents()
	{
		if (!base.Session.Logger.Logging)
		{
			return;
		}
		try
		{
			using StreamReader streamReader = new StreamReader(new FileStream(base.Session.XmlLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8);
			string text = streamReader.ReadToEnd();
			if (_logged == null || _logged != text)
			{
				base.Session.Logger.WriteLine("Log contents:\n{0}", text);
				_logged = text;
			}
			else
			{
				base.Session.Logger.WriteLine("Log contents has not changed");
			}
		}
		catch (Exception ex)
		{
			base.Session.Logger.WriteLine("Error logging log contents [{0}]", ex.Message);
		}
	}

	private void OpenLog()
	{
		if (_closed)
		{
			throw base.Session.Logger.WriteException(new InvalidOperationException("Log was closed already"));
		}
		try
		{
			base.Session.Logger.WriteLine("Opening log without write sharing");
			_stream = new PatientFileStream(base.Session, base.Session.XmlLogPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			_closed = true;
			LogContents();
		}
		catch (IOException)
		{
			base.Session.Logger.WriteLine("Opening log with write sharing");
			_stream = new PatientFileStream(base.Session, base.Session.XmlLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			_closed = false;
		}
		base.Session.Logger.WriteLine("Log opened");
		XmlReaderSettings settings = new XmlReaderSettings
		{
			CheckCharacters = false
		};
		_reader = XmlReader.Create(_stream, settings);
		int num = _position;
		base.Session.Logger.WriteLine("Skipping {0} nodes", num);
		while (num > 0)
		{
			if (!_reader.Read())
			{
				throw base.Session.Logger.WriteException(new SessionLocalException(base.Session, "Read less nodes than in previous log parsing"));
			}
			num--;
		}
	}
}
