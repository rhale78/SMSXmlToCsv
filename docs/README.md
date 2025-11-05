# Documentation

Welcome to the SMSXmlToCsv documentation!

## Overview

SMSXmlToCsv is a modern .NET 9 application that imports messages from various messaging platforms and exports them to multiple formats.

## Quick Links

- [Main README](../README.md) - Project overview and quick start
- [Legacy Code Documentation](../old/docs/) - Documentation for the archived v0.7-1.7 codebase
- [Feature Comparison](../old/FEATURE_COMPARISON.md) - Comparison between legacy and current versions

## Current Documentation

The current version (v2.0+) focuses on clean architecture and extensibility:

### Getting Started
- See the main [README.md](../README.md) for installation and basic usage
- Build: `dotnet build SMSXmlToCsv.sln`
- Run: `dotnet run --project src/SMSXmlToCsv`

### Architecture

The current version follows these principles:
- **Pluggable Importers**: Implement `IDataImporter` to add new import sources
- **Pluggable Exporters**: Implement `IDataExporter` to add new export formats
- **Service-Oriented**: Business logic in service classes
- **Unified Data Model**: All messages normalized to a common format

### Supported Import Sources

- Android SMS Backup & Restore (XML)
- Facebook Messenger (JSON)
- Instagram Messages (JSON)
- Google Takeout - Hangouts (JSON) and Voice (HTML)
- Gmail (.mbox)
- Signal Desktop (placeholder - requires external decryption)

### Supported Export Formats

- CSV - Comma-separated values
- JSONL - JSON Lines format
- HTML - Chat-like interface
- Parquet - Columnar format for analytics

### Adding New Importers

1. Create a class implementing `IDataImporter`
2. Parse the source format
3. Transform to unified `Message` model
4. Add to the importer list in `Program.cs`

### Adding New Exporters

1. Create a class implementing `IDataExporter`
2. Serialize messages to target format
3. Follow naming conventions
4. Add to the exporter list in `Program.cs`

## Legacy Features

Many advanced features from the legacy codebase are planned for porting:

- Network graph visualization
- Contact merge functionality
- Thread analysis
- Response time analysis
- Advanced statistics
- Interactive search
- PDF report generation
- And more...

See [Feature Comparison](../old/FEATURE_COMPARISON.md) for details and [GitHub Issues](../../issues) for tracking.

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Follow the existing code style
4. Submit a pull request

## Support

- [GitHub Issues](../../issues) - Report bugs and request features
- [Discussions](../../discussions) - Ask questions and share ideas

---

**Version**: 2.0+  
**Last Updated**: November 2025
