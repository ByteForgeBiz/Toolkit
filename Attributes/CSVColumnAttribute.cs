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
        /// Gets the index of the column in the CSV file.
        /// </summary>
        public int Index { get; } = -1;

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
        /// <param name="index">The index of the column in the CSV file. Default is -1, which means the index is not specified.</param>
        public CSVColumnAttribute(string name = null, Type dbType = null, int index = -1)
        {
            Name = name;
            DbType = dbType;
            Index = index;
        }
    }
}