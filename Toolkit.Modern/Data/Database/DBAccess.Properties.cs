using System;
using System.Collections.Generic;
using System.Data;

namespace ByteForge.Toolkit;
/*
 *  ___  ___   _                     ___                       _   _        
 * |   \| _ ) /_\  __ __ ___ ______ | _ \_ _ ___ _ __  ___ _ _| |_(_)___ ___
 * | |) | _ \/ _ \/ _/ _/ -_)_-<_-<_|  _/ '_/ _ \ '_ \/ -_) '_|  _| / -/)_-<
 * |___/|___/_/ \_\__\__\___/__/__(_)_| |_| \___/ .__/\___|_|  \__|_\___/__/
 *                                              |_|                         
 */
/// <summary>
/// Provides methods for managing SQL Server extended properties on database objects.
/// Extended properties allow storing descriptive metadata with database objects such as tables, columns, and procedures.
/// </summary>
public partial class DBAccess
{
    #region Extended Properties Enumerations

    /// <summary>
    /// Defines the Level 0 object types in SQL Server extended properties hierarchy.
    /// Level 0 represents the highest level objects in the database schema.
    /// </summary>
    public enum ExtendedPropertyLevel0Type
    {
        /// <summary>Indicates no Level 0 object type (for database-level properties)</summary>
        Null,
        /// <summary>Assembly object type</summary>
        Assembly,
        /// <summary>Contract object type</summary>
        Contract,
        /// <summary>Event notification object type</summary>
        EventNotification,
        /// <summary>Filegroup object type</summary>
        Filegroup,
        /// <summary>Message type object type</summary>
        MessageType,
        /// <summary>Partition function object type</summary>
        PartitionFunction,
        /// <summary>Partition scheme object type</summary>
        PartitionScheme,
        /// <summary>Remote service binding object type</summary>
        RemoteServiceBinding,
        /// <summary>Route object type</summary>
        Route,
        /// <summary>Schema object type</summary>
        Schema,
        /// <summary>Service object type</summary>
        Service,
        /// <summary>User object type</summary>
        User,
        /// <summary>Trigger object type</summary>
        Trigger,
        /// <summary>Type object type</summary>
        Type,
        /// <summary>Plan guide object type</summary>
        PlanGuide,
    }

    /// <summary>
    /// Defines the Level 1 object types in SQL Server extended properties hierarchy.
    /// Level 1 represents objects that belong to Level 0 objects.
    /// </summary>
    public enum ExtendedPropertyLevel1Type
    {
        /// <summary>Indicates no Level 1 object type</summary>
        Null,
        /// <summary>Aggregate object type</summary>
        Aggregate,
        /// <summary>Default object type</summary>
        Default,
        /// <summary>Function object type</summary>
        Function,
        /// <summary>Logical File Name object type</summary>
        LogicalFileName,
        /// <summary>Procedure object type</summary>
        Procedure,
        /// <summary>Queue object type</summary>
        Queue,
        /// <summary>Rule object type</summary>
        Rule,
        /// <summary>Sequence object type</summary>
        Sequence,
        /// <summary>Synonym object type</summary>
        Synonym,
        /// <summary>Table object type</summary>
        Table,
        /// <summary>Table type object type</summary>
        TableType,
        /// <summary>Type object type</summary>
        Type,
        /// <summary>View object type</summary>
        View,
        /// <summary>XML schema collection object type</summary>
        XmlSchemaCollection
    }

    /// <summary>
    /// Defines the Level 2 object types in SQL Server extended properties hierarchy.
    /// Level 2 represents objects that belong to Level 1 objects, such as columns, parameters, and constraints.
    /// </summary>
    public enum ExtendedPropertyLevel2Type
    {
        /// <summary>Indicates no Level 2 object type</summary>
        Null,
        /// <summary>Column object type</summary>
        Column,
        /// <summary>Constraint object type</summary>
        Constraint,
        /// <summary>Event Notification object type</summary>
        EventNotification,
        /// <summary>Index object type</summary>
        Index,
        /// <summary>Parameter object type</summary>
        Parameter,
        /// <summary>Trigger object type</summary>
        Trigger
    }

    #endregion

    #region Extended Properties Data Models

    /// <summary>
    /// Represents an extended property with its metadata.
    /// </summary>
    public class ExtendedProperty
    {
        /// <summary>Gets or sets the name of the extended property.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the value of the extended property.</summary>
        public string? Value { get; set; }

        /// <summary>Gets or sets the Level 0 object type.</summary>
        public string? Level0Type { get; set; }

        /// <summary>Gets or sets the Level 0 object name.</summary>
        public string? Level0Name { get; set; }

        /// <summary>Gets or sets the Level 1 object type.</summary>
        public string? Level1Type { get; set; }

        /// <summary>Gets or sets the Level 1 object name.</summary>
        public string? Level1Name { get; set; }

        /// <summary>Gets or sets the Level 2 object type.</summary>
        public string? Level2Type { get; set; }

        /// <summary>Gets or sets the Level 2 object name.</summary>
        public string? Level2Name { get; set; }

        /// <summary>
        /// Returns a string representation of the extended property.
        /// </summary>
        /// <returns>A formatted string showing the property name, value, and object hierarchy.</returns>
        public override string ToString()
        {
            var objectPath = Level0Name;
            if (!string.IsNullOrEmpty(Level1Name))
                objectPath += "." + Level1Name;
            if (!string.IsNullOrEmpty(Level2Name))
                objectPath += "." + Level2Name;

            return $"{Name} = ‘{Value}’ (on {objectPath})";
        }
    }

    #endregion

    #region Extended Properties Methods

    /// <summary>
    /// Adds an extended property to a database object with all hierarchy levels specified.
    /// </summary>
    /// <param name="propertyName">The name of the extended property.</param>
    /// <param name="propertyValue">The value of the extended property.</param>
    /// <param name="level0Type">The Level 0 object type.</param>
    /// <param name="level0Name">The Level 0 object name.</param>
    /// <param name="level1Type">The Level 1 object type.</param>
    /// <param name="level1Name">The Level 1 object name.</param>
    /// <param name="level2Type">The Level 2 object type.</param>
    /// <param name="level2Name">The Level 2 object name.</param>
    /// <returns><see langword="true"/> if the extended property was added successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="propertyValue"/> is <see langword="null"/>.</exception>
    public bool AddExtendedProperty(string propertyName, 
                                    string propertyValue,
                                    ExtendedPropertyLevel0Type level0Type, string? level0Name,
                                    ExtendedPropertyLevel1Type level1Type, string? level1Name,
                                    ExtendedPropertyLevel2Type level2Type, string? level2Name)
    {
        if (DbType != DataBaseType.SQLServer)
            throw new NotSupportedException("Extended properties are only supported for SQL Server databases.");

        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentNullException(nameof(propertyName));
        if (propertyValue == null)
            throw new ArgumentNullException(nameof(propertyValue));

        var (lvl0TypeStr, lvl0Name, lvl1TypeStr, lvl1Name, lvl2TypeStr, lvl2Name) = PrepareLevels(level0Type, level0Name, level1Type, level1Name, level2Type, level2Name);

        var sql = "EXEC sp_addextendedproperty " +
                      "@name       = @PropertyName, @value      = @Property, " +
                      "@level0type = @Level0Type,   @level0name = @Level0Name, " +
                      "@level1type = @Level1Type,   @level1name = @Level1Name, " +
                      "@level2type = @Level2Type,   @level2name = @Level2Name";

        return ExecuteQuery(sql, 
                            propertyName, propertyValue,
                            lvl0TypeStr, lvl0Name,
                            lvl1TypeStr, lvl1Name,
                            lvl2TypeStr, lvl2Name);
    }

    /// <summary>
    /// Adds an extended property directly to the database (database-level property).
    /// </summary>
    /// <param name="propertyName">The name of the extended property.</param>
    /// <param name="Property">The value of the extended property.</param>
    /// <returns><see langword="true"/> if the extended property was added successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="Property"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.AddExtendedProperty("DatabaseDescription", "Main application database");
    /// </code>
    /// </example>
    public bool AddExtendedProperty(string propertyName, string Property)
    {
        return AddExtendedProperty(propertyName, Property,
                                   ExtendedPropertyLevel0Type.Null, null,
                                   ExtendedPropertyLevel1Type.Null, null,
                                   ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Adds an extended property to a Level 0 database object (e.g., schema, user).
    /// </summary>
    /// <param name="propertyName">The name of the extended property.</param>
    /// <param name="Property">The value of the extended property.</param>
    /// <param name="level0Type">The Level 0 object type.</param>
    /// <param name="level0Name">The Level 0 object name.</param>
    /// <returns><see langword="true"/> if the extended property was added successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="Property"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.AddExtendedProperty("Description", "User schema for application", 
    ///                                           ExtendedPropertyLevel0Type.Schema, "dbo");
    /// </code>
    /// </example>
    public bool AddExtendedProperty(string propertyName, 
                                    string Property,
                                    ExtendedPropertyLevel0Type level0Type, string level0Name)
    {
        return AddExtendedProperty(propertyName, Property,
                                   level0Type, level0Name,
                                   ExtendedPropertyLevel1Type.Null, null,
                                   ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Adds an extended property to a Level 1 database object (e.g., table, view, procedure).
    /// </summary>
    /// <param name="propertyName">The name of the extended property.</param>
    /// <param name="Property">The value of the extended property.</param>
    /// <param name="level0Type">The Level 0 object type (typically Schema).</param>
    /// <param name="level0Name">The Level 0 object name (typically "dbo").</param>
    /// <param name="level1Type">The Level 1 object type.</param>
    /// <param name="level1Name">The Level 1 object name.</param>
    /// <returns><see langword="true"/> if the extended property was added successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="Property"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.AddExtendedProperty("Description", "Customer information table", 
    ///                                           ExtendedPropertyLevel0Type.Schema, "dbo",
    ///                                           ExtendedPropertyLevel1Type.Table, "Customers");
    /// </code>
    /// </example>
    public bool AddExtendedProperty(string propertyName, 
                                    string Property,
                                    ExtendedPropertyLevel0Type level0Type, string level0Name,
                                    ExtendedPropertyLevel1Type level1Type, string level1Name)
    {
        return AddExtendedProperty(propertyName, Property,
                                   level0Type, level0Name,
                                   level1Type, level1Name,
                                   ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Adds an extended property to a table using the default dbo schema.
    /// </summary>
    /// <param name="propertyName">The name of the extended property.</param>
    /// <param name="Property">The value of the extended property.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns><see langword="true"/> if the extended property was added successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="Property"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.AddExtendedPropertyToTable("Description", "Customer information table", "Customers");
    /// </code>
    /// </example>
    public bool AddExtendedPropertyToTable(string propertyName, string Property, string tableName)
    {
        return AddExtendedProperty(propertyName, Property,
                                   ExtendedPropertyLevel0Type.Schema, "dbo",
                                   ExtendedPropertyLevel1Type.Table, tableName);
    }

    /// <summary>
    /// Adds an extended property to a table column using the default dbo schema.
    /// </summary>
    /// <param name="propertyName">The name of the extended property.</param>
    /// <param name="Property">The value of the extended property.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <returns><see langword="true"/> if the extended property was added successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="Property"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.AddExtendedPropertyToColumn("Description", "Customer unique identifier", "Customers", "CustomerID");
    /// </code>
    /// </example>
    public bool AddExtendedPropertyToColumn(string propertyName, string Property, string tableName, string columnName)
    {
        return AddExtendedProperty(propertyName, Property,
                                   ExtendedPropertyLevel0Type.Schema, "dbo",
                                   ExtendedPropertyLevel1Type.Table, tableName,
                                   ExtendedPropertyLevel2Type.Column, columnName);
    }

    /// <summary>
    /// Sets an extended property value on a database object with all hierarchy levels specified.
    /// If the property already exists, it will be updated; otherwise, it will be added.
    /// </summary>
    /// <param name="propertyName">The name of the extended property.</param>
    /// <param name="propertyValue">The value of the extended property.</param>
    /// <param name="level0Type">The Level 0 object type.</param>
    /// <param name="level0Name">The Level 0 object name.</param>
    /// <param name="level1Type">The Level 1 object type.</param>
    /// <param name="level1Name">The Level 1 object name.</param>
    /// <param name="level2Type">The Level 2 object type.</param>
    /// <param name="level2Name">The Level 2 object name.</param>
    /// <returns><see langword="true"/> if the extended property was set successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="propertyValue"/> is <see langword="null"/>.</exception>
    public bool SetExtendedProperty(string propertyName,
                                         string propertyValue,
                                         ExtendedPropertyLevel0Type level0Type, string? level0Name,
                                         ExtendedPropertyLevel1Type level1Type, string? level1Name,
                                         ExtendedPropertyLevel2Type level2Type, string? level2Name)
    {
        if (DbType != DataBaseType.SQLServer)
            throw new NotSupportedException("Extended properties are only supported for SQL Server databases.");

        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentNullException(nameof(propertyName));
        if (propertyValue == null)
            throw new ArgumentNullException(nameof(propertyValue));

        // Check if the property already exists
        var existingValue = GetExtendedProperty(propertyName, level0Type, level0Name, level1Type, level1Name, level2Type, level2Name);

        // If property exists, update it; otherwise, add it
        if (existingValue != null)
            return UpdateExtendedProperty(propertyName, propertyValue, level0Type, level0Name, level1Type, level1Name, level2Type, level2Name);
        else
            return AddExtendedProperty(propertyName, propertyValue, level0Type, level0Name, level1Type, level1Name, level2Type, level2Name);
    }

    /// <summary>
    /// Sets an extended property value directly on the database (database-level property).
    /// If the property already exists, it will be updated; otherwise, it will be added.
    /// </summary>
    /// <param name="propertyName">The name of the extended property.</param>
    /// <param name="Property">The value of the extended property.</param>
    /// <returns><see langword="true"/> if the extended property was set successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="Property"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.SetExtendedProperty("DatabaseDescription", "Main application database");
    /// </code>
    /// </example>
    public bool SetExtendedProperty(string propertyName, string Property)
    {
        return SetExtendedProperty(propertyName, Property,
                                        ExtendedPropertyLevel0Type.Null, null,
                                        ExtendedPropertyLevel1Type.Null, null,
                                        ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Sets an extended property value on a Level 0 database object (e.g., schema, user).
    /// If the property already exists, it will be updated; otherwise, it will be added.
    /// </summary>
    /// <param name="propertyName">The name of the extended property.</param>
    /// <param name="Property">The value of the extended property.</param>
    /// <param name="level0Type">The Level 0 object type.</param>
    /// <param name="level0Name">The Level 0 object name.</param>
    /// <returns><see langword="true"/> if the extended property was set successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="Property"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.SetExtendedProperty("Description", "User schema for application", 
    ///                                                 ExtendedPropertyLevel0Type.Schema, "dbo");
    /// </code>
    /// </example>
    public bool SetExtendedProperty(string propertyName,
                                         string Property,
                                         ExtendedPropertyLevel0Type level0Type, string level0Name)
    {
        return SetExtendedProperty(propertyName, Property,
                                        level0Type, level0Name,
                                        ExtendedPropertyLevel1Type.Null, null,
                                        ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Sets an extended property value on a Level 1 database object (e.g., table, view, procedure).
    /// If the property already exists, it will be updated; otherwise, it will be added.
    /// </summary>
    /// <param name="propertyName">The name of the extended property.</param>
    /// <param name="Property">The value of the extended property.</param>
    /// <param name="level0Type">The Level 0 object type (typically Schema).</param>
    /// <param name="level0Name">The Level 0 object name (typically "dbo").</param>
    /// <param name="level1Type">The Level 1 object type.</param>
    /// <param name="level1Name">The Level 1 object name.</param>
    /// <returns><see langword="true"/> if the extended property was set successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="Property"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.SetExtendedProperty("Description", "Customer information table", 
    ///                                                 ExtendedPropertyLevel0Type.Schema, "dbo",
    ///                                                 ExtendedPropertyLevel1Type.Table, "Customers");
    /// </code>
    /// </example>
    public bool SetExtendedProperty(string propertyName,
                                         string Property,
                                         ExtendedPropertyLevel0Type level0Type, string level0Name,
                                         ExtendedPropertyLevel1Type level1Type, string level1Name)
    {
        return SetExtendedProperty(propertyName, Property,
                                        level0Type, level0Name,
                                        level1Type, level1Name,
                                        ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Sets an extended property value on a table using the default dbo schema.
    /// If the property already exists, it will be updated; otherwise, it will be added.
    /// </summary>
    /// <param name="propertyName">The name of the extended property.</param>
    /// <param name="Property">The value of the extended property.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns><see langword="true"/> if the extended property was set successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="Property"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.SetExtendedPropertyOnTable("Description", "Customer information table", "Customers");
    /// </code>
    /// </example>
    public bool SetExtendedPropertyOnTable(string propertyName, string Property, string tableName)
    {
        return SetExtendedProperty(propertyName, Property,
                                        ExtendedPropertyLevel0Type.Schema, "dbo",
                                        ExtendedPropertyLevel1Type.Table, tableName);
    }

    /// <summary>
    /// Sets an extended property value on a table column using the default dbo schema.
    /// If the property already exists, it will be updated; otherwise, it will be added.
    /// </summary>
    /// <param name="propertyName">The name of the extended property.</param>
    /// <param name="Property">The value of the extended property.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <returns><see langword="true"/> if the extended property was set successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="Property"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.SetExtendedPropertyOnColumn("Description", "Customer unique identifier", "Customers", "CustomerID");
    /// </code>
    /// </example>
    public bool SetExtendedPropertyOnColumn(string propertyName, string Property, string tableName, string columnName)
    {
        return SetExtendedProperty(propertyName, Property,
                                        ExtendedPropertyLevel0Type.Schema, "dbo",
                                        ExtendedPropertyLevel1Type.Table, tableName,
                                        ExtendedPropertyLevel2Type.Column, columnName);
    }

    /// <summary>
             /// Deletes an extended property from a database object with all hierarchy levels specified.
             /// </summary>
             /// <param name="propertyName">The name of the extended property to delete.</param>
             /// <param name="level0Type">The Level 0 object type.</param>
             /// <param name="level0Name">The Level 0 object name.</param>
             /// <param name="level1Type">The Level 1 object type.</param>
             /// <param name="level1Name">The Level 1 object name.</param>
             /// <param name="level2Type">The Level 2 object type.</param>
             /// <param name="level2Name">The Level 2 object name.</param>
             /// <returns><see langword="true"/> if the extended property was deleted successfully; otherwise, <see langword="false"/>.</returns>
             /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty.</exception>
    public bool DeleteExtendedProperty(string propertyName,
                                       ExtendedPropertyLevel0Type level0Type, string? level0Name,
                                       ExtendedPropertyLevel1Type level1Type, string? level1Name,
                                       ExtendedPropertyLevel2Type level2Type, string? level2Name)
    {
        if (DbType != DataBaseType.SQLServer)
            throw new NotSupportedException("Extended properties are only supported for SQL Server databases.");

        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentNullException(nameof(propertyName));

        var (lvl0TypeStr, lvl0Name, lvl1TypeStr, lvl1Name, lvl2TypeStr, lvl2Name) = PrepareLevels(level0Type, level0Name, level1Type, level1Name, level2Type, level2Name);

        var sql = "EXEC sp_dropextendedproperty " +
                      "@name       = @PropertyName, " +
                      "@level0type = @Level0Type,   @level0name = @Level0Name, " +
                      "@level1type = @Level1Type,   @level1name = @Level1Name, " +
                      "@level2type = @Level2Type,   @level2name = @Level2Name";

        return ExecuteQuery(sql,
                            propertyName, 
                            lvl0TypeStr, lvl0Name,
                            lvl1TypeStr, lvl1Name,
                            lvl2TypeStr, lvl2Name);
    }

    /// <summary>
    /// Deletes an extended property directly from the database (database-level property).
    /// </summary>
    /// <param name="propertyName">The name of the extended property to delete.</param>
    /// <returns><see langword="true"/> if the extended property was deleted successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.DeleteExtendedProperty("DatabaseDescription");
    /// </code>
    /// </example>
    public bool DeleteExtendedProperty(string propertyName)
    {
        return DeleteExtendedProperty(propertyName,
                                      ExtendedPropertyLevel0Type.Null, null,
                                      ExtendedPropertyLevel1Type.Null, null,
                                      ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Deletes an extended property from a Level 0 database object (e.g., schema, user).
    /// </summary>
    /// <param name="propertyName">The name of the extended property to delete.</param>
    /// <param name="level0Type">The Level 0 object type.</param>
    /// <param name="level0Name">The Level 0 object name.</param>
    /// <returns><see langword="true"/> if the extended property was deleted successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.DeleteExtendedProperty("Description", 
    ///                                               ExtendedPropertyLevel0Type.Schema, "dbo");
    /// </code>
    /// </example>
    public bool DeleteExtendedProperty(string propertyName,
                                       ExtendedPropertyLevel0Type level0Type, string level0Name)
    {
        return DeleteExtendedProperty(propertyName,
                                      level0Type, level0Name,
                                      ExtendedPropertyLevel1Type.Null, null,
                                      ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Deletes an extended property from a Level 1 database object (e.g., table, view, procedure).
    /// </summary>
    /// <param name="propertyName">The name of the extended property to delete.</param>
    /// <param name="level0Type">The Level 0 object type (typically Schema).</param>
    /// <param name="level0Name">The Level 0 object name (typically "dbo").</param>
    /// <param name="level1Type">The Level 1 object type.</param>
    /// <param name="level1Name">The Level 1 object name.</param>
    /// <returns><see langword="true"/> if the extended property was deleted successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.DeleteExtendedProperty("Description", 
    ///                                               ExtendedPropertyLevel0Type.Schema, "dbo",
    ///                                               ExtendedPropertyLevel1Type.Table, "Customers");
    /// </code>
    /// </example>
    public bool DeleteExtendedProperty(string propertyName,
                                       ExtendedPropertyLevel0Type level0Type, string level0Name,
                                       ExtendedPropertyLevel1Type level1Type, string level1Name)
    {
        return DeleteExtendedProperty(propertyName,
                                      level0Type, level0Name,
                                      level1Type, level1Name,
                                      ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Deletes an extended property from a table using the default dbo schema.
    /// </summary>
    /// <param name="propertyName">The name of the extended property to delete.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns><see langword="true"/> if the extended property was deleted successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.DeleteExtendedPropertyFromTable("Description", "Customers");
    /// </code>
    /// </example>
    public bool DeleteExtendedPropertyFromTable(string propertyName, string tableName)
    {
        return DeleteExtendedProperty(propertyName,
                                      ExtendedPropertyLevel0Type.Schema, "dbo",
                                      ExtendedPropertyLevel1Type.Table, tableName);
    }

    /// <summary>
    /// Deletes an extended property from a table column using the default dbo schema.
    /// </summary>
    /// <param name="propertyName">The name of the extended property to delete.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <returns><see langword="true"/> if the extended property was deleted successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.DeleteExtendedPropertyFromColumn("Description", "Customers", "CustomerID");
    /// </code>
    /// </example>
    public bool DeleteExtendedPropertyFromColumn(string propertyName, string tableName, string columnName)
    {
        return DeleteExtendedProperty(propertyName,
                                      ExtendedPropertyLevel0Type.Schema, "dbo",
                                      ExtendedPropertyLevel1Type.Table, tableName,
                                      ExtendedPropertyLevel2Type.Column, columnName);
    }

    /// <summary>
    /// Gets the value of an extended property from a database object with all hierarchy levels specified.
    /// </summary>
    /// <param name="propertyName">The name of the extended property to retrieve.</param>
    /// <param name="level0Type">The Level 0 object type.</param>
    /// <param name="level0Name">The Level 0 object name.</param>
    /// <param name="level1Type">The Level 1 object type.</param>
    /// <param name="level1Name">The Level 1 object name.</param>
    /// <param name="level2Type">The Level 2 object type.</param>
    /// <param name="level2Name">The Level 2 object name.</param>
    /// <returns>The value of the extended property, or <see langword="null"/> if the property is not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty.</exception>
    public string? GetExtendedProperty(string propertyName,
                                           ExtendedPropertyLevel0Type level0Type, string? level0Name,
                                           ExtendedPropertyLevel1Type level1Type, string? level1Name,
                                           ExtendedPropertyLevel2Type level2Type, string? level2Name)
    {
        if (DbType != DataBaseType.SQLServer)
            throw new NotSupportedException("Extended properties are only supported for SQL Server databases.");

        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentNullException(nameof(propertyName));

        var (lvl0TypeStr, lvl0Name, lvl1TypeStr, lvl1Name, lvl2TypeStr, lvl2Name) = PrepareLevels(level0Type, level0Name, level1Type, level1Name, level2Type, level2Name);

        var sql = "SELECT value FROM ::fn_listextendedproperty(" +
                      "@PropertyName, " +
                      "@Level0Type, @Level0Name, " +
                      "@Level1Type, @Level1Name, " +
                      "@Level2Type, @Level2Name)";

        var results = GetRecord(sql,
                                propertyName,
                                lvl0TypeStr, lvl0Name,
                                lvl1TypeStr, lvl1Name,
                                lvl2TypeStr, lvl2Name);
        if (results == null)
            return null;
        return results["value"]?.ToString();
    }

    /// <summary>
    /// Gets the value of an extended property directly from the database (database-level property).
    /// </summary>
    /// <param name="propertyName">The name of the extended property to retrieve.</param>
    /// <returns>The value of the extended property, or <see langword="null"/> if the property is not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// string description = dbAccess.GetExtendedProperty("DatabaseDescription");
    /// </code>
    /// </example>
    public string? GetExtendedProperty(string propertyName)
    {
        return GetExtendedProperty(propertyName,
                                        ExtendedPropertyLevel0Type.Null, null,
                                        ExtendedPropertyLevel1Type.Null, null,
                                        ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Gets the value of an extended property from a Level 0 database object (e.g., schema, user).
    /// </summary>
    /// <param name="propertyName">The name of the extended property to retrieve.</param>
    /// <param name="level0Type">The Level 0 object type.</param>
    /// <param name="level0Name">The Level 0 object name.</param>
    /// <returns>The value of the extended property, or <see langword="null"/> if the property is not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// string description = dbAccess.GetExtendedProperty("Description", 
    ///                                                       ExtendedPropertyLevel0Type.Schema, "dbo");
    /// </code>
    /// </example>
    public string? GetExtendedProperty(string propertyName,
                                           ExtendedPropertyLevel0Type level0Type, string level0Name)
    {
        return GetExtendedProperty(propertyName,
                                        level0Type, level0Name,
                                        ExtendedPropertyLevel1Type.Null, null,
                                        ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Gets the value of an extended property from a Level 1 database object (e.g., table, view, procedure).
    /// </summary>
    /// <param name="propertyName">The name of the extended property to retrieve.</param>
    /// <param name="level0Type">The Level 0 object type (typically Schema).</param>
    /// <param name="level0Name">The Level 0 object name (typically "dbo").</param>
    /// <param name="level1Type">The Level 1 object type.</param>
    /// <param name="level1Name">The Level 1 object name.</param>
    /// <returns>The value of the extended property, or <see langword="null"/> if the property is not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="Property"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// string description = dbAccess.GetExtendedProperty("Description", 
    ///                                                       ExtendedPropertyLevel0Type.Schema, "dbo",
    ///                                                       ExtendedPropertyLevel1Type.Table, "Customers");
    /// </code>
    /// </example>
    public string? GetExtendedProperty(string propertyName,
                                           ExtendedPropertyLevel0Type level0Type, string level0Name,
                                           ExtendedPropertyLevel1Type level1Type, string level1Name)
    {
        return GetExtendedProperty(propertyName,
                                        level0Type, level0Name,
                                        level1Type, level1Name,
                                        ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Gets the value of an extended property from a table using the default dbo schema.
    /// </summary>
    /// <param name="propertyName">The name of the extended property to retrieve.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>The value of the extended property, or <see langword="null"/> if the property is not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// string description = dbAccess.GetExtendedPropertyFromTable("Description", "Customers");
    /// </code>
    /// </example>
    public string? GetExtendedPropertyFromTable(string propertyName, string tableName)
    {
        return GetExtendedProperty(propertyName,
                                        ExtendedPropertyLevel0Type.Schema, "dbo",
                                        ExtendedPropertyLevel1Type.Table, tableName);
    }

    /// <summary>
    /// Gets the value of an extended property from a table column using the default dbo schema.
    /// </summary>
    /// <param name="propertyName">The name of the extended property to retrieve.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <returns>The value of the extended property, or <see langword="null"/> if the property is not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// string description = dbAccess.GetExtendedPropertyFromColumn("Description", "Customers", "CustomerID");
    /// </code>
    /// </example>
    public string? GetExtendedPropertyFromColumn(string propertyName, string tableName, string columnName)
    {
        return GetExtendedProperty(propertyName,
                                        ExtendedPropertyLevel0Type.Schema, "dbo",
                                        ExtendedPropertyLevel1Type.Table, tableName,
                                        ExtendedPropertyLevel2Type.Column, columnName);
    }

    /// <summary>
    /// Updates an existing extended property value on a database object with all hierarchy levels specified.
    /// </summary>
    /// <param name="propertyName">The name of the extended property to update.</param>
    /// <param name="newProperty">The new value for the extended property.</param>
    /// <param name="level0Type">The Level 0 object type.</param>
    /// <param name="level0Name">The Level 0 object name.</param>
    /// <param name="level1Type">The Level 1 object type.</param>
    /// <param name="level1Name">The Level 1 object name.</param>
    /// <param name="level2Type">The Level 2 object type.</param>
    /// <param name="level2Name">The Level 2 object name.</param>
    /// <returns><see langword="true"/> if the extended property was updated successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="newProperty"/> is <see langword="null"/>.</exception>
    public bool UpdateExtendedProperty(string propertyName, 
                                       string newProperty,
                                       ExtendedPropertyLevel0Type level0Type, string? level0Name,
                                       ExtendedPropertyLevel1Type level1Type, string? level1Name,
                                       ExtendedPropertyLevel2Type level2Type, string? level2Name)
    {
        if (DbType != DataBaseType.SQLServer)
            throw new NotSupportedException("Extended properties are only supported for SQL Server databases.");

        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentNullException(nameof(propertyName));
        if (newProperty == null)
            throw new ArgumentNullException(nameof(newProperty));

        var (lvl0TypeStr, lvl0Name, lvl1TypeStr, lvl1Name, lvl2TypeStr, lvl2Name) = PrepareLevels(level0Type, level0Name, level1Type, level1Name, level2Type, level2Name);

        var sql = "EXEC sp_updateextendedproperty " +
                      "@name       = @PropertyName, @value      = @Property, " +
                      "@level0type = @Level0Type,   @level0name = @Level0Name, " +
                      "@level1type = @Level1Type,   @level1name = @Level1Name, " +
                      "@level2type = @Level2Type,   @level2name = @Level2Name";

        return ExecuteQuery(sql, 
                            propertyName, newProperty,
                            lvl0TypeStr, lvl0Name,
                            lvl1TypeStr, lvl1Name,
                            lvl2TypeStr, lvl2Name);
    }

    /// <summary>
    /// Updates an existing extended property value directly on the database (database-level property).
    /// </summary>
    /// <param name="propertyName">The name of the extended property to update.</param>
    /// <param name="newProperty">The new value for the extended property.</param>
    /// <returns><see langword="true"/> if the extended property was updated successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="newProperty"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.UpdateExtendedProperty("DatabaseDescription", "Updated main application database");
    /// </code>
    /// </example>
    public bool UpdateExtendedProperty(string propertyName, string newProperty)
    {
        return UpdateExtendedProperty(propertyName, newProperty,
                                      ExtendedPropertyLevel0Type.Null, null,
                                      ExtendedPropertyLevel1Type.Null, null,
                                      ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Updates an existing extended property value on a Level 0 database object (e.g., schema, user).
    /// </summary>
    /// <param name="propertyName">The name of the extended property to update.</param>
    /// <param name="newProperty">The new value for the extended property.</param>
    /// <param name="level0Type">The Level 0 object type.</param>
    /// <param name="level0Name">The Level 0 object name.</param>
    /// <returns><see langword="true"/> if the extended property was updated successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="newProperty"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.UpdateExtendedProperty("Description", "Updated schema description", 
    ///                                               ExtendedPropertyLevel0Type.Schema, "dbo");
    /// </code>
    /// </example>
    public bool UpdateExtendedProperty(string propertyName, 
                                       string newProperty,
                                       ExtendedPropertyLevel0Type level0Type, string level0Name)
    {
        return UpdateExtendedProperty(propertyName, newProperty,
                                      level0Type, level0Name,
                                      ExtendedPropertyLevel1Type.Null, null,
                                      ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Updates an existing extended property value on a Level 1 database object (e.g., table, view, procedure).
    /// </summary>
    /// <param name="propertyName">The name of the extended property to update.</param>
    /// <param name="newProperty">The new value for the extended property.</param>
    /// <param name="level0Type">The Level 0 object type (typically Schema).</param>
    /// <param name="level0Name">The Level 0 object name (typically "dbo").</param>
    /// <param name="level1Type">The Level 1 object type.</param>
    /// <param name="level1Name">The Level 1 object name.</param>
    /// <returns><see langword="true"/> if the extended property was updated successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="newProperty"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.UpdateExtendedProperty("Description", "Updated customer information table", 
    ///                                               ExtendedPropertyLevel0Type.Schema, "dbo",
    ///                                               ExtendedPropertyLevel1Type.Table, "Customers");
    /// </code>
    /// </example>
    public bool UpdateExtendedProperty(string propertyName, 
                                       string newProperty,
                                       ExtendedPropertyLevel0Type level0Type, string level0Name,
                                       ExtendedPropertyLevel1Type level1Type, string level1Name)
    {
        return UpdateExtendedProperty(propertyName, newProperty,
                                      level0Type, level0Name,
                                      level1Type, level1Name,
                                      ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Updates an existing extended property value on a table using the default dbo schema.
    /// </summary>
    /// <param name="propertyName">The name of the extended property to update.</param>
    /// <param name="newProperty">The new value for the extended property.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <returns><see langword="true"/> if the extended property was updated successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="newProperty"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.UpdateExtendedPropertyOnTable("Description", "Updated customer information table", "Customers");
    /// </code>
    /// </example>
    public bool UpdateExtendedPropertyOnTable(string propertyName, string newProperty, string tableName)
    {
        return UpdateExtendedProperty(propertyName, newProperty,
                                      ExtendedPropertyLevel0Type.Schema, "dbo",
                                      ExtendedPropertyLevel1Type.Table, tableName);
    }

    /// <summary>
    /// Updates an existing extended property value on a table column using the default dbo schema.
    /// </summary>
    /// <param name="propertyName">The name of the extended property to update.</param>
    /// <param name="newProperty">The new value for the extended property.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <returns><see langword="true"/> if the extended property was updated successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/> or empty, or when <paramref name="newProperty"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// bool success = dbAccess.UpdateExtendedPropertyOnColumn("Description", "Updated customer unique identifier", "Customers", "CustomerID");
    /// </code>
    /// </example>
    public bool UpdateExtendedPropertyOnColumn(string propertyName, string newProperty, string tableName, string columnName)
    {
        return UpdateExtendedProperty(propertyName, newProperty,
                                      ExtendedPropertyLevel0Type.Schema, "dbo",
                                      ExtendedPropertyLevel1Type.Table, tableName,
                                      ExtendedPropertyLevel2Type.Column, columnName);
    }

    /// <summary>
    /// Gets all extended properties for a specific Level 0 database object (e.g., schema, user).
    /// </summary>
    /// <param name="level0Type">The Level 0 object type.</param>
    /// <param name="level0Name">The Level 0 object name.</param>
    /// <returns>A list of ExtendedProperty objects for the specified object, or <see langword="null"/> if an error occurs.</returns>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// var properties = dbAccess.GetExtendedProperties(ExtendedPropertyLevel0Type.Schema, "dbo");
    /// Console.WriteLine($"Found {properties.Count} extended properties for the dbo schema.");
    /// </code>
    /// </example>
    public IList<ExtendedProperty> GetExtendedProperties(ExtendedPropertyLevel0Type level0Type, string level0Name)
    {
        return GetExtendedProperties(level0Type, level0Name,
                                     ExtendedPropertyLevel1Type.Null, null,
                                     ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Gets all extended properties for a specific Level 1 database object (e.g., table, view, procedure).
    /// </summary>
    /// <param name="level0Type">The Level 0 object type (typically Schema).</param>
    /// <param name="level0Name">The Level 0 object name (typically "dbo").</param>
    /// <param name="level1Type">The Level 1 object type.</param>
    /// <param name="level1Name">The Level 1 object name.</param>
    /// <returns>A list of ExtendedProperty objects for the specified object, or <see langword="null"/> if an error occurs.</returns>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// var properties = dbAccess.GetExtendedProperties(ExtendedPropertyLevel0Type.Schema, "dbo",
    ///                                                ExtendedPropertyLevel1Type.Table, "Customers");
    /// Console.WriteLine($"Found {properties.Count} extended properties for the Customers table.");
    /// </code>
    /// </example>
    public IList<ExtendedProperty> GetExtendedProperties(ExtendedPropertyLevel0Type level0Type, string level0Name,
                                                         ExtendedPropertyLevel1Type level1Type, string level1Name)
    {
        return GetExtendedProperties(level0Type, level0Name,
                                   level1Type, level1Name,
                                   ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Gets all extended properties for a specific database object with all hierarchy levels specified.
    /// </summary>
    /// <param name="level0Type">The Level 0 object type.</param>
    /// <param name="level0Name">The Level 0 object name.</param>
    /// <param name="level1Type">The Level 1 object type.</param>
    /// <param name="level1Name">The Level 1 object name.</param>
    /// <param name="level2Type">The Level 2 object type.</param>
    /// <param name="level2Name">The Level 2 object name.</param>
    /// <returns>A list of ExtendedProperty objects for the specified object, or <see langword="null"/> if an error occurs.</returns>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// var properties = dbAccess.GetExtendedProperties(ExtendedPropertyLevel0Type.Schema, "dbo",
    ///                                                ExtendedPropertyLevel1Type.Table, "Customers",
    ///                                                ExtendedPropertyLevel2Type.Column, "CustomerID");
    /// Console.WriteLine($"Found {properties.Count} extended properties for the CustomerID column.");
    /// </code>
    /// </example>
    public IList<ExtendedProperty> GetExtendedProperties(ExtendedPropertyLevel0Type level0Type, string? level0Name,
                                                         ExtendedPropertyLevel1Type level1Type, string? level1Name,
                                                         ExtendedPropertyLevel2Type level2Type, string? level2Name)
    {
        if (DbType != DataBaseType.SQLServer)
            throw new NotSupportedException("Extended properties are only supported for SQL Server databases.");

        try
        {
            var (lvl0TypeStr, lvl0Name, lvl1TypeStr, lvl1Name, lvl2TypeStr, lvl2Name) = PrepareLevels(level0Type, level0Name, level1Type, level1Name, level2Type, level2Name);

            var sql = "SELECT objtype, objname, name, value FROM ::fn_listextendedproperty(" +
                          "NULL, " +
                          "@Level0Type, @Level0Name, " +
                          "@Level1Type, @Level1Name, " +
                          "@Level2Type, @Level2Name)";

            var results = GetRecords(sql,
                                    lvl0TypeStr, lvl0Name,
                                    lvl1TypeStr, lvl1Name,
                                    lvl2TypeStr, lvl2Name);
            if (results == null)
                return [];

            var properties = new List<ExtendedProperty>();
            foreach (DataRow row in results)
            {
                var parts = row["objname"]?.ToString()?.Split('.');
                var property = new ExtendedProperty
                {
                    Name = row["name"]?.ToString(),
                    Value = row["value"]?.ToString(),
                    Level0Name = parts?.Length > 0 ? parts[0] : null,
                    Level1Name = parts?.Length > 1 ? parts[1] : null,
                    Level2Name = parts?.Length > 2 ? parts[2] : null,
                    Level0Type = parts?.Length == 1 ? row["objtype"]?.ToString() : null,
                    Level1Type = parts?.Length == 2 ? row["objtype"]?.ToString() : null,
                    Level2Type = parts?.Length == 3 ? row["objtype"]?.ToString() : null,
                };

                properties.Add(property);
            }

            return properties;
        }
        catch (Exception ex)
        {
            LastException = ex;
            return [];
        }
    }

    /// <summary>
    /// Gets all extended properties directly from the database (database-level properties).
    /// </summary>
    /// <returns>A list of ExtendedProperty objects for database-level properties, or <see langword="null"/> if an error occurs.</returns>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// var properties = dbAccess.GetDatabaseExtendedProperties();
    /// Console.WriteLine($"Found {properties.Count} database-level extended properties.");
    /// </code>
    /// </example>
    public IList<ExtendedProperty> GetDatabaseExtendedProperties()
    {
        return GetExtendedProperties(ExtendedPropertyLevel0Type.Null, null,
                                     ExtendedPropertyLevel1Type.Null, null,
                                     ExtendedPropertyLevel2Type.Null, null);
    }

    /// <summary>
    /// Gets all extended properties for a table using the default dbo schema.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <returns>A list of ExtendedProperty objects for the specified table, or <see langword="null"/> if an error occurs.</returns>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// var properties = dbAccess.GetExtendedPropertiesFromTable("Customers");
    /// Console.WriteLine($"Found {properties.Count} extended properties for the Customers table.");
    /// </code>
    /// </example>
    public IList<ExtendedProperty> GetExtendedPropertiesFromTable(string tableName)
    {
        return GetExtendedProperties(ExtendedPropertyLevel0Type.Schema, "dbo",
                                     ExtendedPropertyLevel1Type.Table, tableName);
    }

    /// <summary>
    /// Gets all extended properties for a table column using the default dbo schema.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <returns>A list of ExtendedProperty objects for the specified column, or <see langword="null"/> if an error occurs.</returns>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// var properties = dbAccess.GetExtendedPropertiesFromColumn("Customers", "CustomerID");
    /// Console.WriteLine($"Found {properties.Count} extended properties for the CustomerID column.");
    /// </code>
    /// </example>
    public IList<ExtendedProperty> GetExtendedPropertiesFromColumn(string tableName, string columnName)
    {
        return GetExtendedProperties(ExtendedPropertyLevel0Type.Schema, "dbo",
                                     ExtendedPropertyLevel1Type.Table, tableName,
                                     ExtendedPropertyLevel2Type.Column, columnName);
    }

    /// <summary>
    /// Gets all extended properties in the entire database.
    /// </summary>
    /// <returns>A list of all ExtendedProperty objects in the database, or <see langword="null"/> if an error occurs.</returns>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// var allProperties = dbAccess.GetAllExtendedProperties();
    /// Console.WriteLine($"Found {allProperties.Count} extended properties in the database.");
    /// </code>
    /// </example>
    public IList<ExtendedProperty> GetAllExtendedProperties()
    => GetExtendedProperties(ExtendedPropertyLevel0Type.Null, null,
                             ExtendedPropertyLevel1Type.Null, null,
                             ExtendedPropertyLevel2Type.Null, null);

    #region Helper Methods

    /// <summary>
    /// Prepares the SQL string values for the extended property levels.
    /// </summary>
    /// <param name="level0Type">The Level 0 object type.</param>
    /// <param name="level0Name">The Level 0 object name.</param>
    /// <param name="level1Type">The Level 1 object type.</param>
    /// <param name="level1Name">The Level 1 object name.</param>
    /// <param name="level2Type">The Level 2 object type.</param>
    /// <param name="level2Name">The Level 2 object name.</param>
    /// <returns>A tuple containing the prepared SQL values.</returns>
    private (string? lvl0TypeStr, string? lvl0Name, string? lvl1TypeStr, string? lvl1Name, string? lvl2TypeStr, string? lvl2Name) 
    PrepareLevels(ExtendedPropertyLevel0Type level0Type, string? level0Name,
                  ExtendedPropertyLevel1Type level1Type, string? level1Name,
                  ExtendedPropertyLevel2Type level2Type, string? level2Name)
    {
        var lvl0TypeStr = GetEnumSqlValue(level0Type);
        var lvl1TypeStr = GetEnumSqlValue(level1Type);
        var lvl2TypeStr = GetEnumSqlValue(level2Type);
        level0Name = lvl0TypeStr == null ? null : string.IsNullOrWhiteSpace(level0Name) ? null : level0Name;
        level1Name = lvl1TypeStr == null ? null : string.IsNullOrWhiteSpace(level1Name) ? null : level1Name;
        level2Name = lvl2TypeStr == null ? null : string.IsNullOrWhiteSpace(level2Name) ? null : level2Name;
        return (lvl0TypeStr, level0Name, lvl1TypeStr, level1Name, lvl2TypeStr, level2Name);
    }

    /// <summary>
    /// Converts an enum value to its corresponding SQL Server string representation.
    /// </summary>
    /// <param name="enumValue">The enum value to convert.</param>
    /// <returns>The SQL Server string representation of the enum value.</returns>
    private string? GetEnumSqlValue(Enum enumValue)
    {
        var strValue = StringUtil.SplitPascalCase(enumValue.ToString()).ToUpperInvariant();
        if (strValue == "NULL")
            return null;
        return strValue;
    }

    #endregion

    #endregion
}
