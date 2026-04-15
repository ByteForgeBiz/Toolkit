namespace ByteForge.WinSCP;

/// <summary>
/// Combines call-flow tracing with mutual-exclusion locking, entering the supplied
/// <see cref="Lock"/> on construction and releasing it on disposal.
/// </summary>
internal class CallstackAndLock : Callstack
{
	/// <summary>
	/// The lock held for the lifetime of this instance, or <see langword="null"/> after
	/// <see cref="DisarmLock"/> has been called.
	/// </summary>
	private Lock _lock;

	/// <summary>
	/// Initializes a new instance of the <see cref="CallstackAndLock"/> class, entering both
	/// the callstack log scope and the supplied lock.
	/// </summary>
	/// <param name="logger">The logger used for call-flow tracing.</param>
	/// <param name="alock">The lock to enter and hold until disposal.</param>
	/// <param name="token">An optional token appended to the method name in log messages.</param>
	public CallstackAndLock(Logger logger, Lock alock, object token = null)
		: base(logger, token)
	{
		_lock = alock;
		_lock.Enter();
	}

	/// <summary>
	/// Releases the held lock (if not previously disarmed) and writes the "leaving" log entry.
	/// </summary>
	public override void Dispose()
	{
		if (_lock != null)
		{
			_lock.Exit();
		}
		base.Dispose();
	}

	/// <summary>
	/// Disarms the lock so that it will not be released when this instance is disposed.
	/// Call this when responsibility for exiting the lock is transferred elsewhere.
	/// </summary>
	public void DisarmLock()
	{
		_lock = null;
	}
}
