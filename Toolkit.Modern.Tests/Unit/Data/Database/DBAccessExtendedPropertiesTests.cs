using ByteForge.Toolkit.Tests.Helpers;
using AwesomeAssertions;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace ByteForge.Toolkit.Tests.Unit.Data.Database
{
    /// <summary>
    /// Unit tests for the Extended Properties functionality of the <see cref="DBAccess"/> class.
    /// </summary>
    /// <remarks>
    /// These tests focus on the SQL Server extended properties capabilities including
    /// CRUD operations, hierarchy management, and convenience methods for common scenarios.
    /// Tests require SQL Server and use the TestUnitDB database.
    /// </remarks>
    [TestClass]
    public class DBAccessExtendedPropertiesTests
    {
        #region Test Setup and Cleanup

        /// <summary>
        /// Verifies that the test database is properly configured before running tests.
        /// </summary>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Verify test database is accessible
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            DatabaseTestHelper.AssertTestDatabaseSetup(dbAccess);
            
            // Verify this is SQL Server (required for extended properties)
            if (dbAccess.DbType != DBAccess.DataBaseType.SQLServer)
                Assert.Inconclusive("Extended Properties tests require SQL Server database");
        }

        /// <summary>
        /// Cleans up test extended properties after each test.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            try
            {
                var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
                CleanupTestExtendedProperties(dbAccess);
            }
            catch (Exception)
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }

        #endregion

        #region Enumeration Tests

        /// <summary>
        /// Tests that ExtendedPropertyLevel0Type enumeration contains expected values.
        /// </summary>
        [TestMethod]
        public void ExtendedPropertyLevel0Type_Enumeration_ShouldContainExpectedValues()
        {
            // Assert
            var values = Enum.GetValues(typeof(DBAccess.ExtendedPropertyLevel0Type));
            values.Length.Should().BeGreaterThan(5, "should contain multiple Level 0 types");

            // Check for specific expected values
            Enum.IsDefined(typeof(DBAccess.ExtendedPropertyLevel0Type), DBAccess.ExtendedPropertyLevel0Type.Null).Should().BeTrue();
            Enum.IsDefined(typeof(DBAccess.ExtendedPropertyLevel0Type), DBAccess.ExtendedPropertyLevel0Type.Schema).Should().BeTrue();
            Enum.IsDefined(typeof(DBAccess.ExtendedPropertyLevel0Type), DBAccess.ExtendedPropertyLevel0Type.User).Should().BeTrue();
            Enum.IsDefined(typeof(DBAccess.ExtendedPropertyLevel0Type), DBAccess.ExtendedPropertyLevel0Type.Type).Should().BeTrue();
        }

        /// <summary>
        /// Tests that ExtendedPropertyLevel1Type enumeration contains expected values.
        /// </summary>
        [TestMethod]
        public void ExtendedPropertyLevel1Type_Enumeration_ShouldContainExpectedValues()
        {
            // Assert
            var values = Enum.GetValues(typeof(DBAccess.ExtendedPropertyLevel1Type));
            values.Length.Should().BeGreaterThan(5, "should contain multiple Level 1 types");

            // Check for specific expected values
            Enum.IsDefined(typeof(DBAccess.ExtendedPropertyLevel1Type), DBAccess.ExtendedPropertyLevel1Type.Null).Should().BeTrue();
            Enum.IsDefined(typeof(DBAccess.ExtendedPropertyLevel1Type), DBAccess.ExtendedPropertyLevel1Type.Table).Should().BeTrue();
            Enum.IsDefined(typeof(DBAccess.ExtendedPropertyLevel1Type), DBAccess.ExtendedPropertyLevel1Type.View).Should().BeTrue();
            Enum.IsDefined(typeof(DBAccess.ExtendedPropertyLevel1Type), DBAccess.ExtendedPropertyLevel1Type.Procedure).Should().BeTrue();
            Enum.IsDefined(typeof(DBAccess.ExtendedPropertyLevel1Type), DBAccess.ExtendedPropertyLevel1Type.Function).Should().BeTrue();
        }

        /// <summary>
        /// Tests that ExtendedPropertyLevel2Type enumeration contains expected values.
        /// </summary>
        [TestMethod]
        public void ExtendedPropertyLevel2Type_Enumeration_ShouldContainExpectedValues()
        {
            // Assert
            var values = Enum.GetValues(typeof(DBAccess.ExtendedPropertyLevel2Type));
            values.Length.Should().BeGreaterThan(3, "should contain multiple Level 2 types");

            // Check for specific expected values
            Enum.IsDefined(typeof(DBAccess.ExtendedPropertyLevel2Type), DBAccess.ExtendedPropertyLevel2Type.Null).Should().BeTrue();
            Enum.IsDefined(typeof(DBAccess.ExtendedPropertyLevel2Type), DBAccess.ExtendedPropertyLevel2Type.Column).Should().BeTrue();
            Enum.IsDefined(typeof(DBAccess.ExtendedPropertyLevel2Type), DBAccess.ExtendedPropertyLevel2Type.Parameter).Should().BeTrue();
            Enum.IsDefined(typeof(DBAccess.ExtendedPropertyLevel2Type), DBAccess.ExtendedPropertyLevel2Type.Index).Should().BeTrue();
        }

        #endregion

        #region ExtendedProperty Data Model Tests

        /// <summary>
        /// Tests that ExtendedProperty class can be instantiated and properties set.
        /// </summary>
        [TestMethod]
        public void ExtendedProperty_Constructor_ShouldCreateValidInstance()
        {
            // Act
            var property = new DBAccess.ExtendedProperty();

            // Assert
            property.Should().NotBeNull();
            property.Name.Should().BeNull();
            property.Value.Should().BeNull();
            property.Level0Type.Should().BeNull();
            property.Level0Name.Should().BeNull();
            property.Level1Type.Should().BeNull();
            property.Level1Name.Should().BeNull();
            property.Level2Type.Should().BeNull();
            property.Level2Name.Should().BeNull();
        }

        /// <summary>
        /// Tests that ExtendedProperty ToString method formats correctly.
        /// </summary>
        [TestMethod]
        public void ExtendedProperty_ToString_ShouldFormatCorrectly()
        {
            // Arrange
            var property = new DBAccess.ExtendedProperty
            {
                Name = "Description",
                Value = "Test Description",
                Level0Name = "dbo",
                Level1Name = "TestTable",
                Level2Name = "TestColumn"
            };

            // Act
            var result = property.ToString();

            // Assert
            result.Should().Be("Description = ‘Test Description’ (on dbo.TestTable.TestColumn)");
        }

        /// <summary>
        /// Tests that ExtendedProperty ToString method handles partial hierarchy.
        /// </summary>
        [TestMethod]
        public void ExtendedProperty_ToString_ShouldHandlePartialHierarchy()
        {
            // Arrange - Only Level 0 and Level 1
            var property = new DBAccess.ExtendedProperty
            {
                Name = "Description",
                Value = "Test Description",
                Level0Name = "dbo",
                Level1Name = "TestTable"
            };

            // Act
            var result = property.ToString();

            // Assert
            result.Should().Be("Description = ‘Test Description’ (on dbo.TestTable)");
        }

        #endregion

        #region Database-Level Extended Properties Tests

        /// <summary>
        /// Tests adding a database-level extended property.
        /// </summary>
        [TestMethod]
        public void AddExtendedProperty_DatabaseLevel_ShouldSucceed()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var propertyName = GenerateTestPropertyName("DB");
            var propertyValue = "Test database description";

            try
            {
                // Act
                var success = dbAccess.AddExtendedProperty(propertyName, propertyValue);

                // Assert
                success.Should().BeTrue();
                dbAccess.LastException.Should().BeNull();

                // Verify property was added
                var retrievedValue = dbAccess.GetExtendedProperty(propertyName);
                retrievedValue.Should().Be(propertyValue);
            }
            finally
            {
                // Cleanup
                dbAccess.DeleteExtendedProperty(propertyName);
            }
        }

        /// <summary>
        /// Tests setting a database-level extended property (upsert behavior).
        /// </summary>
        [TestMethod]
        public void SetExtendedProperty_DatabaseLevel_ShouldUpsertCorrectly()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var propertyName = GenerateTestPropertyName("DBSet");
            var originalValue = "Original value";
            var updatedValue = "Updated value";

            try
            {
                // Act - First set (should add)
                var firstResult = dbAccess.SetExtendedProperty(propertyName, originalValue);
                
                // Assert first set
                firstResult.Should().BeTrue();
                var retrievedValue = dbAccess.GetExtendedProperty(propertyName);
                retrievedValue.Should().Be(originalValue);

                // Act - Second set (should update)
                var secondResult = dbAccess.SetExtendedProperty(propertyName, updatedValue);
                
                // Assert second set
                secondResult.Should().BeTrue();
                var updatedRetrievedValue = dbAccess.GetExtendedProperty(propertyName);
                updatedRetrievedValue.Should().Be(updatedValue);
            }
            finally
            {
                // Cleanup
                dbAccess.DeleteExtendedProperty(propertyName);
            }
        }

        #endregion

        #region Table-Level Extended Properties Tests

        /// <summary>
        /// Tests adding an extended property to a table.
        /// </summary>
        [TestMethod]
        public void AddExtendedPropertyToTable_ShouldSucceed()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var propertyName = GenerateTestPropertyName("Table");
            var propertyValue = "Test table description";
            var tableName = "TestEntities"; // This table should exist in test database

            try
            {
                // Act
                var success = dbAccess.AddExtendedPropertyToTable(propertyName, propertyValue, tableName);

                // Assert
                success.Should().BeTrue();
                dbAccess.LastException.Should().BeNull();

                // Verify property was added
                var retrievedValue = dbAccess.GetExtendedPropertyFromTable(propertyName, tableName);
                retrievedValue.Should().Be(propertyValue);
            }
            finally
            {
                // Cleanup
                dbAccess.DeleteExtendedPropertyFromTable(propertyName, tableName);
            }
        }

        /// <summary>
        /// Tests setting an extended property on a table (upsert behavior).
        /// </summary>
        [TestMethod]
        public void SetExtendedPropertyOnTable_ShouldUpsertCorrectly()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var propertyName = GenerateTestPropertyName("TableSet");
            var originalValue = "Original table description";
            var updatedValue = "Updated table description";
            var tableName = "TestEntities";

            try
            {
                // Act - First set (should add)
                var firstResult = dbAccess.SetExtendedPropertyOnTable(propertyName, originalValue, tableName);
                
                // Assert first set
                firstResult.Should().BeTrue();
                var retrievedValue = dbAccess.GetExtendedPropertyFromTable(propertyName, tableName);
                retrievedValue.Should().Be(originalValue);

                // Act - Second set (should update)
                var secondResult = dbAccess.SetExtendedPropertyOnTable(propertyName, updatedValue, tableName);
                
                // Assert second set
                secondResult.Should().BeTrue();
                var updatedRetrievedValue = dbAccess.GetExtendedPropertyFromTable(propertyName, tableName);
                updatedRetrievedValue.Should().Be(updatedValue);
            }
            finally
            {
                // Cleanup
                dbAccess.DeleteExtendedPropertyFromTable(propertyName, tableName);
            }
        }

        #endregion

        #region Column-Level Extended Properties Tests

        /// <summary>
        /// Tests adding an extended property to a table column.
        /// </summary>
        [TestMethod]
        public void AddExtendedPropertyToColumn_ShouldSucceed()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var propertyName = GenerateTestPropertyName("Column");
            var propertyValue = "Test column description";
            var tableName = "TestEntities";
            var columnName = "Name"; // This column should exist in TestEntities table

            try
            {
                // Act
                var success = dbAccess.AddExtendedPropertyToColumn(propertyName, propertyValue, tableName, columnName);

                // Assert
                success.Should().BeTrue();
                dbAccess.LastException.Should().BeNull();

                // Verify property was added
                var retrievedValue = dbAccess.GetExtendedPropertyFromColumn(propertyName, tableName, columnName);
                retrievedValue.Should().Be(propertyValue);
            }
            finally
            {
                // Cleanup
                dbAccess.DeleteExtendedPropertyFromColumn(propertyName, tableName, columnName);
            }
        }

        /// <summary>
        /// Tests setting an extended property on a column (upsert behavior).
        /// </summary>
        [TestMethod]
        public void SetExtendedPropertyOnColumn_ShouldUpsertCorrectly()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var propertyName = GenerateTestPropertyName("ColumnSet");
            var originalValue = "Original column description";
            var updatedValue = "Updated column description";
            var tableName = "TestEntities";
            var columnName = "Name";

            try
            {
                // Act - First set (should add)
                var firstResult = dbAccess.SetExtendedPropertyOnColumn(propertyName, originalValue, tableName, columnName);
                
                // Assert first set
                firstResult.Should().BeTrue();
                var retrievedValue = dbAccess.GetExtendedPropertyFromColumn(propertyName, tableName, columnName);
                retrievedValue.Should().Be(originalValue);

                // Act - Second set (should update)
                var secondResult = dbAccess.SetExtendedPropertyOnColumn(propertyName, updatedValue, tableName, columnName);
                
                // Assert second set
                secondResult.Should().BeTrue();
                var updatedRetrievedValue = dbAccess.GetExtendedPropertyFromColumn(propertyName, tableName, columnName);
                updatedRetrievedValue.Should().Be(updatedValue);
            }
            finally
            {
                // Cleanup
                dbAccess.DeleteExtendedPropertyFromColumn(propertyName, tableName, columnName);
            }
        }

        #endregion

        #region Update Extended Properties Tests

        /// <summary>
        /// Tests updating an existing extended property.
        /// </summary>
        [TestMethod]
        public void UpdateExtendedProperty_ExistingProperty_ShouldSucceed()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var propertyName = GenerateTestPropertyName("Update");
            var originalValue = "Original value";
            var updatedValue = "Updated value";

            try
            {
                // First add the property
                dbAccess.AddExtendedProperty(propertyName, originalValue);

                // Act
                var success = dbAccess.UpdateExtendedProperty(propertyName, updatedValue);

                // Assert
                success.Should().BeTrue();
                dbAccess.LastException.Should().BeNull();

                // Verify property was updated
                var retrievedValue = dbAccess.GetExtendedProperty(propertyName);
                retrievedValue.Should().Be(updatedValue);
            }
            finally
            {
                // Cleanup
                dbAccess.DeleteExtendedProperty(propertyName);
            }
        }

        /// <summary>
        /// Tests updating an extended property on a table.
        /// </summary>
        [TestMethod]
        public void UpdateExtendedPropertyOnTable_ExistingProperty_ShouldSucceed()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var propertyName = GenerateTestPropertyName("UpdateTable");
            var originalValue = "Original table value";
            var updatedValue = "Updated table value";
            var tableName = "TestEntities";

            try
            {
                // First add the property
                dbAccess.AddExtendedPropertyToTable(propertyName, originalValue, tableName);

                // Act
                var success = dbAccess.UpdateExtendedPropertyOnTable(propertyName, updatedValue, tableName);

                // Assert
                success.Should().BeTrue();
                dbAccess.LastException.Should().BeNull();

                // Verify property was updated
                var retrievedValue = dbAccess.GetExtendedPropertyFromTable(propertyName, tableName);
                retrievedValue.Should().Be(updatedValue);
            }
            finally
            {
                // Cleanup
                dbAccess.DeleteExtendedPropertyFromTable(propertyName, tableName);
            }
        }

        #endregion

        #region Delete Extended Properties Tests

        /// <summary>
        /// Tests deleting a database-level extended property.
        /// </summary>
        [TestMethod]
        public void DeleteExtendedProperty_DatabaseLevel_ShouldSucceed()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var propertyName = GenerateTestPropertyName("Delete");
            var propertyValue = "Value to delete";

            // First add the property
            dbAccess.AddExtendedProperty(propertyName, propertyValue);

            // Act
            var success = dbAccess.DeleteExtendedProperty(propertyName);

            // Assert
            success.Should().BeTrue();
            dbAccess.LastException.Should().BeNull();

            // Verify property was deleted
            var retrievedValue = dbAccess.GetExtendedProperty(propertyName);
            retrievedValue.Should().BeNull();
        }

        /// <summary>
        /// Tests deleting an extended property from a table.
        /// </summary>
        [TestMethod]
        public void DeleteExtendedPropertyFromTable_ShouldSucceed()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var propertyName = GenerateTestPropertyName("DeleteTable");
            var propertyValue = "Table value to delete";
            var tableName = "TestEntities";

            // First add the property
            dbAccess.AddExtendedPropertyToTable(propertyName, propertyValue, tableName);

            // Act
            var success = dbAccess.DeleteExtendedPropertyFromTable(propertyName, tableName);

            // Assert
            success.Should().BeTrue();
            dbAccess.LastException.Should().BeNull();

            // Verify property was deleted
            var retrievedValue = dbAccess.GetExtendedPropertyFromTable(propertyName, tableName);
            retrievedValue.Should().BeNull();
        }

        #endregion

        #region Get Multiple Extended Properties Tests

        /// <summary>
        /// Tests getting all extended properties for a table.
        /// </summary>
        [TestMethod]
        public void GetExtendedPropertiesFromTable_ShouldReturnAllProperties()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var tableName = "TestEntities";
            var property1Name = GenerateTestPropertyName("Multi1");
            var property2Name = GenerateTestPropertyName("Multi2");
            var property1Value = "First property value";
            var property2Value = "Second property value";

            try
            {
                // Add multiple properties
                dbAccess.AddExtendedPropertyToTable(property1Name, property1Value, tableName);
                dbAccess.AddExtendedPropertyToTable(property2Name, property2Value, tableName);

                // Act
                var properties = dbAccess.GetExtendedPropertiesFromTable(tableName);

                // Assert
                properties.Should().NotBeNull();
                properties.Count.Should().BeGreaterThanOrEqualTo(2, "should contain at least the two properties we added");
                
                var testProperties = properties.Where(p => p.Name.StartsWith("Test_")).ToList();
                testProperties.Count.Should().Be(2, "should contain our two test properties");
                
                testProperties.Should().Contain(p => p.Name == property1Name && p.Value == property1Value);
                testProperties.Should().Contain(p => p.Name == property2Name && p.Value == property2Value);
            }
            finally
            {
                // Cleanup
                dbAccess.DeleteExtendedPropertyFromTable(property1Name, tableName);
                dbAccess.DeleteExtendedPropertyFromTable(property2Name, tableName);
            }
        }

        /// <summary>
        /// Tests getting all database-level extended properties.
        /// </summary>
        [TestMethod]
        public void GetDatabaseExtendedProperties_ShouldReturnDatabaseProperties()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var propertyName = GenerateTestPropertyName("DBMulti");
            var propertyValue = "Database property value";

            try
            {
                // Add a database property
                dbAccess.AddExtendedProperty(propertyName, propertyValue);

                // Act
                var properties = dbAccess.GetDatabaseExtendedProperties();

                // Assert
                properties.Should().NotBeNull();
                var testProperty = properties.FirstOrDefault(p => p.Name == propertyName);
                testProperty.Should().NotBeNull();
                testProperty.Value.Should().Be(propertyValue);
            }
            finally
            {
                // Cleanup
                dbAccess.DeleteExtendedProperty(propertyName);
            }
        }

        #endregion

        #region Error Handling and Validation Tests

        /// <summary>
        /// Tests that AddExtendedProperty throws ArgumentNullException for null property name.
        /// </summary>
        [TestMethod]
        public void AddExtendedProperty_NullPropertyName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();

            // Act & Assert
            var action = new Action(() => dbAccess.AddExtendedProperty(null, "test value"));
            action.Should().Throw<ArgumentNullException>()
                  .WithParameterName("propertyName");
        }

        /// <summary>
        /// Tests that AddExtendedProperty throws ArgumentNullException for null property value.
        /// </summary>
        [TestMethod]
        public void AddExtendedProperty_NullPropertyValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();

            // Act & Assert
            var action = new Action(() => dbAccess.AddExtendedProperty("TestProperty", null));
            action.Should().Throw<ArgumentNullException>()
                  .WithParameterName("propertyValue");
        }

        /// <summary>
        /// Tests that extended properties throw NotSupportedException for ODBC databases.
        /// </summary>
        [TestMethod]
        public void AddExtendedProperty_ODBCDatabase_ShouldThrowNotSupportedException()
        {
            // Arrange
            var odbcOptions = DatabaseTestHelper.CreateTestDatabaseOptions();
            odbcOptions.DatabaseType = DBAccess.DataBaseType.ODBC;
            var dbAccess = new DBAccess(odbcOptions);

            // Act & Assert
            var action = new Action(() => dbAccess.AddExtendedProperty("TestProperty", "test value"));
            action.Should().Throw<NotSupportedException>()
                  .WithMessage("*Extended properties are only supported for SQL Server databases*");
        }

        /// <summary>
        /// Tests that GetExtendedProperty returns null for non-existent property.
        /// </summary>
        [TestMethod]
        public void GetExtendedProperty_NonExistentProperty_ShouldReturnNull()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var nonExistentPropertyName = "NonExistent_" + Guid.NewGuid().ToString("N");

            // Act
            var result = dbAccess.GetExtendedProperty(nonExistentPropertyName);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Full Hierarchy Tests

        /// <summary>
        /// Tests adding extended property with full hierarchy specification.
        /// </summary>
        [TestMethod]
        public void AddExtendedProperty_FullHierarchy_ShouldSucceed()
        {
            // Arrange
            var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
            var propertyName = GenerateTestPropertyName("FullHier");
            var propertyValue = "Full hierarchy property";
            var tableName = "TestEntities";
            var columnName = "Name";

            try
            {
                // Act
                var success = dbAccess.AddExtendedProperty(
                    propertyName, propertyValue,
                    DBAccess.ExtendedPropertyLevel0Type.Schema, "dbo",
                    DBAccess.ExtendedPropertyLevel1Type.Table, tableName,
                    DBAccess.ExtendedPropertyLevel2Type.Column, columnName);

                // Assert
                success.Should().BeTrue();
                dbAccess.LastException.Should().BeNull();

                // Verify property was added
                var retrievedValue = dbAccess.GetExtendedProperty(
                    propertyName,
                    DBAccess.ExtendedPropertyLevel0Type.Schema, "dbo",
                    DBAccess.ExtendedPropertyLevel1Type.Table, tableName,
                    DBAccess.ExtendedPropertyLevel2Type.Column, columnName);
                retrievedValue.Should().Be(propertyValue);
            }
            finally
            {
                // Cleanup
                dbAccess.DeleteExtendedProperty(
                    propertyName,
                    DBAccess.ExtendedPropertyLevel0Type.Schema, "dbo",
                    DBAccess.ExtendedPropertyLevel1Type.Table, tableName,
                    DBAccess.ExtendedPropertyLevel2Type.Column, columnName);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Generates a unique test property name to avoid conflicts.
        /// </summary>
        /// <param name="prefix">Prefix for the property name.</param>
        /// <returns>A unique property name for testing.</returns>
        private static string GenerateTestPropertyName(string prefix)
        {
            return $"Test_{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        /// <summary>
        /// Cleans up any test extended properties that might remain.
        /// </summary>
        /// <param name="dbAccess">The database access instance to use.</param>
        
        private static void CleanupTestExtendedProperties(DBAccess dbAccess)
        {
            try
            {
                // Get all extended properties and clean up any that start with "Test_"
                var allProperties = dbAccess.GetAllExtendedProperties();
                if (allProperties != null)
                {
                    foreach (var property in allProperties.Where(p => p.Name?.StartsWith("Test_") == true))
                    {
                        try
                        {
                            // Determine the appropriate delete method based on hierarchy
                            if (!string.IsNullOrEmpty(property.Level2Name))
                            {
                                // Column level
                                dbAccess.DeleteExtendedPropertyFromColumn(property.Name, property.Level1Name, property.Level2Name);
                            }
                            else if (!string.IsNullOrEmpty(property.Level1Name))
                            {
                                // Table level
                                dbAccess.DeleteExtendedPropertyFromTable(property.Name, property.Level1Name);
                            }
                            else
                            {
                                // Database level
                                dbAccess.DeleteExtendedProperty(property.Name);
                            }
                        }
                        catch (Exception)
                        {
                            // Ignore individual cleanup failures
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore overall cleanup failures
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleans up any test data that might have been created during testing.
        /// </summary>
        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                var dbAccess = DatabaseTestHelper.CreateTestDBAccess();
                CleanupTestExtendedProperties(dbAccess);
            }
            catch (Exception)
            {
                // Ignore cleanup errors to avoid masking test failures
            }
        }

        #endregion
    }
}