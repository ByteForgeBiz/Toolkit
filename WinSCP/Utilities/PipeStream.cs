using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ByteForge.WinSCP;

internal class PipeStream : Stream
{
	private readonly Queue<byte> _buffer = new Queue<byte>();

	private bool _isFlushed;

	private bool _isDisposed;

	private bool _closedWrite;

	private long _position;

	public long MaxBufferLength { get; set; } = 209715200L;

	public Action OnDispose { get; set; }

	public override bool CanRead => !_isDisposed;

	public override bool CanSeek => false;

	public override bool CanWrite => false;

	public override long Length
	{
		get
		{
			throw new NotSupportedException();
		}
	}

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

	public override void Flush()
	{
		CheckDisposed();
		_isFlushed = true;
		lock (_buffer)
		{
			Monitor.Pulse(_buffer);
		}
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

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

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
	}

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

	private void CheckDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
	}

	private void Closed()
	{
		Action onDispose = OnDispose;
		OnDispose = null;
		onDispose?.Invoke();
	}
}
