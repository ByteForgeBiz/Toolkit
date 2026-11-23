using ByteForge.Toolkit.Data;
using System;

namespace ByteForge.Toolkit.Security.Rbac;

/// <summary>Provides project-aware authentication over the shared TELCO identity tables.</summary>
public class TelcoIdentityStore
{
    private readonly DBAccess database;
    private readonly string projectKey;
    private readonly TelcoPasswordHashService passwordHashService;

    /// <summary>Initializes a TELCO identity store from a database configuration section.</summary>
    public TelcoIdentityStore(
        string databaseSection,
        string projectKey,
        TelcoPasswordHashService passwordHashService = null)
        : this(
            new DBAccess(Require(databaseSection, nameof(databaseSection))),
            projectKey,
            passwordHashService)
    {
    }

    /// <summary>Initializes a TELCO identity store with an existing database helper.</summary>
    public TelcoIdentityStore(
        DBAccess database,
        string projectKey,
        TelcoPasswordHashService passwordHashService = null)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.projectKey = Require(projectKey, nameof(projectKey));
        if (this.projectKey.Length > 50)
            throw new ArgumentException("The TELCO project key cannot exceed 50 characters.", nameof(projectKey));
        this.passwordHashService = passwordHashService ?? new TelcoPasswordHashService();
    }

    /// <summary>Gets the project boundary enforced by this store.</summary>
    public string ProjectKey => projectKey;

    /// <summary>Authenticates a global identity with active project membership or super-administrator access.</summary>
    public TelcoIdentity Authenticate(string identifier, string password) => Authenticate(identifier, password, false);

    /// <summary>Verifies credentials for portal selection without granting access to any application.</summary>
    public TelcoIdentity AuthenticateForPortalRouting(string identifier, string password) => Authenticate(identifier, password, true);

    private TelcoIdentity Authenticate(string identifier, string password, bool portalRouting)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
            return null;

        var user = database.GetRecord<TelcoIdentityRecord>(
            "SELECT TOP 1 u.Id, u.Username, u.DisplayName, u.Email, u.PasswordSalt, u.PasswordHash, " +
            "       u.PasswordIterations, u.IsActive, u.IsSuperAdmin, u.LastPasswordChangedAtUtc " +
            "  FROM tbl_TELCO_Users u " +
            " WHERE (u.IsSuperAdmin = 1 " +
            (portalRouting ? "OR EXISTS (SELECT 1 FROM tbl_TELCO_UserProjects m JOIN tbl_TELCO_Projects p ON p.Id=m.ProjectId WHERE m.UserId=u.Id AND m.IsActive=1 AND p.IsActive=1) " : string.Empty) +
            (string.Equals(projectKey, "GlobalRbac", StringComparison.OrdinalIgnoreCase)
                ? "OR EXISTS (SELECT 1 FROM tbl_TELCO_UserRoles a JOIN tbl_TELCO_Roles r ON r.Id = a.RoleId JOIN tbl_TELCO_Projects p ON p.Id = r.ProjectId " +
                  "JOIN tbl_TELCO_UserProjects m ON m.UserId = a.UserId AND m.ProjectId = r.ProjectId " +
                  "WHERE a.UserId = u.Id AND p.IsActive = 1 AND m.IsActive = 1 AND r.IsActive = 1 AND (r.Name = 'Admin' OR EXISTS (SELECT 1 FROM tbl_TELCO_RolePrivileges rp JOIN tbl_TELCO_Privileges v ON v.Id=rp.PrivilegeId WHERE rp.RoleId=r.Id AND v.IsActive=1 AND v.Name='Rbac.Delegate'))) " : string.Empty) +
            "OR EXISTS " +
            "       (SELECT 1 FROM tbl_TELCO_UserProjects up " +
            "        JOIN tbl_TELCO_Projects project ON project.Id = up.ProjectId " +
            "        WHERE up.UserId = u.Id AND up.IsActive = 1 AND project.IsActive = 1 AND project.ProjectKey = @ProjectKey)) " +
            "   AND (UPPER(u.Username) = UPPER(@Identifier) OR UPPER(u.Email) = UPPER(@Identifier))",
            projectKey,
            identifier.Trim());

        if (database.LastException != null)
            throw new InvalidOperationException("The shared TELCO identity store could not be read.", database.LastException);
        if (user == null || !user.IsActive || !user.LastPasswordChangedAtUtc.HasValue)
            return null;
        if (!passwordHashService.VerifyPassword(password, user.PasswordSalt, user.PasswordHash, user.PasswordIterations))
            return null;

        database.ExecuteQuery(
            "UPDATE tbl_TELCO_Users SET LastLoginAtUtc = GETUTCDATE(), UpdatedAtUtc = GETUTCDATE() WHERE Id = @Id",
            user.Id);
        if (database.LastException != null)
            throw new InvalidOperationException("The TELCO identity login timestamp could not be updated.", database.LastException);

        return new TelcoIdentity
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            IsSuperAdmin = user.IsSuperAdmin
        };
    }

    private static string Require(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A value is required.", parameterName);
        return value.Trim();
    }

    private sealed class TelcoIdentityRecord : TelcoIdentity
    {
        public string PasswordSalt { get; set; }
        public string PasswordHash { get; set; }
        public int PasswordIterations { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastPasswordChangedAtUtc { get; set; }
    }
}

/// <summary>Represents one authenticated global TELCO identity.</summary>
public class TelcoIdentity
{
    /// <summary>Gets or sets the global user identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the globally unique username.</summary>
    public string Username { get; set; }

    /// <summary>Gets or sets the user-facing display name.</summary>
    public string DisplayName { get; set; }

    /// <summary>Gets or sets the globally unique email address.</summary>
    public string Email { get; set; }

    /// <summary>Gets or sets whether the identity can access every TELCO project.</summary>
    public bool IsSuperAdmin { get; set; }
}
