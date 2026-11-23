using System.Reflection;

namespace ByteForge.Toolkit.Security.Rbac;

/// <summary>Exposes the framework-neutral TELCO RBAC UI assets embedded in Toolkit.</summary>
public static class TelcoRbacUiAssets
{
    /// <summary>Explains shared credentials without revealing whether an account exists or its memberships.</summary>
    public const string InvalidCredentialsMessage = "Incorrect username or password. Your TELCO username and password are shared across the TELCO projects you can access.";

    /// <summary>Explains the cross-project effect before a user sets or resets a shared password.</summary>
    public const string SharedCredentialsNotice = "Your TELCO username and password are shared across all TELCO projects you can access. Setting or resetting your password here changes it for all of them.";

    private const string StyleResource = "ByteForge.Toolkit.HTML.CSS.ByteForge-rbac.css";
    private const string ScriptResource = "ByteForge.Toolkit.HTML.JavaScript.ByteForge-rbac.js";
    private const string SortStyleResource = "ByteForge.Toolkit.HTML.CSS.ByteForge-sort-indicator.css";
    private const string SortScriptResource = "ByteForge.Toolkit.HTML.JavaScript.ByteForge-sort-indicator.js";

    /// <summary>Reads the shared RBAC stylesheet.</summary>
    public static string ReadStyleSheet() => Read(SortStyleResource) + Environment.NewLine + Read(StyleResource);

    /// <summary>Reads the shared RBAC JavaScript component.</summary>
    public static string ReadScript() => Read(SortScriptResource) + Environment.NewLine + Read(ScriptResource);

    /// <summary>Reads the shared self-service account modal.</summary>
    public static string ReadAccountScript() => Read("ByteForge.Toolkit.HTML.JavaScript.ByteForge-account.js");
    /// <summary>Reads the theme-aware account header button and modal styles.</summary>
    public static string ReadAccountStyleSheet() => Read("ByteForge.Toolkit.HTML.CSS.ByteForge-account.css");

    /// <summary>Renders the shared password setup page using application-local API and sign-in paths.</summary>
    public static string ReadInvitationPage(string apiPath, string loginPath, bool requestReset = false) =>
        Read("ByteForge.Toolkit.HTML.Rbac.invitation.html")
            .Replace("{{API_URL}}", System.Net.WebUtility.HtmlEncode(apiPath))
            .Replace("{{LOGIN_URL}}", System.Net.WebUtility.HtmlEncode(loginPath))
            .Replace("{{MODE}}", requestReset ? "request" : "accept")
            .Replace("{{CREDENTIAL_NOTICE}}", System.Net.WebUtility.HtmlEncode(SharedCredentialsNotice))
            .Replace("{{SCRIPT}}", Read("ByteForge.Toolkit.HTML.JavaScript.ByteForge-invitation.js"));

    private static string Read(string resourceName)
    {
        var assembly = typeof(TelcoRbacUiAssets).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Toolkit RBAC resource '{resourceName}' was not embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
