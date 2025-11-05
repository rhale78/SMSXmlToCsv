# SMSXmlToCsv - Core Data Exporter Framework

A flexible and extensible framework for exporting message data to various formats. This project provides a core infrastructure for transforming SMS/MMS message data into different file formats like CSV, JSONL, HTML, etc.

## Project Structure

```
SMSXmlToCsv.Core/
├── Models/                     # Data models
│   ├── Contact.cs             # Contact representation
│   ├── Message.cs             # Message representation
│   └── ExportStrategy.cs      # Export strategy enumeration
├── Exporters/                 # Exporter infrastructure
│   ├── IDataExporter.cs       # Core exporter interface
│   ├── BaseDataExporter.cs    # Abstract base class for exporters
│   ├── CsvExporter.cs         # CSV format exporter
│   └── ExportOrchestrator.cs  # Orchestrates exports with routing logic
└── Utilities/                 # Helper utilities
    └── PathBuilder.cs         # Dynamic path generation with placeholders
```

## Features

### Core Exporter Interface

The `IDataExporter` interface defines the contract for all data exporters:

```csharp
public interface IDataExporter
{
    string FileExtension { get; }
    Task ExportAsync(IEnumerable<Message> messages, string outputDirectory, string baseFileName);
}
```

### Path Builder with Placeholders

The `PathBuilder` class supports dynamic path generation with the following placeholders:

- **Date & Time:**
  - `{date}` - Current date in yyyy-MM-dd format
  - `{time}` - Current time in HH-mm-ss format
  - `{datetime}` - Combined date and time in yyyy-MM-dd_HH-mm-ss format
  - `{year}` - Current year (yyyy)
  - `{month}` - Current month (MM)
  - `{day}` - Current day (dd)

- **Contact Information:**
  - `{contact_name}` - Contact's display name (sanitized for file system)
  - `{contact_id}` - Contact's unique identifier
  - `{phone_number}` - Contact's phone number (sanitized)

#### Usage Example

```csharp
var pathBuilder = new PathBuilder();
var contact = new Contact { Name = "John Doe", PhoneNumber = "+1234567890" };

// Generate path with date and contact name
var path = pathBuilder.BuildPath("exports/{date}/contacts/{contact_name}", contact);
// Result: exports/2025-11-05/contacts/John Doe

// Generate path with specific DateTime
var specificDate = new DateTime(2023, 12, 25, 14, 30, 0);
var path2 = pathBuilder.BuildPath("archive/{year}/{month}/{contact_name}", specificDate, contact);
// Result: archive/2023/12/John Doe
```

### Base Exporter Class

The `BaseDataExporter` abstract class provides common functionality:

- Automatic directory creation
- File path management
- Input validation
- Error handling

#### Creating a Custom Exporter

```csharp
public class CsvExporter : BaseDataExporter
{
    public override string FileExtension => "csv";

    protected override async Task ExportToFileAsync(IEnumerable<Message> messages, string filePath)
    {
        // Implement CSV export logic here
        await File.WriteAllTextAsync(filePath, "Header,Content");
    }
}
```

## Export Strategies

The framework supports multiple export strategies through the `ExportOrchestrator` class:

### All-in-One Export
Exports all messages to a single file, regardless of contact:

```csharp
var orchestrator = new ExportOrchestrator(new CsvExporter());
await orchestrator.ExportAsync(messages, "exports", ExportStrategy.AllInOne);
```

Output structure:
```
exports/
└── messages_2025-11-05.csv  (contains all messages)
```

### Per-Contact Export
Exports messages in separate files per contact, organized in contact-specific folders:

```csharp
var orchestrator = new ExportOrchestrator(new CsvExporter());
await orchestrator.ExportAsync(messages, "exports", ExportStrategy.PerContact);
```

Output structure:
```
exports/
└── contacts/
    ├── John_Doe/
    │   └── messages_2025-11-05.csv
    ├── Jane_Smith/
    │   └── messages_2025-11-05.csv
    └── Unknown/  (for messages without a contact)
        └── messages_2025-11-05.csv
```

### Custom File Name Templates
You can customize the output file names using placeholder templates:

```csharp
await orchestrator.ExportAsync(
    messages, 
    "exports", 
    ExportStrategy.PerContact,
    fileNameTemplate: "{contact_name}_backup_{datetime}"
);
```

## Models

### Message
Represents a single SMS or MMS message with properties:
- `Id` - Unique identifier
- `Contact` - Associated contact
- `Body` - Message content
- `Timestamp` - Date and time
- `Type` - Message type (SMS, MMS)
- `IsSent` - Sent/received indicator
- `PhoneNumber` - Associated phone number

### Contact
Represents a contact with properties:
- `Id` - Unique identifier
- `Name` - Display name
- `PhoneNumber` - Phone number

### ExportStrategy
Enumeration defining export organization strategies:
- `AllInOne` - Export all messages to a single file
- `PerContact` - Export messages in separate files per contact

## Complete Usage Example

```csharp
using SMSXmlToCsv.Core.Models;
using SMSXmlToCsv.Core.Exporters;

// Create sample data
var contact1 = new Contact { Id = "1", Name = "John Doe", PhoneNumber = "+1234567890" };
var contact2 = new Contact { Id = "2", Name = "Jane Smith", PhoneNumber = "+0987654321" };

var messages = new List<Message>
{
    new Message 
    { 
        Id = "1", 
        Contact = contact1, 
        Body = "Hello from John",
        Timestamp = DateTime.Now,
        Type = "SMS",
        IsSent = true,
        PhoneNumber = contact1.PhoneNumber
    },
    new Message 
    { 
        Id = "2", 
        Contact = contact2, 
        Body = "Hi from Jane",
        Timestamp = DateTime.Now,
        Type = "SMS",
        IsSent = false,
        PhoneNumber = contact2.PhoneNumber
    }
};

// Export using CSV format with per-contact strategy
var csvExporter = new CsvExporter();
var orchestrator = new ExportOrchestrator(csvExporter);

await orchestrator.ExportAsync(
    messages,
    "output/exports",
    ExportStrategy.PerContact,
    fileNameTemplate: "conversation_{date}"
);
```

## Building and Testing

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal
```

## Test Coverage

The framework includes comprehensive unit tests:
- **PathBuilderTests** - 18 tests covering all placeholder scenarios and edge cases
- **BaseDataExporterTests** - 9 tests covering exporter functionality and validation
- **CsvExporterTests** - 5 tests covering CSV export functionality and escaping
- **ExportOrchestratorTests** - 7 tests covering export strategies and routing logic

**Total: 37 tests, all passing** ✅

Tests validate:
- Placeholder replacement for all date/time and contact fields
- File name sanitization across platforms
- CSV field escaping (commas, quotes, newlines)
- All-in-one and per-contact export strategies
- Orphaned message handling (messages without contacts)
- Custom file name templates
- Input validation and error handling

## Future Enhancements

- Additional format exporters (JSONL, HTML, XML)
- Batch export capabilities for large datasets
- Progress reporting for long-running exports
- Media file handling and organization
- Report generation (statistics, summaries)
- Configuration system for export settings
- Custom placeholder support
- Compression options for output files

## License

See LICENSE file for details.
