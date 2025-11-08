# TestUnitDB Setup Instructions

## Overview
This guide walks you through setting up the `TestUnitDB` database for ByteForge.Toolkit unit and integration testing.

## Prerequisites
- Local SQL Server instance accessible via `(local)`
- SQL Server Management Studio (SSMS) or sqlcmd command line tool
- Appropriate permissions to create databases

## Setup Steps

### Step 1: Create the Database Schema

1. **Open SQL Server Management Studio**
2. **Connect to your local SQL Server instance** using `(local)` as the server name
3. **Run the schema creation script**:
   - Open `01_CreateTestUnitDB.sql`
   - Execute the entire script
   - This will create the `TestUnitDB` database with all necessary tables, views, procedures, and functions

### Step 2: Seed Test Data

1. **Run the data seeding script**:
   - Open `02_SeedTestData.sql` 
   - Execute the entire script
   - This will populate all tables with comprehensive test data

### Step 3: Configure Authentication (Choose One)

#### Option A: Integrated Security (Recommended)
- Uses your Windows account for authentication
- No additional setup required
- Most secure for development environments

#### Option B: SQL Server Authentication
If you prefer to use SQL authentication:

1. **Uncomment the user creation section** in `01_CreateTestUnitDB.sql`:
```sql
-- Create login
CREATE LOGIN ByteForgeTestUser WITH PASSWORD = 'TestP@ssw0rd123!';

-- Create user in the database
CREATE USER ByteForgeTestUser FOR LOGIN ByteForgeTestUser;

-- Grant necessary permissions
ALTER ROLE db_datareader ADD MEMBER ByteForgeTestUser;
ALTER ROLE db_datawriter ADD MEMBER ByteForgeTestUser;
ALTER ROLE db_ddladmin ADD MEMBER ByteForgeTestUser;
```

2. **Re-run the schema script** to create the test user

### Step 4: Configure Test Settings

1. **Copy the test configuration** from `TestConfiguration.ini` to your test project
2. **Choose the appropriate configuration section**:
   - `TestUnitDB_IntegratedSecurity` for Windows Authentication
   - `TestUnitDB_SqlAuth` for SQL Server Authentication

### Step 5: Verify Setup

Run these queries to verify the database was set up correctly:

```sql
USE TestUnitDB;

-- Check table row counts
SELECT 
    'TestEntities' as TableName, COUNT(*) as RowCount FROM TestEntities
UNION ALL SELECT 'BulkTestEntities', COUNT(*) FROM BulkTestEntities
UNION ALL SELECT 'TestDataTypes', COUNT(*) FROM TestDataTypes
UNION ALL SELECT 'PerformanceTestTable', COUNT(*) FROM PerformanceTestTable;

-- Test the view
SELECT COUNT(*) as ActiveEntities FROM vw_ActiveTestEntities;

-- Test the stored procedure
EXEC sp_GetTestEntityCount @IsActiveOnly = 1;

-- Test the function
SELECT COUNT(*) as EntitiesInRange FROM fn_GetEntitiesByValueRange(50, 200);
```

Expected results:
- TestEntities: ~20 rows
- BulkTestEntities: 50 rows
- TestDataTypes: 5 rows  
- PerformanceTestTable: 5,000 rows

## Database Schema Overview

### Core Test Tables
- **TestEntities**: Basic entity table for CRUD operations
- **TestDataTypes**: Comprehensive data type testing
- **BulkTestEntities**: Bulk operation testing with unique constraints
- **BulkTestComposite**: Composite primary key testing
- **BulkTestIdentity**: Identity column testing

### Specialized Tables
- **ParameterTestTable**: SQL parameter testing
- **ConstraintTestTable**: Database constraint testing
- **ScriptTestOrders/OrderItems**: Complex script execution testing
- **PerformanceTestTable**: Performance and load testing

### Database Objects
- **Views**: `vw_ActiveTestEntities` for query testing
- **Procedures**: `sp_GetTestEntityCount`, `sp_GetMultipleResultSets`, `sp_TestOutputParameters`
- **Functions**: `fn_CalculateTestValue`, `fn_GetEntitiesByValueRange`
- **User-Defined Types**: `BulkEntityTableType` for structured parameters

## Configuration Examples

### Unit Test Configuration (appsettings.json)
```json
{
  "ConnectionStrings": {
    "TestDatabase": "Server=(local);Database=TestUnitDB;Integrated Security=true;Connect Timeout=30;"
  },
  "DatabaseOptions": {
    "CommandTimeout": 300,
    "AutoTrimStrings": true
  }
}
```

### ByteForge Configuration (INI format)
```ini
[Data Source]
SelectedDB=TestUnitDB_IntegratedSecurity

[TestUnitDB_IntegratedSecurity]
sType=SQLServer
sServer=(local)
sDatabaseName=TestUnitDB
bTrustedConnection=true
iConnectionTimeout=30
iCommandTimeout=300
```

## Testing Scenarios Supported

### 1. Basic CRUD Operations
- Insert, Update, Delete, Select operations
- Parameter handling and SQL injection prevention
- Data type conversion and mapping

### 2. Bulk Operations
- Bulk insert with large datasets
- Bulk upsert (update existing, insert new)
- Bulk delete based on key matching
- Progress reporting and error handling

### 3. Complex Script Execution
- Multi-batch scripts with GO statements
- DDL and DML statement mixing
- Multiple result set handling
- Transaction management

### 4. Performance Testing
- Large dataset operations (5,000+ records)
- Connection pooling efficiency
- Query optimization validation
- Memory usage monitoring

### 5. Error Handling
- Database constraint violations
- Network connectivity issues
- Invalid SQL statements
- Parameter mismatches

### 6. Security Testing
- SQL injection prevention
- Encrypted credential handling
- Connection string security
- Parameter escaping

## Maintenance

### Cleaning Test Data
To reset the database to a clean state:
```sql
USE TestUnitDB;
-- Re-run the data seeding portions of 02_SeedTestData.sql
-- OR
-- Run the DELETE statements at the beginning of 02_SeedTestData.sql
```

### Updating Schema
When adding new test scenarios:
1. Update `01_CreateTestUnitDB.sql` with new tables/objects
2. Update `02_SeedTestData.sql` with corresponding test data  
3. Update this documentation

### Performance Tuning
For large-scale performance testing:
1. Increase the `@TotalRecords` variable in the seed script
2. Add additional indexes as needed
3. Consider partitioning large tables

## Troubleshooting

### Common Issues

1. **"Database already exists" error**
   - The script handles this automatically by dropping the existing database

2. **Permission denied errors**  
   - Ensure your account has sysadmin or dbcreator permissions
   - For SQL authentication, ensure the login has proper database permissions

3. **Connection timeout errors**
   - Increase connection timeout values in configuration
   - Check SQL Server service is running

4. **File path errors**
   - Update the database file paths in the CREATE DATABASE statement to match your SQL Server installation

### Verification Queries

```sql
-- Check database exists and is accessible
SELECT name, database_id, create_date FROM sys.databases WHERE name = 'TestUnitDB';

-- Check all tables were created
SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_CATALOG = 'TestUnitDB'
ORDER BY TABLE_NAME;

-- Check data was seeded
SELECT 'TestEntities' as TableName, COUNT(*) as RowCount FROM TestEntities
UNION ALL SELECT 'BulkTestEntities', COUNT(*) FROM BulkTestEntities;

-- Test basic functionality
SELECT TOP 1 * FROM vw_ActiveTestEntities;
EXEC sp_GetTestEntityCount;
```

## Next Steps

After setting up TestUnitDB:

1. **Run the existing unit tests** to ensure they pass with the new database
2. **Create additional test classes** following the strategy in `Database_Testing_Analysis.md`
3. **Set up continuous integration** to run tests automatically
4. **Monitor test performance** and optimize as needed

The database is now ready for comprehensive testing of the ByteForge.Toolkit database functionality!

