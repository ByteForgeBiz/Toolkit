using System.Text.RegularExpressions;

namespace ByteForge.Toolkit.Utils;
/// <summary>
/// Provides helper methods for normalizing and capitalizing personal names while
/// respecting linguistic particles (e.g. "de", "van"), cultural prefixes (e.g. "Mc", "Mac", "O'"),
/// hyphenated structures, and common edge cases.
/// </summary>
/// <remarks>
/// Rules implemented:<br/>
/// - Particles in <see cref="LowercaseParticles"/> remain lowercase unless they are the first token.<br/>
/// - Special prefixes in <see cref="SpecialPrefixes"/> are normalized to their canonical casing.<br/>
/// - Prefixes "Mc"/"Mac" attempt intelligent second-letter capitalization with exception handling.<br/>
/// - Hyphenated components are capitalized individually.<br/>
/// - Handles apostrophe-based prefixes like "O'" and "D'".<br/>
/// - Preserves spacing normalization; multiple internal spaces collapse to single spaces.<br/>
/// </remarks>
/// <example>
/// Examples:<br/>
/// <code>
/// NameUtil.CapitalizeName("mCdonald");        // "McDonald"
/// NameUtil.CapitalizeName("sean o'connor");    // "Sean O'Connor"
/// NameUtil.CapitalizeName("maria de la cruz"); // "Maria de la Cruz"
/// NameUtil.CapitalizeFullName("smith, john de la"); // "Smith, John de la"
/// </code>
/// </example>
public static class NameUtil
{
    /// <summary>
    /// Particles that should remain lowercase unless they are the first component in the name.
    /// </summary>
    /// <remarks>
    /// Includes Romance, Germanic, and some Arabic / Spanish conjunction particles.
    /// Case-insensitive lookup.
    /// </remarks>
    private static readonly HashSet<string> LowercaseParticles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "de", "da", "do", "dos", "das", "del", "della", "delle", "di", "du",
        "van", "von", "der", "den", "het", "la", "le", "les", "el", "al",
        "bin", "ibn", "abu", "y", "e", "o", "und", "and", "of", "the"
    };

    /// <summary>
    /// Known special-case prefixes and their canonical capitalization.
    /// </summary>
    /// <remarks>
    /// Applied using prefix matching (StartsWith). Order of enumeration is not guaranteed;
    /// logic iterates all entries. Case-insensitive keys.
    /// </remarks>
    private static readonly Dictionary<string, string> SpecialPrefixes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "mc", "Mc" },
        { "mac", "Mac" },
        { "o'", "O'" },
        { "d'", "D'" }
    };

    /// <summary>
    /// Capitalizes a single or multi-part name string with culturally-aware rules.
    /// </summary>
    /// <param name="name">Raw input name (may contain irregular casing or spacing).</param>
    /// <returns>The normalized and culturally-aware capitalized name, or the original value if null/whitespace.</returns>
    /// <remarks>
    /// This method:
    /// - Trims leading/trailing whitespace.
    /// - Collapses multiple internal spaces.
    /// - Processes each token with <see cref="CapitalizeNamePart(string, bool)"/>
    /// </remarks>
    public static string CapitalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        // Normalize whitespace and split into parts
        var parts = Regex.Split(name.Trim(), @"\s+");

        for (var i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i]))
                continue;

            parts[i] = CapitalizeNamePart(parts[i], i == 0);
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Capitalizes an individual name component applying particle, prefix, and hyphen rules.
    /// </summary>
    /// <param name="part">The raw part (token) of the name.</param>
    /// <param name="isFirstPart">True if this part is the first token in the full name.</param>
    /// <returns>Capitalized component according to rules.</returns>
    private static string CapitalizeNamePart(string part, bool isFirstPart)
    {
        if (string.IsNullOrEmpty(part))
            return part;

        var lowerPart = part.ToLower();

        // Handle particles (de, van, etc.) - keep lowercase unless first part
        if (!isFirstPart && LowercaseParticles.Contains(lowerPart))
            return lowerPart;

        // Handle special prefixes (Mc, Mac, O', D')
        foreach (var prefix in SpecialPrefixes)
        {
            if (lowerPart.StartsWith(prefix.Key))
            {
                if (lowerPart.Length == prefix.Key.Length)
                    return prefix.Value;

                var remainder = lowerPart.Substring(prefix.Key.Length);

                // Special handling for Mc/Mac + vowel or specific consonants
                if ((prefix.Key == "mc" || prefix.Key == "mac") && remainder.Length > 0)
                {
                    var firstChar = remainder[0];
                    // Determine whether remainder begins an exception pattern
                    if (IsCommonMcMacException(remainder))
                        return prefix.Value + remainder;
                    return prefix.Value + char.ToUpper(firstChar) + remainder.Substring(1);
                }

                // For O' and D', always capitalize the next letter
                if ((prefix.Key == "o'" || prefix.Key == "d'") && remainder.Length > 0)
                    return prefix.Value + char.ToUpper(remainder[0]) + remainder.Substring(1);

                return prefix.Value + remainder;
            }
        }

        // Handle hyphenated names
        if (part.Contains("-"))
        {
            var hyphenParts = part.Split('-');
            for (var i = 0; i < hyphenParts.Length; i++)
                hyphenParts[i] = CapitalizeSimplePart(hyphenParts[i]);
            return string.Join("-", hyphenParts);
        }

        // Default capitalization
        return CapitalizeSimplePart(part);
    }

    /// <summary>
    /// Capitalizes a simple, non-hyphenated token (first letter uppercase, remainder lowercase).
    /// </summary>
    /// <param name="part">The token to transform.</param>
    /// <returns>Capitalized token, or original if null/empty.</returns>
    private static string CapitalizeSimplePart(string part)
    {
        if (string.IsNullOrEmpty(part))
            return part;

        return char.ToUpper(part[0]) + part.Substring(1).ToLower();
    }

    /// <summary>
    /// Determines whether the remainder after a "Mc"/"Mac" prefix matches a known lowercase exception pattern.
    /// </summary>
    /// <param name="remainder">String following the prefix.</param>
    /// <returns>
    /// True if the remainder starts with an exception sequence (e.g. "kay", "kenzie") and should
    /// not capitalize the first letter after the prefix; otherwise false.
    /// </returns>
    private static bool IsCommonMcMacException(string remainder)
    {
        // Common exceptions where the letter after Mc/Mac shouldn't be capitalized
        string[] exceptions = { "kay", "kenzie", "kinley", "lean", "queen" };
        return exceptions.Any(ex => remainder.StartsWith(ex, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Capitalizes a full name which may include comma-delimited segments (e.g. "Last, First Middle").
    /// </summary>
    /// <param name="fullName">Raw full name string.</param>
    /// <returns>Fully capitalized name or original if null/whitespace.</returns>
    /// <remarks>
    /// If a comma is present, each comma-separated segment is individually processed with <see cref="CapitalizeName(string)"/>.
    /// This allows correct handling of formats like: "Smith, john de la".
    /// </remarks>
    public static string CapitalizeFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return fullName;

        // Handle comma-separated names (Last, First Middle)
        if (fullName.Contains(","))
        {
            var commaParts = fullName.Split(',');
            for (var i = 0; i < commaParts.Length; i++)
                commaParts[i] = CapitalizeName(commaParts[i]);
            return string.Join(",", commaParts);
        }

        return CapitalizeName(fullName);
    }

    /// <summary>
    /// Normalizes a user name by trimming whitespace, converting to lowercase, and removing domain information.
    /// </summary>
    /// <param name="name">The user name to normalize. This can include domain information (e.g., "DOMAIN\username" or "username@domain.com").</param>
    /// <returns>The normalized user name, which is the input string converted to lowercase, with leading and trailing whitespace removed, and any domain information stripped. If <paramref name="name"/> is <see langword="null"/> or empty, the method returns the input as is.</returns>
    public static string NormalizeUserName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var normalized = name.Trim().ToLowerInvariant();
        if (normalized.Contains("\\"))
            normalized = name.Split('\\').Last();
        if (normalized.Contains("@"))
            normalized = name.Split('@').First();
        return normalized;
    }
}
