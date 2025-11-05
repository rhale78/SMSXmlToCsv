# SMSXmlToCsv - Core Data Exporter Framework

A flexible and extensible framework for exporting message data to various formats. This project provides a core infrastructure for transforming SMS/MMS message data into different file formats like CSV, JSONL, HTML, etc.

## Project Structure

```
SMSXmlToCsv.Core/
├── Models/                  # Data models
│   ├── Contact.cs          # Contact representation
│   └── Message.cs          # Message representation
├── Exporters/              # Exporter infrastructure
│   ├── IDataExporter.cs    # Core exporter interface
│   └── BaseDataExporter.cs # Abstract base class for exporters
└── Utilities/              # Helper utilities
    └── PathBuilder.cs      # Dynamic path generation with placeholders
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

The framework supports multiple export strategies:

### All-in-One Export
A single file containing messages from all selected contacts:
```
exports/
└── all_messages_2025-11-05.csv
```

### Per-Contact Export
Separate folders for each contact:
```
exports/
└── contacts/
    ├── John_Doe/
    │   └── messages_2025-11-05.csv
    └── Jane_Smith/
        └── messages_2025-11-05.csv
```

### Structured Export with Media and Reports
```
exports/
├── contacts/
│   └── [contact folders]
├── media/
│   └── [media files]
└── reports/
    └── [summary reports]
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
- `PathBuilderTests` - 18 tests covering all placeholder scenarios and edge cases
- `BaseDataExporterTests` - 9 tests covering exporter functionality and validation

All tests pass successfully with 100% coverage of the core framework.

## Future Enhancements

- Concrete exporter implementations (CSV, JSONL, HTML)
- Configuration system for export settings
- Progress reporting for long-running exports
- Batch export capabilities
- Custom placeholder support

## License

See LICENSE file for details.
