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

### 🧠 AI-Powered Network Graph (NEW)

The application includes advanced AI-powered conversation analysis and network graph visualization:

- **Intelligent Topic Extraction**: Uses Ollama AI to identify topics, people, companies, places, dates, events, and promises discussed in conversations
- **Hierarchical Subtopics**: Automatically detects and visualizes subtopics nested under main topics
- **Response Caching**: File-based caching system to avoid re-processing the same messages (stored in `data/` folder)
- **Configurable Limits**: 
  - `MinMessagesPerContact`: Minimum messages required per contact (-1 for no minimum, default: 2)
  - `MinTopicMessageCount`: Minimum mentions for a topic to be included (-1 for all topics, default: -1)
  - `MaxTopicsPerContact`: Maximum topics per contact (-1 for unlimited, default: -1)
- **Enhanced JSON Parsing**: Robust handling of malformed AI responses with automatic bracket matching and error recovery
- **Entity Detection**: Automatically identifies:
  - **Topics** (general subjects) - Orange nodes
  - **People mentioned** (by name) - Red nodes
  - **Companies** (businesses, organizations) - Deep Orange nodes
  - **Places** (locations, venues) - Light Green nodes
  - **Dates/Events** - Purple nodes
  - **Promises** (commitments) - Cyan nodes
  - **Subtopics** (nested topics) - Amber nodes
- **Interactive D3.js Visualization**: Click nodes to highlight connections, zoom, and pan through the graph
- **Message Count Display**: All entity nodes show the number of messages they were mentioned in
- **Both-Sides Analysis**: Optional analysis of both sent and received messages to capture full conversation context
- **Extended Timeout**: 5-minute HTTP timeout for AI processing of large message batches
- **Unknown Contact Handling**: Properly detects and skips contacts with "Unknown" or "(Unknown)" names

### 📊 Analysis & Reports

- **Thread Analysis**: Detect conversation threads with configurable timeout
- **Response Time Analysis**: Calculate average, median, min, and max response times
- **Advanced Statistics**: Comprehensive message statistics and analytics
- **Sentiment Analysis**: AI-powered sentiment detection (requires Ollama)
- **Message Search**: Interactive search through imported messages
- **PDF Reports**: Generate comprehensive PDF reports with statistics

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

### AI Features Setup (Optional)

For AI-powered features like network graph visualization and sentiment analysis:

1. **Install Ollama**: Download from [https://ollama.ai](https://ollama.ai)
2. **Pull a recommended model**:
   ```bash
   ollama pull llama3.2    # Recommended: good balance of speed and accuracy
   # Or choose from: llama3.1, mistral, phi3, gemma2
   ```
3. **Start Ollama**: The service should be running at `http://localhost:11434`

#### AI Response Cache

The application automatically caches AI responses in a `data/` folder located next to the executable:
- Responses are cached based on message content hash (SHA256)
- Cache persists between runs to avoid reprocessing
- Cache includes contact name, batch number, topics, and timestamp
- Old cache entries (>30 days) can be cleaned up automatically

To clear the cache, simply delete the `data/` directory.

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

## Recent Improvements

### AI & Network Graph Enhancements (Latest)

- **Improved JSON Parsing**: Enhanced error handling with automatic bracket matching and repair for malformed AI responses
- **Extended HTTP Timeout**: Increased from 2 to 5 minutes for reliable AI processing
- **Unknown Contact Detection**: Fixed to properly handle both "Unknown" and "(Unknown)" contact names
- **Company & Place Detection**: Added new entity types for automatic detection of businesses and locations
- **Message Count Accuracy**: Now uses AI-provided message counts instead of recalculating, fixing inflated count issues
- **AI Response Caching**: Implemented SHA256-based file caching to avoid reprocessing messages
  - Dramatically speeds up repeated runs
  - Cache stored in `data/` folder with automatic management
  - Prevents hitting API rate limits
- **Configurable Topic Limits**: Made all limits configurable instead of hardcoded
  - `MinMessagesPerContact`: -1 for no minimum (process all contacts)
  - `MinTopicMessageCount`: -1 to include all topics regardless of frequency
  - `MaxTopicsPerContact`: -1 for unlimited topics per contact
- **Enhanced Logging**: All topics are now logged (not just previews), with output to both log file and debug window
- **Subtopic Support**: Added dedicated node type (group 8) with amber color for hierarchical topic relationships
- **Better Entity Visualization**: Each entity type has distinct colors and is properly labeled in the legend

### Key Bug Fixes

- Fixed topic count inflation issues (was showing 200s when mentioned 3-4 times)
- Resolved "You" side message processing for better two-person conversation analysis
- Improved data completeness for high-volume sources (e.g., Google Talk with 10,000s of messages)
- Enhanced topic linking to contacts to show all relevant topics

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
