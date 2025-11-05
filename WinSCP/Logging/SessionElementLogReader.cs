namespace ByteForge.WinSCP;

internal class SessionElementLogReader : ElementLogReader
{
	private bool _disposing;

	public SessionElementLogReader(CustomLogReader parentReader)
		: base(parentReader)
	{
	}

	public override void Dispose()
	{
		using (base.Session.Logger.CreateCallstack())
		{
			_disposing = true;
			base.Dispose();
		}
	}

	public override bool Read(LogReadFlags flags)
	{
		bool result = base.Read(flags);
		if (_read && !_disposing)
		{
			throw base.Session.Logger.WriteException(new SessionLocalException(base.Session, "Session has unexpectedly closed"));
		}
		return result;
	}
}
