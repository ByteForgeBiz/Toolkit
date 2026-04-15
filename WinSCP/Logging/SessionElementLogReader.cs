namespace ByteForge.WinSCP;

/// <summary>
/// Provides an <see cref="ElementLogReader"/> that is scoped to the lifetime of a WinSCP session.
/// When the underlying session closes unexpectedly while the reader is still active,
/// a <see cref="SessionLocalException"/> is thrown to signal the premature termination.
/// </summary>
internal class SessionElementLogReader : ElementLogReader
{
	/// <summary>
	/// Indicates whether the reader is currently being disposed, used to suppress
	/// the unexpected-close exception during normal cleanup.
	/// </summary>
	private bool _disposing;

	/// <summary>
	/// Initializes a new instance of the <see cref="SessionElementLogReader"/> class,
	/// scoped to the current element of the given parent reader.
	/// </summary>
	/// <param name="parentReader">The parent log reader whose current element is used as the scope boundary.</param>
	public SessionElementLogReader(CustomLogReader parentReader)
		: base(parentReader)
	{
	}

	/// <summary>
	/// Releases resources used by the reader, suppressing unexpected-close detection
	/// during the disposal process.
	/// </summary>
	public override void Dispose()
	{
		using (base.Session.Logger.CreateCallstack())
		{
			_disposing = true;
			base.Dispose();
		}
	}

	/// <summary>
	/// Reads the next node from the session log. If the element has been fully read
	/// and the reader is not being disposed, a <see cref="SessionLocalException"/> is thrown
	/// to indicate that the session closed unexpectedly.
	/// </summary>
	/// <param name="flags">Flags controlling the read behavior.</param>
	/// <returns><see langword="true"/> if the next node was read successfully; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="SessionLocalException">
	/// Thrown when the session closes unexpectedly while the reader is still active.
	/// </exception>
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
