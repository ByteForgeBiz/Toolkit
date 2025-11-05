using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("1C2C3740-CB42-4B10-B240-2EF64E03DAA3")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class QueryReceivedEventArgs : EventArgs
{
	internal enum Action
	{
		None,
		Abort,
		Continue
	}

	public string Message { get; internal set; }

	internal Action SelectedAction { get; set; }

	internal QueryReceivedEventArgs()
	{
		SelectedAction = Action.None;
	}

	public void Abort()
	{
		SelectedAction = Action.Abort;
	}

	public void Continue()
	{
		SelectedAction = Action.Continue;
	}
}
