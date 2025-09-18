# Mail Tests

This directory contains unit tests for the ByteForge.Toolkit Mail module, which provides functionality for email operations and attachment handling.

## Overview

The Mail module offers tools for sending emails and managing email attachments efficiently. These tests ensure that email functionality works correctly under various scenarios.

## Test Classes

### EmailAttachmentHandlerTests

Tests for the EmailAttachmentHandler class, which manages email attachments:

- Attachment creation and management
- Temporary file handling
- Attachment compression
- Multi-part attachment processing
- Attachment size limitation
- File type filtering
- Resource cleanup
- Error handling during attachment processing
- Performance with large attachments

## Testing Strategy

The Mail tests follow a comprehensive approach that covers:

1. **Core functionality**: Basic attachment handling operations
2. **Resource management**: Proper creation and cleanup of temporary files
3. **Performance**: Efficient handling of large or numerous attachments
4. **Error scenarios**: Proper response to invalid inputs or error conditions
5. **Edge cases**: Handling unusual scenarios like empty attachments or size limits

## Test Helpers

These tests utilize helper classes:

- **TempFileHelper**: Manages temporary files for testing attachment operations
- **AssertionHelpers**: Contains custom assertions for attachment validation

## Notes

The Mail module focuses on robust email attachment handling, with particular attention to:

1. **Security**: Safe handling of attachments and temporary files
2. **Performance**: Efficient processing of attachments, especially large ones
3. **Resource management**: Proper cleanup of temporary files
4. **Flexibility**: Support for various attachment processing methods

While actual email sending is part of the Mail module, the tests focus primarily on attachment handling to avoid sending test emails during automated testing.