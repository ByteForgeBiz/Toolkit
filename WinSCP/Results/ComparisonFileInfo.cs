using System;
using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents file information used for comparison operations.
/// </summary>
[Guid("2D6EFFB5-69BA-47AA-90E8-A92953E8B58A")]
[ComVisible(true)]
public sealed class ComparisonFileInfo
{
	/// <summary>
	/// Gets the name of the file.
	/// </summary>
	public string FileName { get; internal set; }

	/// <summary>
	/// Gets the last write time of the file.
	/// </summary>
	public DateTime LastWriteTime { get; internal set; }

	/// <summary>
	/// Gets the length of the file in bytes.
	/// </summary>
	public long Length { get; internal set; }

	/// <summary>
	/// Gets the length of the file as a32-bit integer.
	/// </summary>
	public int Length32 => GetLength32();

	/// <summary>
	/// Initializes a new instance of the <see cref="ComparisonFileInfo"/> class.
	/// </summary>
	internal ComparisonFileInfo()
	{
	}

	/// <summary>
	/// Converts the file length to a32-bit integer.
	/// </summary>
	/// <returns>The file length as a32-bit integer.</returns>
	private int GetLength32()
	{
		return Tools.LengthTo32Bit(Length);
	}
}
