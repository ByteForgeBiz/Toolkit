using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("E421924E-87F0-433E-AF38-CE034DC8E8CB")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
[ComVisible(true)]
public sealed class FileTransferProgressEventArgs : EventArgs
{
	public ProgressOperation Operation { get; internal set; }

	public ProgressSide Side { get; internal set; }

	public string FileName { get; internal set; }

	public string Directory { get; internal set; }

	public double OverallProgress { get; internal set; }

	public double FileProgress { get; internal set; }

	public int CPS { get; internal set; }

	public bool Cancel { get; set; }

	internal FileTransferProgressEventArgs()
	{
	}
}
