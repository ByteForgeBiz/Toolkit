using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ByteForge.Toolkit
{

    /*
     * The `ExecuteScript` and `ExecuteQuery` methods both execute SQL commands against the database, but they have some key differences:
     *
     * Similarities:
     * - Both methods open a database connection and execute SQL commands.
     * - Both methods handle exceptions and log errors.
     * - Both methods use a similar pattern for creating and executing commands.
     *
     * Differences:
     * - `ExecuteQuery` executes a single SQL query, while `ExecuteScript` can execute multiple batches of SQL commands separated by "GO".
     * - `ExecuteScript` can capture and return multiple result sets, while `ExecuteQuery` only returns the number of records affected.
     * - `ExecuteScript` has additional logic to handle DDL statements and parameter parsing, which `ExecuteQuery` does not.
     * - `ExecuteScript` returns a `ScriptExecutionResult` object containing detailed information about the execution, while `ExecuteQuery` returns a boolean indicating success or failure.
     */

    /*
     *  ___  ___   _                   
     * |   \| _ ) /_\  __ __ ___ ______
     * | |) | _ \/ _ \/ _/ _/ -_)_-<_-<
     * |___/|___/_/ \_\__\__\___/__/__/
     *                                 
     */
    public partial class DBAccess
    {
        /// <summary>
        /// Executes a SQL script.
        /// </summary>
        /// <param name="script">The SQL script to execute.</param>
        /// <param name="arguments">Optional. The arguments for the SQL script.</param>
        /// <param name="captureResults">Indicates whether to capture the results of the script execution.</param>
        /// <param name="caller">The name of the method that called this method.</param>
        /// <returns>The result of the script execution.</returns>
        /// <remarks>
        /// Use <c>ExecuteScript</c> when you need to execute a complex SQL script that may contain multiple batches of commands separated by "GO".
        /// This method is suitable for running DDL (Data Definition Language) statements, complex transactions, or scripts that require capturing multiple result sets.
        /// It returns a <see cref="ScriptExecutionResult"/> object containing detailed information about the execution, including success status, result sets, and any exceptions encountered.
        /// </remarks>
        public ScriptExecutionResult ExecuteScript(string script, object[] arguments = null, bool captureResults = false, [CallerMemberName] string caller = "")
        {
            var result = new ScriptExecutionResult();

            try
            {
                using (var dbConn = CreateConnection())
                {
                    dbConn.Open();
                    var batches = SplitIntoBatches(script);

                    foreach (var batch in batches)
                    {
                        if (string.IsNullOrWhiteSpace(batch)) continue;

                        using (var cmd = dbConn.CreateCommand())
                        {
                            cmd.CommandTimeout = 240;
                            cmd.CommandText = batch;

                            // Only process parameters for non-DDL statements
                            if (arguments != null && !IsDDLStatement(batch))
                                AddParameters(cmd, batch, arguments);

                            Log.Verbose($"Executing query:{Environment.NewLine}{batch}");
                            Log.Debug(string.Join(", ", cmd.Parameters.Cast<SqlParameter>().Select(p => $"{p.ParameterName} = '{p.Value}'").ToArray()));
                            var timeStart = DateTime.Now;

                            if (captureResults)
                            {
                                using (var reader = cmd.ExecuteReader())
                                {
                                    do
                                    {
                                        var dataTable = new DataTable();
                                        dataTable.Load(reader);
                                        if (dataTable.Rows.Count > 0)
                                            result.ResultSets.Add(dataTable);

                                        result.RecordsAffected.Add(reader.RecordsAffected);
                                    } while (!reader.IsClosed && reader.NextResult());

                                    if (result.ResultSets.Count > 0)
                                        result.BatchResults.Add(result.ResultSets[result.ResultSets.Count - 1].Rows[0][0]);
                                }
                            }
                            else
                            {
                                var affected = cmd.ExecuteNonQuery();
                                result.RecordsAffected.Add(affected);
                                result.BatchResults.Add(affected);
                            }

                            var duration = DateTime.Now - timeStart;
                            Log.Debug($"Query executed in {duration}.");
                            Log.Verbose($"Execution completed. Records affected: {result.RecordsAffected.Last()}");
                        }

                        result.Success = true;
                        LastException = null;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.LastException = ex;
                LastException = ex;
                Log.Error(ex, $"Error executing script in '{caller}'");
            }

            return result;
        }

        /// <summary>
        /// Determines if a batch is a DDL statement (CREATE, ALTER, DROP, etc.)
        /// </summary>
        /// <param name="sqlBatch">The SQL batch to check.</param>
        /// <returns>True if the batch is a DDL statement; otherwise, false.</returns>
        private static bool IsDDLStatement(string sqlBatch)
        {
            var normalizedBatch = sqlBatch.Trim().ToUpperInvariant();
            return new[]
            {
               "CREATE ", "ALTER ", "DROP ", "TRUNCATE ",
               "GRANT ", "DENY ", "REVOKE ",
               "BEGIN TRY", "BEGIN TRANSACTION",
               "DECLARE ", "SET "
           }.Any(ddl => normalizedBatch.StartsWith(ddl));
        }

        /// <summary>
        /// Splits a SQL script into individual batches based on the "GO" statement.
        /// </summary>
        /// <param name="script">The SQL script to split into batches.</param>
        /// <returns>An enumerable collection of SQL script batches.</returns>
        private static IEnumerable<string> SplitIntoBatches(string script)
        {
            var lines = script.Split(new[] { Environment.NewLine, "\r\n", "\n" }, StringSplitOptions.None);
            var currentBatch = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Skip comments
                if (trimmedLine.StartsWith("--")) continue;

                // Check if the line is just GO (case insensitive and allowing spaces)
                if (Regex.IsMatch(trimmedLine, @"^GO\s*$", RegexOptions.IgnoreCase))
                {
                    if (currentBatch.Length > 0)
                    {
                        yield return currentBatch.ToString();
                        currentBatch.Clear();
                    }
                }
                else
                    currentBatch.AppendLine(line);
            }

            // Return the last batch if there is one
            if (currentBatch.Length > 0)
                yield return currentBatch.ToString();
        }

        /// <summary>
        /// Parses the parameters from a SQL batch.
        /// </summary>
        /// <param name="sqlBatch">The SQL batch to parse for parameters.</param>
        /// <returns>A list of parameter names found in the SQL batch.</returns>
        private static List<string> ParseParameters(string sqlBatch)
        {
            var parameters = new List<string>();

            // Remove /* */ style comments
            var noBlockComments = Regex.Replace(sqlBatch, @"/\*.*?\*/", "", RegexOptions.Singleline);

            // Remove -- style comments
            var noComments = Regex.Replace(noBlockComments, @"--.+?$", "", RegexOptions.Multiline);

            // Find parameters (excluding those in string literals)
            var matches = Regex.Matches(noComments, @"(?<![a-zA-Z0-9_.])@[A-Za-z]\w*(?=(?:[^']*'[^']*')*[^']*$)");

            foreach (Match match in matches)
                if (!parameters.Contains(match.Value))
                    parameters.Add(match.Value);

            return parameters;
        }
    }
}