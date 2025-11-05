# Known Issues and Limitations - SMS Backup XML Converter v0.7

This document lists all known issues, limitations, and areas that need further testing.

---

## ?? Critical Issues

### 1. Icon/Emoji Display Problems

**Issue**: Some emoji and Unicode icons appear as `?/?` or incorrect characters in Windows Console.

**Affected Areas**:
- Menu selections (checkboxes, bullets)
- Progress indicators
- File type icons in reports

**Cause**: Windows Console (cmd.exe) has limited Unicode support, especially with complex emoji.

**Workarounds**:
1. **Use Windows Terminal** (provides better but not perfect Unicode support):
   ```bash
   # Download from Microsoft Store or:
   # https://github.com/microsoft/terminal
   ```

2. **Use PowerShell 7+** (still may not fully resolve issue):
   ```bash
   pwsh.exe
   ```

3. **Configure Console Font**:
   - Right-click console title bar ? Properties
   - Font tab ? Select "NSimSun" or "MS Gothic"
   - Results vary by system and font availability

4. **Accept Visual Inconsistency**:
   - Functionality is not affected
   - Icons are cosmetic only
   - All features work regardless of icon display

**Status**: Partially mitigated by Spectre.Console integration in v0.7, but not fully resolved. This is a limitation of Windows Console Unicode support.

---

## ?? Performance Issues

### 2. Network Graph Generation Slow with Large Datasets

**Issue**: Network graph generation with AI topic detection can be very slow for backups with >50,000 messages.

**Expected Performance**:
- <10k messages: <30 seconds
- 10k-50k messages: 1-5 minutes
- >50k messages: 5-15 minutes (with Ollama)

**Causes**:
- AI topic detection requires analysis of message samples
- D3.js graph layout calculations for many nodes
- JSON file generation and HTML rendering

**Workarounds**:
1. Use date filtering to reduce message count:
   ```bash
   SMSXmlToCsv.exe backup.xml --date-from 2024-01-01 --network-graph
   ```

2. Disable topic detection (faster but less insightful):
   - Set `UNLIMITED_TOPICS_MODE = false` in `NetworkGraphGenerator.cs`

3. Skip network graph for initial analysis:
   ```bash
   SMSXmlToCsv.exe backup.xml --sentiment --pdf-report # Skip --network-graph
   ```

**Status**: Optimization planned for v0.8

### 3. PDF Report Generation Memory Usage

**Issue**: Generating PDF reports for contacts with >10,000 messages can use 1-2 GB RAM.

**Causes**:
- QuestPDF renders all pages in memory
- Chart generation for statistics
- Image embedding for charts

**Workarounds**:
1. Generate PDFs per contact rather than combined
2. Close other applications before generating large PDFs
3. Use date filtering to reduce report size

**Status**: Working as expected for most use cases

---

## ?? Untested/Experimental Features

### 4. PostgreSQL and MySQL Exports

**Issue**: Limited real-world testing with actual PostgreSQL/MySQL servers.

**What Works**:
- ? SQL generation
- ? Basic schema creation
- ? Data insertion statements

**What's Untested**:
- ? Large-scale imports (>100k messages)
- ? Character encoding edge cases
- ? Performance with different DB versions
- ? Transaction handling on errors

**Recommendation**: Test with small datasets first. Use SQLite export as primary database option.

**Status**: Needs community testing

### 5. Batch Processing Mode

**Issue**: Batch processing of multiple XML files is experimental.

**Known Limitations**:
- No progress aggregation across files
- Error in one file may stop entire batch
- Configuration applies to all files (can't mix settings)

**Workarounds**:
- Process files individually in automated scripts
- Use `--continue-on-error` flag

**Status**: Experimental feature

### 6. MMS SMIL File Parsing

**Issue**: SMIL files (MMS slideshows with timing) are not fully parsed.

**What Happens**:
- SMIL files are extracted as raw XML
- Individual media files are extracted
- Slideshow timing/order is lost

**Impact**: Most MMS are single images/videos, so limited impact.

**Status**: Enhancement planned for future versions

### 7. Contact Merge Skip Persistence

**Issue**: Skipped merge decisions are saved but may resurface if contact names change between exports.

**Example Scenario**:
1. User skips merging "John" and "John Smith"
2. Next backup, "John Smith" becomes "Johnny"
3. System suggests merge again (different name)

**Workaround**: Permanent merge decisions work correctly. Only "skip" decisions may need re-confirmation.

**Status**: Minor usability issue

---

## ?? MIME Type Recognition

### 8. Exotic MIME Types May Not Be Recognized

**Issue**: Some unusual MIME types from older phones may not be correctly identified.

**Known Working**:
- ? image/jpeg, image/png, image/gif, image/webp
- ? video/mp4, video/3gpp, video/avi
- ? audio/mpeg, audio/mp3, audio/aac, audio/amr
- ? text/plain, text/vcard

**May Not Work**:
- ? Proprietary formats (Nokia .nth, etc.)
- ? Some DRM-protected media
- ? Corrupted MIME headers

**Workaround**: Files are still extracted with original extensions. Use `--validate-mime` flag for strict validation.

**Status**: Works for 99% of common cases

---

## ?? AI Features Limitations

### 9. Ollama Model Download Size

**Issue**: First-time Ollama setup requires 2-4 GB download.

**Models**:
- `llama3.2` (default): 2.0 GB
- `llama2`: 3.8 GB
- Other models will vary in size

**Workaround**: None. This is a one-time download per model.

**Status**: Download size is a trade-off for advanced AI capabilities. Consider your storage and bandwidth before proceeding.

---

### 10. SMS Backup XML Converter AI Model Limitations

**Issue**: Built-in AI models have limitations in understanding context, sarcasm, and some languages/slang.

**Recommendations for Best Results**:
- Use simple, clear language in messages
- Avoid heavily abbreviated texts
- Provide context if the conversation is complex

**Status**: AI improvements planned for future releases. User feedback is essential for training better models.

---

### 11. Automated Sentiment Analysis Limitations

**Issue**: Sentiment analysis may not always match human judgment, especially in complex or ambiguous texts.

**Recommendations**:
- Use specific keywords for clear sentiment
- Be aware of potential misclassifications

**Status**: Continuous improvement planned. Rated by users for accuracy.

---

### 12. WhatsApp Backup Not Supported (Yet)

**Issue**: WhatsApp uses a different export format than Android SMS backups.

**Status**: **NOT PLANNED** for near-term releases. Focus is on Google Takeout and Apple iMessage integration.

**Current Workaround**: None - use Android SMS backup format only.

### 13. Google Takeout Integration Not Yet Available

**Issue**: Planned feature for importing Google Hangouts, Chat, Gmail is not yet implemented.

**Status**: **Planned for v0.8** - High priority

**Related**: Apple iMessage/SMS backup support also planned for v0.8

---

### 20. Emoji Analysis Not Planned

**Issue**: Emoji usage analysis mentioned in some documentation is not currently planned.

**Reason**: Requires extensive emoji parsing libraries and databases. Not a priority feature.

**Status**: Removed from roadmap. May be reconsidered in v1.0+

---

## ?? Features Requiring Testing

### 21. Column Selection (UNTESTED)

**Issue**: Column selection feature has not been tested in production environments.

**Risk Areas**:
- Interactive column picker menu
- `--columns` command-line flag
- Config file column selection
- Required column enforcement
- Compatibility with all exporters

**Recommendation**: Use default columns (export all) until thoroughly tested.

**Status**: Implemented but needs validation

### 22. Language Detection/Translation Not Planned

**Issue**: Multi-language sentiment analysis and translation features are not planned.

**Current Support**: English only for AI features

**Reason**: Requires significant ML model changes and testing across languages.

**Status**: Not on roadmap for v0.8 or v0.9

---

**Last Updated**: Version 0.7 (October 2025)
