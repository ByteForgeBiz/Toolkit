using System.Security.AccessControl;
using System.Security.Principal;

namespace ByteForge.WinSCP;

/// <summary>
/// Represents a generic access rule for security objects.
/// </summary>
internal class GenericAccessRule : AccessRule
{
	/// <summary>
	/// Initializes a new instance of the <see cref="GenericAccessRule"/> class with the specified identity, access mask, and access control type.
	/// </summary>
	/// <param name="identity">The identity to which the access rule applies.</param>
	/// <param name="accessMask">The access mask specifying the access rights.</param>
	/// <param name="type">The type of access control (Allow or Deny).</param>
	public GenericAccessRule(IdentityReference identity, int accessMask, AccessControlType type)
		: base(identity, accessMask, isInherited: false, InheritanceFlags.None, PropagationFlags.None, type)
	{
	}
}
