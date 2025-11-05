# Command-Line Reference - SMS Backup XML Converter v0.7

Complete reference for all command-line options and usage patterns.

---

## Table of Contents

- [Basic Syntax](#basic-syntax)
- [Arguments](#arguments)
- [Output Formats](#output-formats)
- [Feature Flags](#feature-flags)
- [Filtering Options](#filtering-options)
- [Analysis Options](#analysis-options)
- [AI/ML Options](#aiml-options)
- [Configuration Options](#configuration-options)
- [Error Handling](#error-handling)
- [Examples](#examples)

---

## Basic Syntax

```bash
SMSXmlToCsv [options] <input-file>
```

### Help

```bash
SMSXmlToCsv --help
SMSXmlToCsv -h
SMSXmlToCsv /?
```

---

## Arguments

### Input File

```bash
# Positional argument (no flag needed)
SMSXmlToCsv backup.xml

# Or explicit path
SMSXmlToCsv "C:\Backups\sms-20250128.xml"
```

**Note**: If no input file specified, interactive file selection is shown.

---

## Output Formats

### `--formats <format-list>`
### Aliases: `-f`

Specify output formats as comma-separated list.

**Available Formats**:
- `csv` - Excel-friendly CSV
- `json` or `jsonl` - JSON Lines (one message per line)
- `parquet` or `pq` - Apache Parquet (compressed, AI-optimized)
- `sqlite` or `db` - SQLite database
- `html` - HTML chat pages (requires `--split enable`)
- `postgresql`, `postgres`, `pgsql` - PostgreSQL SQL script
- `mysql` - MySQL SQL script
- `markdown` or `md` - Markdown formatted export

**Examples**:
```bash
# Single format
SMSXmlToCsv backup.xml --formats csv

# Multiple formats
SMSXmlToCsv backup.xml --formats csv,json,parquet

# All formats
SMSXmlToCsv backup.xml -f csv,jsonl,parquet,sqlite,html,markdown
```

**Default**: `parquet` (if no formats specified)

---

## Feature Flags

All feature flags accept: `enable`, `disable`, or `ask` (prompt user).

### `--mms <mode>`
### Aliases: `--mms-extraction`

Extract MMS attachments (images, videos, audio).

```bash
SMSXmlToCsv backup.xml --mms enable
SMSXmlToCsv backup.xml --mms-extraction disable
SMSXmlToCsv backup.xml --mms ask  # Prompt user
```

**Default**: `ask`

---

### `--split <mode>`
### Aliases: `--split-contact`

Split messages into separate folders per contact.

```bash
SMSXmlToCsv backup.xml --split enable
```

**Required for**: HTML export, SQLite per-contact, PDF reports

**Default**: `ask`

---

### `--filter <mode>`
### Aliases: `--filter-contacts`

Enable interactive contact selection.

```bash
SMSXmlToCsv backup.xml --split enable --filter enable
```

**Requires**: `--split enable`

**Default**: `disable`

---

### `--sqlite <mode>`
### Aliases: `--sqlite-export`

Export to SQLite database.

```bash
SMSXmlToCsv backup.xml --split enable --sqlite enable
```

**Requires**: `--split enable`

**Default**: `disable`

---

### `--html <mode>`
### Aliases: `--html-export`

Generate HTML chat pages.

```bash
SMSXmlToCsv backup.xml --split enable --html enable
```

**Requires**: `--split enable`

**Default**: `disable`

---

### `--features <feature-list>`

Set multiple features at once using `name=mode` pairs.

```bash
SMSXmlToCsv backup.xml --features mms=enable,split=enable,filter=disable,sqlite=enable,html=enable
```

---

## Filtering Options

### `--date-from <YYYY-MM-DD>`
### Aliases: `--from-date`

Filter messages from specific start date (inclusive).

```bash
SMSXmlToCsv backup.xml --date-from 2023-01-01
```

---

### `--date-to <YYYY-MM-DD>`
### Aliases: `--to-date`

Filter messages up to specific end date (inclusive).

```bash
SMSXmlToCsv backup.xml --date-to 2023-12-31
```

---

### `--select-contacts <contact-list>`
### Aliases: `--contacts-list`

Pre-select specific contacts for export.

**Format**: `"Name1|Phone1,Name2|Phone2"`

```bash
SMSXmlToCsv backup.xml --select-contacts "John Doe|+15551234,Jane Smith|+15555678"
```

**Note**: Phone numbers must match exactly as they appear in backup.

---

### `--columns <column-list>`
### Aliases: `--fields`

Select which columns to export.

**Available Columns**:
- `FromName` (required)
- `FromPhone` (required)
- `ToName` (required)
- `ToPhone` (required)
- `Direction`
- `DateTime`
- `UnixTimestamp`
- `MessageText`
- `MessageLength`
- `HasMMS`

**Format**: Comma-separated list

```bash
SMSXmlToCsv backup.xml --columns "FromName,ToName,DateTime,MessageText"
```

**Default**: All columns

**Note**: Required columns are always included even if not specified.

---

## Analysis Options

### `--thread-analysis`

Enable conversation thread detection.

```bash
SMSXmlToCsv backup.xml --thread-analysis
```

**Output**: `threads.json` with detected conversation threads

---

### `--response-time-analysis`

Analyze response times between messages.

```bash
SMSXmlToCsv backup.xml --response-time-analysis
```

**Output**: `response_times.json` with timing statistics

---

### `--advanced-statistics`

Generate comprehensive statistics.

```bash
SMSXmlToCsv backup.xml --advanced-statistics
```

**Output**: `statistics.json` and `statistics.md`

---

### `--search-mode`
### Aliases: `--interactive-search`

Launch interactive keyword search mode.

```bash
SMSXmlToCsv backup.xml --search-mode
```

**Note**: Processes backup then opens search interface.

---

## AI/ML Options

**All AI features require Ollama to be installed and running.**

### `--sentiment`
### Aliases: `--sentiment-analysis`

Perform AI-powered sentiment analysis.

```bash
SMSXmlToCsv backup.xml --sentiment
```

**Output**: `sentiment_analysis.json`

**Requirements**: Ollama with `llama3.2` model

---

### `--clustering`
### Aliases: `--cluster`

Cluster similar conversations.

```bash
SMSXmlToCsv backup.xml --clustering
```

**Output**: `clusters.json`

---

### `--network-graph`
### Aliases: `--graph`

Generate interactive network visualization with AI topic detection.

```bash
SMSXmlToCsv backup.xml --split enable --network-graph
```

**Output**: `network_graph.json` and `network_graph.html`

**Performance**: Can be slow with >50k messages

---

### `--pdf-report`
### Aliases: `--pdf`

Generate comprehensive PDF report.

```bash
SMSXmlToCsv backup.xml --split enable --pdf-report
```

**Output**: `comprehensive_report.pdf`

**Enhanced**: Includes AI insights if `--sentiment` or `--network-graph` also enabled

---

### `--ollama-model <model-name>`

Specify which Ollama model to use.

```bash
SMSXmlToCsv backup.xml --sentiment --ollama-model llama2
```

**Default**: `llama3.2`

**Available**: Any model installed in Ollama (`ollama list`)

---

### `--no-ollama`

Disable Ollama even if installed. Uses fallback methods.

```bash
SMSXmlToCsv backup.xml --no-ollama --pdf-report
```

---

## Configuration Options

### `--user-name <name>`
### Aliases: `--username`, `--name`

Set your name (appears as "You" in outputs).

```bash
SMSXmlToCsv backup.xml --user-name "John Doe"
```

**Priority**: Command-line > .env > appsettings.json > "User"

---

### `--user-phone <phone>`
### Aliases: `--userphone`, `--phone`

Set your phone number (used for contact splitting).

```bash
SMSXmlToCsv backup.xml --user-phone "+15551234567"
```

**Format**: Include country code (e.g., `+1` for US)

---

### `--output-dir <path>`
### Aliases: `--output`

Specify output directory.

```bash
SMSXmlToCsv backup.xml --output-dir "C:\SMS Exports"
```

**Default**: Same directory as input XML file

---

### `--contacts-dir <name>`
### Aliases: `--contacts`

Set custom name for contacts subfolder.

```bash
SMSXmlToCsv backup.xml --split enable --contacts-dir "People"
```

**Default**: `Contacts`

---

### `--save-config`
### Aliases: `--save`

Save current settings to `appsettings.json` for future use.

```bash
SMSXmlToCsv backup.xml --split enable --mms enable --save-config
```

**Saves**:
- Feature modes
- Selected columns
- Date ranges
- Folder names

---

## Error Handling

### `--continue-on-error`
### Aliases: `--ignore-errors`

Continue processing even if errors occur.

```bash
SMSXmlToCsv backup.xml --continue-on-error
```

**Use case**: When some MMS files are corrupted but you want to process the rest.

---

### `--stop-on-error`
### Aliases: `--strict`

Stop processing on first error (default behavior).

```bash
SMSXmlToCsv backup.xml --stop-on-error
```

---

### `--log-console`
### Aliases: `--console-log`

Log to console in addition to log file.

```bash
SMSXmlToCsv backup.xml --log-console
```

**Use case**: Debugging or monitoring automated runs.

---

## Examples

### Example 1: Basic Conversion

Convert backup to Parquet format (fastest, smallest):

```bash
SMSXmlToCsv backup.xml
```

---

### Example 2: Excel-Friendly Export

Create CSV for analysis in Excel:

```bash
SMSXmlToCsv backup.xml --formats csv --split enable --mms enable
```

---

### Example 3: Complete Analysis

Export everything with AI analysis:

```bash
SMSXmlToCsv backup.xml \
  --split enable \
  --mms enable \
  --formats csv,parquet,html \
  --sentiment \
  --network-graph \
  --pdf-report
```

---

### Example 4: Date Range Export

Export messages from 2023 only:

```bash
SMSXmlToCsv backup.xml \
  --date-from 2023-01-01 \
  --date-to 2023-12-31 \
  --formats csv
```

---

### Example 5: Specific Contacts

Export only conversations with specific people:

```bash
SMSXmlToCsv backup.xml \
  --split enable \
  --select-contacts "John Doe|+15551234,Jane Smith|+15555678" \
  --formats html
```

---

### Example 6: Minimal Columns

Export only essential fields to reduce file size:

```bash
SMSXmlToCsv backup.xml \
  --columns "FromName,ToName,DateTime,MessageText" \
  --formats parquet
```

---

### Example 7: Database Export

Create searchable SQLite database:

```bash
SMSXmlToCsv backup.xml \
  --split enable \
  --sqlite enable \
  --mms enable
```

Query with:
```bash
sqlite3 backup.db "SELECT * FROM messages WHERE MessageText LIKE '%keyword%'"
```

---

### Example 8: Automation

Fully automated with saved configuration:

```bash
# First run: Configure and save
SMSXmlToCsv backup1.xml \
  --features mms=enable,split=enable,filter=disable \
  --formats parquet,csv \
  --save-config

# Future runs: Uses saved settings
SMSXmlToCsv backup2.xml
SMSXmlToCsv backup3.xml
```

---

### Example 9: Network Visualization Only

Generate only the network graph:

```bash
SMSXmlToCsv backup.xml \
  --split enable \
  --network-graph \
  --formats none
```

Open `network_graph.html` in browser.

---

### Example 10: Batch Processing

Process multiple files:

```bash
# Windows
for %f in (*.xml) do SMSXmlToCsv.exe "%f" --split enable

# PowerShell
Get-ChildItem *.xml | ForEach-Object { .\SMSXmlToCsv.exe $_.FullName --split enable }
```

---

## Option Compatibility Matrix

| Feature | Requires | Conflicts With |
|---------|----------|----------------|
| `--html` | `--split enable` | - |
| `--sqlite` | `--split enable` | - |
| `--pdf-report` | `--split enable` | - |
| `--filter` | `--split enable` | - |
| `--sentiment` | Ollama installed | `--no-ollama` |
| `--clustering` | Ollama installed | `--no-ollama` |
| `--network-graph` | Ollama installed (recommended) | - |

---

## Configuration Priority

Settings are applied in this order (highest priority first):

1. **Command-line arguments** (highest)
2. **`.env` file**
3. **`appsettings.json`**
4. **Interactive menu prompts**
5. **Default values** (lowest)

Example:
```
appsettings.json: mms=ask
.env: Not set
Command-line: --mms enable

Result: MMS extraction enabled (command-line wins)
```

---

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Error during processing |
| 2 | Invalid arguments |
| 3 | File not found |
| 4 | Configuration error |

---

## Performance Tips

1. **Use Parquet for large backups**: Fastest to write, smallest file size
2. **Skip HTML for huge datasets**: HTML generation is slowest
3. **Filter by date first**: Reduces processing time
4. **Use `--continue-on-error`**: Don't let one bad file stop entire export
5. **Close other apps**: Especially for AI features (need RAM)

---

## See Also

- [User Guide](USER_GUIDE.md) - Feature walkthroughs
- [Configuration Guide](CONFIGURATION.md) - Configuration file details
- [Known Issues](KNOWN_ISSUES.md) - Limitations and workarounds
- [Troubleshooting](TROUBLESHOOTING.md) - Common problems

---

**Version**: 0.7 | **Last Updated**: October 2025
