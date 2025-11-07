namespace ByteForge.Toolkit.Configuration;
/*
 *   ___           __ _      _  _                  _  _   _       _ _         _       
 *  / __|___ _ _  / _(_)__ _| \| |__ _ _ __  ___  /_\| |_| |_ _ _(_) |__ _  _| |_ ___ 
 * | (__/ _ \ ' \|  _| / _` | .` / _` | '  \/ -_)/ _ \  _|  _| '_| | '_ \ || |  _/ -_)
 *  \___\___/_||_|_| |_\__, |_|\_\__,_|_|_|_\___/_/ \_\__|\__|_| |_|_.__/\_,_|\__\___|
 *                     |___/                                                          
 */
/// <summary>
/// Specifies a custom name for a property in the configuration section.<br/>
/// <br/>
/// This is useful when the property name in the class does not match the name used in the 
/// configuration file. <br/>
/// By applying this attribute to a property, you can map the property 
/// to a different name in the configuration file.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ConfigNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigNameAttribute"/> class.
    /// </summary>
    /// <param name="name">The custom name of the property.</param>
    public ConfigNameAttribute(string name) => Name = name;

    /// <summary>
    /// Gets the custom name of the property.
    /// </summary>
    public string Name { get; }
}
