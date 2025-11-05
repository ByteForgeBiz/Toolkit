using System;

namespace ByteForge.Toolkit
{
    /*
     *  ___  _      _   _                          _  _   _       _ _         _       
     * |   \(_)__  | |_(_)___ _ _  __ _ _ _ _  _  /_\| |_| |_ _ _(_) |__ _  _| |_ ___ 
     * | |) | / _| |  _| / _ \ ' \/ _` | '_| || |/ _ \  _|  _| '_| | '_ \ || |  _/ -_)
     * |___/|_\__|  \__|_\___/_||_\__,_|_|  \_, /_/ \_\__|\__|_| |_|_.__/\_,_|\__\___|
     *                                      |__/                                      
     */
    /// <summary>
    /// Specifies that the decorated property represents a dictionary in the configuration section.
    /// Supports Dictionary&lt;string, string&gt; stored as key=value pairs in a separate INI section.
    /// </summary>
    /// <remarks>
    /// The dictionary is stored in a separate INI section with each key-value pair as a line.
    /// If no section name is specified, defaults to "{PropertyName}Dict".
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class DictionaryAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the configuration section that contains the dictionary.
        /// </summary>
        public string SectionName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryAttribute"/> class.
        /// </summary>
        /// <param name="sectionName">
        /// The name of the configuration section containing the dictionary. 
        /// If <c>null</c>, defaults to "{PropertyName}Dict".
        /// </param>
        public DictionaryAttribute(string sectionName = null) => SectionName = sectionName;
    }
}