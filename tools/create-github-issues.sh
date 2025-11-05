#!/bin/bash

# Script to create GitHub issues from ISSUES_TO_CREATE.md
# Requires: GitHub CLI (gh) to be installed and authenticated
# Usage: ./create-github-issues.sh

# Check if gh CLI is installed
if ! command -v gh &> /dev/null; then
    echo "Error: GitHub CLI (gh) is not installed."
    echo "Please install it from: https://cli.github.com/"
    echo ""
    echo "Installation commands:"
    echo "  macOS:   brew install gh"
    echo "  Linux:   See https://github.com/cli/cli/blob/trunk/docs/install_linux.md"
    echo "  Windows: scoop install gh"
    exit 1
fi

# Check if authenticated
if ! gh auth status &> /dev/null; then
    echo "Error: Not authenticated with GitHub."
    echo "Please run: gh auth login"
    exit 1
fi

REPO="rhale78/SMSXmlToCsv"

echo "=========================================="
echo "GitHub Issues Creator"
echo "Repository: $REPO"
echo "=========================================="
echo ""

# Function to create an issue
create_issue() {
    local title="$1"
    local body="$2"
    local labels="$3"
    
    echo "Creating issue: $title"
    
    # Create the issue
    if gh issue create --repo "$REPO" --title "$title" --body "$body" --label "$labels"; then
        echo "✅ Created: $title"
    else
        echo "❌ Failed to create: $title"
    fi
    echo ""
}

# Issue 1: Network Graph Visualization
create_issue "Add Network Graph Visualization Feature" \
"## Description
Port the network graph visualization feature from the legacy codebase. This feature creates interactive D3.js-based network graphs showing contact relationships and topics.

## Current Status
- ✅ **Working well in legacy code** (\`/old/code/SMSXmlToCsv/Visualization/NetworkGraphGenerator.cs\`)
- ❌ Not implemented in current version

## Features to Implement
- Interactive D3.js network graphs
- AI-powered topic detection
- Contact relationship visualization
- Topic relationships
- Unlimited topics mode with filtering
- Configurable minimum message threshold

## Implementation Notes
- Follow the current architecture patterns (service classes)
- Use the existing \`IDataExporter\` interface if appropriate
- Keep visualization logic separate from data processing
- Consider making it a pluggable analyzer/visualizer

## Reference
- Legacy implementation: \`/old/code/SMSXmlToCsv/Visualization/NetworkGraphGenerator.cs\`
- Feature comparison: \`/old/FEATURE_COMPARISON.md\`" \
"enhancement,feature,high-priority,legacy-port"

# Issue 2: Contact Merge Functionality
create_issue "Add Interactive Contact Merge/Deduplication" \
"## Description
Implement contact merging functionality to handle duplicate contacts. The legacy code has an interactive UI for this that works well.

## Current Status
- ✅ **Working well in legacy code** (\`/old/code/SMSXmlToCsv/Utils/ContactMerger.cs\`)
- ❌ Not implemented in current version

## Features to Implement
- Duplicate contact detection (same phone, email, or similar name)
- Interactive merge UI using Spectre.Console
- Automatic suggestion of potential duplicates
- Skip/merge options
- Persistent merge decisions

## Implementation Notes
- Create a new service class (e.g., \`ContactMergeService\`)
- Use the existing \`Contact\` model
- Add to main menu as optional step after import
- Store merge decisions in configuration for repeat use

## Reference
- Legacy implementation: \`/old/code/SMSXmlToCsv/Utils/ContactMerger.cs\`
- Feature comparison: \`/old/FEATURE_COMPARISON.md\`" \
"enhancement,feature,high-priority,legacy-port"

# Issue 3: Command-Line Interface
create_issue "Implement Comprehensive CLI for Automation" \
"## Description
Add a comprehensive command-line interface to support automation and batch processing, similar to the legacy code.

## Current Status
- ✅ **Well-implemented in legacy code** (\`/old/code/SMSXmlToCsv/CommandLineParser.cs\`)
- ❌ Not implemented in current version (interactive only)

## Features to Implement
- Parse command-line arguments for all operations
- Support headless/non-interactive mode
- Allow specification of:
  - Input file paths
  - Output directory
  - Export formats (multiple)
  - Contact filters
  - Date ranges
  - Configuration overrides
- Help text and examples

## Example Usage
\`\`\`bash
# Export to multiple formats
SMSXmlToCsv --input backup.xml --output ./exports --formats csv,html,parquet

# Filter by date range
SMSXmlToCsv --input backup.xml --start-date 2024-01-01 --end-date 2024-12-31

# Export specific contacts
SMSXmlToCsv --input backup.xml --contacts \"John Doe,Jane Smith\"
\`\`\`

## Reference
- Legacy implementation: \`/old/code/SMSXmlToCsv/CommandLineParser.cs\`
- Documentation: \`/old/docs/COMMAND_LINE.md\`" \
"enhancement,feature,automation,legacy-port"

# Issue 4: Thread Analysis
create_issue "Implement Conversation Thread Analysis" \
"## Description
Port the thread analysis feature from legacy code that automatically detects conversation threads with configurable timeouts.

## Current Status
- ✅ Working in legacy code (\`/old/code/SMSXmlToCsv/Analysis/ConversationThreadAnalyzer.cs\`)
- ❌ Not implemented in current version

## Features to Implement
- Automatic thread detection based on time gaps
- Configurable thread timeout (e.g., 30 minutes)
- Thread statistics (count, duration, message count)
- Export thread information

## Implementation Notes
- Create a new analyzer service (e.g., \`ThreadAnalyzer\`)
- Add as optional post-processing step
- Export thread metadata alongside messages

## Reference
- Legacy implementation: \`/old/code/SMSXmlToCsv/Analysis/ConversationThreadAnalyzer.cs\`
- Feature comparison: \`/old/FEATURE_COMPARISON.md\`" \
"enhancement,feature,analysis,legacy-port"

# Issue 5: Response Time Analysis
create_issue "Implement Response Time Analysis" \
"## Description
Port the response time analysis feature that calculates message-to-message response timing and per-contact statistics.

## Current Status
- ✅ Working in legacy code (\`/old/code/SMSXmlToCsv/Analysis/ResponseTimeAnalyzer.cs\`)
- ❌ Not implemented in current version

## Features to Implement
- Calculate time between sent and received messages
- Per-contact response time statistics
- Average response time calculations
- Response time distributions
- Export to JSON and reports

## Implementation Notes
- Create \`ResponseTimeAnalyzer\` service
- Consider timezone handling carefully
- Add optional analysis step to menu

## Reference
- Legacy implementation: \`/old/code/SMSXmlToCsv/Analysis/ResponseTimeAnalyzer.cs\`
- Feature comparison: \`/old/FEATURE_COMPARISON.md\`" \
"enhancement,feature,analysis,legacy-port"

# Issue 6: Interactive Message Search
create_issue "Implement Interactive Keyword Search" \
"## Description
Add interactive keyword search across all imported messages with context highlighting and filtering.

## Current Status
- ✅ Working in legacy code (\`/old/code/SMSXmlToCsv/CLI/SearchCLI.cs\`)
- ❌ Not implemented in current version

## Features to Implement
- Keyword search across message bodies
- Context highlighting (show surrounding messages)
- Contact filtering
- Case-insensitive search
- Export search results
- Search result navigation

## Implementation Notes
- Add as a main menu option after import
- Use Spectre.Console for rich display
- Consider regex support for advanced users

## Reference
- Legacy implementation: \`/old/code/SMSXmlToCsv/CLI/SearchCLI.cs\`
- Feature comparison: \`/old/FEATURE_COMPARISON.md\`" \
"enhancement,feature,legacy-port"

# Issue 7: PDF Report Generation
create_issue "Implement PDF Report Generation with Charts" \
"## Description
Port the PDF report generation feature that creates professional reports with statistics, charts, and visualizations.

## Current Status
- ✅ Working in legacy code (\`/old/code/SMSXmlToCsv/Reports/\`)
- ❌ Not implemented in current version

## Features to Implement
- Basic PDF reports with message statistics
- Enhanced PDF reports with:
  - Charts and visualizations
  - Topic frequency analysis (if network graph implemented)
  - Response time visualizations (if response time analysis implemented)
  - Contact statistics
- Configurable report templates

## Implementation Notes
- Use a PDF library (e.g., QuestPDF, iTextSharp)
- Create new exporter: \`PdfReportExporter\`
- Keep separate from data exporters (different purpose)

## Reference
- Legacy implementation: \`/old/code/SMSXmlToCsv/Reports/PdfReportGenerator.cs\`
- Feature comparison: \`/old/FEATURE_COMPARISON.md\`" \
"enhancement,feature,legacy-port"

# Issue 8: Google Takeout Enhancement
create_issue "Enhance and Fully Test Google Takeout Import" \
"## Description
The Google Takeout importer exists in the current code but needs thorough testing and potential improvements based on the legacy implementation.

## Current Status
- ⚠️ Partially tested in legacy code
- ✅ Implemented in current code but needs validation
- Covers: Hangouts (JSON) and Voice (HTML)

## Tasks
- [ ] Test with real Google Takeout exports
- [ ] Compare with legacy implementation
- [ ] Fix any parsing issues
- [ ] Add comprehensive error handling
- [ ] Document supported Takeout formats
- [ ] Add test cases

## Formats to Support
- Google Hangouts (JSON)
- Google Voice (HTML)
- Future: Google Chat (if format available)

## Reference
- Current implementation: \`/src/SMSXmlToCsv/Importers/GoogleTakeoutImporter.cs\`
- Legacy implementation: Check \`/old/code/\` for any Google Takeout parsing" \
"enhancement,bug,testing"

# Issue 9: Sentiment Analysis
create_issue "Implement Improved Sentiment Analysis" \
"## Description
Add sentiment analysis feature with improved accuracy compared to the legacy implementation.

## Current Status
- ⚠️ Implemented in legacy code but **accuracy needs improvement**
- ❌ Not implemented in current version

## Considerations
The legacy implementation used Ollama integration but had accuracy issues. Before implementing:
1. Research better sentiment analysis libraries/models
2. Consider alternatives to Ollama (e.g., ML.NET, Azure AI, etc.)
3. Add confidence scores
4. Provide sentiment validation/correction UI
5. Make it optional (can be slow for large datasets)

## Features to Implement
- Per-message sentiment classification (positive/negative/neutral)
- Sentiment statistics and trends
- Optional AI integration for local processing
- Export sentiment data with messages
- Progress indicators

## Implementation Notes
- Don't just port legacy code - improve it first
- Consider using ML.NET or other .NET ML libraries
- Make it optional and configurable
- Add comprehensive logging

## Reference
- Legacy implementation: \`/old/code/SMSXmlToCsv/ML/OllamaIntegration.cs\`
- Feature comparison: \`/old/FEATURE_COMPARISON.md\`" \
"enhancement,feature,ai-ml"

# Issue 10: Date Range Filtering
create_issue "Implement Date Range Filtering for Imports/Exports" \
"## Description
Add ability to filter messages by date range during import or export.

## Current Status
- ✅ Working in legacy code
- ❌ Not implemented in current version

## Features to Implement
- Filter by start date
- Filter by end date
- Support for multiple date formats
- CLI arguments: \`--start-date\`, \`--end-date\`
- Configuration file support
- Menu option for interactive mode

## Example Usage
\`\`\`bash
# CLI
SMSXmlToCsv --input backup.xml --start-date 2024-01-01 --end-date 2024-12-31

# Interactive menu - prompt for date range
\`\`\`

## Implementation Notes
- Add filtering in import or post-import phase
- Use \`DateTime\` parsing with validation
- Support ISO 8601 and common formats (yyyy-MM-dd)

## Reference
- Feature comparison: \`/old/FEATURE_COMPARISON.md\`" \
"enhancement,feature,legacy-port"

# Issue 11: Continue-on-Error Mode
create_issue "Implement Graceful Error Handling with Continue-on-Error" \
"## Description
Add a continue-on-error mode that allows processing to continue when non-fatal errors occur, with comprehensive error reporting at the end.

## Current Status
- ✅ Working in legacy code (\`/old/code/SMSXmlToCsv/Utils/ErrorHandler.cs\`)
- ❌ Not implemented in current version

## Features to Implement
- Configurable continue-on-error mode
- Collect errors during processing
- Display error summary at completion
- Log errors with context
- Option to export error report

## Configuration Example
\`\`\`json
{
  \"ErrorHandling\": {
    \"ContinueOnError\": true,
    \"SaveErrorReport\": true,
    \"ErrorReportPath\": \"./error-report.txt\"
  }
}
\`\`\`

## Implementation Notes
- Wrap operations in try-catch blocks
- Use a centralized error collector
- Don't fail the entire batch for one bad message
- Useful for large datasets with occasional corrupt data

## Reference
- Legacy implementation: \`/old/code/SMSXmlToCsv/Utils/ErrorHandler.cs\`
- Feature comparison: \`/old/FEATURE_COMPARISON.md\`" \
"enhancement,robustness,legacy-port"

# Issue 12: SQLite Database Export
create_issue "Implement SQLite Database Export Format" \
"## Description
Add SQLite database as an export format option, useful for querying messages with SQL.

## Current Status
- ✅ Working in legacy code (\`/old/code/SMSXmlToCsv/Exporters/SQLiteExporter.cs\`)
- ❌ Not implemented in current version

## Features to Implement
- Export messages to SQLite database
- Proper schema design:
  - Messages table
  - Contacts table
  - Attachments table
  - Relationships
- Indexes for performance
- Follow \`IDataExporter\` interface

## Benefits
- SQL queries on message data
- Integration with SQLite tools
- Efficient for large datasets
- Can generate other SQL formats from this

## Implementation Notes
- Use Microsoft.Data.Sqlite or System.Data.SQLite
- Create \`SqliteExporter\` class implementing \`IDataExporter\`
- Consider adding as base for PostgreSQL/MySQL exporters

## Reference
- Legacy implementation: \`/old/code/SMSXmlToCsv/Exporters/SQLiteExporter.cs\`
- Feature comparison: \`/old/FEATURE_COMPARISON.md\`" \
"enhancement,feature,legacy-port"

# Issue 13: Advanced Statistics
create_issue "Implement Advanced Message Statistics and Analytics" \
"## Description
Port the advanced statistics feature that provides comprehensive messaging analytics.

## Current Status
- ✅ Working in legacy code (\`/old/code/SMSXmlToCsv/Analysis/AdvancedStatisticsExporter.cs\`)
- ❌ Not implemented in current version

## Statistics to Include
- Total messages (sent/received/per contact)
- Time-based patterns (by hour/day/month)
- Word count analysis
- Message length statistics
- Most active contacts
- Conversation frequency
- Activity patterns
- Export to JSON and Markdown

## Implementation Notes
- Create \`StatisticsAnalyzer\` service
- Make it an optional analysis step
- Display in console with Spectre.Console tables/charts
- Export as separate file
- Consider adding charts/graphs

## Reference
- Legacy implementation: \`/old/code/SMSXmlToCsv/Analysis/AdvancedStatisticsExporter.cs\`
- Feature comparison: \`/old/FEATURE_COMPARISON.md\`" \
"enhancement,feature,analysis,legacy-port"

echo ""
echo "=========================================="
echo "✅ All issues created successfully!"
echo "=========================================="
echo ""
echo "View issues at: https://github.com/$REPO/issues"
echo ""
echo "Next steps:"
echo "1. Review created issues"
echo "2. Assign to appropriate milestones"
echo "3. Prioritize in project board"
echo "4. Start implementation following IMPLEMENTATION_ROADMAP.md"
