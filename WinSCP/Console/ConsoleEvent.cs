namespace ByteForge.WinSCP;

/// <summary>
/// Identifies the type of console event contained in the shared-memory communication structure.
/// </summary>
public enum ConsoleEvent
{
	/// <summary>
	/// No event; the communication structure is idle.
	/// </summary>
	None,

	/// <summary>
	/// A print event requesting that text be written to the output.
	/// </summary>
	Print,

	/// <summary>
	/// An input event requesting that the user supply a line of text.
	/// </summary>
	Input,

	/// <summary>
	/// A choice event presenting the user with a set of selectable options.
	/// </summary>
	Choice,

	/// <summary>
	/// A title event requesting an update to the console window title.
	/// </summary>
	Title,

	/// <summary>
	/// An initialization event used to negotiate console capabilities at startup.
	/// </summary>
	Init,

	/// <summary>
	/// A progress event reporting file-transfer progress information.
	/// </summary>
	Progress,

	/// <summary>
	/// A transfer-out event delivering data written by the WinSCP process to the outbound pipe.
	/// </summary>
	TransferOut,

	/// <summary>
	/// A transfer-in event requesting data to be read from the inbound pipe by the WinSCP process.
	/// </summary>
	TransferIn
}
