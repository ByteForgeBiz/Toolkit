using System;
using System.Threading;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides a synchronization lock that prevents recursive locking.
/// </summary>
public class Lock
{
	/// <summary>
	/// The underlying monitor object used for mutual exclusion.
	/// </summary>
	private readonly object _lock = new object();

	/// <summary>
	/// Indicates whether the lock is currently held, used to detect and reject recursive
	/// lock attempts.
	/// </summary>
	private bool _locked;

	/// <summary>
	/// Enters the lock. Throws an exception if a recursive lock is attempted.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when a recursive lock is attempted.</exception>
	public void Enter()
	{
		Monitor.Enter(_lock);
		if (_locked)
		{
			Monitor.Exit(_lock);
			throw new InvalidOperationException("Recursive calls not allowed");
		}
		_locked = true;
	}

	/// <summary>
	/// Exits the lock.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when exiting without holding the lock.</exception>
	public void Exit()
	{
		if (!_locked)
		{
			throw new InvalidOperationException("Not locked");
		}
		_locked = false;
		Monitor.Exit(_lock);
	}
}
