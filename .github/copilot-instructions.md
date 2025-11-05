---
name: SMSXmlToCsv Repository Instructions
description: Copilot instructions for the entire repository
---

# Project Overview

This repository contains a utility for converting SMS message data from XML format to CSV format. The tool is designed to parse XML files containing SMS messages and export them in a structured CSV format for analysis, backup, or migration purposes.

## Repository Purpose

- Convert SMS data from XML format to CSV
- Preserve message metadata (sender, receiver, timestamp, message body)
- Handle various XML schemas from different SMS backup applications
- Provide a simple, reliable conversion utility

## Technology Stack

- Language: To be determined based on implementation
- Primary focus: Data parsing and transformation
- File I/O operations

## Coding Standards

### General Guidelines

- **Code Quality**: Write clean, readable, and maintainable code
- **Documentation**: Document complex logic and add comments for non-obvious code sections
- **Error Handling**: Implement robust error handling for file operations and XML parsing
- **Input Validation**: Validate XML structure and content before processing
- **Testing**: Write unit tests for parsing logic and edge cases

### Code Style

- Follow language-specific style guides and conventions
- Use meaningful variable and function names
- Keep functions focused and single-purpose
- Avoid hardcoding values; use configuration where appropriate

### XML Parsing

- Handle malformed XML gracefully
- Support multiple XML schema formats when possible
- Validate XML structure before processing
- Report clear error messages for parsing failures

### CSV Output

- Use standard CSV format with proper escaping
- Include headers in the output file
- Handle special characters (commas, quotes, newlines) correctly
- Ensure UTF-8 encoding for international characters

## Build and Test

### Building the Project

- Build instructions will depend on the chosen technology stack
- Ensure all dependencies are properly documented
- Provide clear setup instructions in README

### Testing

- Test with various XML formats and schemas
- Include edge cases: empty files, malformed XML, special characters
- Validate CSV output format and content accuracy
- Test with large files to ensure performance

### Running Tests

- Run all tests before submitting pull requests
- Ensure tests pass in CI/CD pipeline
- Add new tests for new features or bug fixes

## File Structure

```
.
├── .github/              # GitHub-specific files
│   └── copilot-instructions.md
├── LICENSE               # MIT License
├── README.md             # Project documentation (to be added)
├── src/                  # Source code (to be added)
└── tests/                # Test files (to be added)
```

## Contribution Guidelines

### Pull Request Requirements

- All PRs must include relevant tests
- Code must pass all existing tests
- Follow the coding standards outlined above
- Update documentation if adding new features
- Include a clear description of changes

### Commit Messages

- Use clear, descriptive commit messages
- Start with a verb (Add, Fix, Update, Remove, etc.)
- Keep the first line under 72 characters
- Add detailed description if needed

## Data Privacy and Security

- **Important**: Do not commit real SMS data or personal information
- Use anonymized or synthetic data for testing
- Be mindful of privacy when processing user data
- Document any data handling requirements

## Performance Considerations

- Optimize for large XML files (handle files with thousands of messages)
- Consider streaming/chunked processing for very large files
- Monitor memory usage during processing
- Provide progress indicators for long-running operations

## Error Handling

- Provide clear, actionable error messages
- Log errors appropriately
- Fail gracefully when encountering invalid input
- Include error recovery mechanisms where possible

## Future Enhancements

Consider these areas for future development:
- Support for multiple XML schema formats
- Batch processing of multiple files
- Command-line interface with options
- Output format options (different CSV layouts)
- Filtering and sorting capabilities
