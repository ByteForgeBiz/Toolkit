using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("2D6EFFB5-69BA-47AA-90E8-A92953E8B58A")]
[ComVisible(true)]
public sealed class ComparisonFileInfo
{
	public string FileName { get; internal set; }

	public DateTime LastWriteTime { get; internal set; }

	public long Length { get; internal set; }

	public int Length32 => GetLength32();

	internal ComparisonFileInfo()
	{
	}

	private int GetLength32()
	{
		return Tools.LengthTo32Bit(Length);
	}
}
