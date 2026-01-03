# LLM Prompt Generator - Feature Documentation

## Overview
The LLM Prompt Generator enables users to export daily activity data as formatted prompts for external Large Language Models (ChatGPT, Claude, etc.). LLMs analyze the data and suggest time log entries suitable for Jira worklog submission.

## Feature Scope

### Core Feature (v1)
📄 **Document:** [spec.md](./spec.md)

**Capabilities:**
- Generate prompts from activity data (ActivityNameSummaryStrategy by default)
- Copy prompts to clipboard
- Download prompts as `.txt` files
- Select from 3 default prompt templates (stored in database)
- Date range selection (single day recommended)
- Character count warnings for large prompts
- Data privacy warnings (external LLM sharing)

**UI Location:** Insights Section → "LLM Analysis" tab

**Database:** Uses `llm_prompt_templates` table in `TrackYourDayGeneric.db`

### Template Management Extension (v1.5)
📄 **Document:** [template-management-extension.md](./template-management-extension.md)

**Capabilities:**
- Create custom prompt templates via Settings UI
- Edit existing templates (including defaults)
- Soft delete/restore templates
- Reorder templates (changes dropdown order)
- Duplicate templates for variations
- Preview templates with sample activity data
- Restore default templates if deleted

**UI Location:** Settings → "LLM Prompt Templates" tab

**No Additional Database Changes:** Reuses existing `llm_prompt_templates` table

---

## Architecture

### Data Flow
```
┌──────────────────┐
│ Activity Tracker │──┐
│ Meeting Tracker  │  ├─→ TrackedActivity entities
│ User Tasks       │──┘
└──────────────────┘
         │
         ↓
┌─────────────────────────┐
│ ActivityNameSummary     │ ← Default strategy
│ Strategy                │
└─────────────────────────┘
         │
         ↓
┌─────────────────────────┐
│ GroupedActivity[]       │ ← Date, Description, Duration
└─────────────────────────┘
         │
         ↓
┌─────────────────────────┐
│ LlmPromptTemplate       │ ← Retrieved from database
│ (from DB)               │
└─────────────────────────┘
         │
         ↓
┌─────────────────────────┐
│ PromptGenerator Service │ ← Replaces {ACTIVITY_DATA_PLACEHOLDER}
└─────────────────────────┘
         │
         ↓
┌─────────────────────────┐
│ Rendered Prompt (text)  │ ← User copies or downloads
└─────────────────────────┘
         │
         ↓ (manual paste)
┌─────────────────────────┐
│ External LLM Service    │ ← ChatGPT, Claude, etc.
│ (ChatGPT, Claude, etc.) │
└─────────────────────────┘
         │
         ↓ (LLM response)
┌─────────────────────────┐
│ User manually logs time │ ← NOT automated in v1
│ to Jira                 │
└─────────────────────────┘
```

### Database Schema
```sql
-- Table: llm_prompt_templates
-- Location: %AppData%\TrackYourDay\TrackYourDayGeneric.db

CREATE TABLE llm_prompt_templates (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TemplateKey TEXT NOT NULL UNIQUE,      -- e.g., "detailed"
    Name TEXT NOT NULL,                     -- e.g., "Detailed Time Breakdown"
    SystemPrompt TEXT NOT NULL,             -- Full prompt with placeholder
    IsActive INTEGER NOT NULL DEFAULT 1,    -- 1 = active, 0 = soft deleted
    DisplayOrder INTEGER NOT NULL,          -- Sort order in dropdown
    CreatedAt TEXT NOT NULL,                -- ISO 8601 timestamp
    UpdatedAt TEXT NOT NULL                 -- ISO 8601 timestamp
);
```

### Key Components

**Core Services:**
- `ILlmPromptTemplateRepository` - Database access for templates
- `PromptGeneratorService` - Renders prompts by replacing placeholder with activity data
- `ActivityDataSerializer` - Converts `GroupedActivity[]` to Markdown table format

**UI Components:**
- `LlmAnalysis.razor` - Main page in Insights section (prompt generation)
- `TemplateManagementTab.razor` - Settings tab (template CRUD)
- `TemplateEditorDialog.razor` - Create/Edit modal
- `TemplatePreviewModal.razor` - Preview with sample data

---

## User Workflows

### Workflow 1: Generate Prompt for Today's Work
1. Navigate to **Insights → LLM Analysis**
2. Select today's date (default)
3. Choose "Detailed Time Breakdown" template from dropdown
4. Click **Generate Prompt**
5. Review prompt in preview area (shows all activities + Jira keys)
6. Click **Copy to Clipboard**
7. Paste into ChatGPT/Claude
8. LLM outputs time log suggestions with Jira keys
9. Manually log time in Jira based on LLM output

**Time Saved:** 15-20 minutes of manual time tracking

### Workflow 2: Create Custom Template
1. Navigate to **Settings → LLM Prompt Templates**
2. Click **Create New Template**
3. Fill fields:
   - TemplateKey: `my-custom-template`
   - Name: `My Custom Time Log Format`
   - SystemPrompt: (write custom prompt with `{ACTIVITY_DATA_PLACEHOLDER}`)
   - DisplayOrder: 4
4. Click **Preview with Sample Data** to test
5. Save template
6. Return to **Insights → LLM Analysis**
7. See new template in dropdown

### Workflow 3: Edit Default Template
1. Navigate to **Settings → LLM Prompt Templates**
2. Click **Edit** on "Detailed Time Breakdown" row
3. Modify SystemPrompt (e.g., change output format from table to numbered list)
4. Save changes
5. Return to **Insights → LLM Analysis**
6. Generate prompt → Verify new format

---

## Configuration

### Default Templates (Seeded on First Run)

**Template 1: Detailed Time Breakdown** (`detailed`)
- Outputs Markdown table: `| Description | Duration (hours) | Jira Key |`
- Explicit instructions for 3-9 entries
- Includes note about break time already excluded

**Template 2: Concise Summary** (`concise`)
- Shorter prompt, focuses on merging similar activities
- Uses simpler language for faster LLM processing

**Template 3: Task-Oriented Log** (`task-oriented`)
- Outputs numbered list instead of table
- Emphasizes task grouping over raw activities
- Uses "Administrative" / "Untracked" for non-Jira work

### Customization Options

**Via Settings UI (Recommended):**
- Create new templates
- Edit existing templates
- Reorder templates (affects dropdown order)
- Deactivate unused templates

**Via SQL (Advanced Users):**
```sql
-- Add new template
INSERT INTO llm_prompt_templates (TemplateKey, Name, SystemPrompt, IsActive, DisplayOrder, CreatedAt, UpdatedAt)
VALUES ('custom', 'My Custom Template', '[prompt text with {ACTIVITY_DATA_PLACEHOLDER}]', 1, 4, datetime('now'), datetime('now'));

-- Update existing template
UPDATE llm_prompt_templates 
SET SystemPrompt = '[updated prompt]', UpdatedAt = datetime('now')
WHERE TemplateKey = 'detailed';

-- Soft delete template
UPDATE llm_prompt_templates SET IsActive = 0, UpdatedAt = datetime('now') WHERE TemplateKey = 'concise';

-- Restore template
UPDATE llm_prompt_templates SET IsActive = 1, UpdatedAt = datetime('now') WHERE TemplateKey = 'concise';
```

---

## Validation Rules

### Template Validation
| Field | Rule | Error Message |
|-------|------|---------------|
| TemplateKey | 3-50 chars, `^[a-z0-9-]+$` | "Template key must be lowercase alphanumeric or hyphens." |
| TemplateKey | Unique (case-insensitive) | "Template key already exists." |
| Name | 5-100 chars | "Name must be 5-100 characters." |
| SystemPrompt | 100-10,000 chars | "Prompt must be 100-10,000 characters." |
| SystemPrompt | Contains `{ACTIVITY_DATA_PLACEHOLDER}` | "Prompt must contain placeholder for data injection." |
| DisplayOrder | Positive integer | "Display order must be positive." |

### Date Range Validation
- Start date ≤ End date
- Warning if date range > 1 day (multi-day prompts get very large)

### Activity Data Validation
- Error if no activities exist for selected date range
- Warning if >100 activities (prompt size may exceed LLM limits)

---

## Troubleshooting

### Issue: "No activities found for selected dates"
**Cause:** Selected date range has no tracked activities  
**Solution:** Choose a different date or verify activity tracking is running

### Issue: "Template database corrupted"
**Cause:** SQLite file damaged or manually edited incorrectly  
**Solution:**
1. Backup `%AppData%\TrackYourDay\TrackYourDayGeneric.db`
2. Delete corrupted file
3. Restart app → Database recreates with default templates

### Issue: "Copy to clipboard failed"
**Cause:** Browser clipboard permissions denied or HTTPS requirement  
**Solution:**
1. Use "Download Prompt" button instead
2. OR enable clipboard permissions in browser settings
3. OR ensure app runs on localhost/HTTPS

### Issue: Prompt too large for LLM (context limit exceeded)
**Cause:** Selected date range too long or too many activities  
**Solution:**
1. Reduce date range to single day
2. Use ActivityNameSummaryStrategy (groups activities more aggressively)
3. Edit template to request fewer entries (e.g., 3-5 instead of 3-9)

### Issue: LLM output doesn't match expected format
**Cause:** Template instructions unclear or LLM misinterprets  
**Solution:**
1. Edit template to be more explicit (e.g., "Output EXACTLY as table, no additional text")
2. Add examples to prompt (few-shot learning)
3. Test with different LLM model (GPT-4 vs Claude vs Gemini)

### Issue: Template dropdown empty in LLM Analysis page
**Cause:** All templates soft-deleted or database error  
**Solution:**
1. Go to Settings → LLM Prompt Templates
2. Click "Restore Defaults" button
3. OR restore individual templates (click "Restore" on inactive templates)

---

## Security Considerations

### Data Privacy
⚠️ **WARNING:** Prompts may contain confidential project information (Jira keys, activity descriptions). 

**Recommendations:**
1. Review prompt before copying to external LLM
2. Use private LLM instances (Azure OpenAI, AWS Bedrock) for sensitive data
3. Redact confidential project names before submission
4. Do NOT include API keys or credentials in templates

### Input Validation
✅ **Implemented Protections:**
- SQL injection prevented via parameterized queries
- XSS prevented via Blazor auto-escaping
- `<script>` tags rejected in template names/prompts
- Special characters sanitized before rendering

### Database Security
- Database file stored in `%AppData%\TrackYourDay` (user-specific)
- No encryption at rest (user responsible for OS-level encryption)
- No network access (local SQLite file only)

---

## Performance Benchmarks

**Target Metrics (Windows 10, 8GB RAM, SSD):**
- Template load from DB: <100ms
- Prompt generation (50 activities): <500ms
- Copy to clipboard: <200ms
- File download: <1s
- Settings page load (10 templates): <500ms
- Preview generation: <300ms

**Stress Test Results:**
- 100 activities: Prompt generation <1s
- 500 activities: Prompt generation ~3s (⚠️ prompt size ~50KB)
- 1000 activities: Prompt generation ~6s (⚠️ prompt size ~100KB, may exceed LLM limits)

---

## Known Limitations

### v1 Scope Constraints
- ❌ **No LLM API Integration** - User must manually copy/paste to external LLM
- ❌ **No Response Parsing** - LLM output must be manually entered into Jira
- ❌ **No Multi-Strategy Selection** - ActivityNameSummaryStrategy hardcoded
- ❌ **No Prompt History** - Cannot retrieve previously generated prompts
- ❌ **No Template Export/Import** - Cannot share templates between users

### Technical Constraints
- **Windows-Only** - Uses Windows-specific paths (`%AppData%`)
- **SQLite Concurrency** - Multiple app instances may conflict on DB writes
- **Clipboard Size** - Very large prompts (>1MB) may fail to copy
- **LLM Context Limits** - Prompts >10K characters may exceed model limits

---

## Roadmap

### v1.0 (Current)
✅ Core prompt generation  
✅ 3 default templates (DB-stored)  
✅ Copy to clipboard / Download file  
✅ Template management UI (Settings)  

### v1.5 (Planned)
- [ ] Multi-strategy support (dropdown to switch between ActivityName, JiraEnriched, etc.)
- [ ] Prompt history (last 10 generated prompts)
- [ ] Template testing with real activity data (not just sample)

### v2.0 (Future)
- [ ] LLM API integration (optional, for users with API keys)
- [ ] Response parser (import LLM output → auto-populate Jira worklog form)
- [ ] Template export/import (JSON format)
- [ ] Batch multi-day export

### v3.0 (Exploratory)
- [ ] Direct Jira worklog submission (one-click logging)
- [ ] ML-based template suggestions (analyze user's workflow, recommend templates)
- [ ] Multi-language templates (localized prompts for non-English LLMs)

---

## FAQ

**Q: Can I use this with Copilot/GitHub Models/Google Gemini?**  
A: Yes! The prompts are LLM-agnostic. Copy the prompt and paste into any LLM interface.

**Q: Does this automatically log time to Jira?**  
A: No. v1 only generates prompts. You must manually review LLM output and log time in Jira.

**Q: Can I edit the default templates?**  
A: Yes! Go to Settings → LLM Prompt Templates → Edit button. Changes affect all future prompts.

**Q: What if I delete all templates by accident?**  
A: Click "Restore Defaults" button in Settings to re-add the original 3 templates.

**Q: Can I share my custom templates with colleagues?**  
A: Not in v1. Workaround: Copy the SystemPrompt text and send via email. They can recreate the template manually.

**Q: Why does the prompt include break time data?**  
A: It doesn't! `GroupedActivity.Duration` already excludes breaks. The prompt explicitly states: "Durations already exclude break periods."

**Q: Can I use this for GitLab time tracking instead of Jira?**  
A: Yes! Edit the template to reference "GitLab merge requests" instead of "Jira keys." The LLM will adapt.

**Q: Is my data sent to OpenAI/Anthropic?**  
A: Only if YOU manually paste the prompt. The app itself does NOT call any external APIs.

---

## Support & Contributions

**Bug Reports:** [GitHub Issues](https://github.com/skuty/TrackYourDay/issues)  
**Feature Requests:** [GitHub Issues](https://github.com/skuty/TrackYourDay/issues) (tag: `enhancement`)  
**Documentation:** [docs/features/llm-prompt-generator/](.)

**Contribution Guidelines:**
1. Read [spec.md](./spec.md) and [template-management-extension.md](./template-management-extension.md)
2. All database changes must use migrations in [migration.sql](./migration.sql)
3. All UI text must use resource strings (no hardcoded English)
4. All new features require unit tests (80%+ coverage)

---

## License
Same as TrackYourDay main project (check root LICENSE.md)
