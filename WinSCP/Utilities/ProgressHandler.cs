using System;

namespace ByteForge.WinSCP;

/// <summary>
/// Manages the lifetime of progress-event handling for a WinSCP session.
/// Disables progress handling when disposed, ensuring that progress callbacks
/// are cleanly unregistered after a transfer or operation completes.
/// </summary>
internal class ProgressHandler : IDisposable
{
	/// <summary>
	/// The WinSCP session whose progress handling is managed by this instance.
	/// </summary>
	private readonly Session _session;

	/// <summary>
	/// Initializes a new instance of the <see cref="ProgressHandler"/> class for the given session.
	/// </summary>
	/// <param name="session">The WinSCP session on which progress handling will be disabled upon disposal.</param>
	public ProgressHandler(Session session)
	{
		_session = session;
	}

	/// <summary>
	/// Disables progress handling on the associated session and releases all resources.
	/// </summary>
	public void Dispose()
	{
		using (_session.Logger.CreateCallstack())
		{
			_session.DisableProgressHandling();
		}
	}
}
