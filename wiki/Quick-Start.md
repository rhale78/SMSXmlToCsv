# Quick Start Guide

Get up and running with SMS Backup XML Converter in 5 minutes!

---

## Step 1: Install Prerequisites

### Windows

```bash
# Install .NET 9.0 Runtime
winget install Microsoft.DotNet.Runtime.9

# Verify installation
dotnet --version
```

**Expected output**: `9.0.x`

---

## Step 2: Download Application

1. Visit [Releases Page](https://github.com/rhale78/SMSXmlToCsv/releases)
2. Download **SMSXmlToCsv-v0.7.zip**
3. Extract to a folder (e.g., `C:\Tools\SMSXmlToCsv`)

---

## Step 3: Get Your SMS Backup

### From Android Phone

1. Install **"SMS Backup & Restore"** from Google Play Store
2. Open app ? **Backup**
3. Select **SMS + MMS**
4. Wait for backup to complete
5. Transfer `sms-YYYYMMDD.xml` to your PC

### File Location Options

Transfer via:
- USB cable to PC
- Google Drive
- Email to yourself
- Cloud storage

---

## Step 4: Run Your First Export

### Interactive Mode (Recommended)

```bash
# Navigate to application folder
cd C:\Tools\SMSXmlToCsv

# Launch interactive mode
.\SMSXmlToCsv.exe
```

**What happens**:
1. ? Application detects XML files in current directory
2. ?? Select your backup file
3. ?? Configure features via menu (or use defaults)
4. ?? Press Enter to start
5. ?? Wait for processing
6. ? Open output folder

### Quick Command-Line Export

```bash
# Basic CSV export
.\SMSXmlToCsv.exe backup.xml --formats csv

# Recommended: Parquet (smallest, fastest)
.\SMSXmlToCsv.exe backup.xml --formats parquet

# With contact splitting
.\SMSXmlToCsv.exe backup.xml --split enable --formats parquet
```

---

## Step 5: View Your Results

### Output Location

```
Exports_2025-10-28_HHMMSS/
??? backup.csv   # Spreadsheet-ready
??? backup.parquet      # Data analysis
??? backup.db           # Searchable database
```

### Open in Excel

```bash
# Double-click backup.csv
# Or
start excel backup.csv
```

### Open in Database Browser

```bash
# Install DB Browser for SQLite (if exported SQLite)
# https://sqlitebrowser.org
# Then open backup.db
```

---

## Common First Tasks

### Extract MMS Files

```bash
.\SMSXmlToCsv.exe backup.xml --mms enable --split enable
```

**Result**: Images and videos saved in `Contacts/[Name]/MMS/`

### Filter by Date Range

```bash
# Last year only
.\SMSXmlToCsv.exe backup.xml --date-from 2024-01-01
```

### Export Specific Contacts

```bash
# Interactive mode ? Enable "Contact Filtering"
# Then select contacts from list
.\SMSXmlToCsv.exe
```

---

## Next Steps

### Learn More Features

- **[Export Formats](Export-Formats)** - Understand format options
- **[MMS Extraction](MMS-Extraction)** - Deep dive into media extraction
- **[Contact Management](Contact-Management)** - Merge duplicates, filter contacts

### Set Up AI Features

- **[Installing Ollama](Installing-Ollama)** - Add sentiment analysis
- **[Generate Network Graph](Generate-Network-Graph)** - Visualize relationships

### Get Help

- **[FAQ](FAQ)** - Common questions
- **[Troubleshooting](Installation-Problems)** - Fix issues
- **[User Guide](https://github.com/rhale78/SMSXmlToCsv/blob/main/docs/USER_GUIDE.md)** - Complete documentation

---

## Quick Tips

### ?? Tip 1: Start Small
Test with recent messages first:
```bash
.\SMSXmlToCsv.exe backup.xml --date-from 2025-10-01
```

### ?? Tip 2: Use Parquet Format
Smallest file size, fastest processing:
```bash
--formats parquet
```

### ?? Tip 3: Save Your Configuration
```bash
.\SMSXmlToCsv.exe backup.xml --split enable --save-config
# Future runs will use these settings automatically
```

### ?? Tip 4: Check Logs on Errors
```bash
Get-Content logs\*.log | Select-String "ERROR"
```

---

## Troubleshooting Quick Fixes

### "dotnet not found"
```bash
# Install .NET runtime
winget install Microsoft.DotNet.Runtime.9
# Restart terminal
```

### "File not found"
```bash
# Make sure XML file is in current directory
dir *.xml

# Or provide full path
.\SMSXmlToCsv.exe "C:\Backups\sms-20251028.xml"
```

### Icons show as ?/?
This is normal on older Windows consoles. **Functionality is not affected** - it's purely cosmetic. See [Icon Display Issues](Icon-Display-Issues) for details.

---

## Success! What Did You Just Do?

You've successfully:
- ? Installed SMS Backup XML Converter
- ? Converted your Android SMS backup
- ? Created structured, analyzable data
- ? (Optionally) Extracted MMS files
- ? (Optionally) Split messages by contact

---

## Ready for More?

### Popular Next Steps

1. **[Generate Network Graph](Generate-Network-Graph)** - Visualize your contacts
2. **[Run Sentiment Analysis](Run-Sentiment-Analysis)** - Analyze emotions
3. **[Create PDF Report](Create-PDF-Report)** - Professional reports
4. **[SQLite Queries](SQLite-Queries)** - Advanced searching

---

**Estimated Time**: 5 minutes ??  
**Difficulty**: ? Beginner  
**Prerequisites**: .NET 9.0, Android SMS backup  

---

**[? Back to Wiki Home](Home)** | **[Installation Guide ?](Installation-Guide)**
