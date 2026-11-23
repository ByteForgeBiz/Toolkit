using System.Text.RegularExpressions;

namespace ByteForge.Toolkit.Data;

/// <summary>Applies explicit safety rules to automatically executed SQL scripts.</summary>
public static class SqlScriptSafety
{
    /// <summary>The exact first-line directive that permits reviewed automatic data migrations.</summary>
    public const string AllowDataChangesDirective = "-- BYTEFORGE-SQL: ALLOW-DATA-CHANGES";

    private static readonly Regex DataModificationPattern = new(
        @"^\s*(INSERT|UPDATE|DELETE|MERGE|TRUNCATE)\b",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex OptOutPattern = new(
        @"\A(?:\uFEFF)?[\t ]*--[\t ]*BYTEFORGE-SQL:[\t ]*ALLOW-DATA-CHANGES[\t ]*(?:\r?\n|\z)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex AutomaticSchemaScriptPathPattern = new(
        @"(?:^|[\\/])A\d+_[^\\/]+\.sql$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Determines whether a SQL path uses the automatic A-script naming convention.</summary>
    public static bool IsAutomaticSchemaScriptPath(string path) =>
        AutomaticSchemaScriptPathPattern.IsMatch(path ?? string.Empty);

    /// <summary>Determines whether a script contains a data-modification statement.</summary>
    public static bool ContainsDataModification(string sql) =>
        DataModificationPattern.IsMatch(sql ?? string.Empty);

    /// <summary>Determines whether the exact data-change opt-out is the first non-whitespace line.</summary>
    public static bool HasDataModificationOptOut(string sql) =>
        OptOutPattern.IsMatch(sql ?? string.Empty);

    /// <summary>Determines whether an automatic schema script passes the data-change guard.</summary>
    public static bool AllowsAutomaticExecution(string sql) =>
        !ContainsDataModification(sql) || HasDataModificationOptOut(sql);
}
