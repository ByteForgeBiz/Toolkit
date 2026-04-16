using System;

namespace ByteForge.WinSCP;

// Fields mirror the Win32 JOBOBJECT_BASIC_LIMIT_INFORMATION struct layout for P/Invoke marshalling.
// Most fields exist solely to preserve correct offsets and struct size; only LimitFlags is set.
#pragma warning disable CS0649 // Field is never assigned to and will always have its default value
/// <summary>
/// Contains basic limit information for a Windows Job object, corresponding to the
/// <c>JOBOBJECT_BASIC_LIMIT_INFORMATION</c> native structure.
/// </summary>
internal struct JobObjectBasicLimitInformation
{
	/// <summary>
	/// The per-process user-mode execution time limit, in 100-nanosecond ticks.
	/// Ignored unless the <c>JOB_OBJECT_LIMIT_PROCESS_TIME</c> flag is set in <see cref="LimitFlags"/>.
	/// </summary>
	public long PerProcessUserTimeLimit;

	/// <summary>
	/// The per-job user-mode execution time limit, in 100-nanosecond ticks.
	/// Ignored unless the <c>JOB_OBJECT_LIMIT_JOB_TIME</c> flag is set in <see cref="LimitFlags"/>.
	/// </summary>
	public long PerJobUserTimeLimit;

	/// <summary>
	/// A bitmask of flags that indicate the limits in effect for the job.
	/// </summary>
	public uint LimitFlags;

	/// <summary>
	/// The minimum working set size for each process in the job.
	/// Ignored unless the <c>JOB_OBJECT_LIMIT_WORKINGSET</c> flag is set in <see cref="LimitFlags"/>.
	/// </summary>
	public UIntPtr MinimumWorkingSetSize;

	/// <summary>
	/// The maximum working set size for each process in the job.
	/// Ignored unless the <c>JOB_OBJECT_LIMIT_WORKINGSET</c> flag is set in <see cref="LimitFlags"/>.
	/// </summary>
	public UIntPtr MaximumWorkingSetSize;

	/// <summary>
	/// The maximum number of concurrently active processes in the job.
	/// Ignored unless the <c>JOB_OBJECT_LIMIT_ACTIVE_PROCESS</c> flag is set in <see cref="LimitFlags"/>.
	/// </summary>
	public uint ActiveProcessLimit;

	/// <summary>
	/// The processor affinity for all processes in the job.
	/// Ignored unless the <c>JOB_OBJECT_LIMIT_AFFINITY</c> flag is set in <see cref="LimitFlags"/>.
	/// </summary>
	public UIntPtr Affinity;

	/// <summary>
	/// The priority class for all processes in the job.
	/// Ignored unless the <c>JOB_OBJECT_LIMIT_PRIORITY_CLASS</c> flag is set in <see cref="LimitFlags"/>.
	/// </summary>
	public uint PriorityClass;

	/// <summary>
	/// The scheduling class for all processes in the job.
	/// Ignored unless the <c>JOB_OBJECT_LIMIT_SCHEDULING_CLASS</c> flag is set in <see cref="LimitFlags"/>.
	/// </summary>
	public uint SchedulingClass;
}
#pragma warning restore CS0649
