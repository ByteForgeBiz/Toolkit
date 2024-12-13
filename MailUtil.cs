using System;
using System.Net;
using System.Net.Mail;

namespace ByteForge.Toolkit;

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
    /// <summary>
    /// Gets the mail server address from the configuration.
    /// </summary>
    public static string MailServer => Configuration.Root["Mail Server:sServer"];

    /// <summary>
    /// Gets the port number used for SMTP.
    /// </summary>
    /// <value>
    /// The port number as an integer, obtained from the application's configuration.
    /// If the configuration value cannot be parsed to an integer, the default 587 is used.
    /// </value>
    public static int MailPort
    {
        get
        {
            var cfg = Configuration.Root["Mail Server:iPort"];
            if (int.TryParse(cfg, out _mailPort))
                return _mailPort;
            else
                return 587;
        }
    }
    private static int _mailPort;

    /// <summary>
    /// Gets the security protocol type used for SMTP connections.
    /// </summary>
    /// <value>
    /// The security protocol type as a <see cref="SecurityProtocolType"/>, obtained from the application's configuration.
    /// If the configuration value cannot be parsed to a valid <see cref="SecurityProtocolType"/>, it uses the default <see cref="SecurityProtocolType.Tls12"/>.
    /// </value>
    /// <exception cref="Exception">Thrown when the configured security protocol is invalid.</exception>
    public static SecurityProtocolType MailSecurityProtocol
    {
        get
        {
            var cfg = Configuration.Root["Mail Server:sSecurityProtocol"];
            if (Enum.TryParse<SecurityProtocolType>(cfg, out _securityProtocol))
                return _securityProtocol;
            else
                return _securityProtocol = SecurityProtocolType.Tls12;
        }
    }
    private static SecurityProtocolType _securityProtocol;

    /// <summary>
    /// Gets the username used for SMTP authentication.
    /// </summary>
    /// <value>
    /// The username as a decrypted string, obtained from the application's configuration.
    /// </value>
    public static string MailUser
    {
        get
        {
            _MailUser ??= Configuration.Root["Mail Server:esUser"];
            return Encryptor.Decrypt(13, 16, _MailUser);
        }
    }
    private static string _MailUser;

    /// <summary>
    /// Gets the password used for SMTP authentication.
    /// </summary>
    /// <value>
    /// The password as a decrypted string, obtained from the application's configuration.
    /// </value>
    public static string MailPassword
    {
        get
        {
            _MailPassword ??= Configuration.Root["Mail Server:esPass"];
            return Encryptor.Decrypt(13, 16, _MailPassword);
        }
    }
    private static string _MailPassword;

    /// <summary>
    /// Gets the email address used as the "From" address in emails.
    /// </summary>
    /// <value>
    /// The "From" email address, obtained from the application's configuration.
    /// </value>
    public static string MailFrom => Configuration.Root["Mail Server:sSender"];

    /// <summary>
    /// Gets the email address used as the default "To" address in emails.
    /// </summary>
    /// <value>
    /// The default "To" email address, obtained from the application's configuration.
    /// </value>
    public static string MailTo => Configuration.Root["Mail Server:sSendTo"];

    /// <summary>
    /// Sends an email to the specified email addresses with the given subject and body.
    /// </summary>
    /// <param name="emailAddresses">The email addresses to send the email to, separated by semicolons.</param>
    /// <param name="subject">The subject of the email.</param>
    /// <param name="body">The body of the email.</param>
    /// <param name="fileToAttach">The file path to attach to the email. If null, no file is attached.</param>
    /// <returns>True if the email was sent successfully, otherwise false.</returns>
    public static bool SendMail(string emailAddresses, string subject, string body, string fileToAttach)
    {
        var files = new string[] { fileToAttach };
        if (string.IsNullOrEmpty(fileToAttach))
            files = null;

        return SendMail(emailAddresses, subject, body, files);
    }

    /// <summary>
    /// Sends an email to the specified email addresses with the given subject and body.
    /// </summary>
    /// <param name="emailAddresses">The email addresses to send the email to, separated by semicolons.</param>
    /// <param name="subject">The subject of the email.</param>
    /// <param name="body">The body of the email.</param>
    /// <param name="filesToAttach">An array of file paths to attach to the email.</param>
    /// <returns>True if the email was sent successfully, otherwise false.</returns>
    public static bool SendMail(string emailAddresses, string subject, string body, string[] filesToAttach = null)
    {
        return SendMail(emailAddresses, subject, body, true, filesToAttach);
    }

    /// <summary>
    /// Sends an email to the specified email addresses with the given subject and body, and specifies whether the body is HTML.
    /// </summary>
    /// <param name="emailAddresses">A semicolon-separated list of email addresses to send the email to.</param>
    /// <param name="subject">The subject line of the email.</param>
    /// <param name="body">The body of the email. Can be either plain text or HTML based on the isHtml parameter.</param>
    /// <param name="isHtml">A boolean value indicating whether the body of the email is in HTML format. True if the body is HTML, otherwise false.</param>
    /// <param name="filesToAttach">An array of file paths to attach to the email.</param>
    /// <returns>True if the email was sent successfully, otherwise false.</returns>
    public static bool SendMail(string emailAddresses, string subject, string body, bool isHtml, string[] filesToAttach = null)
    {
        var currentSecurityProtocol = ServicePointManager.SecurityProtocol;

        filesToAttach ??= [];

        try
        {
            /*
             * Sends the email message
             */
            using var email = new MailMessage()
            {
                From = new MailAddress(MailFrom),
                Sender = new MailAddress(MailFrom),
                Subject = subject,
                IsBodyHtml = isHtml,
                SubjectEncoding = System.Text.Encoding.UTF8
            };

            email.ReplyToList.Add(new MailAddress(MailFrom));
            var addrs = emailAddresses.Split([';'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var addr in addrs)
                email.To.Add(addr);

            if (body.Length != 0)
            {
                email.BodyEncoding = System.Text.Encoding.UTF8;
                email.Body = body;
            }

            foreach (var file in filesToAttach)
                email.Attachments.Add(new Attachment(file));

            /*
             * Define the credentials to use the SMTP server
             * and security protocol
             */
            ServicePointManager.SecurityProtocol = MailSecurityProtocol;
            using var smtp = new SmtpClient(MailServer)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(MailUser, MailPassword),
                EnableSsl = true,
                Port = MailPort
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
}
