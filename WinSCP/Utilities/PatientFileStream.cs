using System;
using System.IO;

namespace ByteForge.WinSCP;

/// <summary>
/// A read-only <see cref="Stream"/> wrapper around a <see cref="FileStream"/> that patiently
/// waits for data to become available, dispatching session events while blocking.
/// Useful for reading log files that are still being written by an external process.
/// </summary>
internal class PatientFileStream : Stream
{
	/// <summary>
	/// The initial wait interval in milliseconds before the first retry.
	/// </summary>
	private const int InitialInterval = 50;

	/// <summary>
	/// The underlying file stream being read.
	/// </summary>
	private FileStream _stream;

	/// <summary>
	/// The WinSCP session used to dispatch events and check for timeouts while waiting.
	/// </summary>
	private readonly Session _session;

	/// <summary>
	/// Gets a value indicating whether the stream supports reading.
	/// </summary>
	/// <value><see langword="true"/> if the underlying stream can be read; otherwise <see langword="false"/>.</value>
	public override bool CanRead
	{
		get
		{
			bool canRead = _stream.CanRead;
			_session.Logger.WriteLineLevel(2, "Can read = {0}", canRead);
			return canRead;
		}
	}

	/// <summary>
	/// Gets a value indicating whether the stream supports seeking.
	/// </summary>
	/// <value><see langword="true"/> if the underlying stream supports seeking; otherwise <see langword="false"/>.</value>
	public override bool CanSeek
	{
		get
		{
			bool canSeek = _stream.CanSeek;
			_session.Logger.WriteLineLevel(2, "Can seek = {0}", canSeek);
			return canSeek;
		}
	}

	/// <summary>
	/// Gets a value indicating whether the stream supports writing. Always <see langword="false"/>.
	/// </summary>
	/// <value>Always <see langword="false"/>; this stream is read-only.</value>
	public override bool CanWrite => false;

	/// <summary>
	/// Gets the length of the stream in bytes, waiting until at least one byte has been written
	/// by the remote process if the file is currently empty.
	/// </summary>
	/// <value>The length of the underlying file in bytes.</value>
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

	/// <summary>
	/// Gets or sets the current position within the stream.
	/// </summary>
	/// <value>The current byte offset in the underlying file.</value>
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

	/// <summary>
	/// Initializes a new instance of the <see cref="PatientFileStream"/> class.
	/// </summary>
	/// <param name="session">The WinSCP session used for event dispatching and timeout checks.</param>
	/// <param name="path">The full path to the file to open.</param>
	/// <param name="mode">A <see cref="FileMode"/> value that specifies how the file should be opened.</param>
	/// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
	/// <param name="share">A <see cref="FileShare"/> value that specifies the sharing mode for the file.</param>
	public PatientFileStream(Session session, string path, FileMode mode, FileAccess access, FileShare share)
	{
		_stream = new FileStream(path, mode, access, share);
		_session = session;
	}

	/// <summary>
	/// Closes the underlying file stream and releases associated resources.
	/// </summary>
	public override void Close()
	{
		if (_stream != null)
		{
			_stream.Close();
			_stream = null;
		}
		base.Close();
	}

	/// <summary>
	/// Reads a sequence of bytes from the stream, waiting (with exponential back-off) until
	/// at least one byte is available.
	/// </summary>
	/// <param name="array">The buffer to read into.</param>
	/// <param name="offset">The zero-based byte offset in <paramref name="array"/> at which to begin storing data.</param>
	/// <param name="count">The maximum number of bytes to read.</param>
	/// <returns>The total number of bytes read into <paramref name="array"/>.</returns>
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

	/// <summary>
	/// Waits for the specified interval by dispatching session events, then doubles the interval
	/// up to a maximum of 500 ms and checks for a session timeout.
	/// </summary>
	/// <param name="interval">The current wait interval in milliseconds; updated (doubled, up to 500) after waiting.</param>
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

	/// <summary>
	/// Not supported. Always throws <see cref="NotImplementedException"/>.
	/// </summary>
	/// <exception cref="NotImplementedException">Always thrown.</exception>
	public override void Flush()
	{
		throw _session.Logger.WriteException(new NotImplementedException());
	}

	/// <summary>
	/// Not supported. Always throws <see cref="NotImplementedException"/>.
	/// </summary>
	/// <param name="offset">The byte offset relative to <paramref name="origin"/>.</param>
	/// <param name="origin">A <see cref="SeekOrigin"/> value indicating the reference point for the seek.</param>
	/// <returns>This method does not return.</returns>
	/// <exception cref="NotImplementedException">Always thrown.</exception>
	public override long Seek(long offset, SeekOrigin origin)
	{
		throw _session.Logger.WriteException(new NotImplementedException());
	}

	/// <summary>
	/// Not supported. Always throws <see cref="NotImplementedException"/>.
	/// </summary>
	/// <param name="value">The desired length of the stream in bytes.</param>
	/// <exception cref="NotImplementedException">Always thrown.</exception>
	public override void SetLength(long value)
	{
		throw _session.Logger.WriteException(new NotImplementedException());
	}

	/// <summary>
	/// Not supported. Always throws <see cref="NotImplementedException"/>.
	/// </summary>
	/// <param name="buffer">An array of bytes to write to the stream.</param>
	/// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes.</param>
	/// <param name="count">The number of bytes to write.</param>
	/// <exception cref="NotImplementedException">Always thrown.</exception>
	public override void Write(byte[] buffer, int offset, int count)
	{
		throw _session.Logger.WriteException(new NotImplementedException());
	}
}
