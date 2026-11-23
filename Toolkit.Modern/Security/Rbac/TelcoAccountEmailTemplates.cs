using ByteForge.Toolkit.Configuration;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace ByteForge.Toolkit.Security.Rbac;

/// <summary>Optional per-application account email customization loaded from TELCO Account Emails.</summary>
public sealed class TelcoAccountEmailTemplates
{
    private const string DefaultInvitationSubject = "Your {{ProjectName}} invitation";
    private const string DefaultResetSubject = "Reset your {{ProjectName}} password";
    private const string DefaultInvitationBody = "<p>Hello {{DisplayName}},</p><p>You have been invited to access <strong>{{ProjectName}}</strong>.</p><p><a href=\"{{SetupUrl}}\">Set your TELCO password</a></p><p>This link expires {{ExpiresAtUtc}} and can be used only once. Your password is shared across the TELCO projects you can access.</p><p>If you were not expecting this invitation, you can ignore this email.</p>";
    private const string DefaultResetBody = "<p>Hello {{DisplayName}},</p><p>We received a request to reset your {{ProjectName}} password.</p><p><a href=\"{{SetupUrl}}\">Reset your TELCO password</a></p><p>This link expires {{ExpiresAtUtc}} and can be used only once.</p><p>If you did not request this, ignore this email. Your current password will continue to work.</p>";

    /// <summary>Gets or sets the invitation email subject template.</summary>
    [ConfigName("sInvitationSubject"), DefaultValue(DefaultInvitationSubject), Description("Invitation subject; supports {{ProjectName}}.")]
    public string InvitationSubject { get; set; } = DefaultInvitationSubject;
    /// <summary>Gets or sets the invitation HTML body template.</summary>
    [ConfigName("sInvitationBodyHtml"), DefaultValue(DefaultInvitationBody), Description("Invitation HTML; supports DisplayName, Username, ProjectName, SetupUrl, and ExpiresAtUtc in double braces. SetupUrl is required.")]
    public string InvitationBodyHtml { get; set; } = DefaultInvitationBody;
    /// <summary>Gets or sets the password reset subject template.</summary>
    [ConfigName("sResetSubject"), DefaultValue(DefaultResetSubject), Description("Reset subject; supports {{ProjectName}}.")]
    public string ResetSubject { get; set; } = DefaultResetSubject;
    /// <summary>Gets or sets the password reset HTML body template.</summary>
    [ConfigName("sResetBodyHtml"), DefaultValue(DefaultResetBody), Description("Reset HTML; supports DisplayName, Username, ProjectName, SetupUrl, and ExpiresAtUtc in double braces. SetupUrl is required.")]
    public string ResetBodyHtml { get; set; } = DefaultResetBody;

    /// <summary>Renders a subject while preventing header line breaks and unsupported placeholders.</summary>
    public string RenderSubject(bool invitation, string projectName)
    {
        var template = invitation ? InvitationSubject : ResetSubject;
        if (string.IsNullOrWhiteSpace(template)) throw new InvalidOperationException("The account email subject template cannot be blank.");
        var subject = Render(template, new Dictionary<string, string> { ["ProjectName"] = projectName });
        if (subject.IndexOfAny(new[] { '\r', '\n' }) >= 0) throw new InvalidOperationException("The account email subject cannot contain line breaks.");
        return subject;
    }

    /// <summary>Renders trusted template HTML with every dynamic value HTML-encoded, including the setup URL.</summary>
    public string RenderBody(bool invitation, string projectName, string displayName, string username, string setupUrl, DateTime expiresAtUtc)
    {
        var template = invitation ? InvitationBodyHtml : ResetBodyHtml;
        if (string.IsNullOrWhiteSpace(template) || !template.Contains("{{SetupUrl}}"))
            throw new InvalidOperationException("The account email HTML template must include {{SetupUrl}}.");
        return Render(template, new Dictionary<string, string>
        {
            ["ProjectName"] = WebUtility.HtmlEncode(projectName),
            ["DisplayName"] = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(displayName) ? "there" : displayName),
            ["Username"] = WebUtility.HtmlEncode(username),
            ["SetupUrl"] = WebUtility.HtmlEncode(setupUrl),
            ["ExpiresAtUtc"] = expiresAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)
        });
    }

    /// <summary>Substitutes tokens once so user-supplied token-like text is never interpreted as template markup.</summary>
    private static string Render(string template, IDictionary<string, string> values) =>
        Regex.Replace(template, @"\{\{([^{}]+)\}\}", match => values.TryGetValue(match.Groups[1].Value, out var value)
            ? value : throw new InvalidOperationException("Unsupported account email placeholder: " + match.Value));
}
