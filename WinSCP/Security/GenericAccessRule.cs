using System.Security.AccessControl;
using System.Security.Principal;

namespace ByteForge.WinSCP;

internal class GenericAccessRule : AccessRule
{
	public GenericAccessRule(IdentityReference identity, int accessMask, AccessControlType type)
		: base(identity, accessMask, isInherited: false, InheritanceFlags.None, PropagationFlags.None, type)
	{
	}
}
