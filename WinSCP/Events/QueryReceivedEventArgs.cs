using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides data for the query received event.
/// </summary>
[Guid("1C2C3740-CB42-4B10-B240-2EF64E03DAA3")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class QueryReceivedEventArgs : EventArgs
{
	/// <summary>
	/// Specifies the possible actions in response to a query.
	/// </summary>
	internal enum Action
	{
		/// <summary>
		/// No action has been selected.
		/// </summary>
		None,
		/// <summary>
		/// Abort the operation.
		/// </summary>
		Abort,
		/// <summary>
		/// Continue the operation.
		/// </summary>
		Continue
	}

	/// <summary>
	/// Gets the query message from the remote server.
	/// </summary>
	public string Message { get; internal set; }

	/// <summary>
	/// Gets or sets the selected action in response to the query.
	/// </summary>
	internal Action SelectedAction { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="QueryReceivedEventArgs"/> class.
	/// </summary>
	internal QueryReceivedEventArgs()
	{
		SelectedAction = Action.None;
	}

	/// <summary>
	/// Aborts the operation in response to the query.
	/// </summary>
	public void Abort()
	{
		SelectedAction = Action.Abort;
	}

	/// <summary>
	/// Continues the operation in response to the query.
	/// </summary>
	public void Continue()
	{
		SelectedAction = Action.Continue;
	}
}
