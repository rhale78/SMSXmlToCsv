# Implementation Roadmap

This document provides a structured roadmap for implementing the features documented in `ISSUES_TO_CREATE.md`. Use this as a guide for creating GitHub issues or implementing features directly.

## Quick Links
- [GitHub Issues Template](#github-issues-creation-guide)
- [High Priority Features](#high-priority-features)
- [Medium Priority Features](#medium-priority-features)
- [Lower Priority Features](#lower-priority-features)
- [Implementation Order](#recommended-implementation-order)

---

## GitHub Issues Creation Guide

### Automated Issue Creation Script

If you want to create all issues at once, use the GitHub CLI (`gh`):

```bash
# Install GitHub CLI if not already installed
# https://cli.github.com/

# Authenticate
gh auth login

# Create all issues from ISSUES_TO_CREATE.md
# Note: You'll need to parse the markdown and create issues programmatically
```

### Manual Creation

1. Navigate to: https://github.com/rhale78/SMSXmlToCsv/issues/new
2. Copy the title and body from each issue section in `ISSUES_TO_CREATE.md`
3. Add the labels specified in each issue
4. Submit

---

## High Priority Features

### 1. Network Graph Visualization
**Issue #**: TBD  
**Estimated Effort**: Large (2-3 weeks)  
**Dependencies**: None  
**Labels**: `enhancement`, `feature`, `high-priority`, `legacy-port`

**Implementation Checklist**:
- [ ] Create `Visualization` directory in `Services`
- [ ] Port `NetworkGraphGenerator.cs` from `/old/code/`
- [ ] Update to use current architecture patterns
- [ ] Add D3.js template files
- [ ] Integrate with main menu
- [ ] Add configuration options to `appsettings.json`
- [ ] Write unit tests
- [ ] Test with sample data
- [ ] Update documentation

**Key Files**:
- `/old/code/SMSXmlToCsv/Visualization/NetworkGraphGenerator.cs` (reference)
- New: `/src/SMSXmlToCsv/Services/Visualization/NetworkGraphGenerator.cs`

---

### 2. Contact Merge Functionality
**Issue #**: TBD  
**Estimated Effort**: Medium (1-2 weeks)  
**Dependencies**: None  
**Labels**: `enhancement`, `feature`, `high-priority`, `legacy-port`

**Implementation Checklist**:
- [ ] Create `ContactMergeService.cs` in `Services`
- [ ] Port duplicate detection logic from `/old/code/SMSXmlToCsv/Utils/ContactMerger.cs`
- [ ] Add Spectre.Console interactive UI
- [ ] Implement merge persistence (save decisions)
- [ ] Add to main menu as optional step
- [ ] Write unit tests
- [ ] Test with duplicate contacts
- [ ] Update documentation

**Key Files**:
- `/old/code/SMSXmlToCsv/Utils/ContactMerger.cs` (reference)
- New: `/src/SMSXmlToCsv/Services/ContactMergeService.cs`

---

### 3. Command-Line Interface
**Issue #**: TBD  
**Estimated Effort**: Medium (1-2 weeks)  
**Dependencies**: None  
**Labels**: `enhancement`, `feature`, `automation`, `legacy-port`

**Implementation Checklist**:
- [ ] Add `CommandLineOptions.cs` model
- [ ] Create `CommandLineParser.cs` service
- [ ] Update `Program.cs` to support both CLI and interactive modes
- [ ] Add argument parsing (input, output, formats, filters)
- [ ] Implement headless/non-interactive execution
- [ ] Add help text and examples
- [ ] Write unit tests
- [ ] Test all CLI scenarios
- [ ] Update documentation with CLI examples

**Key Files**:
- `/old/code/SMSXmlToCsv/CommandLineParser.cs` (reference)
- `/old/docs/COMMAND_LINE.md` (reference documentation)
- New: `/src/SMSXmlToCsv/Services/CommandLineParser.cs`
- Update: `/src/SMSXmlToCsv/Program.cs`

---

### 4. Thread Analysis
**Issue #**: TBD  
**Estimated Effort**: Small (3-5 days)  
**Dependencies**: None  
**Labels**: `enhancement`, `feature`, `analysis`, `legacy-port`

**Implementation Checklist**:
- [ ] Create `Analysis` directory in `Services`
- [ ] Port `ConversationThreadAnalyzer.cs`
- [ ] Add configurable thread timeout to `appsettings.json`
- [ ] Implement thread statistics calculation
- [ ] Add thread metadata to exports
- [ ] Add to main menu as optional analysis
- [ ] Write unit tests
- [ ] Test with various conversation patterns
- [ ] Update documentation

**Key Files**:
- `/old/code/SMSXmlToCsv/Analysis/ConversationThreadAnalyzer.cs` (reference)
- New: `/src/SMSXmlToCsv/Services/Analysis/ThreadAnalyzer.cs`

---

### 5. Response Time Analysis
**Issue #**: TBD  
**Estimated Effort**: Small (3-5 days)  
**Dependencies**: None  
**Labels**: `enhancement`, `feature`, `analysis`, `legacy-port`

**Implementation Checklist**:
- [ ] Create `ResponseTimeAnalyzer.cs` in `Services/Analysis`
- [ ] Port logic from legacy code
- [ ] Calculate per-contact response times
- [ ] Generate response time statistics
- [ ] Export to JSON format
- [ ] Add to main menu
- [ ] Write unit tests
- [ ] Test with sample conversations
- [ ] Update documentation

**Key Files**:
- `/old/code/SMSXmlToCsv/Analysis/ResponseTimeAnalyzer.cs` (reference)
- New: `/src/SMSXmlToCsv/Services/Analysis/ResponseTimeAnalyzer.cs`

---

## Medium Priority Features

### 6. Interactive Message Search
**Issue #**: TBD  
**Estimated Effort**: Small (3-5 days)  
**Dependencies**: None  
**Labels**: `enhancement`, `feature`, `legacy-port`

**Implementation Checklist**:
- [ ] Create `MessageSearchService.cs`
- [ ] Implement keyword search with context
- [ ] Add Spectre.Console rich display
- [ ] Support case-insensitive search
- [ ] Add contact filtering
- [ ] Implement result export
- [ ] Add to main menu
- [ ] Write unit tests
- [ ] Test search functionality
- [ ] Update documentation

---

### 7. PDF Report Generation
**Issue #**: TBD  
**Estimated Effort**: Large (2-3 weeks)  
**Dependencies**: Statistics, possibly Thread/Response Time Analysis  
**Labels**: `enhancement`, `feature`, `legacy-port`

**Implementation Checklist**:
- [ ] Evaluate PDF libraries (QuestPDF recommended)
- [ ] Create `Reports` directory in `Services`
- [ ] Implement basic PDF report generator
- [ ] Add charts and visualizations
- [ ] Integrate with statistics analyzer
- [ ] Add configuration for report templates
- [ ] Add to export menu
- [ ] Write unit tests
- [ ] Generate sample reports
- [ ] Update documentation

---

### 8. Date Range Filtering
**Issue #**: TBD  
**Estimated Effort**: Small (2-3 days)  
**Dependencies**: CLI implementation recommended  
**Labels**: `enhancement`, `feature`, `legacy-port`

**Implementation Checklist**:
- [ ] Add date range options to CLI parser
- [ ] Add interactive date input to menu
- [ ] Implement filtering in import/post-import phase
- [ ] Add to `appsettings.json` configuration
- [ ] Support multiple date formats (ISO 8601, common formats)
- [ ] Write unit tests
- [ ] Test with various date ranges
- [ ] Update documentation

---

### 9. Continue-on-Error Mode
**Issue #**: TBD  
**Estimated Effort**: Small (3-5 days)  
**Dependencies**: None  
**Labels**: `enhancement`, `robustness`, `legacy-port`

**Implementation Checklist**:
- [ ] Create `ErrorCollector.cs` service
- [ ] Add continue-on-error configuration
- [ ] Wrap operations in try-catch blocks
- [ ] Collect and display error summary
- [ ] Export error reports
- [ ] Add to `appsettings.json`
- [ ] Write unit tests
- [ ] Test with corrupt data
- [ ] Update documentation

---

### 10. SQLite Database Export
**Issue #**: TBD  
**Estimated Effort**: Medium (1 week)  
**Dependencies**: None  
**Labels**: `enhancement`, `feature`, `legacy-port`

**Implementation Checklist**:
- [ ] Add SQLite NuGet package
- [ ] Create `SqliteExporter.cs` implementing `IDataExporter`
- [ ] Design database schema (Messages, Contacts, Attachments)
- [ ] Implement export logic with indexes
- [ ] Add to exporter list
- [ ] Write unit tests
- [ ] Test with large datasets
- [ ] Update documentation

---

## Lower Priority Features

### 11. Google Takeout Enhancement
**Issue #**: TBD  
**Estimated Effort**: Medium (1 week)  
**Dependencies**: None  
**Labels**: `enhancement`, `bug`, `testing`

**Implementation Checklist**:
- [ ] Obtain real Google Takeout sample data
- [ ] Test current implementation thoroughly
- [ ] Compare with legacy implementation
- [ ] Fix any parsing issues
- [ ] Add comprehensive error handling
- [ ] Document supported formats
- [ ] Write unit tests
- [ ] Update documentation

---

### 12. Sentiment Analysis (Improved)
**Issue #**: TBD  
**Estimated Effort**: Large (2-4 weeks)  
**Dependencies**: Research phase required  
**Labels**: `enhancement`, `feature`, `ai-ml`

**Implementation Checklist**:
- [ ] Research sentiment analysis options (ML.NET, Azure AI, etc.)
- [ ] Evaluate alternatives to Ollama
- [ ] Design improved architecture
- [ ] Implement sentiment classifier
- [ ] Add confidence scores
- [ ] Create validation/correction UI
- [ ] Make optional and configurable
- [ ] Add progress indicators
- [ ] Write unit tests
- [ ] Test accuracy
- [ ] Update documentation

**Note**: Should improve upon legacy implementation, not just port it.

---

### 13. Advanced Statistics and Analytics
**Issue #**: TBD  
**Estimated Effort**: Medium (1-2 weeks)  
**Dependencies**: None  
**Labels**: `enhancement`, `feature`, `analysis`, `legacy-port`

**Implementation Checklist**:
- [ ] Create `StatisticsAnalyzer.cs`
- [ ] Implement comprehensive statistics calculations
- [ ] Add time-based patterns analysis
- [ ] Display with Spectre.Console tables/charts
- [ ] Export to JSON and Markdown
- [ ] Add to main menu
- [ ] Write unit tests
- [ ] Test with sample data
- [ ] Update documentation

---

## Recommended Implementation Order

### Phase 1: Foundation (Weeks 1-4)
1. **Command-Line Interface** - Enables automation
2. **Date Range Filtering** - Common requirement
3. **Continue-on-Error Mode** - Improves robustness

### Phase 2: Core Features (Weeks 5-10)
4. **Contact Merge Functionality** - High value, clean data
5. **SQLite Database Export** - Popular format
6. **Interactive Message Search** - User-requested

### Phase 3: Analysis (Weeks 11-14)
7. **Thread Analysis** - Quick win
8. **Response Time Analysis** - Quick win
9. **Advanced Statistics** - Builds on previous analysis

### Phase 4: Advanced Features (Weeks 15-20)
10. **Network Graph Visualization** - Complex but high impact
11. **PDF Report Generation** - Integrates analysis features
12. **Google Takeout Enhancement** - Validation and testing

### Phase 5: Optional (Future)
13. **Sentiment Analysis** - Research required, lower priority

---

## Testing Strategy

### Unit Tests
- Create test project: `SMSXmlToCsv.Tests`
- Use xUnit or NUnit
- Mock dependencies
- Test each service independently

### Integration Tests
- Test end-to-end workflows
- Use sample data files
- Verify output formats

### Manual Testing
- Test with real-world data
- Verify UI interactions
- Check performance with large datasets

---

## Documentation Updates

For each implemented feature:
- [ ] Update README.md with feature description
- [ ] Add usage examples
- [ ] Update CHANGELOG.md
- [ ] Create docs page if complex
- [ ] Update configuration documentation

---

## Labels to Use

When creating GitHub issues, apply these labels:

- `enhancement` - New feature additions
- `feature` - Significant functionality
- `legacy-port` - Ported from legacy code
- `high-priority` - Should be implemented first
- `automation` - CLI/automation features
- `analysis` - Data analysis features
- `ai-ml` - Machine learning features
- `testing` - Testing and validation
- `robustness` - Error handling and stability
- `bug` - Issues that need fixing
- `documentation` - Documentation updates

---

## Progress Tracking

Use this checklist to track overall progress:

### Created Issues
- [ ] Issue 1: Network Graph Visualization
- [ ] Issue 2: Contact Merge Functionality
- [ ] Issue 3: Command-Line Interface
- [ ] Issue 4: Thread Analysis
- [ ] Issue 5: Response Time Analysis
- [ ] Issue 6: Interactive Message Search
- [ ] Issue 7: PDF Report Generation
- [ ] Issue 8: Google Takeout Support Enhancement
- [ ] Issue 9: Sentiment Analysis (Improved)
- [ ] Issue 10: Date Range Filtering
- [ ] Issue 11: Continue-on-Error Mode
- [ ] Issue 12: SQLite Database Export
- [ ] Issue 13: Advanced Statistics and Analytics

### Implemented Features
- [ ] Issue 1: Network Graph Visualization
- [ ] Issue 2: Contact Merge Functionality
- [ ] Issue 3: Command-Line Interface
- [ ] Issue 4: Thread Analysis
- [ ] Issue 5: Response Time Analysis
- [ ] Issue 6: Interactive Message Search
- [ ] Issue 7: PDF Report Generation
- [ ] Issue 8: Google Takeout Support Enhancement
- [ ] Issue 9: Sentiment Analysis (Improved)
- [ ] Issue 10: Date Range Filtering
- [ ] Issue 11: Continue-on-Error Mode
- [ ] Issue 12: SQLite Database Export
- [ ] Issue 13: Advanced Statistics and Analytics

---

## Notes

- All legacy code references are in `/old/code/` directory
- Feature comparison document: `/old/FEATURE_COMPARISON.md`
- Legacy documentation: `/old/docs/`
- Follow current architecture patterns (service classes, interfaces)
- Use Spectre.Console for rich UI
- Add comprehensive logging with Serilog
- Maintain coding standards (no `var`, one class per file, explicit types)

---

**Last Updated**: November 2025  
**Related Files**: `ISSUES_TO_CREATE.md`, `/old/FEATURE_COMPARISON.md`
