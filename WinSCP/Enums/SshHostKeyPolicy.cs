using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

/// <summary>
/// Specifies the policy for validating SSH host keys.
/// </summary>
[Guid("8A98AB8F-30E8-4539-A3DE-A33DDC43B33C")]
[ComVisible(true)]
public enum SshHostKeyPolicy
{
	/// <summary>
	/// Check the host key against known hosts.
	/// </summary>
	Check,
	/// <summary>
	/// Accept any host key without verification (insecure).
	/// </summary>
	GiveUpSecurityAndAcceptAny,
	/// <summary>
	/// Accept new host keys automatically.
	/// </summary>
	AcceptNew
}
