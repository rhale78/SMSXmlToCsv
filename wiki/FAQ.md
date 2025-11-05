# FAQ - Frequently Asked Questions

Common questions and answers about SMS Backup XML Converter.

---

## General Questions

### What is SMS Backup XML Converter?

A tool that converts Android SMS/MMS backup files (XML format) into various analyzable formats like CSV, JSON, Parquet, SQLite, HTML, and more. It also provides advanced analytics and AI-powered insights.

### Is it free?

Yes! SMS Backup XML Converter is open-source under the MIT License. Free for personal and commercial use.

### What platforms does it support?

**Currently**: Windows 10+ (64-bit)  
**Planned**: macOS and Linux support in future releases

### Do I need programming knowledge?

No! The interactive mode guides you through everything with visual menus. Command-line options are available for advanced users.

---

## Installation & Setup

### What do I need to install?

**Required**:
- .NET 9.0 Runtime
- Your Android SMS backup file (XML format)

**Optional**:
- Ollama (for AI features like sentiment analysis)

### Where do I get an SMS backup file?

Use the **"SMS Backup & Restore"** app on Android:
1. Install from Google Play Store
2. Run backup (SMS + MMS)
3. Transfer XML file to PC

### Can I use backups from other apps?

The tool is designed for XML backups from "SMS Backup & Restore" format. Other formats may not work.

### How big can my backup file be?

Successfully tested with files up to **500 MB** (100k+ messages). Larger files work but may require more RAM and time.

---

## Features

### What formats can I export to?

- **CSV** - Excel/Google Sheets
- **JSON Lines** - Programming/APIs
- **Parquet** - Data analysis (90% smaller!)
- **SQLite** - Searchable database
- **HTML** - Human-readable chat view
- **Markdown** - Documentation
- **PostgreSQL** - Import to PostgreSQL
- **MySQL** - Import to MySQL

### Which format should I use?

| Use Case | Recommended Format |
|----------|-------------------|
| **Excel analysis** | CSV |
| **Smallest file** | Parquet (90% smaller) |
| **Searching** | SQLite |
| **Programming** | JSON Lines |
| **Reading** | HTML |
| **Data science** | Parquet |

### Can I extract images and videos from MMS?

Yes! Enable MMS extraction:
```bash
.\SMSXmlToCsv.exe backup.xml --mms enable --split enable
```

Images, videos, and audio are saved in `Contacts/[Name]/MMS/` folders.

### What AI features are available?

**Requires Ollama**:
- ?? **Sentiment Analysis** - Detect emotions (positive/negative/neutral)
- ?? **Topic Detection** - Identify conversation themes
- ?? **Clustering** - Group similar conversations
- ?? **Network Graphs** - Visualize relationships
- ?? **PDF Reports** - Comprehensive analysis reports

### Do I need internet for AI features?

**Initial setup**: Yes (to download Ollama model ~2-4 GB)  
**After setup**: No, everything runs locally

---

## Usage

### How do I start the application?

**Interactive Mode** (recommended):
```bash
SMSXmlToCsv.exe
```

**Command-Line** (advanced):
```bash
SMSXmlToCsv.exe backup.xml --split enable --formats parquet
```

### How long does processing take?

Depends on backup size:
- **1,000 messages**: ~5 seconds
- **10,000 messages**: ~30 seconds
- **50,000 messages**: ~3 minutes
- **100,000+ messages**: ~10 minutes

**With AI features**: 2-5x longer

### Can I cancel processing?

Yes! Press **Ctrl+C** at any time. Partial results may be saved.

### Where are output files saved?

Default: `Exports_2025-10-28_HHMMSS/` folder next to your XML file.

Change location in `appsettings.json`:
```json
{
  "Folders": {
    "OutputBasePath": "C:\\My Exports"
  }
}
```

### Can I process multiple backups at once?

Yes, experimental batch processing:
```bash
SMSXmlToCsv.exe --batch "C:\Backups" --split enable
```

Or use PowerShell:
```powershell
Get-ChildItem *.xml | ForEach-Object {
  .\SMSXmlToCsv.exe $_.FullName
}
```

---

## Contact Management

### Can I merge duplicate contacts?

Yes! The application detects duplicates:
- Same person with different numbers
- Similar names (e.g., "John" vs "Johnny")
- Typos or variations

Merge via:
1. **Interactive mode** ? M (Merge Contacts)
2. Automatic duplicate detection
3. Manual merge

### Can I export only specific contacts?

Yes! Two methods:

**Interactive**:
1. Enable "Contact Filtering" in menu
2. Select contacts from list

**Command-line**:
```bash
SMSXmlToCsv.exe backup.xml --select-contacts "John|+1234,Jane|+5678"
```

### Can I filter out spam/unknown contacts?

Yes! Enable "Filter Unknown Contacts" in the interactive menu. This removes:
- Contacts without names (just phone numbers)
- Short codes (5-6 digit numbers)
- Service messages

---

## AI & Analytics

### How do I install Ollama?

See **[Installing Ollama](Installing-Ollama)** guide.

Quick version:
```bash
# 1. Download from https://ollama.ai
# 2. Install
# 3. Pull model
ollama pull llama3.2
```

### Why isn't Ollama detected?

**Check**:
1. Is Ollama installed? `ollama --version`
2. Is service running? `Get-Process ollama`
3. Is port 11434 open? `Test-NetConnection localhost -Port 11434`

**Fix**:
```bash
# Start service
ollama serve
```

See **[Ollama Troubleshooting](Ollama-Troubleshooting)** for more.

### How accurate is sentiment analysis?

**Accuracy**: ~75-85% for English text  
**Best for**: Overall trends, not individual messages  
**Limitations**: Sarcasm, context-dependent phrases

### Can I analyze non-English messages?

**Current**: English only for AI features  
**Future**: Multi-language support planned but not in near-term releases

### What's a network graph?

An interactive visualization showing:
- **Nodes**: You + your contacts + discussion topics
- **Links**: Who discusses what with whom
- **Size**: Proportional to message volume

Open `network_graph.html` in any browser to explore.

---

## Performance

### Why is processing slow?

**Common causes**:
- Large backup file (>100k messages)
- AI features enabled (sentiment analysis)
- HTML export (memory-intensive)
- Network graph generation

**Speed up**:
```bash
# Use date filtering
--date-from 2024-01-01

# Skip AI features
--no-ollama

# Use faster formats
--formats parquet,csv
```

### Why is network graph generation stuck?

Network graphs can take **5-15 minutes** for large datasets (50k+ messages). Be patient!

**If truly stuck**:
1. Check Ollama still responding: `ollama run llama3.2 "test"`
2. Check logs: `Get-Content logs\*.log`
3. Reduce dataset: `--date-from 2024-01-01`

### Out of memory errors?

**Solutions**:
1. Use date filtering (smaller dataset)
2. Close other applications
3. Disable HTML export
4. Skip AI features temporarily

---

## Issues & Bugs

### Icons show as ?/? or boxes

**Cause**: Windows Console has limited Unicode support  
**Impact**: Cosmetic only - all features work correctly  
**Status**: Known issue, not fully fixable  
**Workaround**: Windows Terminal provides better (but not perfect) support

See **[Icon Display Issues](Icon-Display-Issues)** for details.

### Column selection not working

**Status**: UNTESTED FEATURE - use with caution  
**Workaround**: Use default columns (export all fields)

See **[Known Issues](Known-Issues)** for current limitations.

### PostgreSQL/MySQL exports failing

**Status**: Limited testing  
**Try**:
1. Verify database connection
2. Check credentials in command-line args
3. Enable logging: `--log-console`
4. Report issue on GitHub

### My question isn't answered here

**Get help**:
- **[Troubleshooting Guide](https://github.com/rhale78/SMSXmlToCsv/blob/main/docs/TROUBLESHOOTING.md)**
- **[Known Issues](Known-Issues)**
- **[GitHub Discussions](https://github.com/rhale78/SMSXmlToCsv/discussions)**
- **[GitHub Issues](https://github.com/rhale78/SMSXmlToCsv/issues)** (for bugs)

---

## Privacy & Security

### Does it send my data anywhere?

**No!** Everything runs locally on your PC. Your messages never leave your computer.

**Exception**: Initial Ollama model download requires internet, but the model runs locally afterward.

### Are my messages safe?

Yes! Your data stays on your PC. The tool only reads your backup file and creates exports on your local disk.

**Best practice**: Keep backup files and exports in encrypted folders if concerned about privacy.

### Can I delete exports after viewing?

Yes! Delete the entire output folder whenever you want. The tool doesn't keep copies.

---

## Development

### Is this open source?

Yes! MIT License. View source on [GitHub](https://github.com/rhale78/SMSXmlToCsv).

### Can I contribute?

Absolutely! See **[Contributing Guide](Contributing)**.

Ways to help:
- Test and report bugs
- Improve documentation
- Add features
- Fix issues

### How was this built?

This project was **VibeCoded** - created entirely using:
- **Claude Sonnet 4.5** (AI assistant)
- **Visual Studio 2026**
- **3 days** of AI-assisted development

See **[VibeCoded Development](VibeCoded-Development)** for the story.

### Will there be new features?

Yes! See **[Roadmap](Roadmap)** for planned features:
- Google Takeout integration (v0.8)
- Apple iMessage support (v0.8)
- Facebook Messenger (v0.8)
- More visualization options

---

## Licensing

### Can I use this commercially?

Yes! MIT License allows commercial use. See [LICENSE](License) for details.

### Can I modify the code?

Yes! MIT License allows modification and redistribution.

### Do I need to credit the project?

Not required, but appreciated! A link to the GitHub repo helps others discover the tool.

---

## Quick Command Reference

### Most Common Commands

```bash
# Interactive mode
SMSXmlToCsv.exe

# Basic CSV export
SMSXmlToCsv.exe backup.xml --formats csv

# Recommended: Parquet with contact splitting
SMSXmlToCsv.exe backup.xml --split enable --formats parquet

# Extract MMS files
SMSXmlToCsv.exe backup.xml --mms enable --split enable

# Full AI analysis
SMSXmlToCsv.exe backup.xml --split enable --sentiment --network-graph --pdf-report

# Date filtering
SMSXmlToCsv.exe backup.xml --date-from 2024-01-01 --date-to 2024-12-31
```

---

## Still Have Questions?

**Search the Wiki**: Use Ctrl+F to search all pages  
**Read the Docs**: [Complete Documentation](https://github.com/rhale78/SMSXmlToCsv/tree/main/docs)  
**Ask the Community**: [GitHub Discussions](https://github.com/rhale78/SMSXmlToCsv/discussions)  
**Report Bugs**: [GitHub Issues](https://github.com/rhale78/SMSXmlToCsv/issues)

---

**Last Updated**: October 2025  
**Version**: 0.7  

**[? Back to Wiki Home](Home)**
