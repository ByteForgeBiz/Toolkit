-- ================================================
-- ByteForge.Toolkit Test Data Seed Script
-- Database: TestUnitDB
-- Purpose: Populate test database with sample data
-- ================================================

USE TestUnitDB;
GO

-- Clear existing test data (if any)
PRINT 'Cleaning existing test data...';

DELETE FROM ScriptTestOrderItems;
DELETE FROM ScriptTestOrders;
DELETE FROM ParameterTestTable;
DELETE FROM ConstraintTestTable;
DELETE FROM ReferenceTable;
DELETE FROM BulkTestComposite;
DELETE FROM BulkTestEntities;
DELETE FROM BulkTestIdentity;
DELETE FROM TestDataTypes;
DELETE FROM TestEntities;
DELETE FROM PerformanceTestTable;

PRINT 'Existing data cleared.';

-- ================================================
-- SECTION 1: Basic Test Entities
-- ================================================
PRINT 'Seeding TestEntities...';

INSERT INTO TestEntities (Name, Description, IsActive, TestValue) VALUES
-- Active entities
('Test Entity 1', 'First test entity for basic operations', 1, 25.50),
('Test Entity 2', 'Second test entity with different category', 1, 75.25),
('Test Entity 3', 'Third test entity for bulk operations', 1, 150.00),
('Customer Record', 'Test customer data for TypeConverter testing', 1, 100.00),
('Product Info', 'Test product information', 1, 89.99),
('Order Details', 'Test order details record', 1, 234.56),
('User Profile', 'Test user profile data', 1, 0.00),
('System Config', 'System configuration test data', 1, 999.99),
('Temp Record', 'Temporary test record', 1, 42.42),
('Sample Data', 'Generic sample data entry', 1, 55.55),

-- Inactive entities for filtering tests
('Inactive Entity 1', 'First inactive test entity', 0, 10.00),
('Inactive Entity 2', 'Second inactive test entity', 0, 20.00),
('Deprecated Record', 'Deprecated test record', 0, 0.00),

-- Entities with special values for edge case testing
('Unicode Test: 中文测试', 'Unicode character testing', 1, 12.34),
('Special Chars: !@#$%^&*()', 'Special character testing', 1, 98.76),
('Empty Description', '', 1, 0.01),
('Null Test', NULL, 1, 0.00),

-- Entities with boundary values
('Max Value Test', 'Testing maximum decimal values', 1, 99999999.99),
('Min Value Test', 'Testing minimum decimal values', 1, 0.01),
('Zero Value Test', 'Testing zero values', 1, 0.00);

PRINT 'TestEntities seeded: ' + CAST(@@ROWCOUNT as nvarchar(10)) + ' rows';

-- ================================================
-- SECTION 2: Test Data Types
-- ================================================
PRINT 'Seeding TestDataTypes...';

INSERT INTO TestDataTypes (
    StringValue, IntValue, BigIntValue, SmallIntValue, TinyIntValue,
    DecimalValue, FloatValue, RealValue, BitValue, DateTimeValue,
    DateValue, TimeValue, GuidValue, BinaryValue, MoneyValue, SmallMoneyValue
) VALUES
-- Normal values
(N'Standard String Value', 12345, 9876543210, 32000, 255, 
 12345.6789, 123.456789, 78.9, 1, '2023-12-01 10:30:45.123',
 '2023-12-01', '14:30:45.123', NEWID(), 0x48656C6C6F, 1234.56, 567.89),

-- Boundary values
(N'Maximum Length String - ' + REPLICATE('X', 450), 2147483647, 9223372036854775807, 32767, 255,
 999999999999.9999, 1.7976931348623157E+308, 3.40282347E+38, 1, '9999-12-31 23:59:59.999',
 '9999-12-31', '23:59:59.999', '00000000-0000-0000-0000-000000000000', CONVERT(VARBINARY, REPLICATE(0xFF, 100)), 922337203685477.58, 214748.36),

-- Minimum/Zero values  
('', -2147483648, -9223372036854775808, -32768, 0,
 -999999999999.9999, -1.7976931348623157E+308, -3.40282347E+38, 0, '1900-01-01 00:00:00.000',
 '1900-01-01', '00:00:00.000', '00000000-0000-0000-0000-000000000001', 0x00, -922337203685477.58, -214748.36),

-- NULL values
(NULL, NULL, NULL, NULL, NULL,
 NULL, NULL, NULL, NULL, NULL,
 NULL, NULL, NULL, NULL, NULL, NULL),

-- Unicode and special characters
(N'Unicode: 🌟🔥💯 中文 العربية русский', 42, 987654321, 12345, 128,
 3.14159, 2.71828, 1.414, 1, '2023-06-15 12:00:00.000',
 '2023-06-15', '12:00:00.000', NEWID(), 0x546573746DA1B2, 999.99, 123.45);

PRINT 'TestDataTypes seeded: ' + CAST(@@ROWCOUNT as nvarchar(10)) + ' rows';

-- ================================================
-- SECTION 3: Bulk Test Entities
-- ================================================
PRINT 'Seeding BulkTestEntities...';

DECLARE @BulkCounter int = 1;
WHILE @BulkCounter <= 50
BEGIN
    INSERT INTO BulkTestEntities (Code, Name, Category, Value, Priority, IsActive)
    VALUES (
        'BULK' + RIGHT('000' + CAST(@BulkCounter as nvarchar(3)), 3),
        'Bulk Test Entity ' + CAST(@BulkCounter as nvarchar(10)),
        CASE (@BulkCounter % 5) 
            WHEN 0 THEN 'Category A'
            WHEN 1 THEN 'Category B' 
            WHEN 2 THEN 'Category C'
            WHEN 3 THEN 'Category D'
            ELSE 'Category E'
        END,
        RAND(CHECKSUM(NEWID())) * 1000,
        (@BulkCounter % 10) + 1,
        CASE WHEN @BulkCounter % 10 = 0 THEN 0 ELSE 1 END
    );
    SET @BulkCounter = @BulkCounter + 1;
END

PRINT 'BulkTestEntities seeded: ' + CAST(@@ROWCOUNT as nvarchar(10)) + ' rows';

-- ================================================
-- SECTION 4: Bulk Test Composite Keys
-- ================================================
PRINT 'Seeding BulkTestComposite...';

INSERT INTO BulkTestComposite (CategoryId, ItemCode, Name, Value) VALUES
(1, 'ITEM001', 'First Category Item 1', 100.00),
(1, 'ITEM002', 'First Category Item 2', 150.00),
(1, 'ITEM003', 'First Category Item 3', 200.00),
(2, 'ITEM001', 'Second Category Item 1', 75.00),
(2, 'ITEM002', 'Second Category Item 2', 125.00),
(2, 'ITEM003', 'Second Category Item 3', 175.00),
(3, 'ITEM001', 'Third Category Item 1', 50.00),
(3, 'ITEM002', 'Third Category Item 2', 100.00),
(3, 'ITEM003', 'Third Category Item 3', 150.00),
(3, 'ITEM004', 'Third Category Item 4', 225.00);

PRINT 'BulkTestComposite seeded: ' + CAST(@@ROWCOUNT as nvarchar(10)) + ' rows';

-- ================================================
-- SECTION 5: Bulk Test Identity
-- ================================================
PRINT 'Seeding BulkTestIdentity...';

INSERT INTO BulkTestIdentity (ExternalId, Name, Status, Amount) VALUES
('EXT001', 'Identity Test 1', 'Active', 100.00),
('EXT002', 'Identity Test 2', 'Pending', 250.50),
('EXT003', 'Identity Test 3', 'Active', 75.25),
('EXT004', 'Identity Test 4', 'Inactive', 0.00),
('EXT005', 'Identity Test 5', 'Active', 999.99);

PRINT 'BulkTestIdentity seeded: ' + CAST(@@ROWCOUNT as nvarchar(10)) + ' rows';

-- ================================================
-- SECTION 6: Reference Data
-- ================================================
PRINT 'Seeding ReferenceTable...';

INSERT INTO ReferenceTable (RefName) VALUES
('Reference 1'),
('Reference 2'), 
('Reference 3'),
('Reference 4'),
('Reference 5');

PRINT 'ReferenceTable seeded: ' + CAST(@@ROWCOUNT as nvarchar(10)) + ' rows';

-- ================================================
-- SECTION 7: Constraint Test Data
-- ================================================
PRINT 'Seeding ConstraintTestTable...';

INSERT INTO ConstraintTestTable (UniqueValue, NotNullValue, CheckValue, ForeignKeyValue) VALUES
('UNIQUE001', 'Required Value 1', 50, 1),
('UNIQUE002', 'Required Value 2', 75, 2),
('UNIQUE003', 'Required Value 3', 25, 3),
('UNIQUE004', 'Required Value 4', 100, 4),
('UNIQUE005', 'Required Value 5', 0, 5),
('UNIQUE006', 'Required Value 6', 50, NULL); -- NULL foreign key is allowed

PRINT 'ConstraintTestTable seeded: ' + CAST(@@ROWCOUNT as nvarchar(10)) + ' rows';

-- ================================================
-- SECTION 8: Parameter Test Data
-- ================================================
PRINT 'Seeding ParameterTestTable...';

INSERT INTO ParameterTestTable (
    StringParam, IntParam, DateParam, DecimalParam, BoolParam, 
    GuidParam, NullableIntParam, BinaryParam, JsonParam, EnumValueParam
) VALUES
('Test String 1', 100, '2023-01-15 10:30:00', 123.45, 1, 
 NEWID(), 42, 0x48656C6C6F, '{"name":"test","value":123}', 1),

('Test String 2', -200, '2023-02-20 15:45:30', -456.78, 0,
 NEWID(), NULL, 0x576F726C64, '{"status":"active","count":5}', 2),

('Unicode: 测试参数', 0, '2023-03-25 09:15:45', 0.00, 1,
 '12345678-1234-1234-1234-123456789012', 999, NULL, NULL, 0),

(NULL, NULL, NULL, NULL, NULL,
 NULL, NULL, NULL, NULL, NULL),

('Special Chars: !@#$%', 2147483647, '1900-01-01 00:00:00', 999999.9999, 0,
 NEWID(), -2147483648, CONVERT(VARBINARY, REPLICATE(0xFF, 50)), '{"empty":null}', 5);

PRINT 'ParameterTestTable seeded: ' + CAST(@@ROWCOUNT as nvarchar(10)) + ' rows';

-- ================================================
-- SECTION 9: Script Test Data (Orders and Order Items)
-- ================================================
PRINT 'Seeding ScriptTestOrders and ScriptTestOrderItems...';

-- Insert orders
INSERT INTO ScriptTestOrders (OrderNumber, CustomerName, TotalAmount, Status) VALUES
('ORD001', 'John Smith', 250.00, 'Completed'),
('ORD002', 'Jane Doe', 175.50, 'Pending'),
('ORD003', 'Bob Johnson', 89.99, 'Processing'),
('ORD004', 'Alice Brown', 456.78, 'Completed'),
('ORD005', 'Charlie Wilson', 123.45, 'Cancelled');

-- Insert order items for the orders
INSERT INTO ScriptTestOrderItems (OrderId, ProductName, Quantity, UnitPrice) VALUES
-- Order 1 items
(1, 'Product A', 2, 75.00),
(1, 'Product B', 1, 100.00),

-- Order 2 items  
(2, 'Product C', 3, 25.50),
(2, 'Product D', 1, 100.00),

-- Order 3 items
(3, 'Product A', 1, 89.99),

-- Order 4 items
(4, 'Product E', 2, 150.00),
(4, 'Product F', 4, 39.195), -- Will be rounded to 39.20 in LineTotal

-- Order 5 items (cancelled order)
(5, 'Product G', 1, 123.45);

PRINT 'ScriptTestOrders seeded: 5 rows';
PRINT 'ScriptTestOrderItems seeded: ' + CAST(@@ROWCOUNT as nvarchar(10)) + ' rows';

-- ================================================
-- SECTION 10: Performance Test Data (Larger Dataset)
-- ================================================
PRINT 'Seeding PerformanceTestTable (this may take a moment)...';

DECLARE @PerfCounter int = 1;
DECLARE @BatchId int = 1;
DECLARE @BatchSize int = 1000;
DECLARE @TotalRecords int = 5000; -- Adjust this for larger performance tests

WHILE @PerfCounter <= @TotalRecords
BEGIN
    INSERT INTO PerformanceTestTable (BatchId, RecordNumber, RandomData, NumericValue, ProcessingFlag)
    VALUES (
        @BatchId,
        (@PerfCounter % @BatchSize) + 1,
        'Random Data ' + CAST(@PerfCounter as nvarchar(10)) + ' - ' + CONVERT(nvarchar(36), NEWID()),
        RAND(CHECKSUM(NEWID())) * 10000.123456,
        CASE WHEN @PerfCounter % 7 = 0 THEN 1 ELSE 0 END
    );
    
    -- Move to next batch every 1000 records
    IF @PerfCounter % @BatchSize = 0
    BEGIN
        SET @BatchId = @BatchId + 1;
    END
    
    SET @PerfCounter = @PerfCounter + 1;
END

PRINT 'PerformanceTestTable seeded: ' + CAST(@@ROWCOUNT as nvarchar(10)) + ' rows';

-- ================================================
-- SECTION 11: Update Statistics
-- ================================================
PRINT 'Updating statistics...';

UPDATE STATISTICS TestEntities;
UPDATE STATISTICS TestDataTypes; 
UPDATE STATISTICS BulkTestEntities;
UPDATE STATISTICS BulkTestComposite;
UPDATE STATISTICS BulkTestIdentity;
UPDATE STATISTICS ConstraintTestTable;
UPDATE STATISTICS ReferenceTable;
UPDATE STATISTICS ParameterTestTable;
UPDATE STATISTICS ScriptTestOrders;
UPDATE STATISTICS ScriptTestOrderItems;
UPDATE STATISTICS PerformanceTestTable;

-- ================================================
-- SECTION 12: Data Verification
-- ================================================
PRINT '';
PRINT '=== Test Data Summary ===';

SELECT 'TestEntities' AS [Table], COUNT(*) AS [Rows] FROM TestEntities UNION ALL
SELECT 'TestDataTypes', COUNT(*) FROM TestDataTypes UNION ALL
SELECT 'BulkTestEntities', COUNT(*) FROM BulkTestEntities UNION ALL
SELECT 'BulkTestComposite', COUNT(*) FROM BulkTestComposite UNION ALL
SELECT 'BulkTestIdentity', COUNT(*) FROM BulkTestIdentity UNION ALL
SELECT 'ReferenceTable', COUNT(*) FROM ReferenceTable UNION ALL
SELECT 'ConstraintTestTable', COUNT(*) FROM ConstraintTestTable UNION ALL
SELECT 'ParameterTestTable', COUNT(*) FROM ParameterTestTable UNION ALL
SELECT 'ScriptTestOrders', COUNT(*) FROM ScriptTestOrders UNION ALL
SELECT 'ScriptTestOrderItems', COUNT(*) FROM ScriptTestOrderItems UNION ALL
SELECT 'PerformanceTestTable', COUNT(*) FROM PerformanceTestTable;

PRINT 'Test data seeding completed successfully!';

-- ================================================
-- SECTION 13: Sample Queries for Verification
-- ================================================
/*
-- Uncomment these queries to verify the data was seeded correctly

-- Basic data verification
SELECT 'Active Entities' as Category, COUNT(*) as Count FROM TestEntities WHERE IsActive = 1
UNION ALL
SELECT 'Inactive Entities', COUNT(*) FROM TestEntities WHERE IsActive = 0;

-- Data type verification
SELECT TOP 1 * FROM TestDataTypes WHERE StringValue IS NOT NULL;

-- Bulk operation verification
SELECT Category, COUNT(*) as Count, AVG(Value) as AvgValue 
FROM BulkTestEntities 
GROUP BY Category 
ORDER BY Category;

-- Constraint verification
SELECT ct.UniqueValue, ct.NotNullValue, rt.RefName
FROM ConstraintTestTable ct
LEFT JOIN ReferenceTable rt ON ct.ForeignKeyValue = rt.RefId;

-- Script test verification
SELECT o.OrderNumber, o.CustomerName, o.TotalAmount, COUNT(oi.OrderItemId) as ItemCount
FROM ScriptTestOrders o
LEFT JOIN ScriptTestOrderItems oi ON o.OrderId = oi.OrderId
GROUP BY o.OrderId, o.OrderNumber, o.CustomerName, o.TotalAmount
ORDER BY o.OrderNumber;
*/