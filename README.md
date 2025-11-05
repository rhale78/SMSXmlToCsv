# SMSXmlToCsv

A comprehensive .NET 9 console application for importing, consolidating, and exporting messages from various messaging platforms. Convert your message history from Android SMS, Facebook Messenger, Instagram, Google Hangouts, Google Voice, Gmail, and more into multiple convenient formats.

> **📦 Legacy Code Archive**: Looking for the original feature-rich version with network graphs, sentiment analysis, and advanced analytics? See the [/old directory](old/README.md) for the archived legacy codebase and documentation. The current version focuses on clean architecture and extensibility.

## Features

### 🚀 Core Capabilities

- **Automatic Project Backup**: On every build, automatically backs up the project to a timestamped directory
- **Unified Data Model**: All messages from different platforms are normalized into a consistent format
- **Timezone Normalization**: All timestamps are converted to UTC for consistency
- **Contact Management**: Supports multiple identifiers (phone numbers, emails) per contact

### 📥 Supported Import Sources

- **Android SMS Backup & Restore** - XML format (SMS and MMS)
- **Facebook Messenger** - JSON data export
- **Instagram Messages** - JSON data export
- **Google Takeout** - Hangouts (JSON) and Voice (HTML)
- **Gmail** - .mbox email archives
- **Signal Desktop** - Placeholder (requires external decryption tools)

### 📤 Supported Export Formats

- **CSV** - Comma-separated values for spreadsheet analysis
- **JSONL** - JSON Lines format for efficient processing
- **HTML** - Beautiful chat-like interface with styling
- **Parquet** - Columnar format optimized for big data analytics

### 🎯 Export Strategies

- **All-in-One**: Single file containing all messages
- **Per-Contact**: Separate files organized by contact in dedicated folders

## Project Structure

```
SMSXmlToCsv/
├── .github/
│   └── workflows/
│       └── dotnet-ci-cd.yml      # CI/CD automation
├── src/
│   └── SMSXmlToCsv/
│       ├── Exporters/            # Data export implementations
│       ├── Importers/            # Data import implementations
│       ├── Models/               # Core data models
│       ├── Services/             # Business logic and services
│       ├── Program.cs            # Application entry point
│       └── appsettings.json      # Configuration file
├── old/                          # Legacy code archive (v0.7-1.7)
│   ├── code/                     # Original implementation
│   ├── docs/                     # Original documentation
│   └── README.md                 # Legacy code documentation
├── tools/                        # Build and utility tools
├── SMSXmlToCsv.sln
├── README.md
└── LICENSE
```

## Requirements

- .NET 9.0 SDK or Runtime
- Visual Studio 2022 or later (optional, for development)
- Supported platforms: Windows, Linux, macOS

## Installation

### Option 1: Download Pre-built Release
1. Download the latest release for your platform from [Releases](../../releases)
2. Extract the archive
3. Run the executable

### Option 2: Build from Source
```bash
git clone https://github.com/rhale78/SMSXmlToCsv.git
cd SMSXmlToCsv
dotnet build --configuration Release
```

## Usage

### Running the Application

```bash
dotnet run --project src/SMSXmlToCsv
```

Or run the compiled executable directly from `bin/Release/net9.0/`

### Configuration

The application is configured via `appsettings.json`:

```json
{
  "BackupSettings": {
    "Enabled": true,
    "BackupDirectory": "../../../Backups/{date}/{time}",
    "ExcludedDirectories": [".git", ".github", "bin", "obj", ...],
    "ExcludedFiles": ["*.tmp", "*.cache", "*.log"]
  },
  "Serilog": {
    "MinimumLevel": "Information",
    ...
  }
}
```

#### Backup Path Placeholders

- `{date}` - Current date (yyyy-MM-dd)
- `{time}` - Current time (HH-mm-ss)
- `{datetime}` - Combined date and time
- `{project}` - Project name
- `{contact_name}` - Contact name (for per-contact exports)

## Architecture

### Data Models

- **Contact**: Represents a person with name, phone numbers, and emails
- **Message**: Unified message structure with sender, recipient, timestamp, body, and attachments
- **MediaAttachment**: Represents attached files (images, videos, audio)
- **MessageDirection**: Enum for sent/received/unknown

### Pluggable Framework

#### Importers
Implement `IDataImporter` interface:
```csharp
public interface IDataImporter
{
    string SourceName { get; }
    Task<IEnumerable<Message>> ImportAsync(string sourcePath);
}
```

#### Exporters
Implement `IDataExporter` interface:
```csharp
public interface IDataExporter
{
    string FileExtension { get; }
    Task ExportAsync(IEnumerable<Message> messages, string outputDirectory, string baseFileName);
}
```

## CI/CD

The project includes automated CI/CD via GitHub Actions:

- **Continuous Integration**: Builds and tests on every push and pull request
- **Automated Releases**: Creates draft releases on merges to main
- **Multi-Platform Builds**: Linux, Windows, and macOS executables
- **Semantic Versioning**: Date-based versioning (vYYYY.MM.BUILD_NUMBER)

## Development

### Coding Standards

This project follows strict coding standards:

- ✅ **No `var` keyword**: Use explicit types for clarity
- ✅ **Object-Oriented Design**: Modular, maintainable, and extensible
- ✅ **One class per file**: Clear organization and easy navigation
- ✅ **Minimal Program.cs**: Business logic in separate service classes
- ✅ **Comprehensive logging**: Powered by Serilog
- ✅ **Rich console UI**: Built with Spectre.Console

### Adding a New Importer

1. Create a new class implementing `IDataImporter`
2. Parse the source data format
3. Transform to unified `Message` model
4. Handle errors gracefully

### Adding a New Exporter

1. Create a new class implementing `IDataExporter`
2. Serialize messages to target format
3. Follow naming conventions
4. Handle empty datasets

## Dependencies

- **Spectre.Console** - Rich terminal UI
- **Serilog** - Structured logging
- **CsvHelper** - CSV serialization
- **Parquet.Net** - Parquet file format
- **MimeKit** - Email parsing
- **HtmlAgilityPack** - HTML parsing
- **Microsoft.Extensions.Configuration** - Configuration management

## License

See [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Follow the coding standards
4. Submit a pull request

## Support

For issues, questions, or feature requests, please [open an issue](../../issues).

## Acknowledgments

This project consolidates message data from various platforms, respecting user privacy and data ownership. All processing is done locally on your machine.
