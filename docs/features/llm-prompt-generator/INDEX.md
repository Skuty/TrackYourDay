# LLM Prompt Generator - Complete Feature Documentation

## 📚 Documentation Index

This feature is fully specified across **6 documents** (112 KB total) covering all aspects from requirements to implementation.

---

## 🎯 Core Documents

### 1. **[README.md](./README.md)** - Start Here
**Purpose:** High-level overview and user guide  
**Audience:** Users, Stakeholders, New Developers  
**Contents:**
- Feature overview and capabilities
- User workflows (3 common scenarios)
- Data flow diagram
- Configuration guide (default templates)
- Troubleshooting (6 common issues)
- FAQ (8 questions)
- Performance benchmarks
- Known limitations
- Roadmap (v1.0 → v3.0)

**When to Read:** Before using the feature or onboarding new team members

---

### 2. **[spec.md](./spec.md)** - Core Requirements
**Purpose:** Detailed requirements for prompt generation feature  
**Audience:** Developers, QA Engineers, Product Owners  
**Contents:**
- Problem statement and user stories (7 stories)
- Acceptance criteria (AC1-AC10)
- Out of scope (7 exclusions)
- Edge cases and risks (10 scenarios)
- Data requirements (LlmPromptTemplate entity, repository interface)
- UI/UX requirements (page layout, validation rules)
- Dependencies (existing features affected)
- Test scenarios (7 manual tests + unit test coverage)
- Migration strategy (database seeding)

**When to Read:** Before implementing core prompt generation logic

---

### 3. **[template-management-extension.md](./template-management-extension.md)** - Settings UI
**Purpose:** Detailed requirements for template CRUD in Settings page  
**Audience:** Developers (UI/UX focus), QA Engineers  
**Contents:**
- Extended user stories (US8-US14)
- Acceptance criteria (AC11-AC25)
- Extended out of scope (9 exclusions)
- Extended edge cases (20+ scenarios)
- Repository interface extensions (8 new methods)
- Validation rules (comprehensive table)
- UI mockups (Settings tab layout)
- Create/Edit dialog specifications
- Preview modal specifications
- Reorder mode behavior
- Test scenarios (15 manual tests + unit tests)

**When to Read:** Before implementing Settings tab and template management UI

---

### 4. **[migration.sql](./migration.sql)** - Database Schema
**Purpose:** SQLite database migration script  
**Audience:** Developers, DBAs, DevOps  
**Contents:**
- Table creation DDL (`llm_prompt_templates`)
- Index creation (performance optimization)
- Seed data (3 default templates)
- Verification queries
- Rollback commands
- Admin maintenance commands (UPDATE, soft delete, restore)

**When to Read:** Before implementing repository or troubleshooting database issues

---

### 5. **[IMPLEMENTATION.md](./IMPLEMENTATION.md)** - Developer Checklist
**Purpose:** Step-by-step implementation guide  
**Audience:** Developers (hands-on implementation)  
**Contents:**
- Phase-by-phase implementation plan (6 phases)
- Acceptance tests per phase
- Quality gates (before code review, before merge)
- Known issues and workarounds (3 issues)
- Success metrics (4 KPIs)
- Deployment checklist (pre/during/post deployment)
- Rollback plan

**When to Read:** During active development (track progress)

---

### 6. **[ADR.md](./ADR.md)** - Architecture Decisions
**Purpose:** Record of all critical design decisions  
**Audience:** Architects, Tech Leads, Future Maintainers  
**Contents:**
- 10 accepted decisions (with rationale):
  - ADR-001: Database storage vs config files
  - ADR-002: No LLM API integration
  - ADR-003: ActivityNameSummaryStrategy hardcoded
  - ADR-004: Soft deletes for templates
  - ADR-005: Arrow buttons vs drag-and-drop
  - ADR-006: Hardcoded sample data in preview
  - ADR-007: Character count warnings
  - ADR-008: No template export/import
  - ADR-009: Prevent deleting last template
  - ADR-010: Optimistic concurrency control
- 3 open decisions (to be resolved)

**When to Read:** When questioning "Why was this designed this way?" or before proposing major changes

---

## 🗂️ Quick Reference Matrix

| Need | Document | Section |
|------|----------|---------|
| Understand feature purpose | README.md | Overview |
| See user workflows | README.md | User Workflows |
| Get acceptance criteria | spec.md | Acceptance Criteria (AC1-AC10) |
| | template-management-extension.md | Acceptance Criteria (AC11-AC25) |
| Implement repository | spec.md | Data Requirements |
| | template-management-extension.md | Repository Interface (Extended) |
| Create database table | migration.sql | Table Creation DDL |
| Build UI (LLM Analysis page) | spec.md | UI/UX Requirements |
| Build UI (Settings tab) | template-management-extension.md | UI/UX Requirements |
| Write tests | spec.md | Test Scenarios |
| | template-management-extension.md | Test Scenarios (Extended) |
| Understand design choices | ADR.md | All ADRs |
| Track implementation progress | IMPLEMENTATION.md | Phase-by-Phase Checklist |
| Troubleshoot issues | README.md | Troubleshooting |
| Deploy to production | IMPLEMENTATION.md | Deployment Checklist |

---

## 📊 Feature Summary

### What It Does
Generates prompts from daily activity data for external LLM analysis. Users copy prompts to ChatGPT/Claude, receive time log suggestions, and manually log to Jira.

### Key Capabilities
✅ **Prompt Generation:**
- Date range selection (single day recommended)
- Template selection (3 defaults: Detailed, Concise, Task-Oriented)
- Copy to clipboard or download as `.txt` file
- Character count warnings for large prompts
- Data privacy warnings

✅ **Template Management (Settings):**
- Create custom templates
- Edit existing templates (including defaults)
- Soft delete/restore templates
- Reorder templates (affects dropdown order)
- Duplicate templates
- Preview with sample data
- Restore default templates

### Architecture
- **Database:** `llm_prompt_templates` table in `TrackYourDayGeneric.db`
- **Strategy:** ActivityNameSummaryStrategy (hardcoded for v1)
- **UI:** 2 pages (Insights → LLM Analysis + Settings → Templates)
- **No External APIs:** All processing client-side, manual LLM paste

### Key Constraints
❌ No LLM API integration (manual copy/paste)  
❌ No response parsing (manual Jira logging)  
❌ No multi-strategy selection (ActivityNameSummaryStrategy only)  
❌ No template export/import (manual SQL workaround)  
❌ No prompt history (cannot retrieve past prompts)  

---

## 🚀 Getting Started

### For Users
1. Read [README.md](./README.md) → Overview + User Workflows
2. Navigate to Insights → LLM Analysis
3. Generate prompt, copy to ChatGPT, review output
4. (Optional) Customize templates in Settings → LLM Prompt Templates

### For Developers
1. Read [spec.md](./spec.md) → Core requirements
2. Read [template-management-extension.md](./template-management-extension.md) → Settings UI requirements
3. Review [ADR.md](./ADR.md) → Understand design decisions
4. Follow [IMPLEMENTATION.md](./IMPLEMENTATION.md) → Phase-by-phase checklist
5. Execute [migration.sql](./migration.sql) → Create database table
6. Write code → Run tests → Deploy

### For QA Engineers
1. Read [spec.md](./spec.md) → AC1-AC10 (core feature)
2. Read [template-management-extension.md](./template-management-extension.md) → AC11-AC25 (Settings UI)
3. Execute test scenarios from both documents (40 total test cases)
4. Verify edge cases from both documents (30+ scenarios)

### For Product Owners / Stakeholders
1. Read [README.md](./README.md) → Feature overview + roadmap
2. Read [spec.md](./spec.md) → Problem statement + user stories
3. Review [ADR.md](./ADR.md) → Understand scope constraints (what's NOT included)

---

## 📈 Success Criteria

### User Adoption (1 month post-launch)
- **Target:** 50% of active users try feature
- **Measure:** Count distinct users who click "Generate Prompt"

### Feature Usage
- **Target:** 10+ prompts generated per week (per user average)
- **Measure:** Log events on "Copy to Clipboard" / "Download"

### Template Customization
- **Target:** 20% of users create custom templates
- **Measure:** Count templates where TemplateKey NOT IN defaults

### Error Rate
- **Target:** <5% of prompt generations fail
- **Measure:** Exception count / total generation attempts

---

## 🔄 Version History

### v1.0 (Current)
- Core prompt generation (AC1-AC10)
- 3 default templates (database-stored)
- Copy to clipboard + download
- Template management UI (AC11-AC25)
- Settings tab with CRUD operations

### v1.5 (Planned)
- Multi-strategy support (dropdown)
- Prompt history (last 10 prompts)
- Template testing with real data

### v2.0 (Future)
- Optional LLM API integration
- Response parser + Jira auto-logging
- Template export/import (JSON)

### v3.0 (Exploratory)
- Direct Jira worklog submission
- AI-assisted template suggestions
- Multi-language templates

---

## 🐛 Known Issues

1. **Clipboard API requires HTTPS**  
   Workaround: Use "Download Prompt" button

2. **SQLite write conflicts (multi-instance)**  
   Workaround: Close other app instances before editing templates

3. **Large prompts (>100KB) lag UI**  
   Workaround: Limit date range to single day

---

## 📞 Support

**Bug Reports:** [GitHub Issues](https://github.com/skuty/TrackYourDay/issues)  
**Feature Requests:** [GitHub Issues](https://github.com/skuty/TrackYourDay/issues) (tag: `enhancement`)  
**Documentation Questions:** Check [README.md](./README.md) FAQ section first

**Developer Contact:** [Your Team/Email]

---

## 📄 License
Same as TrackYourDay main project (check root LICENSE.md)

---

## 🎓 Learning Path

**I want to USE the feature:**
```
README.md → User Workflows → Troubleshooting
```

**I want to UNDERSTAND the requirements:**
```
spec.md → template-management-extension.md → ADR.md
```

**I want to IMPLEMENT the feature:**
```
IMPLEMENTATION.md → spec.md → migration.sql → Code
```

**I want to TEST the feature:**
```
spec.md (Test Scenarios) → template-management-extension.md (Test Scenarios) → Manual Tests
```

**I want to DEPLOY the feature:**
```
IMPLEMENTATION.md (Deployment Checklist) → migration.sql → Monitor Logs
```

---

**Total Documentation:** 6 files, 112 KB, ~15,000 words, 40+ test scenarios, 25 acceptance criteria

**Last Updated:** 2026-01-01  
**Maintained By:** Systems Analyst Team
