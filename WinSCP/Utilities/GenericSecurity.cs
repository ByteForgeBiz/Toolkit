using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ByteForge.WinSCP;

/// <summary>
/// Provides generic security operations for native objects.
/// </summary>
internal class GenericSecurity : NativeObjectSecurity
{
	/// <summary>
	/// Gets the type used for access rights.
	/// </summary>
	public override Type AccessRightType
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Gets the type used for access rules.
	/// </summary>
	public override Type AccessRuleType => typeof(AccessRule);

	/// <summary>
	/// Gets the type used for audit rules.
	/// </summary>
	public override Type AuditRuleType => typeof(AuditRule);

	/// <summary>
	/// Initializes a new instance of the <see cref="GenericSecurity"/> class.
	/// </summary>
	/// <param name="isContainer">Indicates whether the object is a container.</param>
	/// <param name="resType">The resource type.</param>
	/// <param name="objectHandle">The handle to the object.</param>
	/// <param name="sectionsRequested">The access control sections requested.</param>
	public GenericSecurity(bool isContainer, ResourceType resType, SafeHandle objectHandle, AccessControlSections sectionsRequested)
		: base(isContainer, resType, objectHandle, sectionsRequested)
	{
	}

	/// <summary>
	/// Persists the security settings to the specified handle.
	/// </summary>
	/// <param name="handle">The handle to persist to.</param>
	/// <param name="includeSections">The access control sections to include.</param>
	public new void Persist(SafeHandle handle, AccessControlSections includeSections)
	{
		base.Persist(handle, includeSections);
	}

	/// <summary>
	/// Adds an access rule to the security object.
	/// </summary>
	/// <param name="rule">The access rule to add.</param>
	public new void AddAccessRule(AccessRule rule)
	{
		base.AddAccessRule(rule);
	}

	/// <summary>
	/// Factory method for creating access rules. Not implemented.
	/// </summary>
	public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Factory method for creating audit rules. Not implemented.
	/// </summary>
	public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
	{
		throw new NotImplementedException();
	}
}
