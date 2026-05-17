using System;
using System.Text.RegularExpressions;

namespace ByteForge.Toolkit.Logging
{
    /// <summary>
    /// Masks sensitive key/value pairs before they are written to log destinations.
    /// </summary>
    public static class LogSecretMasker
    {
        private const string MaskValue = "[REDACTED]";
        private const string SensitiveKeyPattern =
            "password|passwd|pwd|secret|client[_-]?secret|clientsecret|api[_-]?key|apikey|api[_-]?token|apitoken|access[_-]?token|accesstoken|refresh[_-]?token|refreshtoken";

        private static readonly Regex SensitiveKeyValuePattern = new Regex(
            $@"(?<prefix>(?:[""']?(?:{SensitiveKeyPattern})[""']?\s*[:=]\s*[""']?))(?<secret>[^""'\s,;&}}]+)(?<suffix>[""']?)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Replaces sensitive values in the supplied log text with a fixed redaction marker.
        /// </summary>
        /// <param name="text">The text to scan for sensitive key/value pairs.</param>
        /// <returns>The original text with sensitive values masked, or the original value when no masking is needed.</returns>
        public static string Mask(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return SensitiveKeyValuePattern.Replace(text, match =>
                $"{match.Groups["prefix"].Value}{MaskValue}{match.Groups["suffix"].Value}");
        }

        /// <summary>
        /// Replaces sensitive values in the supplied object value with a fixed redaction marker.
        /// </summary>
        /// <param name="value">The value to convert to text and scan for sensitive key/value pairs.</param>
        /// <returns>The string representation of the value with sensitive values masked.</returns>
        public static string Mask(object value) => value == null ? null : Mask(value.ToString());
    }
}
