# Quick Reference Card - SMS Backup XML Converter v0.7

**One-page cheat sheet for common tasks**

---

## ?? Quick Start

```bash
# Interactive mode (recommended)
SMSXmlToCsv.exe

# Basic conversion to Parquet
SMSXmlToCsv.exe backup.xml

# Full featured with AI
SMSXmlToCsv.exe backup.xml --split enable --sentiment --network-graph --pdf-report
```

---

## ?? Common Commands

### Export to Excel-Friendly CSV
```bash
SMSXmlToCsv.exe backup.xml --formats csv
```

### Extract MMS Media Files Only
```bash
SMSXmlToCsv.exe backup.xml --mms enable --split enable
```

### Create Searchable Database
```bash
SMSXmlToCsv.exe backup.xml --sqlite enable --split enable
```

### Filter by Date Range
```bash
SMSXmlToCsv.exe backup.xml --date-from 2023-01-01 --date-to 2023-12-31
```

### Select Specific Contacts
```bash
SMSXmlToCsv.exe backup.xml --select-contacts "Name1|+1234,Name2|+5678"
```

### Generate Network Visualization
```bash
SMSXmlToCsv.exe backup.xml --split enable --network-graph
```

---

## ?? Output Formats

| Format | Flag | Use Case |
|--------|------|----------|
| **CSV** | `--formats csv` | Excel, Google Sheets |
| **JSON** | `--formats json` | Programming, APIs |
| **Parquet** | `--formats parquet` | Python, R, AI/ML (smallest) |
| **SQLite** | `--sqlite enable` | SQL queries, searching |
| **HTML** | `--html enable` | Chat-style viewing |

---

## ?? Configuration Files

### `.env` - Your Identity
```env
SMS_USER_NAME=YourName
SMS_USER_PHONE=+1234567890
```

### `appsettings.json` - Feature Defaults
```json
{
  "Features": {
    "ExtractMMS": "Enable",
    "SplitByContact": "Enable"
  }
}
```

---

## ?? AI Features (Requires Ollama)

### Setup Ollama
```bash
# 1. Install from https://ollama.ai
# 2. Pull model
ollama pull llama3.2
```

### Use AI Features
```bash
# Sentiment analysis
SMSXmlToCsv.exe backup.xml --sentiment

# Network graph with topics
SMSXmlToCsv.exe backup.xml --network-graph

# Full AI analysis
SMSXmlToCsv.exe backup.xml --sentiment --clustering --network-graph --pdf-report
```

---

## ?? Filtering Options

```bash
# Date range
--date-from 2023-01-01 --date-to 2023-12-31

# Specific contacts
--select-contacts "Name|Phone,Name2|Phone2"

# Specific columns only
--columns "FromName,ToName,DateTime,MessageText"

# Filter unknown contacts
SMSXmlToCsv.exe backup.xml # Then select filter in menu
```

---

## ?? Feature Flags

| Feature | Flag | Requires |
|---------|------|----------|
| MMS Extraction | `--mms enable` | - |
| Split by Contact | `--split enable` | - |
| Contact Filtering | `--filter enable` | `--split` |
| SQLite Database | `--sqlite enable` | `--split` |
| HTML Pages | `--html enable` | `--split` |
| PDF Report | `--pdf-report` | `--split` |

---

## ?? Troubleshooting

### Problem: Icons Show as `?/?`

**Issue**: Emoji/icons display incorrectly in console

**Cause**: Windows Console has limited Unicode support

**Solution Attempts**:
```bash
# Windows Terminal (helps but doesn't fully fix)
# Download from Microsoft Store

# PowerShell 7+ (helps but doesn't fully fix)
pwsh.exe
```

**Actual Fix**: None - this is a Windows Console limitation. Icons are cosmetic only and don't affect functionality.

**Status**: Visual issue only, all features work correctly

### Ollama Not Found
```bash
# Check if running
ollama list

# Start Ollama service
ollama serve
```

### Configuration Not Loading
```bash
# Check file location (next to .exe)
dir appsettings.json

# Validate JSON syntax
# Use: https://jsonlint.com
```

---

## ?? Output Structure

```
backup.xml
??? Exports_2025-01-28_223045/
    ??? backup.parquet       # Main data
    ??? backup.csv   # Excel
  ??? backup.db   # SQLite
    ??? Contacts/ # Per-contact exports
    ?   ??? John_Smith_+1234/
    ? ??? messages.html
    ?       ??? MMS/
    ??? network_graph.html     # Visualization
    ??? sentiment_analysis.json  # AI insights
    ??? comprehensive_report.pdf  # Full report
```

---

## ?? Quick Links

| Resource | Link |
|----------|------|
| **Full Docs** | `docs/DOCUMENTATION_INDEX.md` |
| **Command Reference** | `docs/COMMAND_LINE.md` |
| **Configuration** | `docs/CONFIGURATION.md` |
| **Known Issues** | `docs/KNOWN_ISSUES.md` |
| **Help** | `SMSXmlToCsv.exe --help` |

---

## ?? Pro Tips

1. **Start Small**: Test with `--date-from` for recent messages first
2. **Use Parquet**: Smallest files, fastest processing
3. **Save Config**: Use `--save-config` to reuse settings
4. **Close Apps**: AI features need RAM
5. **Check Logs**: `logs/` folder for detailed info

---

## ?? Getting Help

1. Check `docs/TROUBLESHOOTING.md`
2. Review `docs/KNOWN_ISSUES.md`
3. Search GitHub Issues
4. Ask in GitHub Discussions

---

## ? Performance Tips

- **Date filtering first** ? Reduces processing time
- **Skip HTML for large datasets** ? HTML is slowest
- **Use `--continue-on-error`** ? Don't stop on bad files
- **Parquet is fastest** ? Use for large backups

---

## ?? Example Workflows

### Workflow 1: Quick Export for Excel
```bash
SMSXmlToCsv.exe backup.xml --formats csv --split enable
# Open: Contacts/*/messages.csv in Excel
```

### Workflow 2: Full Analysis with AI
```bash
SMSXmlToCsv.exe backup.xml --split enable --mms enable --sentiment --network-graph --pdf-report
# View: comprehensive_report.pdf
# Explore: network_graph.html
```

### Workflow 3: Searchable Database
```bash
SMSXmlToCsv.exe backup.xml --split enable --sqlite enable
# Query: sqlite3 backup.db "SELECT * FROM messages WHERE MessageText LIKE '%keyword%'"
```

### Workflow 4: Specific Year Export
```bash
SMSXmlToCsv.exe backup.xml --date-from 2023-01-01 --date-to 2023-12-31 --formats csv,parquet
```

---

**Version**: 0.7 | **Last Updated**: October 2025  
**Made with ?? by AI** | Print this page for quick reference!
