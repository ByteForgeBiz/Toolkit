using System;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Attribute to specify database column mapping for a property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DBColumnAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the database column.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the database column.
        /// </summary>
        public Type DbType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DBColumnAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the database column.</param>
        /// <param name="dbType">The type of the database column.</param>
        public DBColumnAttribute(string name = null, Type dbType = null)
        {
            Name = name;
            DbType = dbType;
        }
    }
}