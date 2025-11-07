namespace ByteForge.WinSCP;

/// <summary>
/// Specifies the type of information for job objects.
/// </summary>
internal enum JobObjectInfoType
{
	/// <summary>
	/// Associate a completion port with a job object.
	/// </summary>
	AssociateCompletionPortInformation = 7,
	/// <summary>
	/// Set basic limit information for a job object.
	/// </summary>
	BasicLimitInformation = 2,
	/// <summary>
	/// Set basic UI restrictions for a job object.
	/// </summary>
	BasicUIRestrictions = 4,
	/// <summary>
	/// Set end-of-job time information for a job object.
	/// </summary>
	EndOfJobTimeInformation = 6,
	/// <summary>
	/// Set extended limit information for a job object.
	/// </summary>
	ExtendedLimitInformation = 9,
	/// <summary>
	/// Set security limit information for a job object.
	/// </summary>
	SecurityLimitInformation = 5,
	/// <summary>
	/// Set group information for a job object.
	/// </summary>
	GroupInformation = 11
}
