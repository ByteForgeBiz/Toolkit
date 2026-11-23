using ByteForge.Toolkit.Configuration;
using ByteForge.Toolkit.Mail;
using ByteForge.Toolkit.Security.Rbac;

using System;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Net;

namespace ByteForge.Toolkit.Security.Rbac
{
    /// <summary>Sends shared TELCO setup invitations for project members who have never set a TELCO password.</summary>
    public sealed class TelcoInvitationService
    {
        private readonly TelcoInvitationSettings settings;
        private readonly TelcoAccountEmailTemplates templates;

        /// <summary>The same public result for known, unknown, inactive, and throttled accounts.</summary>
        public const string PasswordResetRequestedMessage = "If an account matches that username or email, a reset link will be sent shortly.";

        /// <summary>Loads deployment-specific invitation settings.</summary>
        public TelcoInvitationService(TelcoInvitationSettings settings, TelcoAccountEmailTemplates templates = null)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.templates = templates ?? ByteForge.Toolkit.Configuration.Configuration.GetSection<TelcoAccountEmailTemplates>("TELCO Account Emails");
        }

        /// <summary>Checks an invitation without consuming it or exposing its identity.</summary>
        public bool IsValid(TelcoRbacStore store, string token) =>
            !string.IsNullOrWhiteSpace(token) && store.GetProjectSetupToken(Hash(token)) != null;

        /// <summary>Sets the invited user's password through the shared project-scoped store.</summary>
        public void Accept(TelcoRbacStore store, string token, string password)
        {
            if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("An invitation token is required.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                throw new ArgumentException("The password must contain at least 8 characters.");
            store.AcceptProjectPasswordSetup(Hash(token), password);
        }

        /// <summary>Hashes a raw setup token for storage and lookup.</summary>
        private static string Hash(string token)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(token))).Replace("-", string.Empty).ToLowerInvariant();
        }

        /// <summary>Checks mail, destination, and recipient configuration before saving a passwordless user.</summary>
        public void Validate(string email, bool invitation = true)
        {
            settings.Validate();
            try { new MailAddress(email ?? string.Empty); }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
            { throw new ArgumentException("A valid email address is required for invitations."); }
            var mail = MailUtil.Settings;
            if (string.IsNullOrWhiteSpace(mail.Server) || string.IsNullOrWhiteSpace(mail.Sender) ||
                string.IsNullOrWhiteSpace(mail.EncryptedUser) || string.IsNullOrWhiteSpace(mail.EncryptedPassword))
                throw new InvalidOperationException("TELCO invitation mail server, sender, and encrypted credentials must be configured.");
            templates.RenderSubject(invitation, "TELCO");
            templates.RenderBody(invitation, "TELCO", "recipient", "recipient", settings.BuildSetupUrl("validation"), DateTime.UtcNow);
        }

        /// <summary>Sends a setup link only if the identity remains eligible; linking established accounts sends no reset.</summary>
        public bool Send(TelcoRbacStore store, TelcoRbacUser user, string requestIp, bool requireExpiredInvitation = false)
        {
            Validate(user.Email);
            var bytes = new byte[32];
            using (var generator = RandomNumberGenerator.Create()) generator.GetBytes(bytes);
            var token = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var hash = Hash(token);
            var expires = DateTime.UtcNow.AddDays(settings.TokenLifetimeDays);
            var url = settings.BuildSetupUrl(token);
            if (!store.CreateProjectInvitationToken(user.Id, hash, expires, requestIp, requireExpiredInvitation)) return false;
            var subject = templates.RenderSubject(true, store.ProjectKey);
            var body = templates.RenderBody(true, store.ProjectKey, user.DisplayName, user.Username, url, expires);
            try
            {
                if (!MailUtil.SendMail(user.Email, subject, body, true))
                    throw new InvalidOperationException("Mail delivery failed.");
            }
            catch (Exception)
            {
                throw new InvalidOperationException("User access was saved, but the invitation email could not be sent. Retry adding the same username and email with a blank password to resend it.");
            }
            return true;
        }

        /// <summary>Sends a short-lived, account-throttled reset link. Hosts must always return the generic public result.</summary>
        public void RequestPasswordReset(TelcoRbacStore store, string identifier, string requestIp)
        {
            if (string.IsNullOrWhiteSpace(identifier) || identifier.Length > 254) return;
            Validate("validation@example.invalid", false);
            var bytes = new byte[32];
            using (var generator = RandomNumberGenerator.Create()) generator.GetBytes(bytes);
            var token = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var expires = DateTime.UtcNow.AddMinutes(settings.ResetTokenLifetimeMinutes);
            var url = settings.BuildSetupUrl(token);
            var user = store.CreateProjectPasswordResetToken(identifier.Trim(), Hash(token), expires, requestIp, settings.ResetRequestCooldownSeconds);
            if (user == null) return;
            if (!MailUtil.SendMail(user.Email, templates.RenderSubject(false, store.ProjectKey),
                templates.RenderBody(false, store.ProjectKey, user.DisplayName, user.Username, url, expires), true))
                throw new InvalidOperationException("Password reset email delivery failed.");
            // SMTP acceptance is not confirmation of inbox delivery. Keep all recipient and token data out of logs.
            ByteForge.Toolkit.Logging.Log.Info(
                $"TELCO password reset email accepted by SMTP server; project={store.ProjectKey}; userId={user.Id}; recipient=[redacted].");
        }
    }
}
