using System;
using System.Collections.Generic;
using System.Data;

namespace ByteForge.Toolkit
{
    /*
     *  ___         _      _   ___                 _   _          ___             _ _   
     * / __| __ _ _(_)_ __| |_| __|_ _____ __ _  _| |_(_)___ _ _ | _ \___ ____  _| | |_ 
     * \__ \/ _| '_| | '_ \  _| _|\ \ / -_) _| || |  _| / _ \ ' \|   / -_)_-< || | |  _|
     * |___/\__|_| |_| .__/\__|___/_\_\___\__|\_,_|\__|_\___/_||_|_|_\___/__/\_,_|_|\__|
     *               |_|                                                                
     */
    /// <summary>
    /// Represents the comprehensive result of executing a SQL script, potentially containing multiple batches.
    /// </summary>
    /// <remarks>
    /// This class encapsulates all relevant information about the execution of a SQL script,
    /// including success status, result sets, affected record counts, and any exceptions that occurred.
    /// <para>
    /// SQL scripts can contain multiple batches (separated by GO statements in T-SQL), and this class
    /// tracks the results of each batch separately in the <see cref="BatchResults"/> and 
    /// <see cref="RecordsAffected"/> collections.
    /// </para>
    /// <para>
    /// This class is typically returned by the <see cref="DBAccess"/> methods that execute
    /// SQL scripts, such as <c>ExecuteScript</c> or <c>ExecuteScriptFile</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var dbAccess = new DBAccess();
    /// var script = @"
    ///     SELECT * FROM Customers WHERE Country = 'USA';
    ///     GO
    ///     UPDATE Products SET Price = Price * 1.1 WHERE Category = 'Electronics';
    ///     GO
    ///     SELECT * FROM Products WHERE Category = 'Electronics';
    /// ";
    /// 
    /// var result = dbAccess.ExecuteScript(script);
    /// 
    /// if (result.Success)
    /// {
    ///     // Access the first result set (USA customers)
    ///     if (result.ResultSets.Count > 0)
    ///     {
    ///         var usaCustomers = result.ResultSets[0];
    ///         Console.WriteLine($"Found {usaCustomers.Rows.Count} USA customers");
    ///     }
    ///     
    ///     // Check how many products were updated
    ///     if (result.RecordsAffected.Count > 1)
    ///     {
    ///         var updatedCount = result.RecordsAffected[1];
    ///         Console.WriteLine($"Updated {updatedCount} electronics products");
    ///     }
    ///     
    ///     // Access the second result set (updated electronics products)
    ///     if (result.ResultSets.Count > 1)
    ///     {
    ///         var updatedProducts = result.ResultSets[1];
    ///         Console.WriteLine($"Retrieved {updatedProducts.Rows.Count} updated products");
    ///     }
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Script execution failed: {result.LastException.Message}");
    /// }
    /// </code>
    /// </example>
    public class ScriptExecutionResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the script executed successfully.
        /// </summary>
        /// <remarks>
        /// A value of <see langword="true" /> indicates that all batches in the script executed without
        /// throwing exceptions. Note that this does not necessarily mean that the script
        /// accomplished its intended business logic - it only indicates that no SQL errors occurred.
        /// <para>
        /// When <see langword="false" />, the <see cref="LastException"/> property will contain
        /// information about the error that caused the failure.
        /// </para>
        /// </remarks>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the results of each batch executed in the script.
        /// </summary>
        /// <remarks>
        /// This collection contains the raw return value from each batch in the script.
        /// For <c>SELECT</c> statements, this is typically the number of rows returned.
        /// For <c>INSERT</c>, <c>UPDATE</c>, and <c>DELETE</c> statements, this is the
        /// number of rows affected.
        /// <para>
        /// The collection maintains the same order as the batches in the script.
        /// </para>
        /// <para>
        /// Note that these values may be redundant with information in <see cref="ResultSets"/>
        /// or <see cref="RecordsAffected"/>, depending on the type of statements executed.
        /// </para>
        /// </remarks>
        public List<object?> BatchResults { get; set; } = new List<object?>();

        /// <summary>
        /// Gets or sets the result sets returned by the script.
        /// </summary>
        /// <remarks>
        /// This collection contains <see cref="DataTable"/> objects representing the
        /// result sets returned by <c>SELECT</c> statements in the script.
        /// <para>
        /// The collection maintains the same order as the <c>SELECT</c> statements in the script.
        /// Note that only <c>SELECT</c> statements generate result sets, so this collection
        /// may have fewer items than <see cref="BatchResults"/> if the script contains
        /// other types of statements.
        /// </para>
        /// <para>
        /// Each <see cref="DataTable"/> contains the columns and rows returned by the
        /// corresponding <c>SELECT</c> statement.
        /// </para>
        /// </remarks>
        public List<DataTable> ResultSets { get; set; } = new List<DataTable>();

        /// <summary>
        /// Gets or sets the number of records affected by each batch in the script.
        /// </summary>
        /// <remarks>
        /// This collection contains the number of rows affected by each batch in the script.
        /// For <c>INSERT</c>, <c>UPDATE</c>, and <c>DELETE</c> statements, this is the
        /// number of rows that were modified by the operation.
        /// <para>
        /// The collection maintains the same order as the batches in the script.
        /// </para>
        /// <para>
        /// For <c>SELECT</c> statements, the corresponding value may be -1 or the number
        /// of rows returned, depending on the database provider.
        /// </para>
        /// </remarks>
        public List<int> RecordsAffected { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the last exception that occurred during script execution.
        /// </summary>
        /// <remarks>
        /// If <see cref="Success"/> is <see langword="false" />, this property contains the exception
        /// that caused the script execution to fail.
        /// <para>
        /// The exception typically includes information such as the error code, error message,
        /// and stack trace, which can be used to diagnose the cause of the failure.
        /// </para>
        /// <para>
        /// This property will be <c>null</c> if <see cref="Success"/> is <see langword="true" />.
        /// </para>
        /// </remarks>
        public Exception? LastException { get; set; }
    }
}