# SMS Backup XML Converter - Wiki Content

This folder contains the complete Wiki content for the SMS Backup XML Converter project.

---

## ?? About This Wiki

The Wiki provides comprehensive documentation including:
- Step-by-step tutorials
- Feature guides
- Troubleshooting help
- How-to articles
- FAQ and common issues
- Development guides

---

## ?? Using This Wiki

### Option 1: GitHub Wiki (Recommended)

These markdown files are designed to be uploaded to the **GitHub Wiki** for your repository:

1. **Go to your repository**: `https://github.com/rhale78/SMSXmlToCsv`
2. **Click "Wiki" tab**
3. **Create/Edit pages**: Copy content from these `.md` files
4. **Link pages**: Wiki automatically converts filenames to links

**Example**: `[Quick Start](Quick-Start)` automatically links to `Quick-Start.md`

### Option 2: Local Viewing

View these markdown files locally:

```bash
# Using markdown viewer
code wiki/Home.md

# Or convert to HTML
pandoc wiki/Home.md -o wiki/Home.html
```

### Option 3: Alternative Wiki Software

Compatible with:
- **GitHub Wiki** (recommended)
- **GitLab Wiki**
- **Gollum** (local wiki server)
- **MkDocs** (static site generator)
- **Docusaurus** (React-based)

---

## ?? Wiki Structure

### Core Pages (Created)

| File | Description | Status |
|------|-------------|--------|
| **Home.md** | Wiki landing page, navigation hub | ? Complete |
| **Quick-Start.md** | 5-minute getting started guide | ? Complete |
| **FAQ.md** | Frequently asked questions | ? Complete |
| **Installing-Ollama.md** | AI setup guide | ? Complete |

### Pages Referenced (To Create)

The following pages are referenced in the wiki but need to be created:

#### Getting Started
- Installation-Guide.md
- First-Export.md
- Basic-Usage.md

#### Features
- Export-Formats.md
- MMS-Extraction.md
- Contact-Management.md
- Date-Filtering.md
- Analysis-Features.md
- AI-Features.md
- Network-Visualization.md
- PDF-Reports.md

#### Configuration
- Configuration-Files.md
- Command-Line-Options.md
- Feature-Modes.md
- Saving-Settings.md

#### AI Setup
- Model-Selection.md
- Ollama-Troubleshooting.md
- AI-Performance.md

#### How-To Guides
- Export-to-Excel.md
- Create-Database.md
- Extract-MMS-Files.md
- Filter-by-Date.md
- Merge-Duplicate-Contacts.md
- Generate-Network-Graph.md
- Run-Sentiment-Analysis.md
- Create-PDF-Report.md
- Batch-Processing.md
- SQLite-Queries.md
- PostgreSQL-Setup.md
- MySQL-Setup.md

#### Troubleshooting
- Installation-Problems.md
- Runtime-Errors.md
- Ollama-Issues.md
- Performance-Issues.md
- Icon-Display-Issues.md
- Known-Issues.md
- Untested-Features.md
- Platform-Support.md

#### Development
- Building-from-Source.md
- Contributing.md
- Code-Structure.md
- Adding-Features.md
- Testing.md

#### Reference
- Command-Line-Reference.md
- Configuration-Reference.md
- Output-Formats.md
- API-Documentation.md
- Glossary.md

#### Tutorials
- Video-Installation.md
- Video-Basic-Export.md
- Video-AI-Features.md
- Video-Network-Graph.md
- Tutorial-Message-Analysis.md
- Tutorial-Reports.md
- Tutorial-Advanced-Filtering.md
- Tutorial-Automation.md

#### Project Info
- Version-History.md
- Planned-Features.md
- Feature-Requests.md
- Roadmap.md
- Getting-Help.md
- Reporting-Bugs.md
- Community.md
- About.md
- VibeCoded-Development.md
- License.md
- Credits.md
- Changelog.md

---

## ?? Quick Setup for GitHub Wiki

### Method 1: Manual Upload

1. **Enable Wiki** in repository settings
2. **Create pages** via GitHub UI
3. **Copy content** from each `.md` file
4. **Save** and repeat

### Method 2: Git Clone (Advanced)

```bash
# Clone wiki repository
git clone https://github.com/rhale78/SMSXmlToCsv.wiki.git

# Copy all wiki files
cp -r wiki/*.md SMSXmlToCsv.wiki/

# Commit and push
cd SMSXmlToCsv.wiki
git add .
git commit -m "Add complete wiki content"
git push
```

### Method 3: Wiki Import Tool

Use a wiki import tool to batch upload all pages at once. Various tools available on GitHub.

---

## ? Completed Pages

### Home.md
- ? Complete navigation hub
- ? Links to all major sections
- ? Popular pages highlighted
- ? Search tips included

### Quick-Start.md
- ? 5-minute setup guide
- ? Step-by-step installation
- ? First export walkthrough
- ? Common tasks examples
- ? Troubleshooting quick fixes

### FAQ.md
- ? 40+ common questions answered
- ? General, installation, features
- ? Usage, contact management
- ? AI/analytics, performance
- ? Issues/bugs, privacy, development

### Installing-Ollama.md
- ? Complete Ollama setup guide
- ? System requirements
- ? Installation steps
- ? Model selection guide
- ? Troubleshooting section
- ? Advanced configuration

---

## ?? Coverage Statistics

| Category | Pages Referenced | Pages Created | Completion |
|----------|------------------|---------------|------------|
| **Core** | 4 | 4 | 100% |
| **Getting Started** | 3 | 0 | 0% |
| **Features** | 8 | 0 | 0% |
| **Configuration** | 4 | 0 | 0% |
| **AI Setup** | 3 | 1 | 33% |
| **How-To** | 12 | 0 | 0% |
| **Troubleshooting** | 8 | 0 | 0% |
| **Development** | 5 | 0 | 0% |
| **Reference** | 5 | 0 | 0% |
| **Tutorials** | 8 | 0 | 0% |
| **Project Info** | 12 | 0 | 0% |
| **TOTAL** | **72** | **4** | **6%** |

---

## ?? Creating Additional Pages

### Priority 1 (Most Needed)
1. Installation-Guide.md
2. Export-Formats.md
3. Known-Issues.md
4. Installation-Problems.md
5. Getting-Help.md

### Priority 2 (Commonly Referenced)
1. MMS-Extraction.md
2. Contact-Management.md
3. AI-Features.md
4. Network-Visualization.md
5. Command-Line-Reference.md

### Priority 3 (Nice to Have)
- All tutorial pages
- Video guide placeholders
- Advanced topics
- Development guides

---

## ?? Page Template

Use this template for new pages:

```markdown
# [Page Title]

[Brief description of what this page covers]

---

## [Main Section 1]

Content...

### [Subsection]

Content...

---

## [Main Section 2]

Content...

---

## Related Pages

- [Link to related page 1](Page-Name-1)
- [Link to related page 2](Page-Name-2)

---

**Last Updated**: October 2025  
**Version**: 0.7  

**[? Back to Wiki Home](Home)**
```

---

## ?? External Documentation

The Wiki complements but doesn't replace:
- **README.md** - Project overview
- **docs/** folder - Complete technical documentation
- **CHANGELOG.md** - Version history
- **CONTRIBUTING.md** - Contribution guidelines

---

## ?? Support

**Questions about Wiki**:
- Create issue: [GitHub Issues](https://github.com/rhale78/SMSXmlToCsv/issues)
- Discuss: [GitHub Discussions](https://github.com/rhale78/SMSXmlToCsv/discussions)

**Contributing to Wiki**:
- See [Contributing.md](../CONTRIBUTING.md)
- Submit pull requests for new pages
- Suggest improvements via issues

---

## ? Future Plans

### v0.7.1
- [ ] Create Priority 1 pages
- [ ] Add screenshots to existing pages
- [ ] Create visual diagrams

### v0.8
- [ ] Complete all referenced pages
- [ ] Add video tutorial scripts
- [ ] Create interactive examples
- [ ] Translate to other languages (future)

---

**Wiki Status**: ?? In Progress  
**Core Pages**: ? 4/4 Complete  
**Total Pages**: ?? 4/72 (6%)  
**Version**: 0.7  
**Last Updated**: October 2025

---

**Made with ?? by AI | Documentation for ?? humans**
