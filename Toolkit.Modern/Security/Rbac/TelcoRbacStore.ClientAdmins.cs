namespace ByteForge.Toolkit.Security.Rbac;

public partial class TelcoRbacStore
{
    /// <summary>Creates the missing common role during project initialization without changing existing grants.</summary>
    public void EnsureClientAdminRole()
    {
        database.ExecuteQuery(
            "INSERT INTO tbl_TELCO_Roles (ProjectId, Name, DisplayName, Description, LandingController, Priority, IsSystem, IsActive, CreatedAtUtc, UpdatedAtUtc) " +
            "SELECT p.Id, 'ClientAdmin', 'Client Admin', 'Manage project users and delegate permissions held by the actor.', '', 0, 1, 1, GETUTCDATE(), GETUTCDATE() " +
            "FROM tbl_TELCO_Projects p WHERE p.ProjectKey = @ProjectKey AND p.IsActive = 1 " +
            "AND NOT EXISTS (SELECT 1 FROM tbl_TELCO_Roles r WHERE r.ProjectId = p.Id AND r.Name = 'ClientAdmin')", projectKey);
        ThrowIfFailed("initialize the common Client Admin role");
    }

    /// <summary>Returns active projects where an identity has Admin authority or delegation permission.</summary>
    public List<string> GetClientAdminProjects(int userId)
    {
        var rows = database.GetRecords<ClientAdminProject>(
            "SELECT DISTINCT p.ProjectKey FROM tbl_TELCO_UserRoles ur " +
            "JOIN tbl_TELCO_Roles r ON r.Id = ur.RoleId " +
            "JOIN tbl_TELCO_Users u ON u.Id = ur.UserId " +
            "JOIN tbl_TELCO_Projects p ON p.Id = r.ProjectId " +
            "JOIN tbl_TELCO_UserProjects up ON up.UserId = u.Id AND up.ProjectId = p.Id " +
            "WHERE u.Id = @UserId AND u.IsActive = 1 AND p.IsActive = 1 AND up.IsActive = 1 " +
            "AND r.IsActive = 1 AND (r.Name = 'Admin' OR EXISTS (SELECT 1 FROM tbl_TELCO_RolePrivileges rp JOIN tbl_TELCO_Privileges v ON v.Id=rp.PrivilegeId WHERE rp.RoleId=r.Id AND v.IsActive=1 AND v.Name='Rbac.Delegate'))", userId);
        ThrowIfFailed("read project administration roles");
        return rows.Select(row => row.ProjectKey).ToList();
    }

    /// <summary>Checks the active delegation permission, independently of role names.</summary>
    public bool IsClientAdministrator(int userId)
    {
        var count = database.GetValue<int>(
            "SELECT COUNT(*) FROM tbl_TELCO_UserRoles ur JOIN tbl_TELCO_Roles r ON r.Id = ur.RoleId " +
            "JOIN tbl_TELCO_Users u ON u.Id = ur.UserId JOIN tbl_TELCO_Projects p ON p.Id = r.ProjectId " +
            "JOIN tbl_TELCO_UserProjects up ON up.UserId = u.Id AND up.ProjectId = p.Id " +
            "WHERE u.Id = @UserId AND p.ProjectKey = @ProjectKey AND u.IsActive = 1 AND p.IsActive = 1 " +
            "AND up.IsActive = 1 AND r.IsActive = 1 AND EXISTS (SELECT 1 FROM tbl_TELCO_RolePrivileges rp JOIN tbl_TELCO_Privileges v ON v.Id=rp.PrivilegeId WHERE rp.RoleId=r.Id AND v.IsActive=1 AND v.Name='Rbac.Delegate')", userId, projectKey);
        ThrowIfFailed("check project delegation permission");
        return count > 0;
    }

    /// <summary>Returns the union of active role permissions; full administrators have every project permission.</summary>
    public List<string> GetClientAdminPrivileges(int userId)
    {
        var access = GetAccess(userId);
        if (access == null) return new List<string>();
        if (access.IsSuperAdmin || access.IsProjectAdmin) return GetPrivileges().Select(p => p.Name).ToList();
        var permissions = database.GetRecords<TelcoRbacPrivilege>(
            "SELECT DISTINCT v.Name FROM tbl_TELCO_UserRoles ur JOIN tbl_TELCO_Roles r ON r.Id = ur.RoleId " +
            "JOIN tbl_TELCO_Projects p ON p.Id = r.ProjectId JOIN tbl_TELCO_RolePrivileges rp ON rp.RoleId = r.Id " +
            "JOIN tbl_TELCO_Privileges v ON v.Id = rp.PrivilegeId " +
            "WHERE ur.UserId = @UserId AND p.ProjectKey = @ProjectKey AND r.IsActive = 1 AND v.IsActive = 1", userId, projectKey);
        ThrowIfFailed("read effective project role permissions");
        return permissions.Select(p => p.Name).ToList();
    }

    /// <summary>Populates role-derived administration information for compatibility with existing callers.</summary>
    public List<TelcoRbacUser> GetUsersWithClientAdministration()
    {
        var users = GetUsers();
        var roles = GetRoles();
        foreach (var user in users)
        {
            user.IsClientAdmin = roles.Any(role => role.IsActive && user.RoleIds.Contains(role.Id) && role.Privileges.Contains(DelegatePrivilegesPrivilege, StringComparer.OrdinalIgnoreCase));
            user.ClientAdminPrivileges = roles.Where(role => role.IsActive && user.RoleIds.Contains(role.Id)).SelectMany(role => role.Privileges).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
        return users;
    }

    /// <summary>Rejects the retired per-user grant contract; assign the common role instead.</summary>
    [Obsolete("Assign ClientAdmin through ReplaceUserRoles and edit its permissions through SaveRole.")]
    public void SetClientAdministration(int actorId, int userId, bool enabled, IEnumerable<string>? privileges)
        => throw new InvalidOperationException("Client Admin is now a normal project role. Reload the editor and assign roles instead of separate grants.");

    /// <summary>Requires an active project administration role or global administrator authority.</summary>
    public TelcoRbacAccess RequireClientAdministration(int actorId)
    {
        var access = GetAccess(actorId);
        if (access == null || (!access.IsSuperAdmin && !access.IsProjectAdmin && !access.IsClientAdmin))
            throw new UnauthorizedAccessException("You cannot administer this project.");
        return access;
    }

    /// <summary>Gets roles whose full permission set can be assigned by the actor.</summary>
    public List<TelcoRbacRole> GetDelegableRoles(int actorId)
    {
        var access = RequireClientAdministration(actorId);
        var roles = GetRoles();
        if (access.IsSuperAdmin || access.IsProjectAdmin) return roles;
        var ceiling = GetClientAdminPrivileges(actorId);
        return roles.Where(role => TelcoClientAdminPolicy.AllowsRole(ceiling, role)).ToList();
    }

    /// <summary>Lists project users independently of mutation rights, including the actor and protected accounts.</summary>
    public List<TelcoRbacUser> GetVisibleUsers(int actorId)
    {
        RequireClientAdministration(actorId);
        var users = GetUsers();
        var manageable = new HashSet<int>(GetDelegableUsers(actorId).Select(user => user.Id));
        foreach (var user in users) user.CanManage = manageable.Contains(user.Id);
        return users;
    }

    /// <summary>Returns only users whose project access the actor may change. Do not use this as a visibility filter.</summary>
    public List<TelcoRbacUser> GetDelegableUsers(int actorId)
    {
        var access = RequireClientAdministration(actorId);
        var users = GetUsersWithClientAdministration();
        if (access.IsSuperAdmin || access.IsProjectAdmin) return users;
        var roles = GetDelegableRoles(actorId).Select(role => role.Id).ToList();
        return users.Where(u => !u.IsSuperAdmin && u.Id != actorId && u.RoleIds.All(roles.Contains)).ToList();
    }

    /// <summary>Atomically creates an identity or updates a managed member's password, roles and access.</summary>
    public int SaveDelegatedUser(int actorId, int userId, string username, string displayName, string email,
        string? password, bool hasAccess, IEnumerable<int>? roleIds, bool allowInvitation = false)
    {
        using var transaction = database.BeginTransaction(autoCommit: false);
        RequireClientAdministration(actorId);
        var selected = (roleIds ?? Enumerable.Empty<int>()).Distinct().ToList();
        var allowed = GetDelegableRoles(actorId).Where(role => role.IsActive).Select(role => role.Id).ToList();
        if (selected.Any(id => !allowed.Contains(id)))
            throw new UnauthorizedAccessException("The selected role is outside your approved permissions.");
        if (userId > 0 && !GetDelegableUsers(actorId).Any(user => user.Id == userId))
            throw new UnauthorizedAccessException("This identity cannot be managed by this Client Admin.");
        if (userId <= 0)
        {
            if (!hasAccess) throw new ArgumentException("A new user must receive project access.");
            if ((!allowInvitation && string.IsNullOrWhiteSpace(password)) || (!string.IsNullOrWhiteSpace(password) && password.Length < 8))
                throw new ArgumentException("An initial password of at least eight characters is required.");
            // Do not let knowledge of another global identity's email grant access or overwrite its password.
            EnsureUniqueGlobalIdentityFields(0, Require(username, nameof(username)), Require(email, nameof(email)));
            userId = CreateGlobalUser(username, displayName, email, password, true, false);
        }
        else if (!string.IsNullOrWhiteSpace(password)) UpdateManagedPassword(actorId, userId, password);
        SetGlobalProjectAccess(userId, hasAccess);
        if (hasAccess) ReplaceUserRoles(userId, selected);
        transaction.Commit();
        return userId;
    }

    /// <summary>Atomically verifies the old and new role grants before a Client Admin changes a shared role.</summary>
    public int SaveDelegatedRole(int actorId, int roleId, string name, string displayName, string? description,
        string? landingPage, int priority, bool active, IEnumerable<string>? privileges)
    {
        using var transaction = database.BeginTransaction(autoCommit: false);
        RequireClientAdministration(actorId);
        var current = roleId > 0 ? GetDelegableRoles(actorId).FirstOrDefault(role => role.Id == roleId) : null;
        if (roleId > 0 && current == null) throw new UnauthorizedAccessException("This role is outside your approved permissions.");
        if (roleId > 0 && GetUsers().Any(user => user.RoleIds.Contains(roleId) && (user.Id == actorId || user.IsSuperAdmin || user.Roles.Any(role => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)))))
            throw new UnauthorizedAccessException("You cannot change a role used by yourself or a full administrator.");
        var proposed = new TelcoRbacRole { Name = current?.Name ?? name, LandingController = landingPage ?? string.Empty,
            Privileges = (privileges ?? Enumerable.Empty<string>()).ToList() };
        if (!TelcoClientAdminPolicy.AllowsRole(GetClientAdminPrivileges(actorId), proposed))
            throw new UnauthorizedAccessException("The role exceeds your approved pages or permissions.");
        var id = SaveRole(roleId, name, displayName, description, landingPage, priority, active, proposed.Privileges);
        transaction.Commit();
        return id;
    }

    private sealed class ClientAdminProject { public string ProjectKey { get; set; } = string.Empty; }
    private sealed class ClientAdminGrant { public int UserId { get; set; } public string Name { get; set; } = string.Empty; }
}
