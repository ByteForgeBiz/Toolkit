using System;

namespace ByteForge.WinSCP;

internal class ProgressHandler : IDisposable
{
	private readonly Session _session;

	public ProgressHandler(Session session)
	{
		_session = session;
	}

	public void Dispose()
	{
		using (_session.Logger.CreateCallstack())
		{
			_session.DisableProgressHandling();
		}
	}
}
