using System;

namespace ByteForge.WinSCP;

internal struct JobObjectExtendedLimitInformation
{
	public JobObjectBasicLimitInformation BasicLimitInformation;

	public IOCounters IoInfo;

	public UIntPtr ProcessMemoryLimit;

	public UIntPtr JobMemoryLimit;

	public UIntPtr PeakProcessMemoryUsed;

	public UIntPtr PeakJobMemoryUsed;
}
