using System;

namespace ByteForge.Toolkit.CommandLine
{
    /// <summary>
    /// Attribute to define an option for a command parameter with a description, name, and optional aliases.
    /// </summary>
    /// <remarks>
    /// The <see cref="OptionAttribute"/> is used to annotate parameters of methods that represent command-line options.<br/>
    /// The name of the option can be set using the <see cref="Name"/> property. If not set, the parameter name will be used as the option name.<br/>
    /// The <see cref="Description"/> property provides a human-readable explanation of the option's purpose and cannot be null.<br/>
    /// Also, multiple aliases for the option can be specified using the <see cref="Aliases"/> property or the constructor parameter.<br/>
    /// </remarks>
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
        public string Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionAttribute"/> class with the specified description.
        /// </summary>
        /// <param name="description">The description of the option. This value provides a human-readable explanation of the option's purpose and cannot be null.</param>
        public OptionAttribute(string description)
        {
            Description = description;
            Aliases = Array.Empty<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionAttribute"/> class.
        /// </summary>
        /// <param name="description">The description of the option.</param>
        /// <param name="aliases">The aliases for the option.</param>
        public OptionAttribute(string description, params string[] aliases) : this(description)
        {
            Aliases = aliases;
        }
    }
}