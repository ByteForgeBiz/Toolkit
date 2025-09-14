using System;
using System.Data;

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
        /// Gets the database type of the column.
        /// </summary>
        public DbType? DbType { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is an identity column.
        /// </summary>
        public bool IsIdentity { get; set; } = false;

        /// <summary>
        /// Gets or sets the seed value for the identity column.
        /// </summary>
        public long IdentitySeed { get; set; } = 1;

        /// <summary>
        /// Gets or sets the increment step value for the identity column.
        /// </summary>
        public long IdentityStep { get; set; } = 1;

        /// <summary>
        /// Gets a value indicating whether the column is a primary key.
        /// </summary>
        public bool IsPrimaryKey { get; }

        /// <summary>
        /// Gets a value indicating whether the column has an associated index.
        /// </summary>
        public bool HasIndex { get; }

        /// <summary>
        /// Gets a value indicating whether the column represents a unique entity.
        /// </summary>
        public bool IsUnique { get; }

        /// <summary>
        /// Gets the converter function to transform the column value.
        /// </summary>
        /// <remarks>
        /// The converter function is retrieved from the <see cref="ValueConverterRegistry"/> 
        /// using the <see cref="ConverterName"/>. If no converter is found, null.
        /// </remarks>
        public Func<object, object> Converter => ValueConverterRegistry.GetConverter(ConverterName);

        /// <summary>
        /// Gets or sets the name of the registered converter function (see <see cref="ValueConverterRegistry"/>) to use for transforming the column value.
        /// </summary>
        public string ConverterName { get; set; }

        /// <summary>
        /// Gets the maximum length for string/binary columns. If null, length will be determined dynamically or use defaults.
        /// </summary>
        public int MaxLength { get; set; }

        public DBColumnAttribute(string name) : this(name, false, false, false) { }

        public DBColumnAttribute(bool isPrimaryKey) : this(null, isPrimaryKey, false, false) { }

        public DBColumnAttribute(string name, bool isPrimaryKey) : this(name, isPrimaryKey, false, false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DBColumnAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the database column. If null, the property name will be used.</param>
        /// <param name="isPrimaryKey">Indicates whether the column is a primary key.</param>
        /// <param name="hasIndex">Indicates whether the column should have an index.</param>
        /// <param name="isUnique">Indicates whether the column should have a unique constraint.</param>
        public DBColumnAttribute(string name = null,
                                 bool isPrimaryKey = false,
                                 bool hasIndex = false,
                                 bool isUnique = false)
        {
            Name = name;
            IsUnique = isUnique;
            IsPrimaryKey = isPrimaryKey;
            HasIndex = isPrimaryKey || hasIndex || isUnique;
        }
    }
}