using ByteForge.Toolkit.Configuration;
using System;
using System.ComponentModel;

namespace ByteForge.Toolkit.Security.Rbac
{
    /// <summary>Configures shared TELCO invitation delivery and password setup.</summary>
    public sealed class TelcoInvitationSettings
    {
        /// <summary>Gets or sets the runtime URL resolved from the application's current host and local setup route.</summary>
        public string SetupUrl { get; set; }

        /// <summary>Gets or sets the setup token lifetime in days.</summary>
        [ConfigName("iTokenLifetimeDays")]
        [DefaultValue(7)]
        [Description("Positive number of days before a TELCO invitation expires.")]
        public int TokenLifetimeDays { get; set; } = 7;

        /// <summary>Gets or sets the shorter expiration for self-service password resets.</summary>
        [ConfigName("iResetTokenLifetimeMinutes"), DefaultValue(60), Description("Password reset link lifetime in minutes, from 1 through 1440.")]
        public int ResetTokenLifetimeMinutes { get; set; } = 60;

        /// <summary>Gets or sets the account-wide minimum interval between reset emails.</summary>
        [ConfigName("iResetRequestCooldownSeconds"), DefaultValue(60), Description("Minimum seconds between reset emails for the same identity, from 1 through 86400.")]
        public int ResetRequestCooldownSeconds { get; set; } = 60;

        /// <summary>Validates configuration before an identity is created or modified.</summary>
        public void Validate()
        {
            if (!Uri.TryCreate(SetupUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                throw new InvalidOperationException("TELCO password setup requires an absolute HTTPS application URL without credentials, query, or fragment.");
            if (TokenLifetimeDays <= 0 || TokenLifetimeDays > 365)
                throw new InvalidOperationException("TELCO Invitations iTokenLifetimeDays must be between 1 and 365.");
            if (ResetTokenLifetimeMinutes <= 0 || ResetTokenLifetimeMinutes > 1440 || ResetRequestCooldownSeconds <= 0 || ResetRequestCooldownSeconds > 86400)
                throw new InvalidOperationException("TELCO password reset lifetime or cooldown configuration is invalid.");
        }

        /// <summary>Copies lifetime settings and resolves the local setup route on the host serving the application.</summary>
        public TelcoInvitationSettings ForApplication(Uri requestUrl, string setupPath)
        {
            if (requestUrl == null || !requestUrl.IsAbsoluteUri || requestUrl.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(requestUrl.UserInfo))
                throw new InvalidOperationException("Open the application over HTTPS before requesting or sending password setup emails.");
            if (string.IsNullOrWhiteSpace(setupPath) || !setupPath.StartsWith("/", StringComparison.Ordinal) ||
                setupPath.StartsWith("//", StringComparison.Ordinal) || setupPath.IndexOfAny(new[] { '\\', '?', '#' }) >= 0)
                throw new ArgumentException("The password setup route must be a local absolute path without query or fragment.", nameof(setupPath));
            var resolved = new TelcoInvitationSettings
            {
                SetupUrl = new Uri(new Uri(requestUrl.GetLeftPart(UriPartial.Authority)), setupPath).AbsoluteUri,
                TokenLifetimeDays = TokenLifetimeDays,
                ResetTokenLifetimeMinutes = ResetTokenLifetimeMinutes,
                ResetRequestCooldownSeconds = ResetRequestCooldownSeconds
            };
            resolved.Validate();
            return resolved;
        }

        /// <summary>Builds a setup link from the resolved application URL and keeps the token in the fragment.</summary>
        public string BuildSetupUrl(string token)
        {
            Validate();
            return new Uri(SetupUrl).AbsoluteUri + "#token=" + Uri.EscapeDataString(token);
        }
    }
}
