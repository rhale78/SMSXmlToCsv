# Installation Guide - SMS Backup XML Converter v0.7

Complete installation instructions for all platforms and configurations.

---

## ?? Table of Contents

- [System Requirements](#system-requirements)
- [Quick Installation](#quick-installation)
- [Detailed Installation](#detailed-installation)
- [Ollama Setup (Optional)](#ollama-setup-optional)
- [Configuration](#configuration)
- [Verification](#verification)
- [Troubleshooting](#troubleshooting)
- [Upgrading](#upgrading)
- [Uninstallation](#uninstallation)

---

## ?? System Requirements

### Minimum Requirements
- **Operating System**: Windows 10 (64-bit) or later
- **.NET Runtime**: .NET 9.0 or later
- **Disk Space**: 500 MB free space
- **RAM**: 2 GB
- **Processor**: Any modern CPU (x64)

### Recommended Requirements
- **Operating System**: Windows 11 (64-bit)
- **.NET SDK**: .NET 9.0 SDK (for development)
- **Disk Space**: 2 GB free space (for large backups with MMS)
- **RAM**: 8 GB
- **Processor**: Multi-core CPU for parallel processing
- **Ollama**: For AI features (additional 4-6 GB disk space)

### Optional Components
- **Windows Terminal** - Better Unicode support (though not perfect)
- **PowerShell 7+** - Enhanced terminal experience
- **Ollama** - Required for AI features (sentiment, topics, clustering)
- **SQLite Browser** - For viewing SQLite exports

---

## ?? Quick Installation

### Method 1: Binary Release (Recommended)

1. **Download Latest Release**
   ```bash
   # Visit: https://github.com/rhale78/SMSXmlToCsv/releases
   # Download: SMSXmlToCsv-v0.7.zip
   ```

2. **Extract Archive**
   ```bash
   # Windows Explorer: Right-click ? Extract All
 # Or PowerShell:
   Expand-Archive -Path SMSXmlToCsv-v0.7.zip -DestinationPath C:\Tools\SMSXmlToCsv
   ```

3. **Install .NET 9.0 Runtime** (if not already installed)
   ```bash
   # Download from: https://dotnet.microsoft.com/download/dotnet/9.0
   # Or use winget:
   winget install Microsoft.DotNet.Runtime.9
   ```

4. **Run Application**
   ```bash
   cd C:\Tools\SMSXmlToCsv
   .\SMSXmlToCsv.exe
   ```

**Done!** ? Application should launch in interactive mode.

---

## ?? Detailed Installation

### Step 1: Install .NET 9.0 Runtime

#### Option A: Using Official Installer
1. Visit https://dotnet.microsoft.com/download/dotnet/9.0
2. Download "**.NET Desktop Runtime 9.0.x**"
3. Run installer
4. Verify installation:
   ```bash
   dotnet --version
   # Should show: 9.0.x
   ```

#### Option B: Using Package Manager (Windows)
```bash
# Using winget (Windows 10+)
winget install Microsoft.DotNet.Runtime.9

# Using Chocolatey
choco install dotnet-9.0-runtime

# Using Scoop
scoop install dotnet-sdk
```

#### Option C: Portable Installation
1. Download .NET 9.0 binaries
2. Extract to `C:\Program Files\dotnet`
3. Add to PATH environment variable

### Step 2: Download Application

#### From GitHub Releases
1. Visit: https://github.com/rhale78/SMSXmlToCsv/releases
2. Find latest version (v0.7 or higher)
3. Download **SMSXmlToCsv-v0.7.zip**
4. Verify checksum (optional but recommended):
   ```bash
   # SHA256 checksum provided in release notes
   Get-FileHash .\SMSXmlToCsv-v0.7.zip -Algorithm SHA256
   ```

#### Build from Source (Developers)
```bash
# Clone repository
git clone https://github.com/rhale78/SMSXmlToCsv.git
cd SMSXmlToCsv

# Restore dependencies
dotnet restore

# Build release
dotnet build --configuration Release

# Output location:
cd SMSXmlToCsv\bin\Release\net9.0
```

### Step 3: Extract and Setup

1. **Extract Archive**
```bash
   # To Program Files (requires admin)
   Expand-Archive -Path SMSXmlToCsv-v0.7.zip -DestinationPath "C:\Program Files\SMSXmlToCsv"

# Or to user directory (no admin)
   Expand-Archive -Path SMSXmlToCsv-v0.7.zip -DestinationPath "$env:USERPROFILE\Tools\SMSXmlToCsv"
   ```

2. **Verify Files**
   ```
   SMSXmlToCsv/
   ??? SMSXmlToCsv.exe           # Main executable
   ??? SMSXmlToCsv.dll
 ??? appsettings.json          # Configuration
   ??? .env.example     # Template
   ??? README.md
   ??? LICENSE
   ??? ... (other DLLs)
   ```

3. **Optional: Add to PATH**
   ```bash
   # Add to user PATH
   $path = [Environment]::GetEnvironmentVariable("Path", "User")
   $newPath = "$path;C:\Program Files\SMSXmlToCsv"
   [Environment]::SetEnvironmentVariable("Path", $newPath, "User")

   # Verify
   SMSXmlToCsv.exe --help
   ```

---

## ?? Ollama Setup (Optional)

**Required for**: Sentiment analysis, topic detection, network graphs, clustering

### Step 1: Install Ollama

#### Windows Installation
1. Visit https://ollama.ai
2. Download **Ollama for Windows**
3. Run installer
4. Verify installation:
   ```bash
   ollama --version
   # Should show: ollama version is x.x.x
   ```

#### Alternative: Winget
```bash
winget install Ollama.Ollama
```

### Step 2: Install AI Model

```bash
# Install recommended model (llama3.2, ~2 GB)
ollama pull llama3.2

# Verify model downloaded
ollama list
# Should show: llama3.2    latest    ...

# Test model
ollama run llama3.2 "Hello"
# Should get a response
```

### Step 3: Start Ollama Service

```bash
# Start service (runs in background)
ollama serve

# Or use Windows service (auto-starts)
# Service should start automatically after installation
```

### Step 4: Verify Integration

```bash
# Run SMSXmlToCsv with AI features
SMSXmlToCsv.exe backup.xml --sentiment

# Should see: "? Ollama available for AI analysis"
```

### Troubleshooting Ollama

**Issue: "Ollama not available"**
```bash
# Check if service is running
Get-Process ollama

# Start service manually
ollama serve

# Check firewall
# Ensure port 11434 is open
```

**Issue: Model download fails**
```bash
# Check internet connection
# Try different mirror
ollama pull llama3.2 --verbose

# Or use smaller model
ollama pull tinyllama
```

---

## ?? Configuration

### Create Configuration Files

#### 1. User Identity (.env file)

```bash
# Copy template
cp .env.example .env

# Edit .env (Windows: use notepad)
notepad .env
```

Add your information:
```env
# Your personal information
SMS_USER_NAME=John Doe
SMS_USER_PHONE=+15551234567

# Optional: Ollama configuration
OLLAMA_BASE_URL=http://localhost:11434
```

**Security**: Never commit `.env` to version control (already in `.gitignore`)

#### 2. Feature Configuration (appsettings.json)

```bash
# Edit appsettings.json
notepad appsettings.json
```

Example configuration:
```json
{
  "SmsConverter": {
    "UserName": "User",
    "UserPhone": "+0000000000"
  },
  "Features": {
    "ExtractMMS": "Ask",
    "SplitByContact": "Enable",
    "EnableFiltering": "Disable",
    "ExportToSQLite": "Disable",
    "ExportToHTML": "Enable"
  },
  "Folders": {
    "OutputBasePath": "",
    "MMSFolderName": "MMS",
    "ContactsFolderName": "Contacts"
  }
}
```

**Note**: Command-line arguments override these settings.

### Set Custom Output Location

```json
{
  "Folders": {
    "OutputBasePath": "C:\\SMS Exports",
    "MMSFolderName": "Media",
    "ContactsFolderName": "People"
  }
}
```

---

## ? Verification

### Test Basic Installation

```bash
# 1. Check version
SMSXmlToCsv.exe --help
# Should show version 0.7 and help text

# 2. Test with sample (if available)
SMSXmlToCsv.exe sample.xml --formats csv

# 3. Verify output
# Should create: sample.csv
```

### Test AI Features

```bash
# 1. Check Ollama
ollama list
# Should show installed models

# 2. Test sentiment analysis
SMSXmlToCsv.exe backup.xml --sentiment --split enable

# Should see:
# "? Ollama detected and ready"
# "Analyzing sentiment..."
```

### Verify File Permissions

```bash
# Test write permissions
New-Item -Path ".\test.txt" -ItemType File
# Should succeed

# Test read permissions
Get-Content .\appsettings.json
# Should display configuration
```

---

## ?? Troubleshooting

### Issue: "dotnet command not found"

**Cause**: .NET runtime not installed or not in PATH

**Solution**:
```bash
# Verify installation
where.exe dotnet
# Should show: C:\Program Files\dotnet\dotnet.exe

# If not found, reinstall .NET runtime
winget install Microsoft.DotNet.Runtime.9

# Restart terminal after installation
```

### Issue: "SMSXmlToCsv.exe not recognized"

**Cause**: Executable not in PATH or wrong directory

**Solution**:
```bash
# Run from application directory
cd C:\Tools\SMSXmlToCsv
.\SMSXmlToCsv.exe

# Or add to PATH (see Step 3 above)
```

### Issue: Icons show as ?/?

**Cause**: Windows Console Unicode limitations (known issue)

**Solution**: This is cosmetic only. See [KNOWN_ISSUES.md](KNOWN_ISSUES.md#1-iconemoji-display-problems)

### Issue: "Access Denied" when extracting

**Cause**: Insufficient permissions

**Solution**:
```bash
# Extract to user directory instead
Expand-Archive -Path SMSXmlToCsv-v0.7.zip -DestinationPath "$env:USERPROFILE\SMSXmlToCsv"
```

### Issue: Ollama not detected

**Symptoms**: "Ollama not available" message

**Solutions**:
```bash
# 1. Check if Ollama is running
Get-Process ollama

# 2. Start Ollama service
ollama serve

# 3. Check port 11434
Test-NetConnection -ComputerName localhost -Port 11434

# 4. Reinstall Ollama if needed
winget uninstall Ollama.Ollama
winget install Ollama.Ollama
```

### Issue: Out of Memory

**Cause**: Large backup file or insufficient RAM

**Solutions**:
1. Use date filtering:
   ```bash
   SMSXmlToCsv.exe backup.xml --date-from 2024-01-01
   ```

2. Close other applications

3. Disable AI features temporarily:
   ```bash
   SMSXmlToCsv.exe backup.xml --no-ollama
   ```

---

## ?? Upgrading

### From v0.6 to v0.7

1. **Backup Configuration**
   ```bash
   copy .env .env.backup
   copy appsettings.json appsettings.json.backup
   ```

2. **Download New Version**
   ```bash
   # Download SMSXmlToCsv-v0.7.zip
   ```

3. **Extract and Replace**
   ```bash
   # Extract to same location
   # Overwrite executables and DLLs
   # Keep .env and appsettings.json
   ```

4. **Verify Upgrade**
   ```bash
   SMSXmlToCsv.exe --help
   # Should show: Version 0.7
   ```

5. **Test Functionality**
   ```bash
   # Run with small test file
   SMSXmlToCsv.exe test.xml --formats csv
   ```

### Configuration Migration

**v0.7 introduces new config options**:
- Column selection
- Contact merge persistence
- Analysis features

Update `appsettings.json` with new sections (see [CONFIGURATION.md](CONFIGURATION.md))

---

## ??? Uninstallation

### Remove Application

```bash
# 1. Delete application directory
Remove-Item -Path "C:\Program Files\SMSXmlToCsv" -Recurse -Force

# 2. Remove from PATH (if added)
$path = [Environment]::GetEnvironmentVariable("Path", "User")
$newPath = $path -replace ";C:\\Program Files\\SMSXmlToCsv", ""
[Environment]::SetEnvironmentVariable("Path", $newPath, "User")

# 3. Delete user data (optional)
Remove-Item -Path "$env:USERPROFILE\.smsxmltocsv" -Recurse -Force
```

### Uninstall Ollama (Optional)

```bash
# Using winget
winget uninstall Ollama.Ollama

# Or using installer
# Control Panel ? Programs ? Uninstall Ollama

# Delete models (optional, frees ~4-6 GB)
Remove-Item -Path "$env:USERPROFILE\.ollama" -Recurse -Force
```

### Clean Up Configuration

```bash
# Remove configuration files
Remove-Item .env
Remove-Item contact_merges.json

# Remove logs (optional)
Remove-Item -Path "logs" -Recurse -Force
```

---

## ?? Next Steps

After installation:

1. **Read User Guide**: [USER_GUIDE.md](USER_GUIDE.md)
2. **Configure Application**: [CONFIGURATION.md](CONFIGURATION.md)
3. **Learn Command Line**: [COMMAND_LINE.md](COMMAND_LINE.md)
4. **Get SMS Backup**: Use "SMS Backup & Restore" app on Android
5. **Run First Export**: `SMSXmlToCsv.exe backup.xml`

---

## ?? Additional Resources

- **Documentation**: [docs/](.)
- **GitHub Issues**: Report problems
- **Discussions**: Ask questions
- **Quick Reference**: [QUICK_REFERENCE.md](../QUICK_REFERENCE.md)
- **Known Issues**: [KNOWN_ISSUES.md](KNOWN_ISSUES.md)

---

## ?? Tips

1. **Start Simple**: Use default settings first
2. **Test Small**: Use `--date-from` for recent messages
3. **Save Config**: Use `--save-config` after finding settings you like
4. **Use Parquet**: Smallest file size, fastest processing
5. **Enable Logging**: Use `--log-console` for troubleshooting

---

**Version**: 0.7  
**Last Updated**: October 2025  
**Platforms**: Windows 10+, .NET 9.0+  
**Support**: See [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
