# TestAccess.accdb Database Setup Guide

This document provides instructions for creating the TestAccess.accdb database for ODBC testing with ByteForge.Toolkit's DBAccess class.

## Prerequisites

- Microsoft Access (part of Microsoft 365)
- Microsoft Access Database Engine (usually included with Office)

## Database Creation Steps

### 1. Create New Access Database

1. Open Microsoft Access
2. Click "Blank database"
3. Name: `TestAccess.accdb`
4. Location: `C:\Users\pauls\source\TelecomInc\ByteForge.Toolkit\TODO\`
5. Click "Create"

### 2. Create Tables

#### Table 1: TestEntities

Create table with the following structure:

| Field Name | Data Type | Size | Properties |
|------------|-----------|------|------------|
| ID | AutoNumber | Long Integer | Primary Key |
| Name | Short Text | 100 | Required: Yes |
| Description | Short Text | 255 | Required: No |
| CreatedDate | Date/Time | - | Default: Now() |
| IsActive | Yes/No | - | Default: Yes |
| TestValue | Currency | - | Default: 0 |
| TestGuid | Short Text | 36 | Required: No |

**SQL DDL (for reference):**
```sql
CREATE TABLE TestEntities (
    ID AUTOINCREMENT PRIMARY KEY,
    Name TEXT(100) NOT NULL,
    Description TEXT(255),
    CreatedDate DATETIME DEFAULT Now(),
    IsActive YESNO DEFAULT True,
    TestValue CURRENCY DEFAULT 0,
    TestGuid TEXT(36)
);
```

#### Table 2: TestDataTypes

Create table for comprehensive data type testing:

| Field Name | Data Type | Size | Properties |
|------------|-----------|------|------------|
| ID | AutoNumber | Long Integer | Primary Key |
| StringValue | Short Text | 500 | Required: No |
| IntValue | Number | Long Integer | Required: No |
| DecimalValue | Currency | - | Required: No |
| BitValue | Yes/No | - | Required: No |
| DateTimeValue | Date/Time | - | Required: No |
| GuidValue | Short Text | 36 | Required: No |

**SQL DDL (for reference):**
```sql
CREATE TABLE TestDataTypes (
    ID AUTOINCREMENT PRIMARY KEY,
    StringValue TEXT(500),
    IntValue LONG,
    DecimalValue CURRENCY,
    BitValue YESNO,
    DateTimeValue DATETIME,
    GuidValue TEXT(36)
);
```

#### Table 3: BulkTestEntities

Create table for bulk operations testing:

| Field Name | Data Type | Size | Properties |
|------------|-----------|------|------------|
| ID | AutoNumber | Long Integer | Primary Key |
| Code | Short Text | 20 | Required: Yes, Indexed |
| Name | Short Text | 100 | Required: Yes |
| Category | Short Text | 50 | Required: No |
| IsActive | Yes/No | - | Default: Yes |
| CreatedDate | Date/Time | - | Default: Now() |

**SQL DDL (for reference):**
```sql
CREATE TABLE BulkTestEntities (
    ID AUTOINCREMENT PRIMARY KEY,
    Code TEXT(20) NOT NULL,
    Name TEXT(100) NOT NULL,
    Category TEXT(50),
    IsActive YESNO DEFAULT True,
    CreatedDate DATETIME DEFAULT Now()
);

CREATE INDEX IX_BulkTestEntities_Code ON BulkTestEntities (Code);
```

### 3. Insert Seed Data

#### TestEntities Sample Data

Execute the following INSERT statements in Access SQL view or VBA:

```sql
INSERT INTO TestEntities (Name, Description, IsActive, TestValue, TestGuid)
VALUES 
('Test Entity 1', 'First test entity', True, 100.50, '{12345678-1234-5678-9ABC-123456789012}'),
('Test Entity 2', 'Second test entity', True, 200.75, '{87654321-4321-8765-CBA9-210987654321}'),
('Test Entity 3', 'Third test entity', False, 0, '{11111111-2222-3333-4444-555555555555}'),
('Active Entity', 'Always active entity', True, 999.99, '{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}'),
('Inactive Entity', 'Always inactive entity', False, -50.25, '{FFFFFFFF-0000-1111-2222-333333333333}');
```

#### TestDataTypes Sample Data

```sql
INSERT INTO TestDataTypes (StringValue, IntValue, DecimalValue, BitValue, DateTimeValue, GuidValue)
VALUES 
('Test String 1', 12345, 123.45, True, #2023-01-15 10:30:00#, '{11111111-1111-1111-1111-111111111111}'),
('Test String 2', -6789, -456.78, False, #2023-06-30 14:45:30#, '{22222222-2222-2222-2222-222222222222}'),
('Unicode: 中文', 0, 0, True, #2023-12-01 09:15:45#, '{33333333-3333-3333-3333-333333333333}');
```

#### BulkTestEntities Sample Data

```sql
INSERT INTO BulkTestEntities (Code, Name, Category, IsActive)
VALUES 
('BULK001', 'Bulk Entity 1', 'Category A', True),
('BULK002', 'Bulk Entity 2', 'Category A', True),
('BULK003', 'Bulk Entity 3', 'Category B', False),
('BULK004', 'Bulk Entity 4', 'Category B', True),
('BULK005', 'Bulk Entity 5', 'Category C', True);
```

### 4. Create Indexes (Optional)

For better performance, create these indexes:

```sql
CREATE INDEX IX_TestEntities_Name ON TestEntities (Name);
CREATE INDEX IX_TestEntities_IsActive ON TestEntities (IsActive);
CREATE INDEX IX_BulkTestEntities_Category ON BulkTestEntities (Category);
```

### 5. ODBC Connection Setup

#### System DSN (Recommended for testing)

1. Open "ODBC Data Source Administrator (64-bit)" from Windows
2. Go to "System DSN" tab
3. Click "Add"
4. Select "Microsoft Access Driver (*.mdb, *.accdb)"
5. Configure:
   - **Data Source Name**: `TestAccessDB`
   - **Description**: `Test database for ByteForge.Toolkit ODBC testing`
   - **Database**: Browse to `TestAccess.accdb`
6. Click "OK"

#### Connection String Options

**Using System DSN:**
```
DSN=TestAccessDB;
```

**Using File DSN (alternative):**
```
Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=C:\Users\pauls\source\TelecomInc\ByteForge.Toolkit\TODO\TestAccess.accdb;
```

### 6. Verification

Test the database setup by:

1. Opening the Access database and verifying tables exist
2. Running sample SELECT queries in Access
3. Testing ODBC connection using Windows ODBC Administrator "Test" button
4. Verifying row counts match expected values:
   - TestEntities: 5 rows
   - TestDataTypes: 3 rows  
   - BulkTestEntities: 5 rows

### 7. Access-Specific Considerations

#### Data Type Mappings

| SQL Server Type | Access Type | Notes |
|----------------|-------------|-------|
| INT IDENTITY | AutoNumber | Primary keys |
| NVARCHAR(n) | Short Text(n) | Up to 255 chars |
| TEXT | Long Text | For larger text |
| DECIMAL/NUMERIC | Currency | Limited precision |
| BIT | Yes/No | Boolean values |
| DATETIME | Date/Time | Combined date/time |
| UNIQUEIDENTIFIER | Short Text(36) | Store as string |

#### Limitations

- No native GUID support (use Text fields)
- Currency type has limited decimal precision
- No BIGINT (use Long Integer, 32-bit)
- Different SQL syntax for some operations
- Case-sensitive field names in some contexts
- Limited concurrent user support

### 8. File Location

Save the TestAccess.accdb file to:
```
C:\Users\pauls\source\TelecomInc\ByteForge.Toolkit\TODO\TestAccess.accdb
```

This location keeps it with other test setup files but separate from the main codebase.

## Next Steps

After creating the database:
1. Update DatabaseTestHelper with Access ODBC configuration methods
2. Create DBAccessMethodsODBCTests class
3. Test ODBC connectivity and basic operations
4. Validate data type handling and parameter binding