using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace ByteForge.Toolkit
{
    /*
     *  __  __      _ _ _   _ _   _ _ 
     * |  \/  |__ _(_) | | | | |_(_) |
     * | |\/| / _` | | | |_| |  _| | |
     * |_|  |_\__,_|_|_|\___/ \__|_|_|
     *                                
     */
    /// <summary>
    /// Provides utilities for sending emails.
    /// </summary>
    public static class MailUtil
    {
        private static MailServerSettings _mailSettings;

        /// <summary>
        /// Gets the mail server settings from the configuration.
        /// </summary>
        public static MailServerSettings Settings => _mailSettings ??= Configuration.GetSection<MailServerSettings>("Mail Server");

        /// <summary>
        /// Sends an email to the default recipient with the specified subject, body, and optional attachments.
        /// </summary>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body of the email.</param>
        /// <param name="isHtml">Indicates whether the body is HTML.</param>
        /// <returns>True if the email was sent successfully, otherwise false.</returns>
        public static bool SendMail(string subject, string body, bool isHtml) => 
            SendMail(Settings.SendTo, subject, body, isHtml);

        /// <summary>
        /// Sends an email to the default recipient with the specified subject, body, HTML flag, and optional attachments.
        /// </summary>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body of the email.</param>
        /// <param name="isHtml">Indicates whether the body is HTML.</param>
        /// <param name="filesToAttach">An array of file paths to attach to the email.</param>
        /// <returns>True if the email was sent successfully, otherwise false.</returns>
        public static bool SendMail(string subject, string body, bool isHtml, params string[] filesToAttach) => 
            SendMail(Settings.SendTo, subject, body, isHtml, filesToAttach);

        /// <summary>
        /// Sends an email to the default recipient with the specified subject, body, HTML flag, attachment name mapping, and optional attachments.
        /// </summary>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body of the email.</param>
        /// <param name="isHtml">Indicates whether the body is HTML.</param>
        /// <param name="fileNameMap">A dictionary mapping original file names to desired attachment names.</param>
        /// <param name="filesToAttach">An array of file paths to attach to the email.</param>
        /// <returns>True if the email was sent successfully, otherwise false.</returns>
        public static bool SendMail(string subject, string body, bool isHtml, Dictionary<string, string> fileNameMap, params string[] filesToAttach) => 
            SendMail(Settings.SendTo, subject, body, isHtml, fileNameMap, filesToAttach);

        /// <summary>
        /// Sends an email to the specified email addresses with the given subject and body.
        /// </summary>
        /// <param name="emailAddresses">The email addresses to send the email to, separated by semicolons.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The body of the email.</param>
        /// <param name="filesToAttach">An array of file paths to attach to the email.</param>
        /// <returns>True if the email was sent successfully, otherwise false.</returns>
        public static bool SendMail(string emailAddresses, string subject, string body, params string[] filesToAttach) => 
            SendMail(emailAddresses, subject, body, true, filesToAttach);

        /// <summary>
        /// Sends an email to the specified email addresses with the given subject and body, and specifies whether the body is HTML.
        /// </summary>
        /// <param name="emailAddresses">A semicolon-separated list of email addresses to send the email to.</param>
        /// <param name="subject">The subject line of the email.</param>
        /// <param name="body">The body of the email. Can be either plain text or HTML based on the isHtml parameter.</param>
        /// <param name="isHtml">A boolean value indicating whether the body of the email is in HTML format. True if the body is HTML, otherwise false.</param>
        /// <param name="filesToAttach">An array of file paths to attach to the email.</param>
        /// <returns>True if the email was sent successfully, otherwise false.</returns>
        public static bool SendMail(string emailAddresses, string subject, string body, bool isHtml, params string[] filesToAttach) =>
            SendMail(emailAddresses, subject, body, isHtml, new Dictionary<string, string>(), filesToAttach);

        /// <summary>
        /// Sends an email to the specified email addresses with the given subject and body, and specifies whether the body is HTML.
        /// </summary>
        /// <param name="emailAddresses">A semicolon-separated list of email addresses to send the email to.</param>
        /// <param name="subject">The subject line of the email.</param>
        /// <param name="body">The body of the email. Can be either plain text or HTML based on the isHtml parameter.</param>
        /// <param name="isHtml">A boolean value indicating whether the body of the email is in HTML format. True if the body is HTML, otherwise false.</param>
        /// <param name="fileNameMap">A dictionary mapping original file names to desired attachment names.</param>
        /// <param name="filesToAttach">An array of file paths to attach to the email.</param>
        /// <returns>True if the email was sent successfully, otherwise false.</returns>
        public static bool SendMail(string emailAddresses, string subject, string body, bool isHtml, Dictionary<string, string> fileNameMap, params string[] filesToAttach)
        {
            var currentSecurityProtocol = ServicePointManager.SecurityProtocol;

            // Validate sender email address
            if (!IsValidEmail(Settings.Sender))
            {
                Log.Error($"The sender email address '{Settings.Sender}' is invalid.");
                return false;
            }
            var sender = new MailAddress(Settings.Sender);

            // Validate email addresses and remove any invalid ones
            emailAddresses = (string.IsNullOrWhiteSpace(emailAddresses) ? Settings.SendTo : emailAddresses).Trim();
            if (emailAddresses.Length == 0)
            {
                Log.Error("No email address specified to send to.");
                return false;
            }

            var addrs = emailAddresses.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var invalidAddrs = addrs.Where(a => !IsValidEmail(a)).ToList();
            if (invalidAddrs.Count > 0)
                Log.Warning($"The following email addresses are invalid and thus removed: {string.Join(", ", invalidAddrs)}");
            addrs = addrs.Except(invalidAddrs).ToArray();

            if (addrs.Length == 0)
            {
                Log.Error("No valid email addresses specified to send to.");
                return false;
            }

            filesToAttach ??= (new string[] { });

            try
            {
                /*
                 * Sends the email message
                 */
                using var email = new MailMessage()
                {
                    From = sender,
                    Sender = sender,
                    Subject = subject,
                    IsBodyHtml = isHtml,
                    SubjectEncoding = System.Text.Encoding.UTF8
                };

                email.ReplyToList.Add(sender);
                foreach (var addr in addrs)
                    email.To.Add(addr);

                if (body.Length != 0)
                {
                    email.BodyEncoding = System.Text.Encoding.UTF8;
                    email.Body = body;
                }

                AttachmentProcessResult result;
                using (var attachmentHandler = new EmailAttachmentHandler())
                {
                    result = attachmentHandler.ProcessAttachments(email, filesToAttach.ToList(), fileNameMap, true);
                }

                // Add notification if needed based on processing method
                if (result.ProcessingMethod == ProcessingMethod.MultiPart)
                {
                    email.Body += "\n\nNote: Due to size constraints, the attachments in this email have been ";
                    email.Body += $"compressed and split into {result.PartDistribution.Count} parts. ";
                    email.Body += "Each part can be opened individually with any ZIP utility.";
                }
                else if (result.ProcessingMethod == ProcessingMethod.Compressed)
                {
                    email.Body += "\n\nNote: Due to size constraints, the attachments in this email have been compressed.";
                }

                // Add warnings about skipped files if any
                if (result.SkippedFiles.Count > 0)
                {
                    email.Body += "\n\nThe following files could not be attached:";
                    foreach (var skipped in result.SkippedFiles)
                        email.Body += $"\n- {skipped.FilePath}: {skipped.Reason}";
                }

                /*
                 * Define the credentials to use the SMTP server
                 * and security protocol
                 */
                ServicePointManager.SecurityProtocol = Settings.SecurityProtocol;
                using var smtp = new SmtpClient(Settings.Server)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(userName: Settings.User, password: Settings.Password),
                    EnableSsl = true,
                    Port = Settings.Port,
                };

                smtp.Send(email);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to send email", ex);
                return false;
            }
            finally
            {
                ServicePointManager.SecurityProtocol = currentSecurityProtocol;
            }
        }

        /// <summary>
        /// Determines whether the specified string is a valid email address.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <returns><see langword="true"/> if the specified string is a valid email address; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method checks the format of the email address and ensures it conforms to standard email address rules.<br/>
        /// It does not verify the existence of the email address or its domain.
        /// </remarks>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    /*
     *  __  __      _ _ ___                      ___      _   _   _              
     * |  \/  |__ _(_) / __| ___ _ ___ _____ _ _/ __| ___| |_| |_(_)_ _  __ _ ___
     * | |\/| / _` | | \__ \/ -_) '_\ V / -_) '_\__ \/ -_)  _|  _| | ' \/ _` (_-<
     * |_|  |_\__,_|_|_|___/\___|_|  \_/\___|_| |___/\___|\__|\__|_|_||_\__, /__/
     *                                                                  |___/    
     */
    /// <summary>
    /// Configuration section for mail server settings.
    /// </summary>
    public class MailServerSettings
    {
        private const int DefaultPort = 587;
        private const SecurityProtocolType DefaultSecurityProtocol = SecurityProtocolType.Tls12;
        /// <summary>
        /// Gets or sets the mail server address.
        /// </summary>
        [ConfigName("sServer")]
        public string Server { get; set; }

        /// <summary>
        /// Gets or sets the port number used for SMTP.
        /// </summary>
        [ConfigName("iPort")]
        [DefaultValue(DefaultPort)]
        public int Port { get; set; } = DefaultPort;

        /// <summary>
        /// Gets or sets the security protocol type used for SMTP connections.
        /// </summary>
        [ConfigName("sSecurityProtocol")]
        [DefaultValue(DefaultSecurityProtocol)]
        public SecurityProtocolType SecurityProtocol { get; set; } = DefaultSecurityProtocol;

        /// <summary>
        /// Gets or sets the encrypted username used for SMTP authentication.
        /// </summary>
        [ConfigName("esUser")]
        public string EncryptedUser { get; set; }

        /// <summary>
        /// Gets or sets the encrypted password used for SMTP authentication.
        /// </summary>
        [ConfigName("esPass")]
        public string EncryptedPassword { get; set; }

        /// <summary>
        /// Gets or sets the email address used as the "From" address in emails.
        /// </summary>
        [ConfigName("sSender")]
        public string Sender { get; set; }

        /// <summary>
        /// Gets or sets the email address used as the default "To" address in emails.
        /// </summary>
        [ConfigName("sSendTo")]
        public string SendTo { get; set; }

        /// <summary>
        /// Gets the decrypted username for SMTP authentication.
        /// </summary>
        [Ignore]
        public string User => Encryptor.Default.Decrypt(EncryptedUser);

        /// <summary>
        /// Gets the decrypted password for SMTP authentication.
        /// </summary>
        [Ignore]
        public string Password => Encryptor.Default.Decrypt(EncryptedPassword);
    }
}