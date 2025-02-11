using System;
using System.Collections.Generic;
using System.Data;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Represents the result of executing a SQL script.
    /// </summary>
    public class ScriptExecutionResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the script executed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the results of each batch executed in the script.
        /// </summary>
        public List<object> BatchResults { get; set; } = new List<object>();

        /// <summary>
        /// Gets or sets the result sets returned by the script.
        /// </summary>
        public List<DataTable> ResultSets { get; set; } = new List<DataTable>();

        /// <summary>
        /// Gets or sets the number of records affected by each batch in the script.
        /// </summary>
        public List<int> RecordsAffected { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the last exception that occurred during script execution.
        /// </summary>
        public Exception LastException { get; set; }
    }
}