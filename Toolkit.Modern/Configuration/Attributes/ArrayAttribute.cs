namespace ByteForge.Toolkit;
/*
 *    _                       _  _   _       _ _         _       
 *   /_\  _ _ _ _ __ _ _  _  /_\| |_| |_ _ _(_) |__ _  _| |_ ___ 
 *  / _ \| '_| '_/ _` | || |/ _ \  _|  _| '_| | '_ \ || |  _/ -_)
 * /_/ \_\_| |_| \__,_|\_, /_/ \_\__|\__|_| |_|_.__/\_,_|\__\___|
 *                     |__/                                      
 */
/// <summary>
/// Specifies that the decorated property represents an array in the configuration section.
/// </summary>
/// <remarks>
/// Optionally, a section name can be provided to indicate the configuration section
/// where the array is located. If no section name is specified, the default section is used.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class ArrayAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the configuration section that contains the array.
    /// </summary>
    public string SectionName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayAttribute"/> class.
    /// </summary>
    /// <param name="sectionName">
    /// The name of the configuration section containing the array. 
    /// If <c>null</c>, the default section is used.
    /// </param>
    public ArrayAttribute(string sectionName  = "") => SectionName = sectionName;
}
