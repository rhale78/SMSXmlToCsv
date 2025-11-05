# SMSXmlToCsv

A .NET 9 console application for importing, processing, and exporting messages from various platforms (SMS, Facebook, Instagram, Google Takeout, Signal, etc.).

## Features

- **Automatic Project Backup**: On every build, automatically backs up the project to a timestamped directory
- **Modular Architecture**: Pluggable importer and exporter framework for easy extensibility
- **Multiple Data Sources**: Support for various message platforms
- **Multiple Export Formats**: CSV, JSONL, HTML, Parquet
- **Rich Console UI**: Built with Spectre.Console for an enhanced terminal experience
- **Comprehensive Logging**: Powered by Serilog

## Project Structure

```
SMSXmlToCsv/
├── src/
│   └── SMSXmlToCsv/
│       ├── Configuration/       # Configuration classes
│       ├── Services/            # Business logic and services
│       ├── BuildTasks/          # Build-time tasks
│       ├── Program.cs           # Application entry point
│       └── appsettings.json     # Configuration file
├── SMSXmlToCsv.sln
└── LICENSE
```

## Requirements

- .NET 9.0 SDK
- Visual Studio 2022 or later (optional)

## Building

```bash
dotnet build
```

The project includes an automatic backup system that creates a timestamped backup of all source files (excluding bin, obj, .git, etc.) before each build.

## Running

```bash
dotnet run --project src/SMSXmlToCsv
```

## Configuration

The application is configured via `appsettings.json`. Key settings include:

- **BackupSettings**: Configure automatic backups
  - `Enabled`: Enable/disable automatic backups
  - `BackupDirectory`: Path template for backups (supports placeholders like `{date}`, `{time}`, `{project}`)
  - `ExcludedDirectories`: List of directories to exclude from backups
  - `ExcludedFiles`: List of file patterns to exclude from backups

## Coding Standards

This project follows strict coding standards:

- **No `var` keyword**: Use explicit types for better readability
- **Object-Oriented Design**: Modular, maintainable, and extensible
- **One class per file**: Clear organization and easy navigation
- **Minimal Program.cs**: Business logic in separate service classes

## License

See LICENSE file for details.
