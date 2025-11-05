# Troubleshooting Guide - SMS Backup XML Converter v0.7

Solutions to common problems and issues.

---

## 📋 Table of Contents

- [Installation Issues](#installation-issues)
- [Runtime Errors](#runtime-errors)
- [Configuration Problems](#configuration-problems)
- [Ollama/AI Issues](#ollamaai-issues)
- [Output Issues](#output-issues)
- [Performance Problems](#performance-problems)
- [File Issues](#file-issues)
- [Display Issues](#display-issues)
- [Getting Help](#getting-help)

---

## 🔧 Installation Issues

### Problem: "dotnet command not found"

**Symptoms**: Cannot run `dotnet` commands

**Cause**: .NET runtime not installed or not in PATH

**Solution**:
```bash
# 1. Check if .NET is installed
where.exe dotnet

# 2. If not found, install .NET 9.0
winget install Microsoft.DotNet.Runtime.9

# 3. Restart terminal
# 4. Verify installation
dotnet --version
```

---

### Problem: "SMSXmlToCsv.exe not recognized"

**Symptoms**: Cannot run application from command line

**Cause**: Application not in PATH or running from wrong directory

**Solution**:
```bash
# Option 1: Run from application directory
cd C:\Path\To\SMSXmlToCsv
.\SMSXmlToCsv.exe

# Option 2: Use full path
C:\Path\To\SMSXmlToCsv\SMSXmlToCsv.exe backup.xml

# Option 3: Add to PATH
$env:Path += ";C:\Path\To\SMSXmlToCsv"
```

---

### Problem: "Access Denied" errors

**Symptoms**: Cannot extract files or run application

**Cause**: Insufficient permissions

**Solution**:
```bash
# Extract to user directory (no admin needed)
Expand-Archive -Path SMSXmlToCsv-v0.7.zip -DestinationPath "$env:USERPROFILE\SMSXmlToCsv"

# Or run as administrator (right-click → Run as administrator)
```

---

## 🚨 Runtime Errors

### Problem: "File not found" when running

**Symptoms**: `System.IO.FileNotFoundException`

**Cause**: Missing dependencies or wrong directory

**Solution**:
```bash
# 1. Verify all files present
dir SMSXmlToCsv.exe
dir *.dll

# 2. Reinstall application
# Download fresh copy from releases

# 3. Check .NET runtime installed
dotnet --list-runtimes
# Should show: Microsoft.NETCore.App 9.0.x
```

---

### Problem: "XML parsing failed"

**Symptoms**: Error reading XML file

**Causes & Solutions**:

**1. Corrupted XML**:
```bash
# Validate XML structure
Select-String -Path backup.xml -Pattern "</smses>"
# Should find closing tag

# Try continue-on-error mode
SMSXmlToCsv.exe backup.xml --continue-on-error
```

**2. Wrong encoding**:
```bash
# Check file encoding
Get-Content backup.xml -TotalCount 1
# Should show: <?xml version="1.0" encoding="UTF-8"?>

# Convert if needed (use text editor with UTF-8 encoding)
```

**3. File too large**:
```bash
# Split by date range
SMSXmlToCsv.exe backup.xml --date-from 2024-01-01 --date-to 2024-06-30
```

---

### Problem: "Out of Memory" errors

**Symptoms**: Application crashes with memory error

**Cause**: Large backup file exceeds available RAM

**Solutions**:

**1. Use date filtering**:
```bash
SMSXmlToCsv.exe backup.xml --date-from 2024-01-01
```

**2. Close other applications**:
```bash
# Free up RAM before running
# Close browsers, IDEs, etc.
```

**3. Disable memory-intensive features**:
```bash
# Skip AI features
SMSXmlToCsv.exe backup.xml --no-ollama

# Skip HTML export (large memory usage)
SMSXmlToCsv.exe backup.xml --html disable
```

**4. Use simpler formats**:
```bash
# CSV uses less memory than HTML
SMSXmlToCsv.exe backup.xml --formats csv
```

---

## ⚙️ Configuration Problems

### Problem: Configuration not loading

**Symptoms**: Settings in `appsettings.json` ignored

**Causes & Solutions**:

**1. File in wrong location**:
```bash
# Must be in same directory as .exe
dir appsettings.json
# Should be next to SMSXmlToCsv.exe

# Copy if needed
copy appsettings.json C:\Path\To\SMSXmlToCsv\
```

**2. Invalid JSON syntax**:
```bash
# Validate JSON
Get-Content appsettings.json
# Check for: missing commas, quotes, brackets

# Or use online validator: https://jsonlint.com
```

**3. Command-line overrides**:
```bash
# Command-line args have higher priority
# Remove CLI args to use config file values
SMSXmlToCsv.exe backup.xml
# (no flags = uses appsettings.json)
```

---

### Problem: .env file not loading

**Symptoms**: User name/phone not set from `.env`

**Cause**: File in wrong location or wrong format

**Solution**:
```bash
# 1. Check file location
dir .env
# Must be in project root OR application directory

# 2. Check file format (no spaces around =)
# Correct:
SMS_USER_NAME=John Doe
SMS_USER_PHONE=+15551234567

# Wrong:
SMS_USER_NAME = John Doe   # (spaces around =)

# 3. Ensure file is named exactly ".env" (not .env.txt)
```

---

## 🤖 Ollama/AI Issues

### Problem: "Ollama not available"

**Symptoms**: Message "Ollama not available for AI features"

**Causes & Solutions**:

**1. Ollama not installed**:
```bash
# Check installation
ollama --version

# Install if missing
winget install Ollama.Ollama
```

**2. Ollama service not running**:
```bash
# Check if running
Get-Process ollama

# Start service
ollama serve

# Or start Windows service
Start-Service Ollama
```

**3. Port blocked**:
```bash
# Check if port 11434 is available
Test-NetConnection -ComputerName localhost -Port 11434

# If blocked, check firewall:
# Windows Firewall → Allow app → Add Ollama
```

**4. Model not downloaded**:
```bash
# List installed models
ollama list

# Install required model
ollama pull llama3.2
```

---

### Problem: AI analysis very slow

**Symptoms**: Sentiment analysis or topic detection takes hours

**Causes & Solutions**:

**1. Large dataset**:
```bash
# Use date filtering
SMSXmlToCsv.exe backup.xml --sentiment --date-from 2024-01-01

# Or limit messages analyzed
# Edit appsettings.json:
"SentimentAnalysisMaxMessages": 500
```

**2. Slow hardware**:
```bash
# Use smaller AI model
ollama pull tinyllama
SMSXmlToCsv.exe backup.xml --ollama-model tinyllama
```

**3. Network graph generation**:
```bash
# Skip network graph for very large datasets
SMSXmlToCsv.exe backup.xml --sentiment --pdf-report
# (no --network-graph)
```

---

### Problem: Ollama errors in logs

**Symptoms**: "Connection refused" or timeout errors

**Solutions**:

**1. Check Ollama logs**:
```bash
# View logs
ollama logs

# Check for errors
```

**2. Restart Ollama**:
```bash
# Stop service
Stop-Process -Name ollama

# Start service
ollama serve
```

**3. Reinstall Ollama**:
```bash
winget uninstall Ollama.Ollama
winget install Ollama.Ollama
ollama pull llama3.2
```

---

## 📤 Output Issues

### Problem: No output files created

**Symptoms**: Application completes but no files in output folder

**Causes & Solutions**:

**1. Check output location**:
```bash
# Default: same directory as input XML
dir

# Or check configured path in appsettings.json:
"OutputBasePath": "C:\\SMS Exports"
```

**2. Permission errors**:
```bash
# Check write permissions
New-Item -Path ".\test.txt" -ItemType File
# Should succeed

# If denied, change output location:
SMSXmlToCsv.exe backup.xml --output-dir "$env:USERPROFILE\SMS Exports"
```

**3. Process errors**:
```bash
# Check logs for errors
Get-Content logs\*.log | Select-String "ERROR"

# Use continue-on-error to complete despite errors
SMSXmlToCsv.exe backup.xml --continue-on-error
```

---

### Problem: Column selection not working (UNTESTED FEATURE)

**Symptoms**: Wrong columns in output despite using `--columns`

**Status**: This feature is UNTESTED and may have bugs

**Workaround**:
```bash
# Use default columns (export all fields)
SMSXmlToCsv.exe backup.xml
# (no --columns flag)

# Or edit output files manually after export
```

**Report**: If you test this feature, please report results in GitHub Issues

---

### Problem: HTML files don't display correctly

**Symptoms**: HTML chat pages show formatting issues

**Causes & Solutions**:

**1. Browser compatibility**:
```bash
# Use modern browser (Chrome, Edge, Firefox)
# Avoid Internet Explorer
```

**2. Missing files**:
```bash
# Check all files present:
messages.html
# Should be in each contact folder
```

**3. MMS images not loading**:
```bash
# Check MMS folder exists
dir Contacts\John_Smith_+1234\MMS
# Should contain image/video files
```

---

### Problem: SQLite database errors

**Symptoms**: Cannot open .db file or query fails

**Solutions**:

**1. Install SQLite browser**:
```bash
# Download DB Browser for SQLite
# https://sqlitebrowser.org
```

**2. Verify database created**:
```bash
# Check file exists
dir *.db

# Test with sqlite3
sqlite3 backup.db "SELECT COUNT(*) FROM messages;"
```

**3. Corrupted database**:
```bash
# Regenerate database
SMSXmlToCsv.exe backup.xml --sqlite enable --split enable
```

---

## ⚡ Performance Problems

### Problem: Very slow processing

**Symptoms**: Processing takes much longer than expected

**Causes & Solutions**:

**1. Large backup file**:
```bash
# Check file size
Get-Item backup.xml | Select-Object Name, Length

# If >100 MB, use date filtering
SMSXmlToCsv.exe backup.xml --date-from 2024-01-01
```

**2. Antivirus scanning**:
```bash
# Temporarily disable antivirus
# Or add SMSXmlToCsv folder to exclusions
```

**3. Disk I/O bottleneck**:
```bash
# Use SSD if available
# Or specify faster output location
SMSXmlToCsv.exe backup.xml --output-dir D:\FastDrive\Output
```

**4. AI features enabled**:
```bash
# Skip AI for faster processing
SMSXmlToCsv.exe backup.xml --no-ollama
```

---

### Problem: Network graph generation stuck

**Symptoms**: "Generating network graph..." never completes

**Cause**: Very large dataset or Ollama timeout

**Solutions**:

**1. Be patient** (may take 5-15 minutes for 50k+ messages)

**2. Check Ollama still responding**:
```bash
# Test Ollama
ollama run llama3.2 "test"
# Should respond
```

**3. Reduce dataset**:
```bash
# Use date range
SMSXmlToCsv.exe backup.xml --network-graph --date-from 2024-01-01

# Or skip network graph
SMSXmlToCsv.exe backup.xml --sentiment --pdf-report
```

---

## 📁 File Issues

### Problem: MMS files not extracting

**Symptoms**: MMS folder empty despite MMS messages

**Causes & Solutions**:

**1. MMS extraction disabled**:
```bash
# Enable MMS extraction
SMSXmlToCsv.exe backup.xml --mms enable --split enable
```

**2. Backup doesn't contain MMS data**:
```bash
# Check if XML has MMS parts
Select-String -Path backup.xml -Pattern "part "
# Should find <part> tags if MMS exists
```

**3. Exotic MIME types**:
```bash
# Some rare file types may not be recognized
# Check logs for warnings
Get-Content logs\*.log | Select-String "MIME"
```

---

### Problem: Duplicate files in output

**Symptoms**: Same message appears multiple times

**Cause**: Backup contains duplicates (from multiple backup apps)

**Solution**:
```bash
# Use SQLite export and deduplicate
SMSXmlToCsv.exe backup.xml --sqlite enable

# Then query unique messages:
sqlite3 backup.db "SELECT DISTINCT * FROM messages"
```

---

## 🎨 Display Issues

### Problem: Icons show as ?/? or boxes

**Symptoms**: Emoji and icons display incorrectly

**Cause**: Windows Console has limited Unicode support (KNOWN ISSUE)

**Status**: This is a fundamental Windows limitation, NOT a bug

**Impact**: Cosmetic only - all functionality works correctly

**Partial Workarounds**:
```bash
# 1. Use Windows Terminal (helps but doesn't fully fix)
# Download from Microsoft Store

# 2. Use PowerShell 7+ (helps but doesn't fully fix)
pwsh.exe

# 3. Accept visual inconsistency
# Icons are decorative only - features work regardless
```

**See**: [KNOWN_ISSUES.md](KNOWN_ISSUES.md#1-iconemoji-display-problems) for details

---

### Problem: Colors not showing

**Symptoms**: No colored output in terminal

**Cause**: Console doesn't support colors

**Solution**:
```bash
# Use Windows Terminal or PowerShell
# Or run with no-color mode (if implemented in future)
```

---

## 🆘 Getting Help

### Before Asking for Help

1. **Check documentation**:
   - [KNOWN_ISSUES.md](KNOWN_ISSUES.md)
   - [INSTALLATION.md](INSTALLATION.md)
   - [CONFIGURATION.md](CONFIGURATION.md)

2. **Check logs**:
   ```bash
   Get-Content logs\*.log | Select-String "ERROR"
   ```

3. **Try with minimal settings**:
   ```bash
   SMSXmlToCsv.exe backup.xml --formats csv
   ```

### Gathering Information

When reporting issues, provide:

1. **Version**:
   ```bash
   SMSXmlToCsv.exe --help
   # Note version number
   ```

2. **Operating System**:
   ```bash
   systeminfo | Select-String "OS Name","OS Version"
   ```

3. **Command used**:
 ```bash
# Copy exact command that failed
   ```

4. **Error message**:
   ```bash
   # Copy full error text
   # Or screenshot
   ```

5. **Logs** (with PII removed):
   ```bash
   Get-Content logs\*.log | Select-String "ERROR" -Context 3
   ```

### Where to Get Help

1. **GitHub Issues**: https://github.com/rhale78/SMSXmlToCsv/issues
   - Search existing issues first
   - Create new issue with information above

2. **GitHub Discussions**: https://github.com/rhale78/SMSXmlToCsv/discussions
   - For questions and general help

3. **Documentation**: [docs/](.)
   - Complete guides and references

---

## 🔍 Diagnostic Commands

### System Information
```bash
# .NET version
dotnet --version

# Ollama version
ollama --version

# Application version
SMSXmlToCsv.exe --help

# Check running processes
Get-Process | Where-Object {$_.Name -match "SMSXmlToCsv|ollama"}
```

### Log Analysis
```bash
# View recent errors
Get-Content logs\*.log | Select-String "ERROR" | Select-Object -Last 20

# Count warnings
Get-Content logs\*.log | Select-String "WARNING" | Measure-Object

# View full latest log
Get-Content (Get-ChildItem logs\*.log | Sort-Object LastWriteTime -Descending | Select-Object -First 1)
```

### File Validation
```bash
# Check XML is valid
Select-Xml -Path backup.xml -XPath "//smses"

# Check file size
Get-Item backup.xml | Select-Object Length

# Count messages in XML
Select-String -Path backup.xml -Pattern "<sms " -AllMatches | Measure-Object
```

---

## 💡 Prevention Tips

1. **Start small**: Test with recent messages first using `--date-from`
2. **Save config**: Use `--save-config` once you find working settings
3. **Use Parquet**: Fastest and most reliable format
4. **Regular backups**: Create SMS backups regularly (smaller files)
5. **Test changes**: Try new options on test data first
6. **Read logs**: Check logs after each run for warnings
7. **Keep updated**: Use latest version for bug fixes

---

**Version**: 0.7  
**Last Updated**: October 2025  
**For More Help**: See [KNOWN_ISSUES.md](KNOWN_ISSUES.md) and [INSTALLATION.md](INSTALLATION.md)
