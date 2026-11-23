using ByteForge.Toolkit.Data;

namespace ByteForge.Toolkit.Security.Rbac;

/// <summary>Provides project-aware administration over the shared TELCO RBAC schema.</summary>
public partial class TelcoRbacStore
{
    /// <summary>Allows project-scoped administration within the actor's existing permissions.</summary>
    public const string DelegatePrivilegesPrivilege = "Rbac.Delegate";
    private readonly DBAccess database;
    private readonly string projectKey;
    private readonly TelcoPasswordHashService passwordHashService;

    /// <summary>Initializes the store from a Toolkit database configuration section.</summary>
    public TelcoRbacStore(string databaseSection, string projectKey)
        : this(new DBAccess(Require(databaseSection, nameof(databaseSection))), projectKey)
    {
    }

    /// <summary>Initializes the store with an existing database helper.</summary>
    public TelcoRbacStore(DBAccess database, string projectKey, TelcoPasswordHashService? passwordHashService = null)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.projectKey = Require(projectKey, nameof(projectKey));
        if (this.projectKey.Length > 50)
            throw new ArgumentException("The TELCO project key cannot exceed 50 characters.", nameof(projectKey));
        this.passwordHashService = passwordHashService ?? new TelcoPasswordHashService();
    }

    /// <summary>Gets the project boundary enforced by the store.</summary>
    public string ProjectKey => projectKey;

    /// <summary>Gets an active identity's administration context for the project.</summary>
    public TelcoRbacAccess? GetAccess(int userId)
    {
        var access = database.GetRecord<TelcoRbacAccess>(
            "SELECT TOP 1 u.Id AS UserId, u.Username, u.DisplayName, @ProjectKey AS ProjectKey, " +
            "       u.IsSuperAdmin, " +
            "       CASE WHEN EXISTS " +
            "       (SELECT 1 FROM tbl_TELCO_UserRoles ur " +
            "        JOIN tbl_TELCO_Roles r ON r.Id = ur.RoleId " +
            "        JOIN tbl_TELCO_Projects project ON project.Id = r.ProjectId " +
            "        WHERE ur.UserId = u.Id AND r.IsActive = 1 AND project.IsActive = 1 " +
            "          AND project.ProjectKey = @ProjectKey AND r.Name = 'Admin') THEN 1 ELSE 0 END AS IsProjectAdmin " +
            "  FROM tbl_TELCO_Users u " +
            " WHERE u.Id = @UserId AND u.IsActive = 1 " +
            "   AND (u.IsSuperAdmin = 1 OR EXISTS " +
            "       (SELECT 1 FROM tbl_TELCO_UserProjects up " +
            "        JOIN tbl_TELCO_Projects project ON project.Id = up.ProjectId " +
            "        WHERE up.UserId = u.Id AND up.IsActive = 1 AND project.IsActive = 1 " +
            "          AND project.ProjectKey = @ProjectKey))",
            projectKey, userId);
        ThrowIfFailed("read TELCO administrative access");
        // DBAccess maps a missing row to an empty instance rather than null.
        if (access == null || access.UserId <= 0) return null;
        access.IsClientAdmin = IsClientAdministrator(userId);
        return access;
    }

    private const string InvitationEligibilitySql =
        "CASE WHEN u.IsActive = 1 AND u.LastLoginAtUtc IS NULL AND u.LastPasswordChangedAtUtc IS NULL " +
        "AND EXISTS (SELECT 1 FROM tbl_TELCO_UserPasswordResetTokens t WHERE t.UserId = u.Id " +
        "AND t.IsInvitation = 1 AND t.ConsumedAtUtc IS NULL AND t.ExpiresAtUtc <= GETUTCDATE()) " +
        "AND NOT EXISTS (SELECT 1 FROM tbl_TELCO_UserPasswordResetTokens t WHERE t.UserId = u.Id " +
        "AND t.ConsumedAtUtc IS NULL AND t.ExpiresAtUtc > GETUTCDATE()) THEN 1 ELSE 0 END AS CanResendInvitation, ";

    /// <summary>Gets global administrators and identities linked to the project.</summary>
    public List<TelcoRbacUser> GetUsers()
    {
        var users = database.GetRecords<TelcoRbacUser>(
            "SELECT u.Id, u.Username, u.DisplayName, u.Email, u.IsActive, u.IsSuperAdmin, u.LastLoginAtUtc, " + InvitationEligibilitySql +
            "       CASE WHEN EXISTS " +
            "       (SELECT 1 FROM tbl_TELCO_UserProjects up " +
            "        JOIN tbl_TELCO_Projects project ON project.Id = up.ProjectId " +
            "        WHERE up.UserId = u.Id AND up.IsActive = 1 AND project.IsActive = 1 " +
            "          AND project.ProjectKey = @ProjectKey) THEN 1 ELSE 0 END AS HasProjectAccess " +
            "  FROM tbl_TELCO_Users u " +
            " WHERE u.IsSuperAdmin = 1 OR EXISTS " +
            "       (SELECT 1 FROM tbl_TELCO_UserProjects up " +
            "        JOIN tbl_TELCO_Projects project ON project.Id = up.ProjectId " +
            "        WHERE up.UserId = u.Id AND up.IsActive = 1 AND project.IsActive = 1 AND project.ProjectKey = @ProjectKey) " +
            " ORDER BY u.Username",
            projectKey).ToList();
        ThrowIfFailed("read TELCO users");

        var assignments = database.GetRecords<TelcoRbacRoleAssignment>(
            "SELECT ur.UserId, r.Id AS RoleId, r.Name " +
            "  FROM tbl_TELCO_UserRoles ur " +
            "  JOIN tbl_TELCO_Roles r ON r.Id = ur.RoleId " +
            "  JOIN tbl_TELCO_Projects project ON project.Id = r.ProjectId " +
            " WHERE project.ProjectKey = @ProjectKey ORDER BY r.Priority DESC, r.Name",
            projectKey);
        ThrowIfFailed("read TELCO user role assignments");
        foreach (var user in users)
        {
            user.CanResendInvitation = user.CanResendInvitation && !user.IsSuperAdmin;
            var userAssignments = assignments.Where(item => item.UserId == user.Id).ToList();
            user.RoleIds = userAssignments.Select(item => item.RoleId).ToList();
            user.Roles = userAssignments.Select(item => item.Name).ToList();
        }
        return users;
    }

    /// <summary>Gets every global TELCO identity without applying a project boundary.</summary>
    public List<TelcoRbacUser> GetGlobalUsers()
    {
        var users = database.GetRecords<TelcoRbacUser>(
            "SELECT u.Id, u.Username, u.DisplayName, u.Email, u.IsActive, u.IsSuperAdmin, u.LastLoginAtUtc, " + InvitationEligibilitySql +
            "CASE WHEN u.IsSuperAdmin = 0 AND NOT EXISTS (SELECT 1 FROM tbl_TELCO_UserProjects up " +
            "WHERE up.UserId = u.Id AND up.IsActive = 1) THEN 1 ELSE 0 END AS CanDelete " +
            "FROM tbl_TELCO_Users u ORDER BY u.Username").ToList();
        ThrowIfFailed("read global TELCO identities");
        return users;
    }

    /// <summary>Gets roles belonging to the project.</summary>
    public List<TelcoRbacRole> GetRoles()
    {
        var roles = database.GetRecords<TelcoRbacRole>(
            "SELECT r.Id, r.Name, r.DisplayName, r.Description, r.LandingController, r.Priority, " +
            "       r.IsSystem, r.IsActive, " +
            "       (SELECT COUNT(*) FROM tbl_TELCO_UserRoles ur WHERE ur.RoleId = r.Id) AS UserCount " +
            "  FROM tbl_TELCO_Roles r " +
            "  JOIN tbl_TELCO_Projects project ON project.Id = r.ProjectId " +
            " WHERE project.ProjectKey = @ProjectKey " +
            " ORDER BY r.Priority DESC, r.DisplayName, r.Name",
            projectKey).ToList();
        ThrowIfFailed("read TELCO roles");
        var assignments = database.GetRecords<TelcoRbacPrivilegeAssignment>(
            "SELECT rp.RoleId, p.Name FROM tbl_TELCO_RolePrivileges rp " +
            "JOIN tbl_TELCO_Privileges p ON p.Id = rp.PrivilegeId " +
            "JOIN tbl_TELCO_Roles r ON r.Id = rp.RoleId " +
            "JOIN tbl_TELCO_Projects project ON project.Id = r.ProjectId " +
            "WHERE project.ProjectKey = @ProjectKey AND p.IsActive = 1 ORDER BY p.Name",
            projectKey);
        ThrowIfFailed("read TELCO role privileges");
        foreach (var role in roles)
            role.Privileges = assignments.Where(item => item.RoleId == role.Id).Select(item => item.Name).ToList();
        return roles;
    }

    /// <summary>Gets privileges exposed to the project.</summary>
    public List<TelcoRbacPrivilege> GetPrivileges()
    {
        var privileges = database.GetRecords<TelcoRbacPrivilege>(
            "SELECT p.Id, p.Name, p.DisplayName, p.Description, p.Category FROM tbl_TELCO_Privileges p " +
            "JOIN tbl_TELCO_ProjectPrivileges pp ON pp.PrivilegeId = p.Id " +
            "JOIN tbl_TELCO_Projects project ON project.Id = pp.ProjectId " +
            "WHERE p.IsActive = 1 AND project.ProjectKey = @ProjectKey ORDER BY p.Category, p.DisplayName, p.Name",
            projectKey).ToList();
        ThrowIfFailed("read TELCO privileges");
        return privileges;
    }

    /// <summary>Synchronizes the project's privilege catalog from pages discovered by its application.</summary>
    public void SynchronizePagePrivileges(IEnumerable<TelcoRbacPageDefinition>? pages)
    {
        SynchronizePrivileges((pages ?? Enumerable.Empty<TelcoRbacPageDefinition>())
            .Select(page => new TelcoRbacPrivilegeDefinition
            {
                Key = page.Key,
                DisplayName = page.DisplayName,
                Description = page.Description,
                Category = page.Category,
                Order = page.Order
            }));
    }

    /// <summary>Synchronizes the project's complete code-discovered privilege catalog.</summary>
    public void SynchronizePrivileges(IEnumerable<TelcoRbacPrivilegeDefinition>? privileges)
    {
        using var transaction = database.BeginTransaction(autoCommit: false);
        foreach (var privilege in (privileges ?? Enumerable.Empty<TelcoRbacPrivilegeDefinition>())
            .Where(item => item != null)
            .GroupBy(item => Require(item.Key, nameof(item.Key)), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First()))
        {
            var key = Require(privilege.Key, nameof(privilege.Key));
            var displayName = Require(privilege.DisplayName, nameof(privilege.DisplayName));
            var category = Require(privilege.Category, nameof(privilege.Category));
            if (key.Length > 100)
                throw new ArgumentException("A TELCO privilege key cannot exceed 100 characters.", nameof(privileges));

            database.ExecuteQuery(
                "IF EXISTS (SELECT 1 FROM tbl_TELCO_Privileges WHERE Name = @Name) " +
                "UPDATE tbl_TELCO_Privileges SET DisplayName = @DisplayName, Description = @Description, " +
                "Category = @Category, IsActive = 1, UpdatedAtUtc = GETUTCDATE() WHERE Name = @Name " +
                "ELSE INSERT INTO tbl_TELCO_Privileges " +
                "(Name, DisplayName, Description, Category, IsActive, CreatedAtUtc, UpdatedAtUtc) " +
                "VALUES (@Name, @DisplayName, @Description, @Category, 1, GETUTCDATE(), GETUTCDATE())",
                key, displayName, privilege.Description, category);
            ThrowIfFailed("synchronize a TELCO privilege");

            database.ExecuteQuery(
                "IF NOT EXISTS (SELECT 1 FROM tbl_TELCO_ProjectPrivileges pp " +
                "JOIN tbl_TELCO_Projects project ON project.Id = pp.ProjectId " +
                "JOIN tbl_TELCO_Privileges privilege ON privilege.Id = pp.PrivilegeId " +
                "WHERE project.ProjectKey = @ProjectKey AND privilege.Name = @Name) " +
                "INSERT INTO tbl_TELCO_ProjectPrivileges (ProjectId, PrivilegeId, CreatedAtUtc) " +
                "SELECT project.Id, privilege.Id, GETUTCDATE() FROM tbl_TELCO_Projects project " +
                "CROSS JOIN tbl_TELCO_Privileges privilege " +
                "WHERE project.ProjectKey = @ProjectKey AND project.IsActive = 1 AND privilege.Name = @Name",
                projectKey, key);
            ThrowIfFailed("link a TELCO privilege to its project");
            if (database.RecordsAffected > 1)
                throw new InvalidOperationException("The TELCO privilege matched more than one project.");
        }
        transaction.Commit();
    }

    /// <summary>Determines whether an active project member has an active privilege through an active role.</summary>
    public bool HasPrivilege(int userId, string privilegeName)
    {
        if (string.IsNullOrWhiteSpace(privilegeName)) return false;
        var access = GetAccess(userId);
        if (access == null) return false;
        if (access.IsSuperAdmin || access.IsProjectAdmin) return true;
        var count = database.GetValue<int>(
            "SELECT COUNT(*) FROM tbl_TELCO_UserRoles ur " +
            "JOIN tbl_TELCO_Roles r ON r.Id = ur.RoleId " +
            "JOIN tbl_TELCO_Projects project ON project.Id = r.ProjectId " +
            "JOIN tbl_TELCO_UserProjects up ON up.UserId = ur.UserId AND up.ProjectId = project.Id " +
            "JOIN tbl_TELCO_RolePrivileges rp ON rp.RoleId = r.Id " +
            "JOIN tbl_TELCO_Privileges p ON p.Id = rp.PrivilegeId " +
            "WHERE ur.UserId = @UserId AND project.ProjectKey = @ProjectKey AND project.IsActive = 1 " +
            "AND up.IsActive = 1 AND r.IsActive = 1 AND p.IsActive = 1 AND p.Name = @PrivilegeName",
            userId, projectKey, Require(privilegeName, nameof(privilegeName)));
        ThrowIfFailed("check TELCO page access");
        return count > 0;
    }

    /// <summary>Creates a global identity or links an exact existing identity to the project.</summary>
    public int CreateOrGrantUser(string username, string displayName, string email, string? password, bool isActive)
        => CreateOrGrantUser(username, displayName, email, password, isActive, false);

    /// <summary>Creates or links an identity, optionally leaving new identities awaiting an invitation.</summary>
    public int CreateOrGrantUser(string username, string displayName, string email, string? password, bool isActive, bool allowInvitation)
    {
        username = Require(username, nameof(username));
        displayName = Require(displayName, nameof(displayName));
        email = Require(email, nameof(email));
        var matches = database.GetRecords<TelcoRbacUser>(
            "SELECT Id, Username, DisplayName, Email, IsActive, IsSuperAdmin " +
            "FROM tbl_TELCO_Users WHERE UPPER(Username) = UPPER(@Username) OR UPPER(Email) = UPPER(@Email)",
            username, email).ToList();
        ThrowIfFailed("check global TELCO identities");

        if (matches.Count > 1 || matches.Any(item =>
            !string.Equals(item.Username, username, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(item.Email, email, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("The username or email belongs to another global TELCO identity.");

        using var transaction = database.BeginTransaction(autoCommit: false);
        int userId;
        if (matches.Count == 1)
        {
            userId = matches[0].Id;
        }
        else
        {
            var hasPassword = !string.IsNullOrWhiteSpace(password);
            if (!hasPassword && !allowInvitation)
                throw new InvalidOperationException("A password is required when creating a new global TELCO identity.");
            var hash = passwordHashService.CreateHash(hasPassword ? password! : Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
            userId = database.GetValue<int>(
                "INSERT INTO tbl_TELCO_Users " +
                "(Username, DisplayName, Email, PasswordSalt, PasswordHash, PasswordIterations, IsActive, IsSuperAdmin, CreatedAtUtc, UpdatedAtUtc, LastPasswordChangedAtUtc) " +
                "VALUES (@Username, @DisplayName, @Email, @PasswordSalt, @PasswordHash, @PasswordIterations, @IsActive, 0, GETUTCDATE(), GETUTCDATE(), " +
                (hasPassword ? "GETUTCDATE()" : "NULL") + ") " +
                "SELECT CONVERT(INT, @@IDENTITY)",
                username, displayName, email, hash.Salt, hash.Hash, hash.Iterations, isActive);
            ThrowIfFailed("create the global TELCO identity");
            if (userId <= 0)
                throw new InvalidOperationException("The global TELCO identity was not created.");
        }

        database.ExecuteQuery(
            "IF EXISTS (SELECT 1 FROM tbl_TELCO_UserProjects up JOIN tbl_TELCO_Projects p ON p.Id = up.ProjectId " +
            "           WHERE up.UserId = @UserId AND p.ProjectKey = @ProjectKey) " +
            "UPDATE up SET IsActive = 1, UpdatedAtUtc = GETUTCDATE() FROM tbl_TELCO_UserProjects up " +
            "JOIN tbl_TELCO_Projects p ON p.Id = up.ProjectId WHERE up.UserId = @UserId AND p.ProjectKey = @ProjectKey " +
            "ELSE INSERT INTO tbl_TELCO_UserProjects (UserId, ProjectId, IsActive, CreatedAtUtc, UpdatedAtUtc) " +
            "SELECT @UserId, Id, 1, GETUTCDATE(), GETUTCDATE() FROM tbl_TELCO_Projects " +
            "WHERE ProjectKey = @ProjectKey AND IsActive = 1",
            userId, projectKey);
        ThrowIfFailed("grant TELCO project access");
        if (database.RecordsAffected != 1)
            throw new InvalidOperationException("The configured TELCO project was not found or is inactive.");
        transaction.Commit();
        return userId;
    }

    /// <summary>Creates a global TELCO identity without granting access to any project.</summary>
    public int CreateGlobalUser(string username, string displayName, string email, string? password, bool isActive, bool isSuperAdmin)
    {
        username = Require(username, nameof(username));
        displayName = Require(displayName, nameof(displayName));
        email = Require(email, nameof(email));
        EnsureUniqueGlobalIdentityFields(0, username, email);

        var hasPassword = !string.IsNullOrWhiteSpace(password);
        var hash = passwordHashService.CreateHash(hasPassword ? password! : Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
        var userId = database.GetValue<int>(
            "INSERT INTO tbl_TELCO_Users " +
            "(Username, DisplayName, Email, PasswordSalt, PasswordHash, PasswordIterations, IsActive, IsSuperAdmin, CreatedAtUtc, UpdatedAtUtc, LastPasswordChangedAtUtc) " +
            "VALUES (@Username, @DisplayName, @Email, @PasswordSalt, @PasswordHash, @PasswordIterations, @IsActive, @IsSuperAdmin, GETUTCDATE(), GETUTCDATE(), " +
            (hasPassword ? "GETUTCDATE()" : "NULL") + ") " +
            "SELECT CONVERT(INT, @@IDENTITY)",
            username, displayName, email, hash.Salt, hash.Hash, hash.Iterations, isActive, isSuperAdmin);
        ThrowIfFailed("create the global TELCO identity");
        if (userId <= 0)
            throw new InvalidOperationException("The global TELCO identity was not created.");
        return userId;
    }

    /// <summary>Sets project access from the global editor, creating a missing membership when access is granted.</summary>
    public void SetGlobalProjectAccess(int userId, bool isActive)
    {
        using var transaction = database.BeginTransaction(autoCommit: false, System.Transactions.IsolationLevel.ReadCommitted);
        database.ExecuteQuery("UPDATE tbl_TELCO_Users SET UpdatedAtUtc=GETUTCDATE() WHERE Id=@UserId", userId);
        RequireOne("recheck the project membership identity");
        if (!isActive && IsSuperAdministrator(userId))
            throw new InvalidOperationException("A super administrator's project access must be changed in Global RBAC.");
        if (isActive)
        {
            database.ExecuteQuery(
                "IF EXISTS (SELECT 1 FROM tbl_TELCO_UserProjects up JOIN tbl_TELCO_Projects project ON project.Id = up.ProjectId " +
                "WHERE up.UserId = @UserId AND project.ProjectKey = @ProjectKey) " +
                "UPDATE up SET IsActive = 1, UpdatedAtUtc = GETUTCDATE() FROM tbl_TELCO_UserProjects up " +
                "JOIN tbl_TELCO_Projects project ON project.Id = up.ProjectId " +
                "WHERE up.UserId = @UserId AND project.ProjectKey = @ProjectKey " +
                "ELSE INSERT INTO tbl_TELCO_UserProjects (UserId, ProjectId, IsActive, CreatedAtUtc, UpdatedAtUtc) " +
                "SELECT @UserId, Id, 1, GETUTCDATE(), GETUTCDATE() FROM tbl_TELCO_Projects " +
                "WHERE ProjectKey = @ProjectKey AND IsActive = 1",
                userId, projectKey);
            RequireOne("grant TELCO project access");
            transaction.Commit();
            return;
        }

        database.ExecuteQuery(
            "UPDATE up SET IsActive = 0, UpdatedAtUtc = GETUTCDATE() FROM tbl_TELCO_UserProjects up " +
            "JOIN tbl_TELCO_Projects project ON project.Id = up.ProjectId " +
            "WHERE up.UserId = @UserId AND project.ProjectKey = @ProjectKey",
            userId, projectKey);
        ThrowIfFailed("revoke TELCO project access");
        database.ExecuteQuery(
            "DELETE ur FROM tbl_TELCO_UserRoles ur JOIN tbl_TELCO_Roles r ON r.Id = ur.RoleId " +
            "JOIN tbl_TELCO_Projects project ON project.Id = r.ProjectId " +
            "WHERE ur.UserId = @UserId AND project.ProjectKey = @ProjectKey",
            userId, projectKey);
        ThrowIfFailed("remove TELCO project roles");
        transaction.Commit();
    }

    /// <summary>Updates global identity fields.</summary>
    public void UpdateUser(int userId, string username, string displayName, string email, bool isActive)
    {
        username = Require(username, nameof(username));
        displayName = Require(displayName, nameof(displayName));
        email = Require(email, nameof(email));
        EnsureUniqueGlobalIdentityFields(userId, username, email);
        database.ExecuteQuery(
            "UPDATE tbl_TELCO_Users SET Username = @Username, DisplayName = @DisplayName, Email = @Email, " +
            "IsActive = @IsActive, UpdatedAtUtc = GETUTCDATE() WHERE Id = @UserId",
            username, displayName, email, isActive, userId);
        RequireOne("update the global TELCO identity");
    }

    /// <summary>Replaces global identity password material.</summary>
    public void UpdatePassword(int userId, string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new ArgumentException("A password of at least eight characters is required.");
        using var transaction = database.BeginTransaction(autoCommit: false);
        var hash = passwordHashService.CreateHash(password);
        database.ExecuteQuery(
            "UPDATE tbl_TELCO_Users SET PasswordSalt = @Salt, PasswordHash = @Hash, PasswordIterations = @Iterations, " +
            "LastPasswordChangedAtUtc = GETUTCDATE(), UpdatedAtUtc = GETUTCDATE() WHERE Id = @UserId",
            hash.Salt, hash.Hash, hash.Iterations, userId);
        RequireOne("update the TELCO password");
        database.ExecuteQuery("UPDATE tbl_TELCO_UserPasswordResetTokens SET ConsumedAtUtc = GETUTCDATE() WHERE UserId = @UserId AND ConsumedAtUtc IS NULL", userId);
        ThrowIfFailed("invalidate previous TELCO password setup links");
        transaction.Commit();
    }

    /// <summary>Changes a managed member's shared password with the current actor's project authority.</summary>
    public void UpdateManagedPassword(int actorId, int userId, string password)
    {
        using var transaction = database.BeginTransaction(autoCommit: false);
        var access = RequireClientAdministration(actorId);
        var target = GetDelegableUsers(actorId).FirstOrDefault(user => user.Id == userId);
        if (target == null || (target.IsSuperAdmin && !access.IsSuperAdmin))
            throw new UnauthorizedAccessException("You cannot change this identity's password.");
        UpdatePassword(userId, password);
        transaction.Commit();
    }

    /// <summary>Deletes an unassigned identity, checking every membership inside the deletion transaction.</summary>
    public void DeleteGlobalUser(int actorId, int userId)
    {
        using var transaction = database.BeginTransaction(autoCommit: false, System.Transactions.IsolationLevel.ReadCommitted);
        var actor = database.GetValue<int>("SELECT COUNT(*) FROM tbl_TELCO_Users WHERE Id = @ActorId AND IsActive = 1 AND IsSuperAdmin = 1", actorId);
        ThrowIfFailed("verify global deletion authority");
        if (actor != 1) throw new UnauthorizedAccessException("Only a Super Admin can delete global identities.");
        if (actorId == userId) throw new InvalidOperationException("You cannot delete your own account.");
        var eligible = database.GetValue<int>("SELECT COUNT(*) FROM tbl_TELCO_Users u WHERE u.Id = @UserId AND u.IsSuperAdmin = 0 " +
            "AND NOT EXISTS (SELECT 1 FROM tbl_TELCO_UserProjects up WHERE up.UserId = u.Id AND up.IsActive = 1)", userId);
        ThrowIfFailed("check all project memberships before deletion");
        if (eligible != 1) throw new InvalidOperationException("Only an existing identity without project access or Super Admin access can be deleted.");
        database.ExecuteQuery("UPDATE u SET UpdatedAtUtc=GETUTCDATE() FROM tbl_TELCO_Users u WHERE u.Id=@UserId AND u.IsSuperAdmin=0 " +
            "AND NOT EXISTS (SELECT 1 FROM tbl_TELCO_UserProjects up WHERE up.UserId=u.Id AND up.IsActive=1) " +
            "AND EXISTS (SELECT 1 FROM tbl_TELCO_Users actor WHERE actor.Id=@ActorId AND actor.IsActive=1 AND actor.IsSuperAdmin=1)", userId, actorId);
        RequireOne("recheck identity deletion eligibility");
        foreach (var table in new[] { "tbl_TELCO_ClientAdminPrivileges", "tbl_TELCO_ClientAdmins", "tbl_TELCO_UserPasswordResetTokens", "tbl_TELCO_UserRoles", "tbl_TELCO_UserProjects" })
        {
            database.ExecuteQuery("IF OBJECT_ID('" + table + "') IS NOT NULL DELETE FROM " + table + " WHERE UserId = @UserId", userId);
            ThrowIfFailed("remove identity memberships and credentials");
        }
        database.ExecuteQuery("DELETE FROM tbl_TELCO_Users WHERE Id = @UserId", userId);
        RequireOne("delete the unassigned TELCO identity");
        transaction.Commit();
    }

    /// <summary>Creates a setup token only for a pending, active member of this project; established accounts are never reset.</summary>
    public bool CreateProjectInvitationToken(int userId, string tokenHash, DateTime expiresAtUtc, string? requestIp, bool requireExpiredInvitation = false)
    {
        using var transaction = database.BeginTransaction(autoCommit: false, System.Transactions.IsolationLevel.ReadCommitted);
        database.ExecuteQuery("UPDATE u SET UpdatedAtUtc=GETUTCDATE() FROM tbl_TELCO_Users u WHERE u.Id=@UserId AND u.IsActive=1 AND u.IsSuperAdmin=0 " +
            "AND u.LastPasswordChangedAtUtc IS NULL AND u.LastLoginAtUtc IS NULL AND EXISTS (SELECT 1 FROM tbl_TELCO_UserProjects up " +
            "JOIN tbl_TELCO_Projects p ON p.Id=up.ProjectId WHERE up.UserId=u.Id AND up.IsActive=1 AND p.IsActive=1 AND p.ProjectKey=@ProjectKey)", userId, projectKey);
        ThrowIfFailed("claim pending project invitation");
        if (database.RecordsAffected != 1) return false;
        var eligible = database.GetValue<int>(
            "SELECT COUNT(*) FROM tbl_TELCO_Users u " +
            "WHERE u.Id = @UserId AND u.IsActive = 1 AND u.IsSuperAdmin = 0 " +
            "AND u.LastPasswordChangedAtUtc IS NULL AND u.LastLoginAtUtc IS NULL " +
            "AND EXISTS (SELECT 1 FROM tbl_TELCO_UserProjects up " +
            "JOIN tbl_TELCO_Projects p ON p.Id = up.ProjectId " +
            "WHERE up.UserId = u.Id AND up.IsActive = 1 AND p.IsActive = 1 AND p.ProjectKey = @ProjectKey)",
            userId, projectKey);
        ThrowIfFailed("check project invitation eligibility");
        if (eligible != 1) return false;
        if (requireExpiredInvitation)
        {
            var target = database.GetRecord<TelcoRbacUser>(
                "SELECT u.Id, " + InvitationEligibilitySql + "u.Username FROM tbl_TELCO_Users u WHERE u.Id = @UserId", userId);
            ThrowIfFailed("check expired project invitation");
            if (target?.CanResendInvitation != true) return false;
        }
        ReplaceInvitationToken(userId, Require(tokenHash, nameof(tokenHash)), expiresAtUtc, requestIp);
        transaction.Commit();
        return true;
    }

    /// <summary>Replaces active global invitation tokens with one new token.</summary>
    public void CreateGlobalInvitationToken(int userId, string tokenHash, DateTime expiresAtUtc, string? requestIp, bool requireExpiredInvitation = false)
    {
        RequireGlobalScope();
        using var transaction = database.BeginTransaction(autoCommit: false, System.Transactions.IsolationLevel.ReadCommitted);
        database.ExecuteQuery("UPDATE tbl_TELCO_Users SET UpdatedAtUtc=GETUTCDATE() WHERE Id=@UserId AND IsActive=1", userId);
        RequireOne("claim the global invitation request");
        if (requireExpiredInvitation)
        {
            var target = database.GetRecord<TelcoRbacUser>(
                "SELECT u.Id, " + InvitationEligibilitySql + "u.Username FROM tbl_TELCO_Users u WHERE u.Id = @UserId", userId);
            ThrowIfFailed("check expired global invitation");
            if (target?.CanResendInvitation != true)
                throw new InvalidOperationException("Only a never-used identity with an expired invitation can be reinvited.");
        }
        ReplaceInvitationToken(userId, tokenHash, expiresAtUtc, requestIp);
        transaction.Commit();
    }

    /// <summary>Replaces setup tokens inside the caller's transaction after its scope checks.</summary>
    private void ReplaceInvitationToken(int userId, string tokenHash, DateTime expiresAtUtc, string? requestIp, bool isInvitation = true)
    {
        database.ExecuteQuery(
            "DELETE FROM tbl_TELCO_UserPasswordResetTokens WHERE UserId = @UserId AND ConsumedAtUtc IS NULL",
            userId);
        ThrowIfFailed("replace global invitation tokens");
        database.ExecuteQuery(
            "INSERT INTO tbl_TELCO_UserPasswordResetTokens " +
            "(UserId, TokenHash, RequestIp, RequestedAtUtc, ExpiresAtUtc, IsInvitation) " +
            "SELECT Id, @TokenHash, @RequestIp, GETUTCDATE(), @ExpiresAtUtc, @IsInvitation " +
            "FROM tbl_TELCO_Users WHERE Id = @UserId AND IsActive = 1",
            tokenHash, requestIp, expiresAtUtc, isInvitation, userId);
        RequireOne("create the global invitation token");
        database.ExecuteQuery(
            "UPDATE tbl_TELCO_Users SET PasswordResetRequestedAtUtc = GETUTCDATE(), UpdatedAtUtc = GETUTCDATE() WHERE Id = @UserId",
            userId);
        RequireOne("record the global invitation request");
    }

    /// <summary>Gets an unexpired global invitation token by its SHA-256 hash.</summary>
    public TelcoRbacInvitationToken? GetGlobalInvitationToken(string tokenHash)
    {
        RequireGlobalScope();
        var token = database.GetRecord<TelcoRbacInvitationToken>(
            "SELECT TOP 1 Id, UserId, ExpiresAtUtc, ConsumedAtUtc, IsInvitation " +
            "FROM tbl_TELCO_UserPasswordResetTokens " +
            "WHERE TokenHash = @TokenHash AND IsInvitation = 1 AND ConsumedAtUtc IS NULL AND ExpiresAtUtc > GETUTCDATE()",
            Require(tokenHash, nameof(tokenHash)));
        ThrowIfFailed("read the global invitation token");
        return token;
    }

    /// <summary>Sets the invited identity password and invalidates all of its outstanding tokens.</summary>
    public void AcceptGlobalInvitation(int tokenId, int userId, string password)
    {
        RequireGlobalScope();
        using var transaction = database.BeginTransaction(autoCommit: false, System.Transactions.IsolationLevel.ReadCommitted);
        AcceptInvitation(tokenId, userId, password);
        transaction.Commit();
    }

    /// <summary>Reads a valid setup token only for an active, pending member of this project.</summary>
    public TelcoRbacInvitationToken? GetProjectInvitationToken(string tokenHash)
        => GetProjectSetupToken(tokenHash, true);

    /// <summary>Reads a valid project invitation or self-service reset token.</summary>
    public TelcoRbacInvitationToken? GetProjectSetupToken(string tokenHash, bool invitationsOnly = false)
    {
        var token = database.GetRecord<TelcoRbacInvitationToken>(
            "SELECT TOP 1 t.Id, t.UserId, t.ExpiresAtUtc, t.ConsumedAtUtc, t.IsInvitation " +
            "FROM tbl_TELCO_Users u JOIN tbl_TELCO_UserPasswordResetTokens t ON t.UserId = u.Id " +
            "WHERE t.TokenHash = @TokenHash AND t.ConsumedAtUtc IS NULL AND t.ExpiresAtUtc > GETUTCDATE() " +
            (invitationsOnly ? "AND t.IsInvitation = 1 " : "") +
            "AND u.IsActive = 1 AND (t.IsInvitation = 0 OR (u.IsSuperAdmin = 0 AND u.LastPasswordChangedAtUtc IS NULL AND u.LastLoginAtUtc IS NULL)) " +
            "AND " + PasswordSetupAccessSql,
            Require(tokenHash, nameof(tokenHash)), projectKey);
        ThrowIfFailed("read the project invitation token");
        return token?.Id > 0 ? token : null;
    }

    /// <summary>Accepts a project invitation once, preserving the project boundary and invalidating outstanding setup tokens.</summary>
    public void AcceptProjectInvitation(string tokenHash, string password)
        => AcceptProjectSetup(tokenHash, password, true);

    /// <summary>Accepts a valid invitation or password reset, consuming all outstanding tokens atomically.</summary>
    public void AcceptProjectPasswordSetup(string tokenHash, string password)
        => AcceptProjectSetup(tokenHash, password, false);

    /// <summary>Checks the project and token type under the same transaction that updates the password.</summary>
    private void AcceptProjectSetup(string tokenHash, string password, bool invitationsOnly)
    {
        using var transaction = database.BeginTransaction(autoCommit: false, System.Transactions.IsolationLevel.ReadCommitted);
        var token = GetProjectSetupToken(tokenHash, invitationsOnly)
            ?? throw new InvalidOperationException("This password setup link is invalid or has expired.");
        AcceptInvitation(token.Id, token.UserId, password, !invitationsOnly);
        transaction.Commit();
    }

    /// <summary>Updates password material inside the caller's transaction after validating its invitation scope.</summary>
    private void AcceptInvitation(int tokenId, int userId, string password, bool allowPasswordReset = false)
    {
        var hash = passwordHashService.CreateHash(Require(password, nameof(password)));
        // Token creation and password changes write the identity first. Preserve that
        // order so accepting a link cannot deadlock with replacing its token.
        database.ExecuteQuery("UPDATE tbl_TELCO_Users SET UpdatedAtUtc=GETUTCDATE() WHERE Id=@UserId AND IsActive=1", userId);
        RequireOne("claim the password setup identity");
        database.ExecuteQuery("UPDATE tbl_TELCO_UserPasswordResetTokens SET ConsumedAtUtc=GETUTCDATE() WHERE Id=@TokenId AND UserId=@UserId " +
            (allowPasswordReset ? "" : "AND IsInvitation=1 ") + "AND ConsumedAtUtc IS NULL AND ExpiresAtUtc>GETUTCDATE()", tokenId, userId);
        RequireOne("consume the password setup token once");
        var projectScoped = !string.Equals(projectKey, "GlobalRbac", StringComparison.OrdinalIgnoreCase);
        var values = new List<object?> { hash.Salt, hash.Hash, hash.Iterations, userId, tokenId };
        if (projectScoped) values.Add(projectKey);
        database.ExecuteQuery(
            "UPDATE u SET PasswordSalt = @Salt, PasswordHash = @Hash, PasswordIterations = @Iterations, " +
            "LastPasswordChangedAtUtc = GETUTCDATE(), PasswordResetRequestedAtUtc = NULL, IsActive = 1, UpdatedAtUtc = GETUTCDATE() " +
            "FROM tbl_TELCO_Users u WHERE u.Id = @UserId AND u.IsActive=1 AND EXISTS (SELECT 1 FROM tbl_TELCO_UserPasswordResetTokens " +
            "WHERE Id = @TokenId AND UserId = @UserId " + (allowPasswordReset ? "" : "AND IsInvitation = 1 ") +
            "AND ConsumedAtUtc IS NOT NULL AND ExpiresAtUtc > GETUTCDATE() " +
            (projectScoped ? "AND (IsInvitation=0 OR (u.IsSuperAdmin=0 AND u.LastPasswordChangedAtUtc IS NULL AND u.LastLoginAtUtc IS NULL)) " : "") +
            ") " + (projectScoped ? "AND " + PasswordSetupAccessSql : ""), values.ToArray());
        RequireOne("accept the global invitation");
        database.ExecuteQuery(
            "UPDATE tbl_TELCO_UserPasswordResetTokens SET ConsumedAtUtc = GETUTCDATE() " +
            "WHERE UserId = @UserId AND ConsumedAtUtc IS NULL",
            userId);
        ThrowIfFailed("invalidate global invitation tokens");
    }

    /// <summary>Issues a rate-limited reset token for an active project member without exposing account existence to callers of the public endpoint.</summary>
    public TelcoRbacUser? CreateProjectPasswordResetToken(string identifier, string tokenHash, DateTime expiresAtUtc, string? requestIp, int cooldownSeconds)
    {
        if (cooldownSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));
        using var transaction = database.BeginTransaction(autoCommit: false, System.Transactions.IsolationLevel.ReadCommitted);
        var user = database.GetRecord<TelcoRbacUser>(
            "SELECT TOP 1 u.Id, u.Username, u.DisplayName, u.Email FROM tbl_TELCO_Users u " +
            "WHERE (UPPER(u.Username) = UPPER(@Identifier) OR UPPER(u.Email) = UPPER(@Identifier)) AND u.IsActive = 1 " +
            "AND (u.PasswordResetRequestedAtUtc IS NULL OR u.PasswordResetRequestedAtUtc <= DATEADD(second, -@CooldownSeconds, GETUTCDATE())) " +
            "AND " + PasswordSetupAccessSql,
            Require(identifier, nameof(identifier)), cooldownSeconds, projectKey);
        ThrowIfFailed("check password reset eligibility");
        if (user == null || user.Id <= 0 || string.IsNullOrWhiteSpace(user.Email)) return null;
        database.ExecuteQuery("UPDATE u SET PasswordResetRequestedAtUtc=GETUTCDATE(),UpdatedAtUtc=GETUTCDATE() FROM tbl_TELCO_Users u " +
            "WHERE u.Id=@UserId AND u.IsActive=1 AND (u.PasswordResetRequestedAtUtc IS NULL OR u.PasswordResetRequestedAtUtc<=DATEADD(second,-@Cooldown,GETUTCDATE())) AND " + PasswordSetupAccessSql,
            user.Id, cooldownSeconds, projectKey);
        ThrowIfFailed("claim password reset cooldown");
        if (database.RecordsAffected != 1) return null;
        ReplaceInvitationToken(user.Id, Require(tokenHash, nameof(tokenHash)), expiresAtUtc, requestIp, false);
        transaction.Commit();
        return user;
    }

    /// <summary>Matches recovery eligibility to Global RBAC login, while preserving project membership checks elsewhere.</summary>
    private string PasswordSetupAccessSql => string.Equals(projectKey, "GlobalRbac", StringComparison.OrdinalIgnoreCase)
        ? "('GlobalRbac' = @ProjectKey AND (u.IsSuperAdmin = 1 OR EXISTS (SELECT 1 FROM tbl_TELCO_UserRoles ur " +
          "JOIN tbl_TELCO_Roles r ON r.Id = ur.RoleId JOIN tbl_TELCO_Projects p ON p.Id = r.ProjectId " +
          "JOIN tbl_TELCO_UserProjects up ON up.UserId = ur.UserId AND up.ProjectId = p.Id " +
          "WHERE ur.UserId = u.Id AND p.IsActive = 1 AND up.IsActive = 1 " +
          "AND r.IsActive = 1 AND (r.Name = 'Admin' OR EXISTS (SELECT 1 FROM tbl_TELCO_RolePrivileges rp JOIN tbl_TELCO_Privileges v ON v.Id=rp.PrivilegeId WHERE rp.RoleId=r.Id AND v.IsActive=1 AND v.Name='Rbac.Delegate')))))"
        : "EXISTS (SELECT 1 FROM tbl_TELCO_Projects p WHERE p.ProjectKey = @ProjectKey AND p.IsActive = 1 " +
          "AND (u.IsSuperAdmin = 1 OR EXISTS (SELECT 1 FROM tbl_TELCO_UserProjects up WHERE up.ProjectId = p.Id AND up.UserId = u.Id AND up.IsActive = 1)))";
    /// <summary>Activates or revokes the identity's project membership.</summary>
    public void SetProjectAccess(int userId, bool isActive)
    {
        using var transaction = database.BeginTransaction(autoCommit: false, System.Transactions.IsolationLevel.ReadCommitted);
        database.ExecuteQuery("UPDATE tbl_TELCO_Users SET UpdatedAtUtc=GETUTCDATE() WHERE Id=@UserId", userId);
        RequireOne("recheck the project membership identity");
        if (!isActive && IsSuperAdministrator(userId))
            throw new InvalidOperationException("A super administrator's project access must be changed in Global RBAC.");
        database.ExecuteQuery(
            "UPDATE up SET IsActive = @IsActive, UpdatedAtUtc = GETUTCDATE() FROM tbl_TELCO_UserProjects up " +
            "JOIN tbl_TELCO_Projects project ON project.Id = up.ProjectId " +
            "WHERE up.UserId = @UserId AND project.ProjectKey = @ProjectKey",
            isActive, userId, projectKey);
        RequireOne("update TELCO project access");
        if (!isActive)
        {
            database.ExecuteQuery(
                "DELETE ur FROM tbl_TELCO_UserRoles ur JOIN tbl_TELCO_Roles r ON r.Id = ur.RoleId " +
                "JOIN tbl_TELCO_Projects project ON project.Id = r.ProjectId " +
                "WHERE ur.UserId = @UserId AND project.ProjectKey = @ProjectKey",
                userId, projectKey);
            ThrowIfFailed("remove TELCO project roles");
        }
        transaction.Commit();
    }

    /// <summary>Grants or revokes global super-administrator access.</summary>
    public void SetSuperAdmin(int userId, bool isSuperAdmin)
    {
        database.ExecuteQuery(
            "UPDATE tbl_TELCO_Users SET IsSuperAdmin = @IsSuperAdmin, UpdatedAtUtc = GETUTCDATE() WHERE Id = @UserId",
            isSuperAdmin, userId);
        RequireOne("update TELCO super-administrator access");
    }

    /// <summary>Replaces the identity's role assignments for this project only.</summary>
    public void ReplaceUserRoles(int userId, IEnumerable<int>? roleIds)
    {
        var selectedRoleIds = (roleIds ?? Enumerable.Empty<int>()).Distinct().ToList();
        if (IsSuperAdministrator(userId))
        {
            var adminRoleId = database.GetValue<int>(
                "SELECT TOP 1 r.Id FROM tbl_TELCO_Roles r " +
                "JOIN tbl_TELCO_Projects project ON project.Id = r.ProjectId " +
                "WHERE project.ProjectKey = @ProjectKey AND project.IsActive = 1 AND r.IsActive = 1 AND UPPER(r.Name) = 'ADMIN'",
                projectKey);
            ThrowIfFailed("find the TELCO project Admin role");
            if (adminRoleId <= 0)
                throw new InvalidOperationException($"Project '{projectKey}' does not have an active Admin role required by the super administrator.");
            if (!selectedRoleIds.Contains(adminRoleId)) selectedRoleIds.Add(adminRoleId);
        }
        using var transaction = database.BeginTransaction(autoCommit: false);
        database.ExecuteQuery(
            "DELETE ur FROM tbl_TELCO_UserRoles ur JOIN tbl_TELCO_Roles r ON r.Id = ur.RoleId " +
            "JOIN tbl_TELCO_Projects project ON project.Id = r.ProjectId " +
            "WHERE ur.UserId = @UserId AND project.ProjectKey = @ProjectKey",
            userId, projectKey);
        ThrowIfFailed("replace TELCO user roles");
        foreach (var roleId in selectedRoleIds)
        {
            database.ExecuteQuery(
                "INSERT INTO tbl_TELCO_UserRoles (UserId, RoleId, CreatedAtUtc) " +
                "SELECT @UserId, r.Id, GETUTCDATE() FROM tbl_TELCO_Roles r " +
                "JOIN tbl_TELCO_Projects project ON project.Id = r.ProjectId " +
                "JOIN tbl_TELCO_UserProjects up ON up.UserId = @UserId AND up.ProjectId = project.Id AND up.IsActive = 1 " +
                "WHERE r.Id = @RoleId AND r.IsActive = 1 AND project.ProjectKey = @ProjectKey",
                userId, roleId, projectKey);
            RequireOne("assign a TELCO user role");
        }
        transaction.Commit();
    }

    /// <summary>Determines whether the global identity has super-administrator access.</summary>
    private bool IsSuperAdministrator(int userId)
    {
        var count = database.GetValue<int>(
            "SELECT COUNT(*) FROM tbl_TELCO_Users WHERE Id = @UserId AND IsSuperAdmin = 1",
            userId);
        ThrowIfFailed("check TELCO super-administrator access");
        return count > 0;
    }

    /// <summary>Creates or updates a project role and replaces its project-scoped privileges.</summary>
    public int SaveRole(int roleId, string name, string displayName, string? description, string? landingController, int priority, bool isActive, IEnumerable<string>? privileges)
    {
        name = Require(name, nameof(name));
        var existingClientAdmin = roleId > 0 && GetRoles().Any(role => role.Id == roleId && string.Equals(role.Name, "ClientAdmin", StringComparison.OrdinalIgnoreCase));
        if (existingClientAdmin && (!isActive || !string.Equals(name, "ClientAdmin", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("The common Client Admin role must remain active and retain its name.");
        displayName = Require(displayName, nameof(displayName));
        using var transaction = database.BeginTransaction(autoCommit: false);
        if (roleId <= 0)
        {
            roleId = database.GetValue<int>(
                "INSERT INTO tbl_TELCO_Roles (ProjectId, Name, DisplayName, Description, LandingController, Priority, IsSystem, IsActive, CreatedAtUtc, UpdatedAtUtc) " +
                "SELECT Id, @Name, @DisplayName, @Description, @LandingController, @Priority, 0, @IsActive, GETUTCDATE(), GETUTCDATE() " +
                "FROM tbl_TELCO_Projects WHERE ProjectKey = @ProjectKey AND IsActive = 1 " +
                "SELECT CONVERT(INT, @@IDENTITY)",
                name, displayName, description, landingController, priority, isActive, projectKey);
            ThrowIfFailed("create the TELCO role");
            if (roleId <= 0)
                throw new InvalidOperationException($"The configured TELCO project '{projectKey}' was not found or is inactive.");
        }
        else
        {
            database.ExecuteQuery(
                "UPDATE r SET DisplayName = @DisplayName, Description = @Description, LandingController = @LandingController, " +
                "Priority = @Priority, IsActive = @IsActive, UpdatedAtUtc = GETUTCDATE() FROM tbl_TELCO_Roles r " +
                "JOIN tbl_TELCO_Projects project ON project.Id = r.ProjectId " +
                "WHERE r.Id = @RoleId AND project.ProjectKey = @ProjectKey",
                displayName, description, landingController, priority, isActive, roleId, projectKey);
            RequireOne("update the TELCO role");
        }

        database.ExecuteQuery(
            "DELETE rp FROM tbl_TELCO_RolePrivileges rp JOIN tbl_TELCO_Roles r ON r.Id = rp.RoleId " +
            "JOIN tbl_TELCO_Projects project ON project.Id = r.ProjectId " +
            "WHERE r.Id = @RoleId AND project.ProjectKey = @ProjectKey",
            roleId, projectKey);
        ThrowIfFailed("replace TELCO role privileges");
        foreach (var privilege in (privileges ?? Enumerable.Empty<string>()).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            database.ExecuteQuery(
                "INSERT INTO tbl_TELCO_RolePrivileges (RoleId, PrivilegeId, CreatedAtUtc) " +
                "SELECT r.Id, p.Id, GETUTCDATE() FROM tbl_TELCO_Roles r " +
                "JOIN tbl_TELCO_Projects project ON project.Id = r.ProjectId " +
                "JOIN tbl_TELCO_ProjectPrivileges pp ON pp.ProjectId = project.Id " +
                "JOIN tbl_TELCO_Privileges p ON p.Id = pp.PrivilegeId " +
                "WHERE r.Id = @RoleId AND project.ProjectKey = @ProjectKey AND p.Name = @PrivilegeName AND p.IsActive = 1",
                roleId, projectKey, privilege);
            RequireOne("assign a TELCO role privilege");
        }
        transaction.Commit();
        return roleId;
    }

    private static string Require(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A value is required.", parameterName);
        return value.Trim();
    }

    private void RequireGlobalScope()
    {
        if (!string.Equals(projectKey, "GlobalRbac", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Global invitation operations require the GlobalRbac store scope.");
    }

    private void EnsureUniqueGlobalIdentityFields(int excludedUserId, string username, string email)
    {
        var duplicateUsername = database.GetValue<int>(
            "SELECT COUNT(*) FROM tbl_TELCO_Users WHERE Id <> @ExcludedUserId AND UPPER(Username) = UPPER(@Username)",
            excludedUserId, username);
        ThrowIfFailed("check the TELCO username");
        if (duplicateUsername > 0)
            throw new InvalidOperationException($"Username '{username}' is already assigned to another global TELCO identity.");

        var duplicateEmail = database.GetValue<int>(
            "SELECT COUNT(*) FROM tbl_TELCO_Users WHERE Id <> @ExcludedUserId AND UPPER(Email) = UPPER(@Email)",
            excludedUserId, email);
        ThrowIfFailed("check the TELCO email address");
        if (duplicateEmail > 0)
            throw new InvalidOperationException($"Email address '{email}' is already assigned to another global TELCO identity.");
    }

    private void RequireOne(string operation)
    {
        ThrowIfFailed(operation);
        if (database.RecordsAffected != 1)
            throw new InvalidOperationException($"Unable to {operation}; the target was not found or was outside project '{projectKey}'.");
    }

    private void ThrowIfFailed(string operation)
    {
        if (database.LastException != null)
            throw new InvalidOperationException($"Unable to {operation}.", database.LastException);
    }
}
