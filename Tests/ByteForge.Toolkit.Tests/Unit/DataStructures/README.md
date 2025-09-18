# DataStructures Tests

This directory contains unit tests for the ByteForge.Toolkit DataStructures module, which provides specialized data structures for efficient data organization and manipulation.

## Overview

The DataStructures module includes implementations of common data structures like binary search trees and specialized types like URL processing. These tests ensure that all data structures function correctly and efficiently.

## Test Classes

### BinarySearchTreeTests

Tests for the BinarySearchTree<T> class, which implements a self-balancing binary search tree:

- Insertion and removal of nodes
- Tree balancing operations
- In-order, pre-order, and post-order traversal
- Search operations
- Minimum and maximum value finding
- Handling of duplicate values
- Performance with large datasets
- Edge cases like empty trees and single nodes

### UrlTests

Tests for the Url class, which provides URL parsing and manipulation:

- URL parsing from strings
- Component extraction (scheme, host, port, path, query, fragment)
- URL modification
- Query parameter handling
- URL encoding and decoding
- Relative URL resolution
- URL comparison and equality
- URL validation
- Edge cases like malformed URLs and special characters

## Testing Strategy

The DataStructures tests follow a comprehensive approach that covers:

1. **Core functionality**: Basic operations of each data structure
2. **Performance**: Efficient operation with large datasets
3. **Edge cases**: Handling of unusual or extreme inputs
4. **Correctness**: Validation against expected behaviors
5. **API usability**: Testing the public interface for completeness and consistency

## Test Helpers

These tests may utilize helper classes to assist with test setup and validation:

- **AssertionHelpers**: Contains custom assertions for data structure validation
- Custom comparison and equality methods for specialized testing

## Notes

The DataStructures module focuses on providing efficient, specialized data structures that aren't available in the standard .NET Framework collections. These implementations are optimized for specific use cases while maintaining broad compatibility.