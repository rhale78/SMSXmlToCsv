# SMS Backup XML Converter v0.7

**A comprehensive tool for converting Android SMS backup XML files to various formats with advanced analytics and AI-powered insights.**

> ?? **VibeCoded Project**: This application was created exclusively using **Claude Sonnet 4.5** AI agent in **Visual Studio 2026**. The initial version (v0.7) was developed in just **3 days** of AI-assisted programming sessions.

---

## ?? Quick Start

```bash
# Interactive mode (recommended for first-time users)
SMSXmlToCsv.exe

# Basic conversion
SMSXmlToCsv.exe backup.xml --formats parquet

# Full-featured with AI analysis (requires Ollama)
SMSXmlToCsv.exe backup.xml --split enable --sentiment --network-graph --pdf-report
```

---

## ? Key Features

### Core Functionality
- ? **Multi-format export**: CSV, JSON, Parquet, SQLite, HTML, Markdown, PostgreSQL, MySQL
- ? **MMS extraction**: Automatically extract images, videos, and audio with organized folders
- ? **Contact management**: Split by contact, merge duplicates, filter unknown contacts
- ? **Date filtering**: Export specific time ranges
- ? **Column selection**: Choose which fields to export

### Advanced Analytics
- ?? **Thread analysis**: Detect conversation threads with timing
- ?? **Response tracking**: Measure response patterns between contacts
- ?? **Statistics**: Comprehensive messaging statistics and patterns
- ?? **Search**: Interactive keyword search with highlighting

### AI-Powered Features (Requires Ollama)
- ?? **Sentiment analysis**: AI emotion detection per message
- ?? **Topic detection**: Automatic conversation topic extraction
- ?? **Network visualization**: Interactive D3.js graphs of contacts and topics
- ?? **Clustering**: Group similar conversations intelligently
- ?? **PDF reports**: Professional reports with charts and insights

---

## ?? What Does It Do?

This tool transforms Android SMS/MMS backup files (from apps like "SMS Backup & Restore") into structured, analyzable data. Originally a simple XML-to-CSV converter, it evolved into a comprehensive messaging analysis platform.

### Example Output

```
backup.xml
??? Exports_2025-10-28_223045/
    ??? backup.parquet         # Compressed data
    ??? backup.csv      # Excel-friendly
    ??? backup.db   # SQLite database
    ??? Contacts/       # Per-contact exports
    ?   ??? John_Smith_+1234/
    ?   ?   ??? messages.html     # Chat-style view
    ?   ?   ??? messages.parquet
    ?   ?   ??? MMS/       # Extracted media
    ?   ?       ??? photo_001.jpg
    ?   ?       ??? video_001.mp4
    ?   ??? Jane_Doe_+5678/
    ?  ??? ...
    ??? network_graph.html       # Interactive visualization
    ??? network_graph.json
    ??? sentiment_analysis.json
 ??? statistics.json
    ??? statistics.md
??? comprehensive_report.pdf   # Full analysis report
```

---

## ?? Installation

### Requirements
- **.NET 9.0 Runtime** ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- **(Optional) Ollama** for AI features ([Download](https://ollama.ai))

### Steps

1. **Download & Extract**:
   ```bash
   # Download from Releases page
unzip SMSXmlToCsv-v0.7.zip
   cd SMSXmlToCsv
   ```

2. **Configure (Optional)**:
   ```bash
   cp .env.example .env
   # Edit .env with your name and phone
   ```

3. **Run**:
   ```bash
   SMSXmlToCsv.exe
   ```

### For AI Features

```bash
# Install Ollama from https://ollama.ai
ollama pull llama3.2
```

---

## ?? Usage

### Interactive Mode

```bash
SMSXmlToCsv.exe
```

The interactive menu guides you through:
1. File selection (auto-detects XML files)
2. Feature configuration (visual menus)
3. Contact merging (intelligent duplicate detection)
4. Output format selection
5. Processing with real-time progress

### Command-Line Examples

```bash
# Convert with date filter
SMSXmlToCsv.exe backup.xml --date-from 2023-01-01 --date-to 2023-12-31

# Extract MMS only
SMSXmlToCsv.exe backup.xml --mms enable --split enable

# Create searchable database
SMSXmlToCsv.exe backup.xml --sqlite enable --split enable

# Full analysis with AI
SMSXmlToCsv.exe backup.xml --split enable --mms enable --sentiment --network-graph --pdf-report

# Select specific contacts
SMSXmlToCsv.exe backup.xml --select-contacts "John|+1234,Jane|+5678"

# Export only specific columns
SMSXmlToCsv.exe backup.xml --columns "FromName,ToName,DateTime,MessageText"
```

---

## ?? Configuration

### Configuration Files

1. **`.env`** - User identity (not tracked in git)
   ```env
   SMS_USER_NAME=YourName
   SMS_USER_PHONE=+1234567890
   ```

2. **`appsettings.json`** - Feature defaults
   ```json
   {
"Features": {
       "ExtractMMS": "Ask",
       "SplitByContact": "Enable",
    "EnableFiltering": "Disable"
     }
   }
   ```

Priority: **Command-line > .env > appsettings.json > Interactive Menu**

### Saving Configuration

```bash
# Save current settings for future runs
SMSXmlToCsv.exe backup.xml --split enable --mms enable --save-config
```

---

## ?? Documentation

Comprehensive documentation is available in the [`docs/`](docs/) folder:

| Document | Description |
|----------|-------------|
| **[User Guide](docs/USER_GUIDE.md)** | Complete feature walkthrough |
| **[Installation](docs/INSTALLATION.md)** | Detailed setup instructions |
| **[Configuration](docs/CONFIGURATION.md)** | All settings explained with examples |
| **[Command-Line Reference](docs/COMMAND_LINE.md)** | Complete CLI documentation |
| **[Developer Guide](docs/DEVELOPER_GUIDE.md)** | Contributing and extending |
| **[Technical Architecture](docs/TECHNICAL_ARCHITECTURE.md)** | System design |
| **[Troubleshooting](docs/TROUBLESHOOTING.md)** | Common issues and solutions |
| **[Known Issues](docs/KNOWN_ISSUES.md)** | Current limitations and bugs |

---

## ?? Known Issues

### Currently Known Problems

1. **Icon Display (?? Issue)**:
   - Emoji/icons may show as `?/?` or incorrect characters in Windows Console
   - **Cause**: Limited Unicode support in Windows Console (cmd.exe)
   - **Status**: NOT fixed by PowerShell 7+ or Windows Terminal in all cases
   - **Workaround**: Visual appearance issue only - functionality not affected
   - **Alternative**: Spectre.Console integration provides better emoji support where available

2. **Performance (?? Issue)**:
   - Network graphs with >50k messages can be slow
   - AI features require significant CPU/RAM

3. **Not Fully Tested**:
   - PostgreSQL/MySQL exports have limited testing
   - Batch processing mode is experimental
   - Some MMS MIME types may not be recognized
   - **Column selection feature is UNTESTED** - use with caution

4. **AI Requirements**:
   - Sentiment analysis requires Ollama (2-4 GB download)
   - Topic detection needs internet for initial setup
   - Clustering works best with English text

See [KNOWN_ISSUES.md](docs/KNOWN_ISSUES.md) for complete list and workarounds.

---

## ?? Future Development

### Planned Features (v0.8+)

**Google Takeout Integration**:
- Import Google Hangouts/Chat
- Import Gmail conversations
- Import Google Voice messages

**Additional Platform Support**:
- Apple iMessage/SMS backup import
- Facebook Messenger export
- Instagram DM export
- Telegram export
- Signal backup support

**Enhanced Analytics**:
- Conversation summary generation (AI)
- Relationship strength scoring
- Activity heat maps
- Timeline visualizations

**Visualization Improvements**:
- Timeline views
- Heat maps of messaging activity
- Sentiment trend charts
- Contact interaction matrices

**Note**: WhatsApp support, emoji analysis, and multi-language features are not currently planned for near-term releases.

---

## ?? Contributing

Contributions are welcome! This project was AI-generated but benefits from human oversight and testing.

### How to Contribute

1. **Test**: Try the tool with various SMS backups
2. **Report Issues**: Submit bugs with details
3. **Documentation**: Improve guides and examples
4. **Code**: Fix bugs or add features (see [CONTRIBUTING.md](CONTRIBUTING.md))

### Development

```bash
git clone https://github.com/rhale78/SMSXmlToCsv.git
cd SMSXmlToCsv
dotnet restore
dotnet build
```

---

## ?? License

MIT License - see [LICENSE](LICENSE) file.

### Third-Party Dependencies

- QuestPDF (MIT) - PDF generation
- Parquet.NET (MIT) - Parquet format
- Spectre.Console (MIT) - Terminal UI
- Serilog (Apache 2.0) - Logging
- See [docs/THIRD_PARTY_LICENSES.md](docs/THIRD_PARTY_LICENSES.md) for complete list

---

## ?? Acknowledgments

- **Claude (Anthropic)** - AI that wrote all the code
- **Visual Studio 2026** - Development environment
- **Ollama** - Local AI runtime
- **SMS Backup & Restore** - Android backup app
- Open-source community for excellent libraries

---

## ?? Support

- **Issues**: [GitHub Issues](https://github.com/rhale78/SMSXmlToCsv/issues)
- **Discussions**: [GitHub Discussions](https://github.com/rhale78/SMSXmlToCsv/discussions)
- **Wiki**: [Project Wiki](https://github.com/rhale78/SMSXmlToCsv/wiki) - Quick Start, FAQ, How-To Guides
- **Documentation**: [Complete Docs](docs/) - Technical reference and detailed guides

---

## ?? Star This Project

If you find this useful, please star it on GitHub to help others discover it!

---

**Made with ?? by AI | Version 0.7 | VibeCoded in 3 Days**
