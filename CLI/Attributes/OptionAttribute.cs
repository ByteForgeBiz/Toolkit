using System;

namespace ByteForge.Toolkit.CommandLine
{
    /// <summary>
    /// Attribute to define an option for a command parameter with a description, name, and optional aliases.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class OptionAttribute : Attribute
    {
        /// <summary>
        /// Gets the description of the option.
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// Gets the aliases for the option.
        /// </summary>
        public string[] Aliases { get; }
        /// <summary>
        /// Gets the name of the option.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionAttribute"/> class.
        /// </summary>
        /// <param name="description">The description of the option.</param>
        /// <param name="name">The name of the option.</param>
        /// <param name="aliases">The aliases for the option.</param>
        public OptionAttribute(string description = null, string name = null, params string[] aliases)
        {
            Description = description;
            Name = name;
            Aliases = aliases;
        }
    }
}