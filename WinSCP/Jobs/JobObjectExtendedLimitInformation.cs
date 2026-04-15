using System;

namespace ByteForge.WinSCP;

/// <summary>
/// Contains extended limit information for a Windows Job object, corresponding to the
/// <c>JOBOBJECT_EXTENDED_LIMIT_INFORMATION</c> native structure.
/// </summary>
internal struct JobObjectExtendedLimitInformation
{
	/// <summary>
	/// The basic limit information for the job, including flags and per-process/per-job limits.
	/// </summary>
	public JobObjectBasicLimitInformation BasicLimitInformation;

	/// <summary>
	/// I/O counters that track the I/O activity of all processes in the job.
	/// </summary>
	public IOCounters IoInfo;

	/// <summary>
	/// The per-process virtual memory commit limit in bytes.
	/// Ignored unless the <c>JOB_OBJECT_LIMIT_PROCESS_MEMORY</c> flag is set.
	/// </summary>
	public UIntPtr ProcessMemoryLimit;

	/// <summary>
	/// The per-job virtual memory commit limit in bytes.
	/// Ignored unless the <c>JOB_OBJECT_LIMIT_JOB_MEMORY</c> flag is set.
	/// </summary>
	public UIntPtr JobMemoryLimit;

	/// <summary>
	/// The peak virtual memory usage of any process ever in the job.
	/// </summary>
	public UIntPtr PeakProcessMemoryUsed;

	/// <summary>
	/// The peak virtual memory usage of all processes currently in the job.
	/// </summary>
	public UIntPtr PeakJobMemoryUsed;
}
