# Configuration Guide - SMS Backup XML Converter v0.7

Complete guide to configuring the SMS Backup XML Converter using configuration files, environment variables, and command-line options.

---

## Configuration Files

### `.env` - User Identity (Not Tracked in Git)

**Purpose**: Store personal information (name, phone number) without committing to version control.

**Location**: Project root directory

**Create from template**:
```bash
cp .env.example .env
```

**Example `.env`**:
```env
# Your personal information
SMS_USER_NAME=John Doe
SMS_USER_PHONE=+15551234567

# Optional: Ollama configuration
OLLAMA_BASE_URL=http://localhost:11434
```

**Security**: This file is automatically ignored by git (.gitignore). Never commit it.

---

### `appsettings.json` - Feature Defaults

**Purpose**: Configure default behavior for features, filtering, and output options.

**Location**: Next to SMSXmlToCsv.exe

**Full Example**:
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

  "Filtering": {
  "DateFrom": null,
    "DateTo": null,
    "PreConfiguredContacts": [
      "John Doe|+15551234",
      "Jane Smith|+15555678"
    ]
  },

"Columns": {
    "Selected": [
"FromName",
      "FromPhone",
      "ToName",
 "ToPhone",
    "Direction",
      "DateTime",
      "MessageText"
    ]
  },

  "Folders": {
    "OutputBasePath": "",
 "MMSFolderName": "MMS",
    "ContactsFolderName": "Contacts"
  },

  "ErrorHandling": {
    "ContinueOnError": false
  },

  "Analysis": {
    "EnableThreadAnalysis": false,
"EnableResponseTimeAnalysis": false,
    "EnableAdvancedStatistics": false,
    "ThreadTimeoutMinutes": 120,
    "MinimumThreadLength": 3
  },

  "MLFeatures": {
 "EnableSentimentAnalysis": false,
    "EnableClustering": false,
    "GenerateNetworkGraph": false,
    "GeneratePdfReport": false,
    "UseOllama": true,
"OllamaModel": "llama3.2",
    "SentimentAnalysisMaxMessages": 1000
  }
}
```

---

## Configuration Sections

### `SmsConverter` - User Identity

```json
"SmsConverter": {
  "UserName": "Your Name",
  "UserPhone": "+1234567890"
}
```

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `UserName` | string | Your name (appears as "You") | `"John Doe"` |
| `UserPhone` | string | Your phone with country code | `"+15551234567"` |

**Priority**: `.env` > `appsettings.json` > Command-line prompt

---

### `Features` - Core Feature Modes

```json
"Features": {
  "ExtractMMS": "Ask",
  "SplitByContact": "Enable",
  "EnableFiltering": "Disable",
  "ExportToSQLite": "Disable",
  "ExportToHTML": "Enable"
}
```

**Modes**:
- `"Enable"` - Feature always on
- `"Disable"` - Feature always off
- `"Ask"` - Prompt user interactively

| Feature | Description | Dependencies |
|---------|-------------|--------------|
| `ExtractMMS` | Extract images/videos from MMS | None |
| `SplitByContact` | Separate folders per contact | None |
| `EnableFiltering` | Interactive contact selection | Requires `SplitByContact` |
| `ExportToSQLite` | Create SQLite database | Requires `SplitByContact` |
| `ExportToHTML` | Generate HTML chat pages | Requires `SplitByContact` |

**Example Configurations**:

**Minimal (fastest)**:
```json
"Features": {
  "ExtractMMS": "Disable",
  "SplitByContact": "Disable",
  "EnableFiltering": "Disable",
  "ExportToSQLite": "Disable",
  "ExportToHTML": "Disable"
}
```

**Full-featured**:
```json
"Features": {
  "ExtractMMS": "Enable",
  "SplitByContact": "Enable",
  "EnableFiltering": "Enable",
  "ExportToSQLite": "Enable",
  "ExportToHTML": "Enable"
}
```

---

### `Filtering` - Message Filtering

```json
"Filtering": {
  "DateFrom": "2023-01-01",
  "DateTo": "2023-12-31",
  "PreConfiguredContacts": [
    "John Doe|+15551234",
    "Jane Smith|+15555678"
  ]
}
```

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `DateFrom` | string/null | Start date (inclusive) | `"2023-01-01"` |
| `DateTo` | string/null | End date (inclusive) | `"2023-12-31"` |
| `PreConfiguredContacts` | array | Pre-selected contacts | See below |

**Date Format**: `YYYY-MM-DD`

**Contact Format**: `"Name|Phone"`
- Name: Exact match as it appears in backup
- Phone: Include country code

**Example**:
```json
"PreConfiguredContacts": [
  "John Doe|+15551234567",
  "Jane Smith|+15555555555",
  "Company ABC|+18005551234"
]
```

---

### `Columns` - Field Selection

```json
"Columns": {
"Selected": [
    "FromName",
    "ToName",
    "DateTime",
    "MessageText"
  ]
}
```

**Available Columns**:
- `FromName` (required)
- `FromPhone` (required)
- `ToName` (required)
- `ToPhone` (required)
- `Direction` - "Sent" or "Received"
- `DateTime` - Timestamp
- `UnixTimestamp` - Unix epoch milliseconds
- `MessageText` - Message content
- `MessageLength` - Character count
- `HasMMS` - Boolean, has attachments

**Required Columns**: `FromName`, `FromPhone`, `ToName`, `ToPhone` (always included)

**Example - Minimal**:
```json
"Columns": {
  "Selected": [
    "FromName",
    "ToName",
  "DateTime",
    "MessageText"
  ]
}
```

**Example - All Fields**:
```json
"Columns": {
  "Selected": [
    "FromName",
    "FromPhone",
    "ToName",
    "ToPhone",
    "Direction",
  "DateTime",
    "UnixTimestamp",
    "MessageText",
    "MessageLength",
    "HasMMS"
  ]
}
```

---

### `Folders` - Output Organization

```json
"Folders": {
  "OutputBasePath": "",
  "MMSFolderName": "MMS",
  "ContactsFolderName": "Contacts"
}
```

| Field | Type | Description | Default |
|-------|------|-------------|---------|
| `OutputBasePath` | string | Base output directory | Same as input XML |
| `MMSFolderName` | string | MMS subfolder name | `"MMS"` |
| `ContactsFolderName` | string | Contacts subfolder name | `"Contacts"` |

**Example - Custom Paths**:
```json
"Folders": {
  "OutputBasePath": "C:\\SMS Exports",
  "MMSFolderName": "Media",
  "ContactsFolderName": "People"
}
```

**Resulting Structure**:
```
C:\SMS Exports\
??? Exports_2025-01-28_223045/
    ??? People/
        ??? John_Doe_+1234/
            ??? Media/
```

---

### `ErrorHandling` - Error Behavior

```json
"ErrorHandling": {
  "ContinueOnError": false
}
```

| Field | Type | Description |
|-------|------|-------------|
| `ContinueOnError` | boolean | Continue processing on errors |

**When `true`**:
- Logs errors but continues
- Useful for large backups with some corrupted data
- Final summary shows how many errors occurred

**When `false`** (default):
- Stops on first error
- Safest option to catch problems

---

### `Analysis` - Analysis Features (v1.6+)

```json
"Analysis": {
  "EnableThreadAnalysis": true,
  "EnableResponseTimeAnalysis": true,
  "EnableAdvancedStatistics": true,
  "ThreadTimeoutMinutes": 120,
  "MinimumThreadLength": 3
}
```

| Field | Type | Description | Default |
|-------|------|-------------|---------|
| `EnableThreadAnalysis` | boolean | Detect conversation threads | `false` |
| `EnableResponseTimeAnalysis` | boolean | Measure response times | `false` |
| `EnableAdvancedStatistics` | boolean | Generate detailed stats | `false` |
| `ThreadTimeoutMinutes` | int | Gap to separate threads | `120` |
| `MinimumThreadLength` | int | Min messages per thread | `3` |

**Thread Timeout**: If no message for this many minutes, thread is considered ended.

**Min Thread Length**: Threads with fewer messages are filtered out.

---

### `MLFeatures` - AI/ML Options (v1.7+)

```json
"MLFeatures": {
"EnableSentimentAnalysis": false,
  "EnableClustering": false,
  "GenerateNetworkGraph": false,
  "GeneratePdfReport": false,
  "UseOllama": true,
  "OllamaModel": "llama3.2",
  "SentimentAnalysisMaxMessages": 1000
}
```

| Field | Type | Description | Default |
|-------|------|-------------|---------|
| `EnableSentimentAnalysis` | boolean | AI sentiment detection | `false` |
| `EnableClustering` | boolean | Cluster conversations | `false` |
| `GenerateNetworkGraph` | boolean | Network visualization | `false` |
| `GeneratePdfReport` | boolean | PDF report | `false` |
| `UseOllama` | boolean | Use Ollama for AI | `true` |
| `OllamaModel` | string | Ollama model name | `"llama3.2"` |
| `SentimentAnalysisMaxMessages` | int | Max messages to analyze | `1000` |

**Requirements**: All AI features require Ollama installed with specified model.

**Available Models**:
- `llama3.2` - Recommended (2 GB, fast, accurate)
- `llama2` - Alternative (3.8 GB, slower)
- Others: Any model supported by Ollama

---

## Contact Merges Configuration

**File**: `contact_merges.json` (auto-generated)

**Purpose**: Store merge decisions and skipped pairs for reuse.

**Structure**:
```json
{
  "MergeDecisions": [
    {
      "SourceContacts": [
        "John|+15551111",
        "Johnny|+15552222"
   ],
      "TargetName": "John Doe",
    "TargetPhone": "+15551111",
      "IsSkipped": false,
    "Reason": "Same person, different numbers"
    },
    {
      "SourceContacts": [
  "Spam|+18001234",
     "Unknown|+18005678"
      ],
      "TargetName": "Spam",
      "TargetPhone": "+18001234",
      "IsSkipped": true,
      "Reason": "Skipped: Different spam callers"
 }
  ]
}
```

**Auto-Generated**: Created when you save merge decisions in interactive mode.

**Reuse**: Automatically applied on next run if source contacts match.

**Skip Tracking**: `IsSkipped: true` prevents duplicate from showing again.

---

## Configuration Priority

Settings are applied in this order (highest priority first):

1. **Command-line arguments** (e.g., `--user-name "John"`)
2. **Environment variables** (`.env` file)
3. **Configuration file** (`appsettings.json`)
4. **Interactive prompts** (if mode is "Ask")
5. **Default values**

**Example**:
```
appsettings.json: UserName = "User"
.env: SMS_USER_NAME="John Doe"
Command-line: --user-name "Jane Smith"

Result: UserName = "Jane Smith" (command-line wins)
```

---

## Common Configurations

### Configuration 1: Quick Export (No Extras)

**Use Case**: Just convert to Parquet, no analysis

**appsettings.json**:
```json
{
  "Features": {
    "ExtractMMS": "Disable",
  "SplitByContact": "Disable",
    "EnableFiltering": "Disable",
 "ExportToSQLite": "Disable",
    "ExportToHTML": "Disable"
  }
}
```

**Command**:
```bash
SMSXmlToCsv backup.xml
```

---

### Configuration 2: Full Analysis with AI

**Use Case**: Comprehensive analysis with all features

**appsettings.json**:
```json
{
  "Features": {
    "ExtractMMS": "Enable",
    "SplitByContact": "Enable",
    "EnableFiltering": "Disable",
    "ExportToSQLite": "Enable",
    "ExportToHTML": "Enable"
  },
  "Analysis": {
    "EnableThreadAnalysis": true,
    "EnableResponseTimeAnalysis": true,
    "EnableAdvancedStatistics": true
  },
  "MLFeatures": {
    "EnableSentimentAnalysis": true,
    "GenerateNetworkGraph": true,
    "GeneratePdfReport": true
  }
}
```

**Command**:
```bash
SMSXmlToCsv backup.xml --save-config
```

---

### Configuration 3: Date-Filtered Export

**Use Case**: Export specific year(s) only

**appsettings.json**:
```json
{
  "Features": {
    "SplitByContact": "Enable"
  },
  "Filtering": {
    "DateFrom": "2023-01-01",
    "DateTo": "2023-12-31"
  }
}
```

---

### Configuration 4: Specific Contacts Only

**Use Case**: Export conversations with family members

**appsettings.json**:
```json
{
  "Features": {
    "SplitByContact": "Enable",
    "EnableFiltering": "Enable"
  },
  "Filtering": {
    "PreConfiguredContacts": [
      "Mom|+15551111",
 "Dad|+15552222",
  "Sister|+15553333"
    ]
  }
}
```

---

### Configuration 5: Automation-Friendly

**Use Case**: Batch processing without prompts

**appsettings.json**:
```json
{
  "Features": {
    "ExtractMMS": "Enable",
    "SplitByContact": "Enable",
    "EnableFiltering": "Disable",
    "ExportToSQLite": "Enable",
"ExportToHTML": "Disable"
},
  "ErrorHandling": {
    "ContinueOnError": true
  }
}
```

**Command**:
```bash
for %f in (*.xml) do SMSXmlToCsv.exe "%f" --formats parquet,csv
```

---

## Validation

### Configuration Validation

The application validates configuration on startup:

**Valid Modes**: `"Enable"`, `"Disable"`, `"Ask"` (case-insensitive)

**Date Format**: Must be `YYYY-MM-DD` or `null`

**Contact Format**: Must be `Name|Phone`

**Column Names**: Must match available columns exactly

**Warnings**:
- Unknown columns are ignored with warning
- Invalid dates are ignored with warning
- Malformed contacts are skipped with warning

---

## Troubleshooting Configuration

### Problem: Settings Not Applied

**Symptoms**: Changes to `appsettings.json` not taking effect

**Causes**:
1. Edited wrong `appsettings.json` (check correct directory)
2. Command-line arguments override
3. Invalid JSON syntax

**Solutions**:
1. Verify file location (next to .exe)
2. Remove command-line arguments
3. Validate JSON: https://jsonlint.com

---

### Problem: .env Not Loading

**Symptoms**: User name/phone not read from `.env`

**Causes**:
1. File not in correct location (project root)
2. Typo in variable names
3. Missing `DotNetEnv` package

**Solutions**:
1. Ensure `.env` is in same directory as `.sln` or `.csproj`
2. Check variable names: `SMS_USER_NAME`, `SMS_USER_PHONE`
3. Reinstall: `dotnet restore`

---

### Problem: Contact Merges Not Persisting

**Symptoms**: Merge decisions not remembered

**Cause**: `contact_merges.json` not being created/saved

**Solutions**:
1. Check file permissions (write access)
2. Answer "yes" when prompted to save
3. Manually verify `contact_merges.json` exists after saving

---

## See Also

- [Command-Line Reference](COMMAND_LINE.md) - CLI options
- [User Guide](USER_GUIDE.md) - Feature walkthroughs
- [Known Issues](KNOWN_ISSUES.md) - Limitations
- [Troubleshooting](TROUBLESHOOTING.md) - Common problems

---

**Version**: 0.7 | **Last Updated**: October 2025
