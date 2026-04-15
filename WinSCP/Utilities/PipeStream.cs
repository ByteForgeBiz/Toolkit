using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ByteForge.WinSCP;

/// <summary>
/// A thread-safe, in-memory pipe stream that supports one writer and one reader.
/// Data is written via <see cref="WriteInternal"/> and read via <see cref="Read"/>.
/// The stream is read-only from the <see cref="Stream"/> API perspective; write operations
/// through the standard <see cref="Write"/> override are not supported.
/// </summary>
internal class PipeStream : Stream
{
	/// <summary>
	/// The internal byte buffer shared between the writer and reader.
	/// </summary>
	private readonly Queue<byte> _buffer = new Queue<byte>();

	/// <summary>
	/// Indicates whether <see cref="Flush"/> has been called, allowing reads to proceed
	/// even if fewer bytes than requested are buffered.
	/// </summary>
	private bool _isFlushed;

	/// <summary>
	/// Indicates whether this stream has been disposed.
	/// </summary>
	private bool _isDisposed;

	/// <summary>
	/// Indicates whether the write side of the pipe has been closed via <see cref="CloseWrite"/>.
	/// </summary>
	private bool _closedWrite;

	/// <summary>
	/// The current read position (cumulative bytes read).
	/// </summary>
	private long _position;

	/// <summary>
	/// Gets or sets the maximum number of bytes the internal buffer may hold before
	/// <see cref="WriteInternal"/> blocks. Defaults to 200 MB.
	/// </summary>
	/// <value>The maximum buffer length in bytes.</value>
	public long MaxBufferLength { get; set; } = 209715200L;

	/// <summary>
	/// Gets or sets a callback invoked when the stream is closed or disposed.
	/// The callback is invoked at most once and cleared afterwards.
	/// </summary>
	/// <value>An <see cref="Action"/> to call on close, or <see langword="null"/>.</value>
	public Action OnDispose { get; set; }

	/// <summary>
	/// Gets a value indicating whether the stream can be read.
	/// </summary>
	/// <value><see langword="true"/> while the stream has not been disposed; otherwise <see langword="false"/>.</value>
	public override bool CanRead => !_isDisposed;

	/// <summary>
	/// Gets a value indicating whether the stream supports seeking. Always <see langword="false"/>.
	/// </summary>
	/// <value>Always <see langword="false"/>.</value>
	public override bool CanSeek => false;

	/// <summary>
	/// Gets a value indicating whether the stream supports writing through the standard API.
	/// Always <see langword="false"/>; use <see cref="WriteInternal"/> instead.
	/// </summary>
	/// <value>Always <see langword="false"/>.</value>
	public override bool CanWrite => false;

	/// <summary>
	/// Not supported. Always throws <see cref="NotSupportedException"/>.
	/// </summary>
	/// <value>Not applicable.</value>
	/// <exception cref="NotSupportedException">Always thrown.</exception>
	public override long Length
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	/// <summary>
	/// Gets the cumulative number of bytes read so far. Setting is not supported.
	/// </summary>
	/// <value>The total number of bytes read from this stream since creation.</value>
	/// <exception cref="NotSupportedException">Thrown when attempting to set the position.</exception>
	public override long Position
	{
		get
		{
			lock (_buffer)
			{
				return _position;
			}
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	/// <summary>
	/// Marks the stream as flushed, unblocking any pending <see cref="Read"/> call even
	/// if fewer bytes than requested are buffered.
	/// </summary>
	/// <exception cref="ObjectDisposedException">Thrown if the stream has been disposed.</exception>
	public override void Flush()
	{
		CheckDisposed();
		_isFlushed = true;
		lock (_buffer)
		{
			Monitor.Pulse(_buffer);
		}
	}

	/// <summary>
	/// Not supported. Always throws <see cref="NotSupportedException"/>.
	/// </summary>
	/// <param name="offset">The byte offset relative to <paramref name="origin"/>.</param>
	/// <param name="origin">A <see cref="SeekOrigin"/> value specifying the reference point.</param>
	/// <returns>This method does not return.</returns>
	/// <exception cref="NotSupportedException">Always thrown.</exception>
	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Not supported. Always throws <see cref="NotSupportedException"/>.
	/// </summary>
	/// <param name="value">The desired length of the stream in bytes.</param>
	/// <exception cref="NotSupportedException">Always thrown.</exception>
	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Reads up to <paramref name="count"/> bytes from the pipe buffer into <paramref name="buffer"/>,
	/// blocking until data is available or the write side is closed.
	/// </summary>
	/// <param name="buffer">The byte array to read data into.</param>
	/// <param name="offset">The zero-based index in <paramref name="buffer"/> at which to begin storing bytes.</param>
	/// <param name="count">The maximum number of bytes to read.</param>
	/// <returns>The number of bytes actually read, or 0 if the stream has been disposed.</returns>
	/// <exception cref="NotSupportedException">Thrown if <paramref name="offset"/> is negative.</exception>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="buffer"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown if the sum of <paramref name="offset"/> and <paramref name="count"/> exceeds the buffer length.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> or <paramref name="count"/> is negative.</exception>
	/// <exception cref="ObjectDisposedException">Thrown if the stream has been disposed.</exception>
	public override int Read(byte[] buffer, int offset, int count)
	{
		if (offset < 0)
		{
			throw new NotSupportedException("Offset cnnot be negative");
		}
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (offset + count > buffer.Length)
		{
			throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
		}
		if (offset < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException("offset", "offset or count is negative.");
		}
		CheckDisposed();
		if (count == 0)
		{
			return 0;
		}
		int i = 0;
		lock (_buffer)
		{
			while (!_isDisposed && !_closedWrite && !ReadAvailable(1))
			{
				Monitor.Wait(_buffer);
			}
			if (_isDisposed)
			{
				return 0;
			}
			for (; i < count; i++)
			{
				if (_buffer.Count <= 0)
				{
					break;
				}
				buffer[offset + i] = _buffer.Dequeue();
			}
			if (_closedWrite)
			{
				Closed();
			}
			Monitor.Pulse(_buffer);
			_position += i;
			return i;
		}
	}

	/// <summary>
	/// Determines whether at least <paramref name="count"/> bytes are available to read,
	/// taking the flushed state into account.
	/// </summary>
	/// <param name="count">The minimum number of bytes that must be available.</param>
	/// <returns>
	/// <see langword="true"/> if the buffer contains at least <paramref name="count"/> bytes,
	/// or if the stream has been flushed (in which case any non-zero count suffices);
	/// otherwise <see langword="false"/>.
	/// </returns>
	/// <exception cref="ObjectDisposedException">Thrown if the stream has been disposed.</exception>
	public bool ReadAvailable(int count)
	{
		CheckDisposed();
		int count2 = _buffer.Count;
		if (!_isFlushed)
		{
			return count2 >= count;
		}
		return true;
	}

	/// <summary>
	/// Not supported. Always throws <see cref="NotSupportedException"/>.
	/// Use <see cref="WriteInternal"/> to enqueue data.
	/// </summary>
	/// <param name="buffer">The byte array containing data to write.</param>
	/// <param name="offset">The zero-based index in <paramref name="buffer"/> at which to begin reading bytes.</param>
	/// <param name="count">The number of bytes to write.</param>
	/// <exception cref="NotSupportedException">Always thrown.</exception>
	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Enqueues bytes from <paramref name="buffer"/> into the pipe, blocking if the internal
	/// buffer has reached <see cref="MaxBufferLength"/> until the reader consumes some data.
	/// </summary>
	/// <param name="buffer">The byte array containing data to enqueue.</param>
	/// <param name="offset">The zero-based index in <paramref name="buffer"/> at which to begin reading bytes.</param>
	/// <param name="count">The number of bytes to enqueue.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="buffer"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown if the sum of <paramref name="offset"/> and <paramref name="count"/> exceeds the buffer length.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> or <paramref name="count"/> is negative.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the write side of the pipe has already been closed.</exception>
	public void WriteInternal(byte[] buffer, int offset, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (offset + count > buffer.Length)
		{
			throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
		}
		if (offset < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException("offset", "offset or count is negative.");
		}
		if (count == 0)
		{
			return;
		}
		if (_closedWrite)
		{
			throw new InvalidOperationException("Stream closed for writes");
		}
		lock (_buffer)
		{
			while (_buffer.Count >= MaxBufferLength)
			{
				Monitor.Wait(_buffer);
			}
			if (!_isDisposed)
			{
				_isFlushed = false;
				for (int i = offset; i < offset + count; i++)
				{
					_buffer.Enqueue(buffer[i]);
				}
				Monitor.Pulse(_buffer);
			}
		}
	}

	/// <summary>
	/// Releases the resources used by the stream, waking any blocked reader threads.
	/// </summary>
	/// <param name="disposing"><see langword="true"/> when called from <see cref="IDisposable.Dispose"/>; <see langword="false"/> when called from a finalizer.</param>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (!_isDisposed)
		{
			lock (_buffer)
			{
				_isDisposed = true;
				_buffer.Clear();
				Monitor.Pulse(_buffer);
			}
			Closed();
		}
	}

	/// <summary>
	/// Signals that no more data will be written to the pipe, waking any blocked reader
	/// so it can drain the remaining buffered data and reach end-of-stream.
	/// </summary>
	public void CloseWrite()
	{
		lock (_buffer)
		{
			if (!_closedWrite && !_isDisposed)
			{
				_closedWrite = true;
				Monitor.Pulse(_buffer);
			}
		}
	}

	/// <summary>
	/// Throws <see cref="ObjectDisposedException"/> if this stream has already been disposed.
	/// </summary>
	/// <exception cref="ObjectDisposedException">Thrown if the stream has been disposed.</exception>
	private void CheckDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
	}

	/// <summary>
	/// Invokes the <see cref="OnDispose"/> callback (if set) exactly once and clears it.
	/// </summary>
	private void Closed()
	{
		Action onDispose = OnDispose;
		OnDispose = null;
		onDispose?.Invoke();
	}
}
