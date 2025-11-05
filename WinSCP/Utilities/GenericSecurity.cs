using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ByteForge.WinSCP;

internal class GenericSecurity : NativeObjectSecurity
{
	public override Type AccessRightType
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override Type AccessRuleType => typeof(AccessRule);

	public override Type AuditRuleType => typeof(AuditRule);

	public GenericSecurity(bool isContainer, ResourceType resType, SafeHandle objectHandle, AccessControlSections sectionsRequested)
		: base(isContainer, resType, objectHandle, sectionsRequested)
	{
	}

	public new void Persist(SafeHandle handle, AccessControlSections includeSections)
	{
		base.Persist(handle, includeSections);
	}

	public new void AddAccessRule(AccessRule rule)
	{
		base.AddAccessRule(rule);
	}

	public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
	{
		throw new NotImplementedException();
	}

	public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
	{
		throw new NotImplementedException();
	}
}
