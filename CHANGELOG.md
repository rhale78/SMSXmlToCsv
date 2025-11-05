# Changelog - SMS Backup XML Converter

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.0.0] - 2025-11-05 (Current)

### 🎉 Major Release - Architecture Redesign & Code Reorganization

This release represents a complete architectural overhaul focused on clean code, modularity, and maintainability.

### ✨ Added
- **Multiple Export Format Selection**
  - Export to multiple formats simultaneously (CSV + HTML + Parquet, etc.)
  - Progress tracking for each export
  - Individual success/failure reporting per format
  - User-friendly multi-select interface

- **Legacy Code Archive**
  - Moved v0.7-1.7 codebase to `/old/code/`
  - Comprehensive legacy documentation in `/old/docs/`
  - Feature comparison document (`/old/FEATURE_COMPARISON.md`)
  - 13 GitHub issues created for feature porting roadmap
  - No PII found in archived code or documentation

- **New Documentation Structure**
  - Updated main README with legacy code reference
  - Created `/docs/` directory for current version
  - Moved wiki pages to `/old/wiki/`
  - Deployment guides archived to `/old/docs/`

### 🏗️ Architecture Changes
- **Pluggable Framework**: Clean importer/exporter interfaces
- **Service-Oriented Design**: Business logic separated from UI
- **One Class Per File**: Improved navigation and maintainability
- **Explicit Types**: No `var` keyword for clarity
- **Smaller Program.cs**: ~300 lines vs 1000+ in legacy

### 📋 Legacy Features To Port
See `ISSUES_TO_CREATE.md` for complete list:
- Network graph visualization (high priority)
- Contact merge functionality (high priority)
- Command-line interface (high priority)
- Thread analysis
- Response time analysis
- Interactive search
- PDF report generation
- And more...

### 🔄 Migration Notes
- Legacy code remains accessible in `/old/` directory
- Current version is actively maintained in `/src/`
- Both codebases fully documented

---

## [0.7.0] - 2025-10-28 (Legacy Archive)

### ?? Major Release - Documentation & Version Management

This release establishes v0.7 as the first fully documented version with comprehensive user and developer documentation.

### Added
- **Version Management System** (`Version.cs`)
  - Centralized version string management
  - Version display at startup
  - Version in help command
  - Easy to update for future releases
  
- **Comprehensive Documentation Suite**
  - `README.md` - Complete project overview (~3,000 words)
  - `docs/DOCUMENTATION_INDEX.md` - Navigation hub
  - `docs/COMMAND_LINE.md` - Complete CLI reference (~4,000 words)
  - `docs/CONFIGURATION.md` - All configuration options (~4,500 words)
  - `docs/KNOWN_ISSUES.md` - 22 documented issues with workarounds (~3,500 words)
  - `docs/PROJECT_SUMMARY.md` - Comprehensive project overview (~3,000 words)
  
- **VibeCoded Attribution**
  - Prominent mention in README
  - Development timeline (3 days to MVP)
  - Claude Sonnet 4.5 attribution
  - Visual Studio 2026 development environment noted

### Fixed
- Contact merge skip functionality now persistent
- Sentiment analysis progress display
- Unknown contact filtering edge cases

### Documentation
- 50+ code examples with NO PII
- 30+ configuration examples
- 20+ reference tables
- 10 complete usage examples
- Known issues with workarounds
- Future development roadmap

### Meta
- **Total Documentation**: ~20,000 words
- **Build Status**: ? Passing
- **Known Issues**: 22 documented
- **PII in Docs**: 0 (zero)

---

## [1.7.0] - 2024-12-XX

### Added - ML & Advanced Features
- **AI-Powered Sentiment Analysis**
  - Ollama integration for local AI
  - Per-message emotion detection
  - Sentiment statistics and trends
  
- **Network Graph Visualization**
  - Interactive D3.js network graphs
  - AI-powered topic detection
  - Contact and topic relationships
  - Unlimited topics mode with filtering
  
- **Conversation Clustering**
  - AI-based similarity grouping
  - Automatic cluster labeling
  - Export to JSON format

- **Enhanced PDF Reports**
  - AI insights integration
  - Sentiment trend charts
  - Topic frequency analysis
  - Response time visualizations

### Changed
- Network graph now uses unlimited topic detection with minimum message threshold
- PDF reports enhanced with AI-generated insights when available
- Improved topic deduplication in network graphs

### Fixed
- Network graph topic filtering (zero-value topics)
- HTML export direction indicators
- MMS file path organization

---

## [1.6.0] - 2024-11-XX

### Added - Analysis Features
- **Thread Analysis**
  - Automatic conversation thread detection
  - Configurable timeout between threads
  - Thread statistics export
  
- **Response Time Analysis**
  - Message-to-message response timing
  - Per-contact response statistics
  - Average response time calculations
  
- **Advanced Statistics**
  - Comprehensive messaging statistics
  - Time-based patterns
  - Word count analysis
  - Export to JSON and Markdown
  
- **Interactive Search**
  - Keyword search across all messages
  - Context highlighting
  - Contact filtering
  - Export search results

---

## [1.5.0] - 2024-10-XX

### Added - Filtering & Configuration
- **Date Range Filtering**
  - Filter by start date
  - Filter by end date
  - Command-line and config file support
  
- **Column Selection**
  - Choose which fields to export
  - Required columns always included
  - Reduces file size for large exports

- **Contact Pre-Selection**
  - Pre-configure contacts in config file
  - Automation-friendly
  - Combine with date filtering
  
- **Enhanced Error Handling**
  - Continue-on-error mode
  - Detailed error logging
  - Error summary at completion
  
- **Improved Logging**
  - Console logging option
  - PII masking in logs
  - Structured logging with Serilog

### Fixed
- SMIL file parsing edge cases
- MMS extraction path issues
- Memory usage with large backups

---

## [1.4.0] - 2024-09-XX

### Added - Command-Line Interface
- Comprehensive command-line parser
- Automation support
- Batch processing capability
- Template system for common configurations

### Changed
- Interactive menu improvements
- Spectre.Console integration for better UI

---

## [1.3.0] - 2024-08-XX

### Added - Interactive Features
- Interactive configuration menu
- Visual feature toggles
- In-app help system
- Contact merge interface

### Fixed
- Unknown contact handling
- Phone number formatting

---

## [1.2.0] - 2024-07-XX

### Added - Database & Export Features
- SQLite database export
- HTML chat page export
- Markdown export
- PostgreSQL SQL script generation
- MySQL SQL script generation

### Changed
- Improved folder organization
- Enhanced MMS extraction

---

## [1.1.0] - 2024-06-XX

### Added - Multi-Format Support
- JSON Lines export
- Apache Parquet export
- Per-contact splitting
- Contact filtering

### Fixed
- CSV encoding issues
- Large file performance

---

## [1.0.0] - 2024-05-XX

### Added - Initial Release
- XML to CSV conversion
- MMS attachment extraction
- Basic contact splitting
- Configuration file support
- .env file support

---

## Version Numbering

This project uses [Semantic Versioning](https://semver.org/):
- **Major**: Breaking changes or significant rewrites
- **Minor**: New features, backward compatible
- **Patch**: Bug fixes, documentation updates

### Version History Summary

| Version | Date | Major Changes |
|---------|------|---------------|
| 0.7.0 | 2025-10-28 | Documentation, version management |
| 1.7.0 | 2024-12-XX | AI/ML features |
| 1.6.0 | 2024-11-XX | Analysis features |
| 1.5.0 | 2024-10-XX | Filtering & configuration |
| 1.4.0 | 2024-09-XX | Command-line interface |
| 1.3.0 | 2024-08-XX | Interactive menus |
| 1.2.0 | 2024-07-XX | Database exports |
| 1.1.0 | 2024-06-XX | Multi-format support |
| 1.0.0 | 2024-05-XX | Initial release |

---

## Future Releases

### [0.8.0] - Planned Q2 2025

#### Planned Features
- **Google Takeout Integration**
  - Google Hangouts import
  - Gmail conversation import
  - Google Voice import
  
- **Apple Ecosystem Support**
  - iMessage backup import
  - Apple SMS backup support
  
- **Platform Expansion**
  - Facebook Messenger export
  - Instagram DM support
  - Telegram export
  - Signal backup support
  
- **Performance Improvements**
  - Network graph optimization
  - Memory usage reduction
  - Parallel processing for large datasets
  
- **Enhanced Analytics**
  - Conversation summarization (AI)
  - Relationship strength scoring

#### Features NOT Planned
- ? WhatsApp backup support (different priorities)
- ? Emoji usage analysis (not prioritized)
- ? Multi-language sentiment (English only for now)
- ? Language detection/translation (not on roadmap)

### [1.0.0] - Planned Q3 2025

#### Planned Features
- **Web Interface**
  - Browser-based UI
  - Real-time progress tracking
  - Interactive configuration
  - Report viewer
  
- **Advanced Visualization**
  - Timeline views
  - Heat maps
  - Relationship strength scoring
  - Activity patterns

---

## Breaking Changes

### None Yet

This project maintains backward compatibility. Configuration files from v1.0+ work with latest version.

---

## Deprecations

### None Yet

All features remain supported.

---

## Security Updates

### None Required

- No vulnerabilities reported
- All dependencies up to date
- No cloud services (local processing only)

---

## Contributors

- **Claude Sonnet 4.5** (Anthropic) - All code and documentation
- **Human Developer** - Requirements, testing, oversight

---

## Links

- **Repository**: [GitHub](https://github.com/rhale78/SMSXmlToCsv)
- **Issues**: [Report Bugs](https://github.com/rhale78/SMSXmlToCsv/issues)
- **Releases**: [Download](https://github.com/rhale78/SMSXmlToCsv/releases)
- **Documentation**: [docs/](docs/)

---

**Changelog Format**: Based on [Keep a Changelog](https://keepachangelog.com/)  
**Versioning**: [Semantic Versioning](https://semver.org/)  
**Last Updated**: Version 0.7.0 (October 28, 2025)
