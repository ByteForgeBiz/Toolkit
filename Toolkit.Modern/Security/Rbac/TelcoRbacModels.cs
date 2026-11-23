namespace ByteForge.Toolkit.Security.Rbac;

/// <summary>Represents a global TELCO identity as seen from one project.</summary>
public class TelcoRbacUser
{
    /// <summary>Gets or sets whether the identity has no remaining project or global access.</summary>
    public bool CanDelete { get; set; }
    /// <summary>Gets or sets whether a pending identity has an expired invitation and no current setup link.</summary>
    public bool CanResendInvitation { get; set; }
    /// <summary>Gets or sets whether the current editor may change this user's project access.</summary>
    public bool CanManage { get; set; } = true;
    /// <summary>Gets or sets the global identity identifier.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the globally unique username.</summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the globally unique email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Gets or sets whether the global identity is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets whether the identity can access every project.</summary>
    public bool IsSuperAdmin { get; set; }
    /// <summary>Gets or sets whether the project membership is active.</summary>
    public bool HasProjectAccess { get; set; }
    /// <summary>Gets or sets the explicit project Client Admin assignment.</summary>
    public bool IsClientAdmin { get; set; }
    /// <summary>Gets or sets the Super Admin-approved page and permission ceiling.</summary>
    public List<string> ClientAdminPrivileges { get; set; } = new();
    /// <summary>Gets or sets the most recent UTC login time.</summary>
    public DateTime? LastLoginAtUtc { get; set; }
    /// <summary>Gets or sets assigned project-role identifiers.</summary>
    public List<int> RoleIds { get; set; } = new();
    /// <summary>Gets or sets assigned project-role names.</summary>
    public List<string> Roles { get; set; } = new();
}

/// <summary>Represents a project-scoped TELCO role.</summary>
public class TelcoRbacRole
{
    /// <summary>Gets or sets the role identifier.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the project-unique role name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the display label.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the role description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional landing route.</summary>
    public string LandingController { get; set; } = string.Empty;
    /// <summary>Gets or sets the role priority.</summary>
    public int Priority { get; set; }
    /// <summary>Gets or sets whether the role is protected system data.</summary>
    public bool IsSystem { get; set; }
    /// <summary>Gets or sets whether the role is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets the assigned user count.</summary>
    public int UserCount { get; set; }
    /// <summary>Gets or sets assigned privilege names.</summary>
    public List<string> Privileges { get; set; } = new();
}

/// <summary>Represents a global TELCO privilege available to project roles.</summary>
public class TelcoRbacPrivilege
{
    /// <summary>Gets or sets the privilege identifier.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the globally unique privilege name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the display label.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the privilege description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the display category.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Gets or sets whether this privilege opens a page and can be selected as a landing page.</summary>
    public bool IsPage { get; set; } = true;
}

/// <summary>Describes one identity's administrative access to a project.</summary>
public class TelcoRbacAccess
{
    /// <summary>Gets or sets the global identity identifier.</summary>
    public int UserId { get; set; }
    /// <summary>Gets or sets the username.</summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the current project key.</summary>
    public string ProjectKey { get; set; } = string.Empty;
    /// <summary>Gets or sets whether access is global.</summary>
    public bool IsSuperAdmin { get; set; }
    /// <summary>Gets or sets whether the identity can administer the current project.</summary>
    public bool IsProjectAdmin { get; set; }
    /// <summary>Gets or sets whether the identity is a delegated Client Admin for this project.</summary>
    public bool IsClientAdmin { get; set; }
    /// <summary>Gets whether the identity has global or project administration access.</summary>
    public bool CanAdminister => IsSuperAdmin || IsProjectAdmin || IsClientAdmin;
}

internal sealed class TelcoRbacRoleAssignment
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal sealed class TelcoRbacPrivilegeAssignment
{
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>Represents a global account-invitation token.</summary>
public sealed class TelcoRbacInvitationToken
{
    /// <summary>Gets or sets the token identifier.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the global identity identifier.</summary>
    public int UserId { get; set; }
    /// <summary>Gets or sets the token expiration time in UTC.</summary>
    public DateTime ExpiresAtUtc { get; set; }
    /// <summary>Gets or sets when the token was consumed.</summary>
    public DateTime? ConsumedAtUtc { get; set; }
    /// <summary>Gets or sets whether the row is an invitation.</summary>
    public bool IsInvitation { get; set; }
}
