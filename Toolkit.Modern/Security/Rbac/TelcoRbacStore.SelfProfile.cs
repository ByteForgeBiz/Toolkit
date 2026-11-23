

namespace ByteForge.Toolkit.Security.Rbac;

public partial class TelcoRbacStore
{
    /// <summary>Reads only the profile belonging to the server-authenticated identity.</summary>
    public TelcoSelfProfile GetSelfProfile(int authenticatedUserId)
    {
        if (GetAccess(authenticatedUserId) == null &&
            !(string.Equals(projectKey, "GlobalRbac", StringComparison.OrdinalIgnoreCase) && GetClientAdminProjects(authenticatedUserId).Count > 0))
            throw new UnauthorizedAccessException("Your TELCO session no longer has access to this application.");
        var profile = database.GetRecord<TelcoSelfProfile>(
            "SELECT Username, DisplayName, Email FROM tbl_TELCO_Users WHERE Id = @UserId AND IsActive = 1", authenticatedUserId);
        ThrowIfFailed("read your TELCO profile");
        if (profile == null || string.IsNullOrWhiteSpace(profile.Username))
            throw new UnauthorizedAccessException("Your TELCO account is no longer active.");
        return profile;
    }

    /// <summary>Changes only the signed-in display name and, after current-password verification, password atomically.</summary>
    public void SaveSelfProfile(int authenticatedUserId, TelcoSelfProfileUpdate request)
    {
        if (request == null) throw new ArgumentException("Your profile is required.");
        var displayName = Require(request.DisplayName, nameof(request.DisplayName));
        if (displayName.Length > 200) throw new ArgumentException("The display name cannot exceed 200 characters.");
        if (!string.IsNullOrEmpty(request.NewPassword) && request.NewPassword.Length < 8)
            throw new ArgumentException("The new password must contain at least 8 characters.");
        using var transaction = database.BeginTransaction(autoCommit: false, System.Transactions.IsolationLevel.ReadCommitted);
        GetSelfProfile(authenticatedUserId);
        var secret = database.GetRecord<SelfPasswordRecord>(
            "SELECT PasswordSalt, PasswordHash, PasswordIterations FROM tbl_TELCO_Users WHERE Id = @UserId AND IsActive = 1",
            authenticatedUserId);
        ThrowIfFailed("verify your TELCO password");
        if (!string.IsNullOrEmpty(request.NewPassword) && (string.IsNullOrEmpty(request.CurrentPassword) || secret == null ||
            !passwordHashService.VerifyPassword(request.CurrentPassword, secret.PasswordSalt, secret.PasswordHash, secret.PasswordIterations)))
            throw new ArgumentException("Your current password is incorrect.");
        database.ExecuteQuery("UPDATE tbl_TELCO_Users SET DisplayName = @DisplayName, UpdatedAtUtc = GETUTCDATE() WHERE Id = @UserId AND IsActive = 1", displayName, authenticatedUserId);
        RequireOne("update your TELCO display name");
        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            // Preserve the exact password typed, including intentional leading/trailing spaces.
            var hash = passwordHashService.CreateHash(request.NewPassword);
            database.ExecuteQuery("UPDATE tbl_TELCO_Users SET PasswordSalt = @Salt, PasswordHash = @Hash, PasswordIterations = @Iterations, LastPasswordChangedAtUtc = GETUTCDATE(), PasswordResetRequestedAtUtc = NULL, UpdatedAtUtc = GETUTCDATE() WHERE Id = @UserId AND IsActive = 1 AND PasswordHash=@PreviousHash AND PasswordSalt=@PreviousSalt AND PasswordIterations=@PreviousIterations",
                hash.Salt, hash.Hash, hash.Iterations, authenticatedUserId, secret.PasswordHash, secret.PasswordSalt, secret.PasswordIterations);
            RequireOne("change your TELCO password");
            database.ExecuteQuery("UPDATE tbl_TELCO_UserPasswordResetTokens SET ConsumedAtUtc = GETUTCDATE() WHERE UserId = @UserId AND ConsumedAtUtc IS NULL", authenticatedUserId);
            ThrowIfFailed("invalidate old TELCO password links");
        }
        transaction.Commit();
    }

    /// <summary>Internal password material that is never returned to the browser.</summary>
    private sealed class SelfPasswordRecord
    {
        /// <summary>Gets or sets the current hash salt.</summary>
        public string PasswordSalt { get; set; } = string.Empty;
        /// <summary>Gets or sets the current password hash.</summary>
        public string PasswordHash { get; set; } = string.Empty;
        /// <summary>Gets or sets the PBKDF2 work factor.</summary>
        public int PasswordIterations { get; set; }
    }
}

/// <summary>Safe self-service profile fields, without permissions or password material.</summary>
public class TelcoSelfProfile
{
    /// <summary>Gets or sets the administrator-managed login name.</summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>Gets or sets the user's display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the administrator-managed email address.</summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>Self-service input; the account ID always comes from the authenticated server context.</summary>
public sealed class TelcoSelfProfileUpdate
{
    /// <summary>Gets or sets the requested display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the current password, required only when changing it.</summary>
    public string CurrentPassword { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional new password without changing its characters.</summary>
    public string NewPassword { get; set; } = string.Empty;
}
