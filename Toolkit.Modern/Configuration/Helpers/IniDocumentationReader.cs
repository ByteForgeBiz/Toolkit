using System;
using System.Collections.Generic;
using System.IO;

namespace ByteForge.Toolkit.Configuration
{
    /// <summary>
    /// Reads Toolkit INI documentation blocks and validates names before the Microsoft INI provider loads values.
    /// </summary>
    internal static class IniDocumentationReader
    {
        /// <summary>
        /// Reads documentation and validation metadata from an INI file.
        /// </summary>
        /// <param name="path">The INI file path.</param>
        /// <returns>The parsed documentation catalog.</returns>
        public static ConfigDocumentationCatalog Read(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var catalog = new ConfigDocumentationCatalog();
            var sections = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var keysBySection = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            var currentSection = string.Empty;
            var pendingDocLines = new List<string>();
            var collectingDoc = false;
            var lines = File.ReadAllLines(path);

            for (var index = 0; index < lines.Length; index++)
            {
                var lineNumber = index + 1;
                var line = lines[index];
                var trimmedLine = line.Trim();

                if (trimmedLine == ";;;")
                {
                    pendingDocLines.Clear();
                    collectingDoc = true;
                    continue;
                }

                if (collectingDoc)
                {
                    if (trimmedLine.StartsWith(";") && trimmedLine != ";;;")
                    {
                        pendingDocLines.Add(UnwrapDocumentationLine(trimmedLine));
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(trimmedLine))
                        continue;

                    collectingDoc = false;
                }

                if (IsIniCommentOrBlank(trimmedLine))
                    continue;

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
                    ValidateReservedName(currentSection, "section", path, lineNumber);

                    if (sections.TryGetValue(currentSection, out var firstLine))
                        throw new FormatException($"The section '{currentSection}' was found again in file '{path}' at line {lineNumber}. Its first appearance was at line {firstLine}.");

                    sections.Add(currentSection, lineNumber);
                    keysBySection[currentSection] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    AttachSectionDocumentation(catalog, currentSection, pendingDocLines, path, lineNumber);
                    pendingDocLines.Clear();
                    continue;
                }

                var equalsIndex = trimmedLine.IndexOf('=');
                if (equalsIndex < 0)
                {
                    pendingDocLines.Clear();
                    continue;
                }

                var key = trimmedLine.Substring(0, equalsIndex).Trim();
                ValidateReservedName(key, "key", path, lineNumber);

                if (!keysBySection.TryGetValue(currentSection, out var sectionKeys))
                {
                    sectionKeys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    keysBySection[currentSection] = sectionKeys;
                }

                if (sectionKeys.TryGetValue(key, out var firstKeyLine))
                {
                    var displaySection = string.IsNullOrEmpty(currentSection) ? "<root>" : currentSection;
                    throw new FormatException($"The key '{key}' in section '{displaySection}' was found again in file '{path}' at line {lineNumber}. Its first appearance was at line {firstKeyLine}.");
                }

                sectionKeys.Add(key, lineNumber);
                AttachItemDocumentation(catalog, currentSection, key, pendingDocLines, path, lineNumber);
                pendingDocLines.Clear();
            }

            return catalog;
        }

        /// <summary>
        /// Validates that an INI name does not contain reserved hierarchy separators.
        /// </summary>
        /// <param name="name">The section or key name.</param>
        /// <param name="nameKind">The display name for the kind of name being validated.</param>
        /// <param name="path">The INI file path.</param>
        /// <param name="lineNumber">The one-based line number where the name was found.</param>
        private static void ValidateReservedName(string name, string nameKind, string path, int lineNumber)
        {
            if (name?.IndexOf(':') >= 0)
                throw new FormatException($"The {nameKind} name '{name}' in file '{path}' at line {lineNumber} contains ':', which is reserved for Toolkit configuration paths.");
        }

        /// <summary>
        /// Adds pending documentation to a section.
        /// </summary>
        /// <param name="catalog">The documentation catalog to update.</param>
        /// <param name="section">The section name.</param>
        /// <param name="pendingDocLines">The pending documentation lines.</param>
        /// <param name="path">The INI file path.</param>
        /// <param name="lineNumber">The section line number.</param>
        private static void AttachSectionDocumentation(ConfigDocumentationCatalog catalog, string section, IReadOnlyCollection<string> pendingDocLines, string path, int lineNumber)
        {
            var description = JoinDocumentation(pendingDocLines);
            if (!string.IsNullOrWhiteSpace(description))
                catalog.SetPhysicalSection(section, description, path, lineNumber);
        }

        /// <summary>
        /// Adds pending documentation to a configuration item.
        /// </summary>
        /// <param name="catalog">The documentation catalog to update.</param>
        /// <param name="section">The section name.</param>
        /// <param name="key">The item key.</param>
        /// <param name="pendingDocLines">The pending documentation lines.</param>
        /// <param name="path">The INI file path.</param>
        /// <param name="lineNumber">The item line number.</param>
        private static void AttachItemDocumentation(ConfigDocumentationCatalog catalog, string section, string key, IReadOnlyCollection<string> pendingDocLines, string path, int lineNumber)
        {
            var description = JoinDocumentation(pendingDocLines);
            if (!string.IsNullOrWhiteSpace(description))
                catalog.SetPhysicalItem(section, key, description, path, lineNumber);
        }

        /// <summary>
        /// Converts collected documentation lines into a description string.
        /// </summary>
        /// <param name="pendingDocLines">The pending documentation lines.</param>
        /// <returns>The normalized description text.</returns>
        private static string JoinDocumentation(IReadOnlyCollection<string> pendingDocLines)
            => pendingDocLines == null || pendingDocLines.Count == 0
                ? null
                : string.Join(Environment.NewLine, pendingDocLines).Trim();

        /// <summary>
        /// Determines whether a trimmed INI line is blank or starts with a supported comment marker.
        /// </summary>
        /// <param name="trimmedLine">The trimmed INI line to inspect.</param>
        /// <returns><see langword="true"/> when the line is blank or starts with <c>;</c> or <c>#</c>; otherwise, <see langword="false"/>.</returns>
        private static bool IsIniCommentOrBlank(string trimmedLine)
            => string.IsNullOrWhiteSpace(trimmedLine)
                || trimmedLine.StartsWith(";")
                || trimmedLine.StartsWith("#");

        /// <summary>
        /// Removes the comment leader from a documentation line.
        /// </summary>
        /// <param name="line">The trimmed documentation comment line.</param>
        /// <returns>The documentation text without the comment leader.</returns>
        private static string UnwrapDocumentationLine(string line)
        {
            var value = line.Substring(1);
            return value.StartsWith(" ") ? value.Substring(1) : value;
        }
    }
}
