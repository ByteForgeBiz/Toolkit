# Database Tests

This directory contains unit tests for the ByteForge.Toolkit Data Database module, which provides database access and operations functionality.

## Overview

The Database module offers a flexible and efficient way to interact with various database systems, with a focus on SQL Server and ODBC connections. These tests ensure that database operations work correctly and efficiently.

## Test Classes

### DBAccessCoreTests

Tests for the core functionality of the DBAccess class:

- Constructor and initialization
- Database configuration through DatabaseOptions
- Connection string generation
- Database type detection and configuration
- Basic property access
- Connection testing and management
- RecordsAffected and LastException tracking
- Performance and resource usage

### DBAccessMethodsSQLServerTests

Tests for SQL Server-specific database operations:

- CRUD operations (Create, Read, Update, Delete)
- Parameter handling
- Transaction management
- Stored procedure execution
- Bulk operations
- Error handling for SQL Server-specific scenarios
- Data type mapping

### DBAccessMethodsODBCTests

Tests for ODBC database operations:

- Connection to ODBC data sources
- Basic CRUD operations
- Parameter handling with ODBC syntax
- Data type conversion for ODBC
- Error handling in ODBC context

### DBAccessParameterParsingTests & DBAccessParameterParsingSimpleTests

Tests for parameter parsing and handling:

- Named parameter detection
- Parameter value substitution
- Various parameter data types
- Complex parameter expressions
- Performance of parameter parsing
- Edge cases in parameter syntax

### DBAccessScriptExecutionTests

Tests for SQL script execution:

- Multi-statement script execution
- Script parsing and batching
- Error handling during script execution
- Transaction management for scripts
- GO statement handling
- Script performance

### BulkDbProcessorTests & BulkDbProcessorEdgeCaseTests

Tests for bulk database operations:

- Bulk insert functionality
- Mapping between object collections and database tables
- Attribute-based column mapping
- Performance with large datasets
- Transaction support in bulk operations
- Error handling during bulk operations
- Edge cases like empty collections, missing mappings

## Testing Strategy

The Database tests follow a comprehensive approach that covers:

1. **Core functionality**: Basic database operations
2. **Data access patterns**: Various ways to interact with databases
3. **Performance**: Efficient database operations with large datasets
4. **Error handling**: Proper response to database errors
5. **Resource management**: Connection and transaction handling
6. **Multi-database support**: SQL Server and ODBC compatibility

## Test Helpers

These tests utilize helper classes located in the `Tests\ByteForge.Toolkit.Tests\Helpers` directory:

- **DatabaseTestHelper**: Provides utilities for database testing
- **TempFileHelper**: Manages temporary files for scripts
- **AssertionHelpers**: Contains custom assertions for database validation

## Test Prerequisites

Many of these tests require a working database connection. The tests are configured to use:

- A SQL Server instance (local or remote)
- Test database "TestUnitDB"
- Windows Authentication (Integrated Security)

Tests will be skipped if the database connection cannot be established.
