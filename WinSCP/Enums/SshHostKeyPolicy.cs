using System.Runtime.InteropServices;

namespace ByteForge.WinSCP;

[Guid("8A98AB8F-30E8-4539-A3DE-A33DDC43B33C")]
[ComVisible(true)]
public enum SshHostKeyPolicy
{
	Check,
	GiveUpSecurityAndAcceptAny,
	AcceptNew
}
