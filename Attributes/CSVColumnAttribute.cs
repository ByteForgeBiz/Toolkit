using System;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Attribute to specify CSV column mapping for a property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CSVColumnAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the CSV column.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the database column.
        /// </summary>
        public Type DbType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSVColumnAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the CSV column.</param>
        /// <param name="dbType">The type of the database column.</param>
        public CSVColumnAttribute(string name = null, Type dbType = null)
        {
            Name = name;
            DbType = dbType;
        }
    }
}