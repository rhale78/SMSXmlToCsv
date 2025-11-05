# Legacy Code Archive

This directory contains the original, feature-rich version of SMSXmlToCsv (versions 0.7.0 - 1.7.0). While this codebase has been superseded by the more modular and maintainable version in `/src`, it contains several advanced features that are valuable references for future development.

## Directory Structure

```
old/
├── code/          # Original SMSXmlToCsv implementation
│   └── SMSXmlToCsv/
└── docs/          # Original documentation
```

## Why This Code Was Archived

The legacy codebase grew organically and accumulated many features over time. While functional and feature-rich, it had several architectural challenges:

- Large, monolithic files (Program.cs with 1000+ lines)
- Mixed concerns and responsibilities
- Less modular architecture
- Harder to maintain and extend

The new version (`/src/SMSXmlToCsv/`) was built with:
- Clean architecture and separation of concerns
- Pluggable importer/exporter interfaces
- Better testability
- Clearer code organization
- Modern .NET 9 practices

## Notable Features in Legacy Code

The legacy code contains several advanced features that work well and can serve as reference implementations:

### ✅ Working Well
- **Network Graph Visualization**: Interactive D3.js-based network graphs showing contact relationships and topics
- **Thread Analysis**: Automatic conversation thread detection with configurable timeouts
- **Response Time Analysis**: Message-to-message response timing with per-contact statistics
- **Advanced Statistics**: Comprehensive messaging statistics with time-based patterns
- **Interactive Search**: Keyword search across all messages with context highlighting
- **PDF Report Generation**: Enhanced PDF reports with charts and visualizations
- **Contact Merge Interface**: Interactive UI for merging duplicate contacts
- **Command-line Interface**: Comprehensive CLI for automation and batch processing
- **Template System**: Configuration templates for common use cases
- **Enhanced Error Handling**: Continue-on-error mode with detailed logging

### ⚠️ Needs Improvement
- **Sentiment Analysis**: Implementation exists but accuracy could be improved
- **Google Takeout Support**: Partially implemented but not fully tested
- **Performance**: Some features could benefit from optimization for large datasets

## Features to Port to New Code

See the GitHub issues for tracking the implementation of these features in the new codebase:

1. Network graph visualization (#TBD)
2. Thread analysis (#TBD)
3. Response time analysis (#TBD)
4. Advanced statistics (#TBD)
5. Interactive search (#TBD)
6. PDF report generation (#TBD)
7. Contact merge functionality (#TBD)
8. Multiple export format selection (#TBD)
9. Enhanced Google Takeout support (#TBD)
10. Improved sentiment analysis (#TBD)

## Using the Legacy Code

### Building
```bash
# Note: The solution file now points to the new code in /src
# To build the legacy code, you would need to create a separate solution file
cd old/code/SMSXmlToCsv
dotnet build SMSXmlToCsv.csproj
```

### Running
```bash
cd old/code/SMSXmlToCsv/bin/Debug/net9.0
./SMSXmlToCsv
```

### Documentation
See the `docs/` directory for comprehensive documentation:
- `COMMAND_LINE.md` - Complete CLI reference
- `CONFIGURATION.md` - All configuration options
- `INSTALLATION.md` - Installation guide
- `KNOWN_ISSUES.md` - Known issues and workarounds
- `TROUBLESHOOTING.md` - Troubleshooting guide

## Security Note

This code has been reviewed for PII (Personally Identifiable Information) leaks:
- ✅ No real phone numbers, emails, or personal information found
- ✅ No machine names or hostnames exposed
- ✅ All example data uses placeholder values

## Version History

The legacy code represents versions 0.7.0 through 1.7.0 of the project:
- **v0.7.0** (2025-10-28): Documentation and version management
- **v1.7.0** (2024-12-XX): AI/ML features (sentiment analysis, network graphs)
- **v1.6.0** (2024-11-XX): Analysis features (threads, response times)
- **v1.5.0** (2024-10-XX): Filtering and configuration enhancements
- **v1.4.0** (2024-09-XX): Command-line interface
- **v1.3.0** (2024-08-XX): Interactive features
- **v1.2.0** (2024-07-XX): Database and export features
- **v1.1.0** (2024-06-XX): Multi-format support
- **v1.0.0** (2024-05-XX): Initial release

See `../CHANGELOG.md` for detailed version history.

## License

See the main LICENSE file in the repository root.

## Acknowledgments

This legacy codebase was developed with significant assistance from:
- **Claude Sonnet 4.5** (Anthropic) - Code generation and documentation
- **VibeCoded Development Environment**

---

**Status**: Archived  
**Last Updated**: November 2025  
**Maintained**: No (reference only)  
**Replacement**: `/src/SMSXmlToCsv/`
