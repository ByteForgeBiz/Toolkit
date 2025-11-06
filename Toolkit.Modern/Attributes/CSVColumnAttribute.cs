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
        public string? Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSVColumnAttribute"/> class with default values.
        /// </summary>
        /// <remarks>This constructor sets the column index to -1 and the column name to <see langword="null"/>.</remarks>
        public CSVColumnAttribute() : this(-1, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSVColumnAttribute"/> class with the specified column name.
        /// </summary>
        /// <param name="name">The name of the CSV column associated with this attribute. Cannot be null or empty.</param>
        public CSVColumnAttribute(string name) : this(-1, name) { }

        /// <summary>
        /// Specifies metadata for a column in a CSV file, including its index and optional name.
        /// </summary>
        /// <param name="index">The zero-based index of the column in the CSV file. Must be a non-negative integer.</param>
        /// <param name="name">The optional name of the column. If not provided, the column will be identified by its index.</param>
        public CSVColumnAttribute(int index, string? name = null)
        {
            Name = name;
            Index = index;
        }
    }
}