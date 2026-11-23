namespace ByteForge.Toolkit.Security.Rbac;

/// <summary>Marks a controller as exposing a page that can be granted through TELCO RBAC.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class TelcoRbacPageAttribute : Attribute
{
    /// <summary>Initializes a discovered RBAC page.</summary>
    public TelcoRbacPageAttribute(string key, string displayName, string category, string route, int order)
    {
        Key = Require(key, nameof(key));
        DisplayName = Require(displayName, nameof(displayName));
        Category = Require(category, nameof(category));
        Route = Require(route, nameof(route));
        Order = order;
    }

    /// <summary>Gets the stable privilege key.</summary>
    public string Key { get; }

    /// <summary>Gets the label displayed by RBAC editors.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the page grouping.</summary>
    public string Category { get; }

    /// <summary>Gets the application-relative route.</summary>
    public string Route { get; }

    /// <summary>Gets the display order.</summary>
    public int Order { get; }

    /// <summary>Gets or sets an optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets an optional role that must own the page.</summary>
    public string? RequiredRole { get; set; }

    private static string Require(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("A value is required.", parameterName)
            : value.Trim();
}

/// <summary>Marks a controller or action as exposing a non-page permission through TELCO RBAC.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class TelcoRbacPrivilegeAttribute : Attribute
{
    /// <summary>Initializes a discovered RBAC permission.</summary>
    public TelcoRbacPrivilegeAttribute(string key, string displayName, string category, int order)
    {
        Key = Require(key, nameof(key));
        DisplayName = Require(displayName, nameof(displayName));
        Category = Require(category, nameof(category));
        Order = order;
    }

    /// <summary>Gets the stable privilege key.</summary>
    public string Key { get; }

    /// <summary>Gets the label displayed by RBAC editors.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the privilege grouping.</summary>
    public string Category { get; }

    /// <summary>Gets the display order.</summary>
    public int Order { get; }

    /// <summary>Gets or sets an optional description.</summary>
    public string? Description { get; set; }

    private static string Require(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("A value is required.", parameterName)
            : value.Trim();
}

/// <summary>Describes a project's public, code-discovered RBAC page catalog.</summary>
public sealed class TelcoRbacPageCatalog
{
    /// <summary>Gets or sets the contract version.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Gets or sets the stable TELCO project key.</summary>
    public string ProjectKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the project label displayed by the editor.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the absolute application-owned portal handoff page URL.</summary>
    public string? PortalSignInUrl { get; set; }

    /// <summary>Gets or sets the discovered pages.</summary>
    public List<TelcoRbacPageDefinition> Pages { get; set; } = new();

    /// <summary>Gets or sets discovered non-page permissions.</summary>
    public List<TelcoRbacPrivilegeDefinition> Privileges { get; set; } = new();
}

/// <summary>Describes one code-discovered page grant.</summary>
public sealed class TelcoRbacPageDefinition
{
    /// <summary>Gets or sets the stable privilege key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the displayed page name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional page description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the page group.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the application-relative route.</summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>Gets or sets the display order.</summary>
    public int Order { get; set; }

    /// <summary>Gets or sets an optional required role.</summary>
    public string? RequiredRole { get; set; }
}

/// <summary>Describes one code-discovered non-page permission.</summary>
public sealed class TelcoRbacPrivilegeDefinition
{
    /// <summary>Gets or sets the stable privilege key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the displayed permission name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional permission description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the permission group.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the display order.</summary>
    public int Order { get; set; }
}
