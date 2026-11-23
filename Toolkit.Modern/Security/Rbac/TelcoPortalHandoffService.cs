using ByteForge.Toolkit.Data;
using System.Security.Cryptography;
using System.Text;

namespace ByteForge.Toolkit.Security.Rbac;

/// <summary>Issues audience-bound, single-use sign-in codes through the shared TELCO database.</summary>
public sealed class TelcoPortalHandoffService
{
    private readonly DBAccess database;
    /// <summary>Uses the configured shared TELCO database section.</summary>
    public TelcoPortalHandoffService(string databaseSection) : this(new DBAccess(databaseSection)) { }
    /// <summary>Uses an existing Toolkit database helper.</summary>
    public TelcoPortalHandoffService(DBAccess database) => this.database = database ?? throw new ArgumentNullException(nameof(database));

    /// <summary>Creates a short-lived code for an existing active project membership.</summary>
    public string Issue(int userId, string projectKey, string audience, bool keepLoggedIn, int lifetimeSeconds)
    {
        if (lifetimeSeconds < 30 || lifetimeSeconds > 300) throw new ArgumentException("Portal handoff lifetime must be between 30 and 300 seconds.");
        var url = RequireAudience(audience);
        var bytes = new byte[32];
        using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
        var rawToken = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        database.ExecuteQuery(
            "INSERT INTO tbl_TELCO_PortalHandoffs (UserId, ProjectKey, Audience, TokenHash, PasswordChangedAtUtc, KeepLoggedIn, ExpiresAtUtc) " +
            "SELECT u.Id, p.ProjectKey, @Audience, @TokenHash, u.LastPasswordChangedAtUtc, @KeepLoggedIn, DATEADD(second,@LifetimeSeconds,GETUTCDATE()) " +
            "FROM tbl_TELCO_Users u JOIN tbl_TELCO_UserProjects m ON m.UserId=u.Id JOIN tbl_TELCO_Projects p ON p.Id=m.ProjectId " +
            "WHERE u.Id=@UserId AND u.IsActive=1 AND u.LastPasswordChangedAtUtc IS NOT NULL AND m.IsActive=1 AND p.IsActive=1 AND p.ProjectKey=@ProjectKey",
            url, Hash(rawToken), keepLoggedIn, lifetimeSeconds, userId, projectKey);
        Check();
        if (database.RecordsAffected != 1) throw new UnauthorizedAccessException("Your account no longer has access to that portal.");
        return rawToken;
    }

    /// <summary>Consumes a code atomically and rechecks account, password, membership, project, expiry, and audience.</summary>
    public TelcoPortalHandoffIdentity? Consume(string rawToken, string projectKey, string audience)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length != 43 || rawToken.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_')) return null;
        using var transaction = database.BeginConnectionTransaction(System.Data.IsolationLevel.Serializable);
        var identity = database.GetRecord<TelcoPortalHandoffIdentity>(
            "SELECT h.Id AS HandoffId,u.Id,u.Username,u.DisplayName,u.Email,u.IsSuperAdmin,h.KeepLoggedIn " +
            "FROM tbl_TELCO_PortalHandoffs h JOIN tbl_TELCO_Users u ON u.Id=h.UserId " +
            "JOIN tbl_TELCO_UserProjects m ON m.UserId=u.Id JOIN tbl_TELCO_Projects p ON p.Id=m.ProjectId " +
            "WHERE h.TokenHash=@TokenHash AND h.ProjectKey=@ProjectKey AND h.Audience=@Audience " +
            "AND h.ConsumedAtUtc IS NULL AND h.ExpiresAtUtc>GETUTCDATE() AND u.IsActive=1 " +
            "AND u.LastPasswordChangedAtUtc=h.PasswordChangedAtUtc AND m.IsActive=1 AND p.IsActive=1 AND p.ProjectKey=h.ProjectKey",
            Hash(rawToken), projectKey, RequireAudience(audience));
        Check();
        if (identity == null || identity.HandoffId <= 0) return null;
        database.ExecuteQuery("UPDATE tbl_TELCO_PortalHandoffs SET ConsumedAtUtc=GETUTCDATE() WHERE Id=@Id AND ConsumedAtUtc IS NULL AND ExpiresAtUtc>GETUTCDATE()", identity.HandoffId);
        Check();
        if (database.RecordsAffected != 1) return null;
        transaction.Complete();
        return identity;
    }

    private void Check()
    {
        if (database.LastException != null) throw new InvalidOperationException("The TELCO portal sign-in service is unavailable.", database.LastException);
    }
    private static string RequireAudience(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.UserInfo.Length>0 || uri.Query.Length>0 || uri.Fragment.Length>0 ||
            (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
            throw new ArgumentException("Portal sign-in URLs must use HTTPS and contain no credentials, query, or fragment.");
        if (uri.AbsoluteUri.Length>500) throw new ArgumentException("The portal sign-in URL is too long.");
        return uri.AbsoluteUri;
    }
    private static string Hash(string value)
    {
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(b => b.ToString("x2")));
    }
}

/// <summary>Safe identity fields returned after consuming a portal code.</summary>
public sealed class TelcoPortalHandoffIdentity : TelcoIdentity
{
    /// <summary>Gets or sets the consumed database record identifier.</summary>
    public int HandoffId { get; set; }
    /// <summary>Gets or sets the persistence preference verified at sign-in.</summary>
    public bool KeepLoggedIn { get; set; }
}
