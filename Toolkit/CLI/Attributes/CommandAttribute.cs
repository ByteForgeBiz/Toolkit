using System;

namespace ByteForge.Toolkit.CommandLine
{
    /// <summary>
    /// Attribute to define a command with a name, description, and optional aliases.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="description">The description of the command.</param>
        /// <param name="aliases">The aliases for the command.</param>
        public CommandAttribute(string name, string description, params string[] aliases)
        {
            Name = name;
            Description = description;
            Aliases = aliases;
        }

        /// <summary>
        /// Gets the name of the option.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Gets the description of the option.
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// Gets the aliases for the option.
        /// </summary>
        public string[] Aliases { get; }
    }
}