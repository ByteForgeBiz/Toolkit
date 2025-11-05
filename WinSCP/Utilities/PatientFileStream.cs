using System;
using System.IO;

namespace ByteForge.WinSCP;

internal class PatientFileStream : Stream
{
	private const int InitialInterval = 50;

	private FileStream _stream;

	private readonly Session _session;

	public override bool CanRead
	{
		get
		{
			bool canRead = _stream.CanRead;
			_session.Logger.WriteLineLevel(2, "Can read = {0}", canRead);
			return canRead;
		}
	}

	public override bool CanSeek
	{
		get
		{
			bool canSeek = _stream.CanSeek;
			_session.Logger.WriteLineLevel(2, "Can seek = {0}", canSeek);
			return canSeek;
		}
	}

	public override bool CanWrite => false;

	public override long Length
	{
		get
		{
			long length = _stream.Length;
			if (length == 0L)
			{
				_session.Logger.WriteLineLevel(2, "File is empty yet, waiting", length);
				int interval = 50;
				do
				{
					length = _stream.Length;
					if (length == 0L)
					{
						Wait(ref interval);
						continue;
					}
					_session.Logger.WriteLineLevel(2, "File length = {0}", length);
				}
				while (length == 0L);
			}
			return length;
		}
	}

	public override long Position
	{
		get
		{
			long length = _stream.Length;
			_session.Logger.WriteLineLevel(2, "File position = {0}", length);
			return length;
		}
		set
		{
			_session.Logger.WriteLineLevel(2, "Setting file position to {0}", value);
			_stream.Position = value;
		}
	}

	public PatientFileStream(Session session, string path, FileMode mode, FileAccess access, FileShare share)
	{
		_stream = new FileStream(path, mode, access, share);
		_session = session;
	}

	public override void Close()
	{
		if (_stream != null)
		{
			_stream.Close();
			_stream = null;
		}
		base.Close();
	}

	public override int Read(byte[] array, int offset, int count)
	{
		int interval = 50;
		int num;
		do
		{
			num = _stream.Read(array, offset, count);
			if (num == 0)
			{
				Wait(ref interval);
				continue;
			}
			_session.Logger.WriteLineLevel(2, "Read {0} bytes from log", num);
		}
		while (num == 0);
		return num;
	}

	private void Wait(ref int interval)
	{
		_session.Logger.WriteLine("Waiting for log update and dispatching events for {0}", interval);
		_session.DispatchEvents(interval);
		_session.CheckForTimeout();
		if (interval < 500)
		{
			interval *= 2;
		}
	}

	public override void Flush()
	{
		throw _session.Logger.WriteException(new NotImplementedException());
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw _session.Logger.WriteException(new NotImplementedException());
	}

	public override void SetLength(long value)
	{
		throw _session.Logger.WriteException(new NotImplementedException());
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw _session.Logger.WriteException(new NotImplementedException());
	}
}
