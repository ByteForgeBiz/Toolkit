using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

namespace ByteForge.WinSCP;

/// <summary>
/// Reads the WinSCP XML session log incrementally, blocking until new XML nodes
/// become available, and exposes them through the base-class <see cref="XmlReader"/>
/// contract. Handles timeouts, session failures embedded in the log, and
/// automatic re-open of the log file when it is replaced between reads.
/// </summary>
internal class SessionLogReader : CustomLogReader
{
	/// <summary>
	/// The number of XML nodes that have been successfully read from the log so far.
	/// Used to skip already-processed nodes when the underlying file must be re-opened.
	/// </summary>
	private int _position;

	/// <summary>
	/// The active <see cref="XmlReader"/> over the session log file stream.
	/// </summary>
	private XmlReader _reader;

	/// <summary>
	/// The underlying file stream opened with the appropriate sharing mode for the log file.
	/// </summary>
	private PatientFileStream _stream;

	/// <summary>
	/// Indicates whether the log has been permanently closed (no more data expected).
	/// </summary>
	private bool _closed;

	/// <summary>
	/// The last full contents of the log file that were written to the diagnostic logger,
	/// used to avoid logging unchanged content repeatedly.
	/// </summary>
	private string _logged;

	/// <summary>
	/// Indicates that the session has timed out and no further reads should be attempted.
	/// </summary>
	private bool _timeouted;

	/// <summary>
	/// Gets the underlying <see cref="XmlReader"/> positioned on the log stream.
	/// </summary>
	/// <value>The active XML reader for the session log.</value>
	/// <exception cref="SessionLocalException">
	/// Thrown when <see cref="Read"/> has not been called yet and reading has not commenced.
	/// </exception>
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

	/// <summary>
	/// Initializes a new instance of <see cref="SessionLogReader"/> for the specified session.
	/// </summary>
	/// <param name="session">The WinSCP session whose XML log file will be read.</param>
	public SessionLogReader(Session session)
		: base(session)
	{
		_position = 0;
	}

	/// <summary>
	/// Marks the session as timed out so that subsequent <see cref="Read"/> calls
	/// return <see langword="false"/> immediately without attempting further I/O.
	/// </summary>
	public void SetTimeouted()
	{
		_timeouted = true;
	}

	/// <summary>
	/// Closes the log stream, logs any remaining log contents, cleans up resources,
	/// and calls the base-class disposal logic.
	/// </summary>
	public override void Dispose()
	{
		using (base.Session.Logger.CreateCallstack())
		{
			LogContents();
			Cleanup();
		}
		base.Dispose();
	}

	/// <summary>
	/// Releases the underlying file stream and XML reader, setting both to
	/// <see langword="null"/> so they will be re-created if needed.
	/// </summary>
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

	/// <summary>
	/// Advances to the next XML node in the session log, blocking until one is available.
	/// Skips <c>&lt;failure&gt;</c> elements and, when <paramref name="flags"/> includes
	/// <see cref="LogReadFlags.ThrowFailures"/>, rethrows any remote session failure as an exception.
	/// </summary>
	/// <param name="flags">Controls whether embedded failure elements cause an exception to be thrown.</param>
	/// <returns>
	/// <see langword="true"/> if a node was read; <see langword="false"/> if the log is
	/// exhausted or the session has timed out.
	/// </returns>
	/// <exception cref="SessionRemoteException">
	/// Thrown when a <c>&lt;failure&gt;</c> element is encountered and
	/// <paramref name="flags"/> includes <see cref="LogReadFlags.ThrowFailures"/>.
	/// </exception>
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

	/// <summary>
	/// Performs a single low-level read attempt, opening the log file if necessary and
	/// polling with exponential back-off until a node becomes available or the log is closed.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if an XML node was read; <see langword="false"/> if the
	/// log reached end-of-file and was closed.
	/// </returns>
	/// <exception cref="SessionLocalException">
	/// Thrown when the XML log file cannot be parsed or fewer nodes are available than
	/// were read in a previous parsing pass.
	/// </exception>
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

	/// <summary>
	/// Reads the entire session log file and writes its contents to the diagnostic logger,
	/// but only when the contents have changed since the last call.
	/// </summary>
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

	/// <summary>
	/// Opens the session XML log file, first attempting exclusive read access and
	/// falling back to shared read/write access when the WinSCP process still holds
	/// the file open. Skips ahead past any nodes already read in previous passes.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the log was already permanently closed before this call.
	/// </exception>
	/// <exception cref="SessionLocalException">
	/// Thrown when the log file contains fewer nodes than were read in a previous pass.
	/// </exception>
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
