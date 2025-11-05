# Installing Ollama for AI Features

Complete guide to installing and configuring Ollama for AI-powered features.

---

## What is Ollama?

**Ollama** is a local AI runtime that allows you to run large language models (LLMs) on your own computer. It enables:
- ?? Sentiment analysis
- ?? Topic detection
- ?? Conversation clustering
- ?? Network graph generation

**Key Benefits**:
- ? Runs locally (no cloud/API needed)
- ? Privacy-friendly (data never leaves your PC)
- ? Free and open-source
- ? Fast after initial setup

---

## System Requirements

### Minimum
- **OS**: Windows 10 (64-bit) or later
- **RAM**: 8 GB
- **Disk**: 4 GB free space (for model)
- **Internet**: Required for initial download

### Recommended
- **RAM**: 16 GB
- **CPU**: Multi-core processor
- **GPU**: Optional but speeds up processing
- **Disk**: 6 GB free space (for multiple models)

---

## Installation Steps

### Step 1: Download Ollama

**Official Website Method**:
1. Visit https://ollama.ai
2. Click **"Download for Windows"**
3. Save `OllamaSetup.exe`

**Package Manager Method**:
```bash
# Using winget (Windows 10+)
winget install Ollama.Ollama

# Using Chocolatey
choco install ollama

# Using Scoop
scoop install ollama
```

### Step 2: Install Ollama

1. **Run installer** (`OllamaSetup.exe`)
2. **Follow prompts**:
   - Accept license
   - Choose install location (default: `C:\Program Files\Ollama`)
   - Install
3. **Wait for completion**
4. **Restart terminal** (if already open)

### Step 3: Verify Installation

```bash
# Check version
ollama --version
```

**Expected output**:
```
ollama version is 0.1.x
```

**If not found**:
```bash
# Check PATH
where.exe ollama

# Restart terminal
# Try again
```

### Step 4: Install AI Model

**Recommended Model** (llama3.2):
```bash
# Pull model (~2.3 GB download)
ollama pull llama3.2
```

**Progress**:
```
pulling manifest
pulling 42a2c5c1....b7e60 100% ???????????????? 2.3 GB
pulling 3f8eb4da....8abf2 100% ????????????????  254 MB
...
success
```

**Verify**:
```bash
# List installed models
ollama list
```

**Expected output**:
```
NAME     SIZE      MODIFIED
llama3.2:latest 2.3 GB    5 minutes ago
```

### Step 5: Test Model

```bash
# Interactive test
ollama run llama3.2
```

**Type a question**:
```
>>> Hello, how are you?
I'm doing well, thank you for asking! How can I help you today?

>>> exit
```

**Success!** ? Ollama is ready.

---

## Starting Ollama Service

### Automatic (Windows)

Ollama installs as a Windows service and **starts automatically** after installation.

**Check if running**:
```bash
Get-Process ollama
```

### Manual Start

If service isn't running:

```bash
# Start service
ollama serve
```

**Output**:
```
Listening on http://localhost:11434
```

**Keep this terminal open** while using AI features.

---

## Integrating with SMS Backup XML Converter

### Verify Detection

```bash
# Run with AI feature
SMSXmlToCsv.exe backup.xml --sentiment
```

**Expected**:
```
? Ollama detected and ready
Analyzing sentiment...
```

**If not detected**:
```
? Ollama not available for AI features
  Solution: Install Ollama from https://ollama.ai
```

See [Troubleshooting](#troubleshooting) below.

---

## Model Selection

### Available Models

| Model | Size | Speed | Accuracy | Recommended For |
|-------|------|-------|----------|-----------------|
| **llama3.2** | 2.3 GB | Medium | High | **Default choice** |
| **llama3** | 4.7 GB | Slow | Very High | Accuracy matters |
| **tinyllama** | 637 MB | Fast | Medium | Quick testing |
| **mistral** | 4.1 GB | Medium | High | Alternative |

### Download Additional Models

```bash
# Larger, more accurate model
ollama pull llama3

# Smaller, faster model
ollama pull tinyllama

# Alternative model
ollama pull mistral
```

### Using Different Model

**Command-line**:
```bash
SMSXmlToCsv.exe backup.xml --sentiment --ollama-model tinyllama
```

**Configuration file** (`appsettings.json`):
```json
{
  "MLFeatures": {
    "OllamaModel": "tinyllama"
  }
}
```

---

## Troubleshooting

### Ollama Not Detected

**Problem**: "Ollama not available" message

**Check 1**: Is Ollama installed?
```bash
ollama --version
```

**Check 2**: Is service running?
```bash
Get-Process ollama
```

**Check 3**: Is port accessible?
```bash
Test-NetConnection -ComputerName localhost -Port 11434
```

**Fix**: Start service
```bash
ollama serve
```

### Model Download Fails

**Problem**: Download interrupted or fails

**Solutions**:

1. **Check internet connection**
2. **Retry download**:
   ```bash
   ollama pull llama3.2
   ```
3. **Use smaller model**:
   ```bash
   ollama pull tinyllama
   ```
4. **Check disk space**:
   ```bash
   Get-PSDrive C | Select-Object Used,Free
   ```

### Slow Performance

**Problem**: AI features taking very long

**Solutions**:

1. **Use smaller model**:
   ```bash
   ollama pull tinyllama
   SMSXmlToCsv.exe backup.xml --sentiment --ollama-model tinyllama
   ```

2. **Limit analysis**:
   - Use date filtering: `--date-from 2024-01-01`
   - Configure max messages in `appsettings.json`:
 ```json
  {
       "MLFeatures": {
         "SentimentAnalysisMaxMessages": 500
  }
     }
```

3. **Close other applications**

4. **Use GPU** (if available):
   - Ollama automatically uses GPU if detected
   - Check: https://ollama.ai/docs/gpu

### Port 11434 Already in Use

**Problem**: Another application using the port

**Find process**:
```bash
Get-Process -Id (Get-NetTCPConnection -LocalPort 11434).OwningProcess
```

**Solutions**:
1. Stop conflicting process
2. Or change Ollama port (advanced)

### Connection Refused

**Problem**: Cannot connect to Ollama

**Check firewall**:
1. Windows Firewall ? Allow an app
2. Find **Ollama**
3. Allow **Private** and **Public** networks

**Or temporarily disable firewall** (test only):
```bash
# Run as Administrator
Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False
# Test
# Re-enable after: -Enabled True
```

---

## Uninstalling

### Remove Ollama

```bash
# Using winget
winget uninstall Ollama.Ollama

# Or: Control Panel ? Programs ? Uninstall Ollama
```

### Remove Models

Models stored in: `C:\Users\[YourName]\.ollama`

```bash
# Delete models (frees 2-4 GB)
Remove-Item -Path "$env:USERPROFILE\.ollama" -Recurse -Force
```

---

## Advanced Configuration

### Change Model Storage Location

**Default**: `C:\Users\[YourName]\.ollama`

**Change** (environment variable):
```bash
# PowerShell (Admin)
[Environment]::SetEnvironmentVariable("OLLAMA_MODELS", "D:\AI\Models", "User")

# Restart Ollama
Stop-Process -Name ollama
ollama serve
```

### Custom Ollama URL

If running Ollama on different machine:

**Environment variable**:
```env
# In .env file
OLLAMA_BASE_URL=http://192.168.1.100:11434
```

**Or command-line**:
```bash
$env:OLLAMA_HOST="http://192.168.1.100:11434"
SMSXmlToCsv.exe backup.xml --sentiment
```

---

## Performance Tips

### 1. Close Other Apps
Free RAM for faster processing.

### 2. Use SSD
Install models on SSD if possible.

### 3. Limit Dataset
```bash
# Process fewer messages
--date-from 2024-01-01
```

### 4. Use Smaller Model
```bash
# Faster but less accurate
--ollama-model tinyllama
```

### 5. Update Ollama
```bash
# Check for updates
ollama --version

# Reinstall if outdated
winget upgrade Ollama.Ollama
```

---

## Next Steps

After installing Ollama:

1. **[Run Sentiment Analysis](Run-Sentiment-Analysis)** - Analyze emotions
2. **[Generate Network Graph](Generate-Network-Graph)** - Visualize relationships
3. **[Create PDF Report](Create-PDF-Report)** - Comprehensive reports
4. **[AI Features Guide](AI-Features)** - Complete AI documentation

---

## Additional Resources

- **Ollama Documentation**: https://ollama.ai/docs
- **Model Library**: https://ollama.ai/library
- **GitHub**: https://github.com/ollama/ollama
- **Discord Community**: https://discord.gg/ollama

---

**Estimated Setup Time**: 10-15 minutes  
**Difficulty**: ?? Intermediate  
**Required**: For AI features only  

---

**[? Back to Wiki Home](Home)** | **[AI Features Guide ?](AI-Features)**
