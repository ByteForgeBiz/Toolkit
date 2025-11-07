namespace ByteForge.Toolkit.Configuration;
/*
 *  ___      _  _     _   ___            _    _     _  _   _       _ _         _       
 * |   \ ___| \| |___| |_| _ \___ _ _ __(_)__| |_  /_\| |_| |_ _ _(_) |__ _  _| |_ ___ 
 * | |) / _ \ .` / _ \  _|  _/ -_) '_(_-< (_-<  _|/ _ \  _|  _| '_| | '_ \ || |  _/ -_)
 * |___/\___/_|\_\___/\__|_| \___|_| /__/_/__/\__/_/ \_\__|\__|_| |_|_.__/\_,_|\__\___|
 *                                                                                     
 */
/// <summary>
/// An attribute to indicate that a property should not be persisted.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DoNotPersistAttribute : Attribute { }

/*
 *  ___                         _  _   _       _ _         _       
 * |_ _|__ _ _ _  ___ _ _ ___  /_\| |_| |_ _ _(_) |__ _  _| |_ ___ 
 *  | |/ _` | ' \/ _ \ '_/ -_)/ _ \  _|  _| '_| | '_ \ || |  _/ -_)
 * |___\__, |_||_\___/_| \___/_/ \_\__|\__|_| |_|_.__/\_,_|\__\___|
 *     |___/                                                       
 */
/// <summary>
/// Indicates that the associated member should be ignored during reading or persisting operations.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IgnoreAttribute : DoNotPersistAttribute { }
