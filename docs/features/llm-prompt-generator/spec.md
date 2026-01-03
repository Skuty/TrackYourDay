# Feature: LLM Prompt Generator for Time Logging

> **Note:** This spec covers the core prompt generation feature (read-only template usage). For template management UI (CRUD operations in Settings), see [template-management-extension.md](./template-management-extension.md).

## Problem Statement
Users need to generate prompts for external LLMs to analyze daily activities and suggest Jira time log entries. The system must provide prompt templates, inject activity data, and support copy-to-clipboard or file download workflows.

---

## User Stories

**Primary Persona:** Developer/Knowledge Worker who tracks time in Jira and uses external LLM services (ChatGPT, Claude, etc.)

- **US1:** As a user, I want to select a prompt template from 3+ predefined options so that I can tailor the LLM's analysis style to my needs
- **US2:** As a user, I want the system to automatically inject activity summary data into the prompt so that I don't manually format data
- **US3:** As a user, I want to copy the generated prompt to clipboard so that I can paste it into an external LLM interface
- **US4:** As a user, I want to download the prompt as a text file so that I can handle large outputs (>10KB) or save for records
- **US5:** As a user, I want the LLM to identify 3-9 distinct work activities per day so that time logging is granular but not overwhelming
- **US6:** As a user, I want the LLM to suggest Jira ticket keys when applicable so that I can directly log time to correct issues
- **US7:** As a user, I want the LLM to still generate summaries even when no Jira tickets match so that non-ticket work is still logged

---

## Acceptance Criteria

### AC1: UI Location and Navigation
**Given** the user is on the main application  
**When** they navigate to the Insights section  
**Then** they should see a new tab labeled "LLM Analysis" alongside existing Analytics tabs

### AC2: Prompt Template Selection
**Given** the user is on the LLM Analysis page  
**When** the page loads  
**Then** the system queries `ILlmPromptTemplateRepository.GetActiveTemplates()`  
**And** a dropdown displays all active templates ordered by `DisplayOrder`  
**And** at least 3 default templates exist: "Detailed Time Breakdown", "Concise Summary", "Task-Oriented Log"  
**And** no template is pre-selected (user must choose)

### AC3: Activity Data Injection - Default Strategy
**Given** the user has selected a prompt template  
**When** they click "Generate Prompt"  
**Then** the system injects activity data using **ActivityNameSummaryStrategy by default**  
**And** the injected data includes: Date, Description, Duration for each GroupedActivity  
**And** the data covers the currently selected date range (same date picker behavior as Analytics page)

### AC4: Prompt Output Structure
**Given** a prompt is generated  
**When** the system processes the selected template  
**Then** it retrieves `LlmPromptTemplate.SystemPrompt` from database  
**And** replaces `{ACTIVITY_DATA_PLACEHOLDER}` with serialized activity data (Markdown table format)  
**And** the final prompt contains:
- System instruction section defining output format (3-9 activities)
- Explicit instruction to identify Jira keys using regex pattern `[A-Z][A-Z0-9]+-\d+`
- Fallback instruction to create descriptions without Jira keys if none found
- Serialized activity data in Markdown table format:
  ```
  | Date       | Activity Description | Duration  |
  |------------|---------------------|-----------|
  | 2026-01-01 | Visual Studio Code  | 02:15:30  |
  ```

### AC5: Copy to Clipboard
**Given** a prompt is generated  
**When** the user clicks "Copy to Clipboard" button  
**Then** the entire prompt text is copied to system clipboard  
**And** a toast notification confirms "Prompt copied successfully"  
**And** the button remains enabled for re-copying

### AC6: Download as File
**Given** a prompt is generated  
**When** the user clicks "Download Prompt" button  
**Then** a `.txt` file downloads with filename pattern: `llm-prompt-{YYYY-MM-DD}.txt`  
**And** the file contains identical content to clipboard version  
**And** the file uses UTF-8 encoding

### AC7: Large Data Handling
**Given** the generated prompt exceeds 50KB in size  
**Then** the "Copy to Clipboard" button shows a warning tooltip: "Large prompt - consider downloading instead"  
**And** both copy and download remain functional

### AC8: Empty Activity Data
**Given** no activities exist for the selected date range  
**When** the user attempts to generate a prompt  
**Then** an error message displays: "No activities found for selected dates. Please choose a different date range."  
**And** no prompt is generated

### AC9: Date Range Validation
**Given** the user selects a date range spanning more than 1 day  
**When** they attempt to generate a prompt  
**Then** a warning displays: "LLM prompts work best for single-day analysis. Consider selecting one day at a time."  
**And** prompt generation still proceeds if user confirms

### AC10: Localization Support (Future-Proofing)
**Given** the application supports multiple languages  
**Then** all UI labels, button text, and error messages must use resource strings (not hardcoded English)  
**And** prompt templates themselves are stored in English (LLM consumption language)

---

## Out of Scope

**Explicitly NOT included in this feature:**

1. **No LLM API Integration** - The system does NOT call external LLM APIs. Users manually copy/paste to external services.
2. **No Response Parsing** - The system does NOT parse LLM responses or auto-populate Jira worklog entries.
3. **No Custom Prompt Editing** - Users cannot modify templates in-app (templates are hardcoded/config-based).
4. **No Multi-Day Batch Processing** - Each prompt generation targets a single day (multi-day requires multiple generations).
5. **No Strategy Switching UI** - The default ActivityNameSummaryStrategy is used; no dropdown to change strategies in this version.
6. **No Jira Worklog API Submission** - Users must manually log time in Jira after receiving LLM output.
7. **No Prompt History** - The system does not store previously generated prompts.

---

## Edge Cases & Risks

### Data Quality Risks
1. **What if ActivityNameSummaryStrategy groups too granularly?**  
   - Risk: 50+ grouped activities result in a 100KB prompt that LLMs truncate.  
   - Mitigation: Display character count and warn if >10,000 characters.

2. **What if activity descriptions contain PII or sensitive data?**  
   - Risk: User copies prompt containing confidential project names to public LLM.  
   - Mitigation: Display warning banner: "⚠️ Review prompt before sharing with external services. Ensure no confidential data is included."

3. **What if no Jira keys exist in activity data?**  
   - Risk: Prompt asks LLM to identify keys, but LLM hallucinates fake ticket IDs.  
   - Mitigation: Template explicitly states: "If no Jira key found in activity description, output 'N/A' for Jira Key field."

4. **What if multiple summary strategies produce conflicting descriptions?**  
   - Risk: User expects JiraEnrichedSummaryStrategy but gets ActivityNameSummaryStrategy.  
   - Mitigation: Display "Using Strategy: ActivityNameSummaryStrategy" label above prompt preview.

### Integration Risks
5. **What if clipboard API fails (browser permissions)?**  
   - Mitigation: Show fallback modal with selectable text: "Copy failed. Please select and copy manually."

6. **What if date range spans break periods?**  
   - Expected Behavior: Breaks are already excluded in GroupedActivity duration calculation (existing logic).

7. **What if user has no Jira integration configured?**  
   - Expected Behavior: Prompt still generates, but LLM receives no Jira keys in data. LLM output will omit keys.

### Usability Risks
8. **What if user doesn't understand "3-9 activities" constraint?**  
   - Mitigation: Add tooltip on template selection: "LLMs will group your activities into 3-9 time log entries for optimal Jira logging."

9. **What if prompt templates become stale as Jira worklog format changes?**  
   - Mitigation: Store templates in database for easy updates via SQL or future admin UI. Provide SQL update script in docs:
     ```sql
     UPDATE llm_prompt_templates 
     SET SystemPrompt = '[new template text]', UpdatedAt = datetime('now')
     WHERE TemplateKey = 'detailed';
     ```

10. **What if user manually edits database and breaks template syntax?**  
   - Risk: Malformed SQL injection or broken placeholder.  
   - Mitigation: Repository methods use parameterized queries (prevents SQL injection). Template validation on retrieval checks for placeholder existence.

---

## Data Requirements

### New Entities

#### **LlmPromptTemplate**
```csharp
public class LlmPromptTemplate
{
    public int Id { get; set; }
    public string TemplateKey { get; set; } = string.Empty;  // "detailed", "concise", "task-oriented"
    public string Name { get; set; } = string.Empty;          // "Detailed Time Breakdown"
    public string SystemPrompt { get; set; } = string.Empty;  // Full template text
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Validation Rules:**
- `TemplateKey`: Required, 3-50 chars, lowercase alphanumeric + hyphens only
- `Name`: Required, 5-100 chars
- `SystemPrompt`: Required, 100-10,000 chars, must contain `{ACTIVITY_DATA_PLACEHOLDER}` substring
- `DisplayOrder`: Positive integer, unique per active template

### Repository Pattern

#### **ILlmPromptTemplateRepository**
```csharp
public interface ILlmPromptTemplateRepository
{
    IReadOnlyList<LlmPromptTemplate> GetActiveTemplates();         // Ordered by DisplayOrder
    LlmPromptTemplate? GetByKey(string templateKey);
    void Save(LlmPromptTemplate template);                         // Insert or Update
    void Delete(string templateKey);                               // Soft delete (IsActive = 0)
    void SeedDefaultTemplates();                                   // Called on first run
}
```

#### **SqliteLlmPromptTemplateRepository : ILlmPromptTemplateRepository**
- Implements above interface
- Uses same database file: `%AppData%\TrackYourDay\TrackYourDayGeneric.db`
- Follows existing patterns from `SqliteGenericSettingsRepository`

### Existing Entities Used
- `GroupedActivity` - Read-only access to Description, Duration, Date
- `ISummaryStrategy` - Use ActivityNameSummaryStrategy by default
- Date range from Analytics page date pickers

### New Database Table: `llm_prompt_templates`

**Schema:**
```sql
CREATE TABLE IF NOT EXISTS llm_prompt_templates (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TemplateKey TEXT NOT NULL UNIQUE,  -- e.g., "detailed", "concise", "task-oriented"
    Name TEXT NOT NULL,                 -- Display name in UI dropdown
    SystemPrompt TEXT NOT NULL,         -- The actual prompt template with {ACTIVITY_DATA_PLACEHOLDER}
    IsActive INTEGER NOT NULL DEFAULT 1,-- Soft delete flag (1 = active, 0 = deleted)
    DisplayOrder INTEGER NOT NULL,      -- Sort order in UI (1, 2, 3...)
    CreatedAt TEXT NOT NULL,            -- ISO 8601 timestamp
    UpdatedAt TEXT NOT NULL             -- ISO 8601 timestamp
);

CREATE INDEX IF NOT EXISTS idx_llm_templates_active ON llm_prompt_templates(IsActive, DisplayOrder);
CREATE UNIQUE INDEX IF NOT EXISTS idx_llm_templates_key ON llm_prompt_templates(TemplateKey);
```

**Why Database Instead of Config File:**
1. **User Customization Future-Proofing:** Admin UI for template management (future v2 feature) requires CRUD operations
2. **Version Control:** UpdatedAt timestamp enables template version tracking
3. **Soft Deletes:** IsActive flag allows template deprecation without breaking existing workflows
4. **Consistency:** Aligns with existing TrackYourDay architecture (uses SQLite for settings, historical data)
5. **Deployment Independence:** Template updates don't require app redeployment

**Migration Strategy:**
- **First-Run Seeding:** On application startup, if table is empty, seed with 3 default templates
- **Idempotent Init:** Check `COUNT(*) FROM llm_prompt_templates` before seeding
- **No Upgrade Migration Needed:** New table, no schema changes to existing tables

---

## UI/UX Requirements

### Page Layout
- **Location:** New tab in Insights section: `Index.razor` → Add "LLM Analysis" tab
- **Components:**
  1. Date range picker (reuse from Analytics.razor)
  2. Prompt template dropdown (MudSelect)
  3. "Generate Prompt" button (MudButton - Primary)
  4. Prompt preview area (MudTextField multiline, read-only, min-height: 400px)
  5. Action buttons row:
     - "Copy to Clipboard" (MudButton - Secondary, icon: ContentCopy)
     - "Download Prompt" (MudButton - Secondary, icon: Download)
  6. Character count label: "Prompt size: {count} characters"
  7. Warning banner (conditional): "⚠️ Review for confidential data before sharing"

### Interaction Flow
1. User selects date range (defaults to today)
2. User selects prompt template from dropdown
3. User clicks "Generate Prompt"
4. System validates:
   - Date range not empty
   - Activities exist for range
   - Template selected
5. System generates prompt (loading spinner during generation)
6. Prompt displays in preview area
7. User clicks "Copy" or "Download"
8. Success toast appears

### Validation Rules
- **Date Range:** Must not be null. End date >= Start date.
- **Template Selection:** Must select a template (no default).
- **Activity Data:** Must have at least 1 GroupedActivity in range.

### Error States
| Condition | Error Message |
|-----------|---------------|
| No date selected | "Please select a date range." |
| No activities found | "No activities found for selected dates. Please choose a different date range." |
| No template selected | "Please select a prompt template." |
| Clipboard API denied | "Copy failed. Please select and copy manually." |

### Responsive Behavior
- Prompt preview area scrollable on small screens
- Buttons stack vertically on mobile (<600px width)

---

## Dependencies

### Existing Features Affected
1. **Analytics.razor** - Share date picker logic and ISummaryStrategy injection
2. **ActivitiesAnalyser.GetAvailableStrategies()** - Use ActivityNameSummaryStrategy explicitly
3. **GroupedActivity** - Read-only access to Duration, Description, Date properties
4. **GenericDataRepository pattern** - New `SqliteLlmPromptTemplateRepository` follows same pattern (same DB file, similar Init/CRUD methods)

### New Dependencies Introduced
- **Repository Pattern:** New `ILlmPromptTemplateRepository` interface + SQLite implementation
- **Database Schema:** New table `llm_prompt_templates` in shared `TrackYourDayGeneric.db`
- **Service Registration:** `LlmPromptTemplateRepository` must be registered in DI container

### External Service Assumptions
- **Clipboard API:** Requires HTTPS or localhost (Blazor limitation)
- **File Download:** Uses browser download API (no server-side storage)

### Technology Stack
- **MudBlazor Components:** MudSelect, MudButton, MudTextField, MudAlert
- **Blazor JSInterop:** For clipboard and file download operations
- **.NET 8.0 APIs:** System.Text.Json for data serialization

---

## Non-Functional Requirements

### Performance
- Prompt generation must complete in <2 seconds for 100 activities
- Clipboard copy must execute in <500ms
- File download must initiate in <1 second

### Security
- **No API Keys:** Feature does not handle LLM API credentials
- **Data Privacy Warning:** Visible banner warning users about external data sharing
- **No Server Transmission:** All prompt generation happens client-side

### Accessibility
- All buttons must have ARIA labels
- Prompt preview must be keyboard-navigable
- Error messages must be screen-reader compatible

### Localization
- UI labels stored in resource files (`.resx` or equivalent)
- Prompt templates remain in English (LLM consumption language)
- Support for future multi-language UI (e.g., Polish, German)

---

## Implementation Notes

### Prompt Template Examples

#### Template 1: Detailed Time Breakdown
```
You are a time tracking assistant. Analyze the following activity log and generate a detailed time log report.

REQUIREMENTS:
- Generate between 3 and 9 time log entries
- Group similar activities together
- Identify Jira ticket keys using pattern: [A-Z][A-Z0-9]+-\d+
- If no Jira key found, use "N/A" for Jira Key field
- Sum durations for grouped activities
- Each entry must include: Description, Duration (decimal hours), Jira Key

ACTIVITY DATA:
{ACTIVITY_DATA_PLACEHOLDER}

OUTPUT FORMAT:
| Description | Duration (hours) | Jira Key |
|-------------|------------------|----------|
| ... | ... | ... |

Generate the report now.
```

#### Template 2: Concise Summary
```
Summarize the following workday into 3-9 time log entries suitable for Jira worklog submission.

Rules:
1. Merge similar activities
2. Extract Jira keys (format: ABC-123) from descriptions
3. If no key found, write "No ticket" in Jira Key column
4. Convert durations to decimal hours (e.g., 1h 30m = 1.5)

Data:
{ACTIVITY_DATA_PLACEHOLDER}

Output as table with columns: Task, Hours, Jira Ticket
```

#### Template 3: Task-Oriented Log
```
Act as a project manager reviewing an engineer's workday. Group the activities below into distinct tasks (minimum 3, maximum 9).

For each task:
- Write a clear description
- Sum total time spent
- Identify associated Jira ticket (if mentioned in activity names)
- If no ticket, indicate "Administrative" or "Untracked"

Activities:
{ACTIVITY_DATA_PLACEHOLDER}

Format output as:
1. [Jira Key or "N/A"] - Description (X.X hours)
2. ...
```

### Activity Data Serialization Format

**Option A: Markdown Table**
```markdown
| Date       | Activity Description                      | Duration  |
|------------|-------------------------------------------|-----------|
| 2026-01-01 | Visual Studio Code - TrackYourDay project | 02:15:30  |
| 2026-01-01 | PROJ-456: Code Review                     | 01:30:00  |
```

**Option B: JSON**
```json
{
  "date": "2026-01-01",
  "activities": [
    {
      "description": "Visual Studio Code - TrackYourDay project",
      "duration": "02:15:30",
      "durationHours": 2.26
    }
  ]
}
```

**Recommendation:** Use Markdown table for better LLM readability.

---

## Open Questions

### Critical Decisions Needed

1. **Multi-Day Handling:**  
   - Should the system allow multi-day date ranges and generate separate prompts per day?  
   - OR enforce single-day selection with validation error?  
   - **Recommendation:** Warn but allow (AC9 addresses this).

2. **Strategy Selection:**  
   - Should users be able to switch from ActivityNameSummaryStrategy to JiraEnrichedSummaryStrategy?  
   - OR hardcode to ActivityNameSummaryStrategy for v1?  
   - **Recommendation:** Hardcode for v1. Add dropdown in v2 if users request it.

3. **Prompt Template Customization:**  
   - Should power users be able to edit templates or add custom ones?  
   - OR keep templates fixed in database with admin-only SQL access?  
   - **Recommendation:** Database storage enables future admin UI (v2). For v1, templates editable only via SQL. Document SQL update commands in README.

4. **LLM Response Import (Future):**  
   - Is there interest in parsing LLM responses back into the app for auto-logging?  
   - **Impact:** This would require response format standardization and Jira API integration (out of scope for v1).

5. **Break Time Handling:**  
   - Should prompts include a note about break time already being deducted from durations?  
   - **Recommendation:** Add to system prompt: "Note: Durations already exclude break periods."

6. **Clipboard Size Limits:**  
   - What's the maximum prompt size before clipboard API fails on Windows?  
   - **Research Needed:** Test clipboard with 100KB+ payloads on target Windows versions.

---

## Test Scenarios

### Manual Test Cases

1. **Happy Path - Single Day:**
   - Select today's date
   - Choose "Detailed Time Breakdown" template
   - Verify prompt includes all activities
   - Copy to clipboard → Paste in Notepad → Verify content matches preview

2. **Edge Case - No Activities:**
   - Select a future date with no tracked activities
   - Attempt generation → Verify error message appears

3. **Edge Case - Large Dataset:**
   - Select date with 100+ activities
   - Verify character count warning
   - Download file → Verify file size and UTF-8 encoding

4. **Jira Key Detection:**
   - Generate prompt for day with activities named "PROJ-123: Bug fix"
   - Verify prompt instructs LLM to extract PROJ-123
   - Manually paste into ChatGPT → Verify LLM identifies key

5. **Multi-Day Warning:**
   - Select 3-day date range
   - Verify warning dialog appears
   - Proceed → Verify all 3 days' data included

6. **Clipboard Failure Simulation:**
   - Disable clipboard permissions in browser
   - Attempt copy → Verify fallback modal appears

### Unit Test Coverage

- `PromptGenerator.GeneratePrompt(template, activities)` → Returns non-empty string
- `PromptGenerator.SerializeActivities(activities)` → Outputs Markdown table format
- `PromptTemplateService.GetTemplates()` → Returns exactly 3 templates
- `ClipboardService.CopyToClipboard(text)` → Throws exception handled gracefully
- `FileDownloadService.DownloadAsFile(content, filename)` → Triggers browser download

---

## Migration & Rollout

### Database Migration

#### **Migration Script: V1_AddLlmPromptTemplates.sql**
```sql
-- Create table
CREATE TABLE IF NOT EXISTS llm_prompt_templates (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TemplateKey TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    SystemPrompt TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    DisplayOrder INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_llm_templates_active ON llm_prompt_templates(IsActive, DisplayOrder);
CREATE UNIQUE INDEX IF NOT EXISTS idx_llm_templates_key ON llm_prompt_templates(TemplateKey);

-- Seed default templates
INSERT OR IGNORE INTO llm_prompt_templates (TemplateKey, Name, SystemPrompt, IsActive, DisplayOrder, CreatedAt, UpdatedAt)
VALUES 
('detailed', 'Detailed Time Breakdown', 
'You are a time tracking assistant. Analyze the following activity log and generate a detailed time log report.

REQUIREMENTS:
- Generate between 3 and 9 time log entries
- Group similar activities together
- Identify Jira ticket keys using pattern: [A-Z][A-Z0-9]+-\d+
- If no Jira key found, use "N/A" for Jira Key field
- Sum durations for grouped activities
- Each entry must include: Description, Duration (decimal hours), Jira Key
- Note: Durations already exclude break periods

ACTIVITY DATA:
{ACTIVITY_DATA_PLACEHOLDER}

OUTPUT FORMAT:
| Description | Duration (hours) | Jira Key |
|-------------|------------------|----------|
| ... | ... | ... |

Generate the report now.', 
1, 1, datetime('now'), datetime('now')),

('concise', 'Concise Summary',
'Summarize the following workday into 3-9 time log entries suitable for Jira worklog submission.

Rules:
1. Merge similar activities
2. Extract Jira keys (format: ABC-123) from descriptions
3. If no key found, write "No ticket" in Jira Key column
4. Convert durations to decimal hours (e.g., 1h 30m = 1.5)
5. Break time already excluded from durations

Data:
{ACTIVITY_DATA_PLACEHOLDER}

Output as table with columns: Task, Hours, Jira Ticket',
1, 2, datetime('now'), datetime('now')),

('task-oriented', 'Task-Oriented Log',
'Act as a project manager reviewing an engineer''s workday. Group the activities below into distinct tasks (minimum 3, maximum 9).

For each task:
- Write a clear description
- Sum total time spent (already excludes breaks)
- Identify associated Jira ticket (if mentioned in activity names)
- If no ticket, indicate "Administrative" or "Untracked"

Activities:
{ACTIVITY_DATA_PLACEHOLDER}

Format output as:
1. [Jira Key or "N/A"] - Description (X.X hours)
2. ...',
1, 3, datetime('now'), datetime('now'));
```

#### **Migration Execution Strategy**
1. **On Application Startup:**
   - `LlmPromptTemplateRepository` constructor calls `InitializeStructure()` (creates table if not exists)
   - Service layer calls `SeedDefaultTemplates()` (inserts defaults if table empty)
   - No manual migration scripts needed (follows existing GenericDataRepository pattern)

2. **Idempotency Check:**
   ```csharp
   public void SeedDefaultTemplates()
   {
       var existingCount = ExecuteScalar<int>("SELECT COUNT(*) FROM llm_prompt_templates WHERE IsActive = 1");
       if (existingCount == 0)
       {
           // Insert 3 default templates
       }
   }
   ```

3. **Rollback Plan:**
   - If critical bug: Set `IsActive = 0` for all templates via SQL
   - Feature disabled gracefully (UI shows "No templates available")
   - Drop table: `DROP TABLE IF EXISTS llm_prompt_templates;`

### Feature Flag (Optional)
- If using feature flags, gate behind `EnableLlmPromptGenerator` flag
- Default to `true` for all users

### User Communication
- Update README.md with new feature documentation
- Add "What's New" notification in app (if notification system exists)

---

## Future Enhancements

1. ~~**Custom Template Editor**~~ - ✅ **IMPLEMENTED** (see [template-management-extension.md](./template-management-extension.md))
2. **Multi-Strategy Support** - Dropdown to switch between summary strategies (v2)
3. **LLM Response Parser** - Import LLM output back into app for auto-logging (v3)
4. **Jira Worklog API Integration** - One-click time logging from LLM suggestions (v3)
5. **Prompt History** - Save last 10 generated prompts for quick access (v2)
6. **Batch Multi-Day Export** - Generate one prompt per day for a week (v2)
7. **Localized Prompt Templates** - Multi-language prompt templates for non-English LLMs (v4)

---

## Definition of Done

- [ ] Database table `llm_prompt_templates` created with migration script
- [ ] `LlmPromptTemplate` entity class implemented with validation
- [ ] `ILlmPromptTemplateRepository` interface and `SqliteLlmPromptTemplateRepository` implementation
- [ ] Repository methods: GetActiveTemplates, GetByKey, Save, Delete, SeedDefaultTemplates
- [ ] Seed logic runs on first app startup (idempotent)
- [ ] UI tab "LLM Analysis" visible in Insights section
- [ ] Dropdown loads templates from database (not hardcoded)
- [ ] 3 default templates seeded: "Detailed Time Breakdown", "Concise Summary", "Task-Oriented Log"
- [ ] Prompt generation replaces `{ACTIVITY_DATA_PLACEHOLDER}` with activity data
- [ ] Prompt generation uses ActivityNameSummaryStrategy by default
- [ ] Copy to clipboard functional on Windows 10+
- [ ] Download as `.txt` file functional
- [ ] Character count displays below preview
- [ ] Warning banner for data privacy visible
- [ ] Error handling for empty activity data
- [ ] Error handling for database failures (corrupt DB, missing templates)
- [ ] Unit tests for `LlmPromptTemplate` validation (80%+ coverage)
- [ ] Unit tests for `SqliteLlmPromptTemplateRepository` (all CRUD operations)
- [ ] Unit tests for PromptGenerator service (template substitution, activity serialization)
- [ ] Integration tests for database seeding and retrieval
- [ ] Manual testing completed for all test scenarios (including DB corruption)
- [ ] Documentation updated in README.md
- [ ] SQL migration script documented in `docs/features/llm-prompt-generator/migration.sql`
- [ ] Code review approved
- [ ] No breaking changes to existing Analytics page or database schema

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **Database table creation fails on startup** | Low | High | Try-catch on InitializeStructure(), log error, disable feature with error banner |
| **Template seed fails (permissions, disk full)** | Low | High | Check seed result, retry once, display error if still fails |
| **Database file corrupted (user edits manually)** | Low | Medium | Validate queries return expected data, fallback to hardcoded templates if DB fails |
| **Template missing {ACTIVITY_DATA_PLACEHOLDER}** | Low | Medium | Validate on retrieval, show error: "Template invalid", skip broken template |
| **Concurrent writes to DB (multi-instance app)** | Low | Medium | SQLite handles locking, but add retry logic with exponential backoff |
| LLMs hallucinate Jira keys not in data | High | Medium | Explicit prompt instruction: "Only use keys found in activity descriptions" |
| Users share confidential data to public LLMs | Medium | High | Visible warning banner on UI |
| Clipboard API fails due to browser restrictions | Low | Low | Fallback modal with manual copy |
| Prompt too large for LLM context window | Medium | Medium | Display character count + warning at 10K chars |
| Activity descriptions contain special chars breaking template | Low | Low | Escape/sanitize descriptions during serialization |
| Users expect LLM response auto-import | Medium | Low | Clearly label feature as "Generator" not "Assistant" |
| **UpdatedAt timestamp desync (multiple updates in same second)** | Low | Low | Use ISO 8601 with milliseconds: `datetime('now', 'subsec')` |

---

## Appendix: Domain Context

### Existing Architecture Levels

**System Level:**
- Low-level event tracking (mouse, keyboard, app focus)
- Outputs: `EndedActivity` entities

**Application Level:**
- Jira activity tracking via `JiraTracker`
- Teams meeting detection
- User tasks
- Outputs: `JiraActivity`, `EndedMeeting`, `UserTask` entities

**Insights Level:**
- Activity aggregation via `ISummaryStrategy` implementations
- Break detection and exclusion
- Workday analysis
- Outputs: `GroupedActivity` collections

**This Feature's Level:** **Insights Level** - Consumes `GroupedActivity` data for prompt generation.

### Key Domain Rules

1. **Break Periods Are Excluded:** `GroupedActivity.Duration` already subtracts break time (existing logic in `ActivitiesAnalyser`).
2. **Jira Key Matching:** `JiraEnrichedSummaryStrategy` uses regex `[A-Z][A-Z0-9]+-\d+` for key detection.
3. **Activity Grouping:** Different strategies produce different GroupedActivity counts (e.g., ActivityNameSummaryStrategy groups by description string equality).

### Constraints from Existing Code

- `GroupedActivity` is read-only (no setters) → Cannot modify descriptions post-grouping.
- `ISummaryStrategy.Generate()` returns `IReadOnlyCollection<GroupedActivity>` → Must work with collection as-is.
- Date range logic already exists in `Analytics.razor` → Reuse same date picker component.
- `ActivitiesAnalyser.GetAvailableStrategies()` provides all strategies → Explicitly choose `ActivityNameSummaryStrategy` for this feature.
