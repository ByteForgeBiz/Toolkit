namespace ByteForge.Toolkit.Security.Rbac;

/// <summary>Limits delegated role grants to permissions the actor already holds.</summary>
public static class TelcoClientAdminPolicy
{
    /// <summary>Requires every selected permission and landing page to fit the approved ceiling.</summary>
    public static bool Allows(IEnumerable<string> ceiling, IEnumerable<string>? privileges, string? landingPage)
    {
        var allowed = new HashSet<string>(ceiling, StringComparer.OrdinalIgnoreCase);
        return (privileges ?? Enumerable.Empty<string>()).All(allowed.Contains)
            && (string.IsNullOrWhiteSpace(landingPage) || allowed.Contains(landingPage));
    }

    /// <summary>Excludes unrestricted Admin authority while allowing ordinary common roles within the actor's permissions.</summary>
    public static bool AllowsRole(IEnumerable<string> ceiling, TelcoRbacRole role) =>
        !string.Equals(role.Name?.Trim(), "Admin", StringComparison.OrdinalIgnoreCase)
        && Allows(ceiling, role.Privileges, role.LandingController);
}
