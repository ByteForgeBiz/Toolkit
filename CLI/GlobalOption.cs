using System;
using System.Collections.Generic;
using System.Linq;

namespace ByteForge.Toolkit.CommandLine
{
    /// <summary>
    /// Represents a global command-line option that is processed before main command parsing.
    /// Global options are available across all commands and are typically used for application-wide settings.
    /// </summary>
    public class GlobalOption
    {
        /// <summary>
        /// Gets the primary name of the global option (without prefix).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the global option.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the action to execute when this option is encountered.
        /// </summary>
        public Action<string> Action { get; }

        /// <summary>
        /// Gets the custom aliases provided by the developer.
        /// </summary>
        public string[] CustomAliases { get; }

        /// <summary>
        /// Gets all aliases for this option (both auto-generated and custom).
        /// </summary>
        public string[] AllAliases { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this option expects a value.
        /// </summary>
        public bool ExpectsValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalOption"/> class.
        /// </summary>
        /// <param name="name">The primary name of the option (without dashes).</param>
        /// <param name="description">The description of the option.</param>
        /// <param name="action">The action to execute when the option is encountered.</param>
        /// <param name="expectsValue">Whether the option expects a value.</param>
        /// <param name="aliases">Custom aliases for the option.</param>
        public GlobalOption(string name, string description, Action action, bool expectsValue = false, params string[] aliases)
            : this(name,
                   description,
                   action == null ? throw new ArgumentNullException(nameof(action)) : (expectsValue ? (Action<string>)(value => action()) : (value => action())),
                   expectsValue,
                   aliases) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalOption"/> class with value support.
        /// </summary>
        /// <param name="name">The primary name of the option (without dashes).</param>
        /// <param name="description">The description of the option.</param>
        /// <param name="action">The action to execute when the option is encountered, receiving the option value.</param>
        /// <param name="expectsValue">Whether the option expects a value.</param>
        /// <param name="aliases">Custom aliases for the option.</param>
        public GlobalOption(string name, string description, Action<string> action, bool expectsValue = true, params string[] aliases)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Option name cannot be null or empty.", nameof(name));

            Name = name.TrimStart('-', '/');
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Action = action ?? throw new ArgumentNullException(nameof(action));
            ExpectsValue = expectsValue;
            CustomAliases = aliases ?? new string[0];
        }

        /// <summary>
        /// Generates all aliases for this global option using the same logic as CommandBuilder.
        /// </summary>
        /// <param name="usedNames">Names already in use to avoid conflicts.</param>
        internal void GenerateAliases(HashSet<string> usedNames)
        {
            var generatedAliases = GenerateAliases(Name);
            var normalizedCustom = NormalizeCustomAliases(CustomAliases);
            
            var allAliases = generatedAliases
                .Concat(normalizedCustom)
                .Where(alias => !usedNames.Contains(alias))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            AllAliases = allAliases;

            // Add these aliases to used names
            foreach (var alias in allAliases)
                usedNames.Add(alias);
        }

        /// <summary>
        /// Checks if the given argument matches any of this option's aliases.
        /// </summary>
        /// <param name="arg">The command-line argument to check.</param>
        /// <returns>True if the argument matches this option; otherwise, false.</returns>
        public bool Matches(string arg)
        {
            if (AllAliases == null) return false;
            return AllAliases.Any(alias => string.Equals(alias, arg, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Generates standard aliases for the specified option name using CommandBuilder's logic.
        /// </summary>
        /// <param name="name">The option name.</param>
        /// <returns>An array of generated aliases.</returns>
        private static string[] GenerateAliases(string name)
        {
            // Create short name based on option length (same logic as CommandBuilder)
            var shortName = name.Length >= 3
                ? name.Substring(0, 3).ToLowerInvariant()
                : name.ToLowerInvariant();

            // Generate standard aliases with single character and short name
            var standardAliases = new[]
            {
                $"/{shortName[0]}",            // Single char with /
                $"-{shortName[0]}",            // Single char with -
                $"--{shortName[0]}",           // Single char with --
                $"/{shortName}",               // Short name  with /
                $"-{shortName}",               // Short name  with -
                $"--{shortName}",              // Short name  with --
                $"--{name.ToLowerInvariant()}" // Full name   with --
            };
            return standardAliases;
        }

        /// <summary>
        /// Normalizes custom aliases by trimming prefixes and generating standard variations.
        /// </summary>
        /// <param name="aliases">The custom aliases to normalize.</param>
        /// <returns>A collection of normalized aliases.</returns>
        private static IEnumerable<string> NormalizeCustomAliases(string[] aliases)
        {
            if (aliases == null || aliases.Length == 0) return Array.Empty<string>();
            return aliases.Select(a => a.TrimStart('-', '/')).SelectMany(a => GenerateAliases(a)).Distinct();
        }
    }
}