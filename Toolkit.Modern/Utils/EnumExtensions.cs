using System;
using System.ComponentModel;

namespace ByteForge.Toolkit
{
    /*
     *  ___                ___     _               _             
     * | __|_ _ _  _ _ __ | __|_ _| |_ ___ _ _  __(_)___ _ _  ___
     * | _|| ' \ || | '  \| _|\ \ /  _/ -_) ' \(_-< / _ \ ' \(_-<
     * |___|_||_\_,_|_|_|_|___/_\_\\__\___|_||_/__/_\___/_||_/__/
     *                                                           
     */
    /// <summary>
    /// Provides extension methods for enumerations.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets the description attribute of an enum value.
        /// </summary>
        /// <param name="value">The enum value.</param>
        /// <returns>The description of the enum value, or the enum value as a string if no description is found.</returns>
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());

            if (field == null)
                return value.ToString();

            var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));

            return attribute?.Description ?? value.ToString();
        }
    }
}
