-- ================================================
-- ByteForge.Toolkit Test Database Setup Script
-- Database: TestUnitDB
-- Purpose: Unit and Integration Testing
-- ================================================

USE master;
GO

-- Drop the database if it exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'TestUnitDB')
BEGIN
    ALTER DATABASE TestUnitDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TestUnitDB;
END
GO

-- Drop the login if it exists so the script can be rerun cleanly
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ByteForgeTestUser')
BEGIN
    DROP LOGIN ByteForgeTestUser;
END
GO

-- Create the test database
CREATE DATABASE TestUnitDB;
GO

USE TestUnitDB;
GO

-- ================================================
-- Create Test User
-- ================================================
-- Uncomment these lines if you need a dedicated test user
-- Create login
CREATE LOGIN ByteForgeTestUser WITH PASSWORD = 'TestP@ssw0rd123!';
GO

-- Create user in the database
CREATE USER ByteForgeTestUser FOR LOGIN ByteForgeTestUser;
GO

-- Grant necessary permissions
ALTER ROLE db_datareader ADD MEMBER ByteForgeTestUser;
ALTER ROLE db_datawriter ADD MEMBER ByteForgeTestUser;
ALTER ROLE db_ddladmin ADD MEMBER ByteForgeTestUser;
GO

-- ================================================
-- SECTION 1: Basic Test Tables
-- ================================================

-- Simple entity for basic CRUD testing
CREATE TABLE TestEntities (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    Description nvarchar(255),
    CreatedDate datetime2(7) DEFAULT GETDATE(),
    IsActive bit DEFAULT 1,
    TestValue decimal(10,2) DEFAULT 0.0,
    TestGuid uniqueidentifier DEFAULT NEWID()
);
GO

-- Entity with various data types for TypeConverter testing
CREATE TABLE TestDataTypes (
    Id bigint IDENTITY(1,1) PRIMARY KEY,
    StringValue nvarchar(500),
    IntValue int,
    BigIntValue bigint,
    SmallIntValue smallint,
    TinyIntValue tinyint,
    DecimalValue decimal(18,4),
    FloatValue float,
    RealValue real,
    BitValue bit,
    DateTimeValue datetime2(7),
    DateValue date,
    TimeValue time(7),
    GuidValue uniqueidentifier,
    BinaryValue varbinary(255),
    XmlValue xml,
    MoneyValue money,
    SmallMoneyValue smallmoney
);
GO

-- ================================================
-- SECTION 2: Bulk Operation Test Tables
-- ================================================

-- Table for BulkDbProcessor testing with primary key
CREATE TABLE BulkTestEntities (
    Id uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
    Code nvarchar(50) NOT NULL,
    Name nvarchar(200) NOT NULL,
    Category nvarchar(100),
    Value decimal(18,4) DEFAULT 0,
    Priority int DEFAULT 1,
    IsActive bit DEFAULT 1,
    CreatedDate datetime2(7) DEFAULT GETDATE(),
    ModifiedDate datetime2(7) DEFAULT GETDATE()
);
GO

-- Add unique constraint for upsert testing
CREATE UNIQUE NONCLUSTERED INDEX IX_BulkTestEntities_Code 
ON BulkTestEntities (Code);
GO

-- Table for testing bulk operations with composite keys
CREATE TABLE BulkTestComposite (
    CategoryId int NOT NULL,
    ItemCode nvarchar(20) NOT NULL,
    Name nvarchar(100) NOT NULL,
    Value decimal(10,2),
    LastUpdated datetime2(7) DEFAULT GETDATE(),
    CONSTRAINT PK_BulkTestComposite PRIMARY KEY CLUSTERED (CategoryId, ItemCode)
);
GO

-- Table for testing bulk operations with identity and non-identity columns
CREATE TABLE BulkTestIdentity (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ExternalId nvarchar(50) NOT NULL UNIQUE,
    Name nvarchar(100) NOT NULL,
    Status nvarchar(20) DEFAULT 'Active',
    Amount decimal(15,2) DEFAULT 0,
    CreatedDate datetime2(7) DEFAULT GETDATE()
);
GO

-- ================================================
-- SECTION 3: Script Execution Test Tables
-- ================================================

-- Tables for testing complex script execution
CREATE TABLE ScriptTestOrders (
    OrderId int IDENTITY(1,1) PRIMARY KEY,
    OrderNumber nvarchar(50) NOT NULL UNIQUE,
    CustomerName nvarchar(200) NOT NULL,
    OrderDate datetime2(7) DEFAULT GETDATE(),
    TotalAmount decimal(12,2) DEFAULT 0,
    Status nvarchar(20) DEFAULT 'Pending'
);
GO

CREATE TABLE ScriptTestOrderItems (
    OrderItemId int IDENTITY(1,1) PRIMARY KEY,
    OrderId int NOT NULL,
    ProductName nvarchar(200) NOT NULL,
    Quantity int NOT NULL DEFAULT 1,
    UnitPrice decimal(10,2) NOT NULL DEFAULT 0,
    LineTotal AS (Quantity * UnitPrice) PERSISTED,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES ScriptTestOrders(OrderId)
);
GO

-- ================================================
-- SECTION 4: Parameter Testing Tables
-- ================================================

-- Table for testing various parameter scenarios
CREATE TABLE ParameterTestTable (
    Id int IDENTITY(1,1) PRIMARY KEY,
    StringParam nvarchar(255),
    IntParam int,
    DateParam datetime2(7),
    DecimalParam decimal(18,4),
    BoolParam bit,
    GuidParam uniqueidentifier,
    NullableIntParam int NULL,
    BinaryParam varbinary(MAX),
    JsonParam nvarchar(MAX), -- For testing JSON-like string parameters
    EnumValueParam int -- For testing enum parameter conversion
);
GO

-- ================================================
-- SECTION 5: Performance Test Tables
-- ================================================

-- Large table for performance testing
CREATE TABLE PerformanceTestTable (
    Id bigint IDENTITY(1,1) PRIMARY KEY,
    BatchId int NOT NULL,
    RecordNumber int NOT NULL,
    RandomData nvarchar(100),
    NumericValue decimal(18,6),
    DateCreated datetime2(7) DEFAULT GETDATE(),
    ProcessingFlag bit DEFAULT 0
);
GO

-- Index for performance testing
CREATE NONCLUSTERED INDEX IX_PerformanceTestTable_BatchId 
ON PerformanceTestTable (BatchId) 
INCLUDE (RecordNumber, ProcessingFlag);
GO

-- ================================================
-- SECTION 6: Error Testing Tables
-- ================================================

-- Table with constraints for error testing
CREATE TABLE ConstraintTestTable (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UniqueValue nvarchar(50) NOT NULL UNIQUE,
    NotNullValue nvarchar(100) NOT NULL,
    CheckValue int CHECK (CheckValue >= 0 AND CheckValue <= 100),
    ForeignKeyValue int,
    CreatedDate datetime2(7) DEFAULT GETDATE()
);
GO

-- Reference table for foreign key testing
CREATE TABLE ReferenceTable (
    RefId int IDENTITY(1,1) PRIMARY KEY,
    RefName nvarchar(100) NOT NULL
);
GO

-- Add foreign key constraint
ALTER TABLE ConstraintTestTable 
ADD CONSTRAINT FK_ConstraintTest_Reference 
FOREIGN KEY (ForeignKeyValue) REFERENCES ReferenceTable(RefId);
GO

-- ================================================
-- SECTION 7: User-Defined Table Types (for SQL Server bulk operations)
-- ================================================

-- Table type for testing structured parameters
CREATE TYPE BulkEntityTableType AS TABLE (
    Code nvarchar(50),
    Name nvarchar(200),
    Category nvarchar(100),
    Value decimal(18,4),
    IsActive bit
);
GO

-- ================================================
-- SECTION 8: Test Stored Procedures
-- ================================================

-- Simple stored procedure for testing procedure execution
CREATE PROCEDURE sp_GetTestEntityCount
    @CategoryFilter nvarchar(100) = NULL,
    @IsActiveOnly bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT COUNT(*) as EntityCount
    FROM TestEntities 
    WHERE (@CategoryFilter IS NULL OR Description LIKE '%' + @CategoryFilter + '%')
    AND (@IsActiveOnly = 0 OR IsActive = @IsActiveOnly);
END
GO

-- Stored procedure that returns multiple result sets
CREATE PROCEDURE sp_GetMultipleResultSets
AS
BEGIN
    SET NOCOUNT ON;
    
    -- First result set: Basic entity count
    SELECT COUNT(*) as TotalEntities FROM TestEntities;
    
    -- Second result set: Active entities
    SELECT Id, Name, CreatedDate 
    FROM TestEntities 
    WHERE IsActive = 1 
    ORDER BY CreatedDate DESC;
    
    -- Third result set: Summary by status
    SELECT IsActive, COUNT(*) as Count, AVG(TestValue) as AvgValue
    FROM TestEntities 
    GROUP BY IsActive;
END
GO

-- Stored procedure for testing output parameters
CREATE PROCEDURE sp_TestOutputParameters
    @InputValue int,
    @OutputValue int OUTPUT,
    @ReturnMessage nvarchar(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    SET @OutputValue = @InputValue * 2;
    SET @ReturnMessage = 'Input was ' + CAST(@InputValue as nvarchar(10)) + ', output is ' + CAST(@OutputValue as nvarchar(10));
    
    RETURN 0;
END
GO

-- ================================================
-- SECTION 9: Test Views
-- ================================================

-- View for testing query operations
CREATE VIEW vw_ActiveTestEntities AS
SELECT 
    Id,
    Name,
    Description,
    CreatedDate,
    TestValue,
    CASE 
        WHEN TestValue > 100 THEN 'High'
        WHEN TestValue > 50 THEN 'Medium'
        ELSE 'Low'
    END as ValueCategory
FROM TestEntities 
WHERE IsActive = 1;
GO

-- ================================================
-- SECTION 10: Test Functions
-- ================================================

-- Scalar function for testing function calls
CREATE FUNCTION fn_CalculateTestValue(@baseValue decimal(10,2), @multiplier decimal(5,2))
RETURNS decimal(15,2)
AS
BEGIN
    RETURN @baseValue * @multiplier;
END
GO

-- Table-valued function for testing
CREATE FUNCTION fn_GetEntitiesByValueRange(@minValue decimal(10,2), @maxValue decimal(10,2))
RETURNS TABLE
AS
RETURN (
    SELECT Id, Name, TestValue, CreatedDate
    FROM TestEntities
    WHERE TestValue BETWEEN @minValue AND @maxValue
    AND IsActive = 1
);
GO

-- ================================================
-- SECTION 11: Initial Test Data Setup
-- ================================================

-- We'll create a separate script for seed data to keep this focused on schema
-- See 02_SeedTestData.sql

PRINT 'TestUnitDB database schema created successfully!';
PRINT 'Next steps:';
PRINT '1. Run 02_SeedTestData.sql to populate with test data';
PRINT '2. Configure test connection strings';
PRINT '3. Run unit tests';
GO
