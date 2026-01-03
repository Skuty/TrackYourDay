# LLM Prompt Generator - Architecture Decision Record (ADR)

## Decision Log
All critical architectural and design decisions for the LLM Prompt Generator feature.

---

## ADR-001: Database Storage vs Config Files for Templates
**Date:** 2026-01-01  
**Status:** ✅ Accepted  
**Decision Owner:** Systems Analyst

### Context
Templates could be stored in:
1. `appsettings.json` / config files
2. SQLite database (`llm_prompt_templates` table)
3. Embedded resources (compiled into DLL)

### Decision
**Store templates in SQLite database.**

### Rationale
**Pros:**
- Enables future template management UI (CRUD operations)
- Supports soft deletes (IsActive flag)
- Version tracking via UpdatedAt timestamps
- Aligns with existing TrackYourDay architecture (uses SQLite for settings, historical data)
- No app redeployment needed for template updates
- Supports user customization without code changes

**Cons:**
- Slightly more complex than config files
- Requires database migration logic
- Database corruption risk (mitigated by auto-repair logic)

**Alternatives Rejected:**
- Config files: Would require app restart for template changes, no soft deletes
- Embedded resources: No customization possible, breaks user workflow flexibility

### Consequences
- Must implement `ILlmPromptTemplateRepository` with full CRUD
- Must handle database initialization on first run
- Must implement seeding logic (idempotent, INSERT OR IGNORE)
- Settings UI can now provide template management (v1.5 feature)

---

## ADR-002: No LLM API Integration in v1
**Date:** 2026-01-01  
**Status:** ✅ Accepted  
**Decision Owner:** Product Owner

### Context
Feature could:
1. Generate prompts only (manual copy/paste to LLM)
2. Directly call OpenAI/Anthropic APIs and return results
3. Provide optional API integration (user supplies API key)

### Decision
**v1 generates prompts only. No API integration.**

### Rationale
**Pros:**
- Simpler implementation (no HTTP clients, API key management, rate limiting)
- No cost implications (user pays LLM provider directly)
- Works with any LLM (ChatGPT, Claude, Gemini, local models)
- No privacy concerns (data never leaves user's machine)
- No API versioning/deprecation risks

**Cons:**
- Manual copy/paste workflow (slightly less convenient)
- No auto-parsing of LLM responses
- Cannot provide one-click Jira logging

**Alternatives Rejected:**
- Direct API integration: Adds complexity, cost, security risks (API key storage)
- Optional integration: Scope creep, doubles testing effort

### Consequences
- Feature labeled "Prompt Generator" not "LLM Assistant"
- Future v2 can add optional API integration without breaking v1
- Users must manually review LLM output before logging to Jira

---

## ADR-003: ActivityNameSummaryStrategy Hardcoded (No Dropdown)
**Date:** 2026-01-01  
**Status:** ✅ Accepted  
**Decision Owner:** Systems Analyst

### Context
LLM Analysis page could:
1. Hardcode ActivityNameSummaryStrategy
2. Provide dropdown to select strategy (ActivityName, JiraEnriched, TimeBasedSummary, etc.)
3. Default to ActivityNameSummaryStrategy with optional override

### Decision
**Hardcode ActivityNameSummaryStrategy for v1.**

### Rationale
**Pros:**
- Simplifies UI (one less dropdown)
- ActivityNameSummaryStrategy is most intuitive (groups by activity description)
- Reduces user confusion (no need to explain strategy differences)
- Faster implementation

**Cons:**
- Power users cannot use JiraEnrichedSummaryStrategy (better Jira key detection)
- Less flexible for edge cases

**Alternatives Rejected:**
- Strategy dropdown: Adds UI complexity, requires user education on strategy differences

### Consequences
- v2 can add strategy dropdown if users request it
- Documentation must clarify: "Uses ActivityNameSummaryStrategy by default"
- Users with complex Jira workflows may need workarounds (edit activity descriptions to include keys)

---

## ADR-004: Soft Deletes for Templates (IsActive Flag)
**Date:** 2026-01-01  
**Status:** ✅ Accepted  
**Decision Owner:** Systems Analyst

### Context
Template deletion could:
1. Hard delete (DELETE FROM table WHERE TemplateKey = ?)
2. Soft delete (UPDATE table SET IsActive = 0 WHERE TemplateKey = ?)
3. Archive to separate table

### Decision
**Use soft deletes with `IsActive` flag.**

### Rationale
**Pros:**
- Reversible (users can restore accidentally deleted templates)
- Preserves audit trail (UpdatedAt timestamp shows when deleted)
- Enables future version history feature (track template changes over time)
- Prevents data loss during user error

**Cons:**
- Table grows over time (inactive templates never purged)
- Queries must filter WHERE IsActive = 1

**Alternatives Rejected:**
- Hard delete: Irreversible, no recovery from user error
- Archive table: Adds complexity, requires migrations for table structure changes

### Consequences
- All repository queries must filter `WHERE IsActive = 1` for active templates
- Settings UI shows both active and inactive templates (grayed out)
- Must prevent deletion of last active template (AC18)

---

## ADR-005: Reorder Mode with Arrow Buttons (Not Drag-and-Drop)
**Date:** 2026-01-01  
**Status:** ✅ Accepted  
**Decision Owner:** UX Designer

### Context
Template reordering UI could:
1. Drag-and-drop rows (complex, no mobile support)
2. Arrow buttons (↑ ↓) per row (simple, mobile-friendly)
3. Numeric input for DisplayOrder (manual, error-prone)

### Decision
**Use arrow buttons for v1. Defer drag-and-drop to v2.**

### Rationale
**Pros:**
- Mobile-friendly (touch devices don't support drag-and-drop well)
- Simpler implementation (no JavaScript drag events)
- Keyboard-accessible (Up/Down arrow keys)
- Familiar UX pattern (similar to list reordering in many apps)

**Cons:**
- Less intuitive than drag-and-drop for desktop users
- Reordering multiple items requires multiple clicks

**Alternatives Rejected:**
- Drag-and-drop: Complex, breaks on mobile, requires custom JavaScript
- Numeric input: Error-prone (user might enter duplicate DisplayOrder)

### Consequences
- Reorder Mode toggle enables arrow buttons beside each row
- BulkUpdateDisplayOrder() repository method handles re-sequencing
- v2 can add drag-and-drop as enhancement without breaking existing arrow buttons

---

## ADR-006: Preview Uses Hardcoded Sample Data (Not Real Activities)
**Date:** 2026-01-01  
**Status:** ✅ Accepted  
**Decision Owner:** Product Owner

### Context
Template preview could:
1. Use hardcoded sample data (5 activities with varied descriptions)
2. Fetch real activity data from database (requires date picker)
3. Offer both options (toggle between sample and real)

### Decision
**Use hardcoded sample data for v1.**

### Rationale
**Pros:**
- Faster preview generation (<100ms, no DB query)
- Works even if user has no activities (new installation)
- Consistent preview experience (same data every time)
- Simpler implementation (no date range validation)

**Cons:**
- Doesn't test template against user's actual activity patterns
- May not reveal edge cases (e.g., very long activity descriptions)

**Alternatives Rejected:**
- Real data: Requires date picker, DB query, loading spinner (scope creep)
- Both options: Doubles UI complexity for marginal benefit

### Consequences
- Preview modal shows hardcoded 5 activities (2 with Jira keys, 3 without)
- Users must generate actual prompt to validate against real data
- v2 can add "Test with Real Data" button as enhancement

---

## ADR-007: Character Count Warnings (Not Hard Limits)
**Date:** 2026-01-01  
**Status:** ✅ Accepted  
**Decision Owner:** UX Designer

### Context
SystemPrompt character limits could be:
1. Hard limit (block save at 10,000 chars)
2. Soft limit with warning (allow save, show warning at 5,000 chars)
3. No limit (risk database bloat)

### Decision
**Soft limit: Warn at 5,000 chars, block at 10,000 chars.**

### Rationale
**Pros:**
- Balances flexibility with safety (users can exceed 5K if needed)
- Prevents extreme cases (10K is reasonable upper bound)
- Copy-paste workflows work (no mid-paste blocking)

**Cons:**
- Users might ignore warnings and create 9,999-char templates
- Large templates may exceed LLM context windows (not enforced here)

**Alternatives Rejected:**
- Hard limit at 5K: Too restrictive, breaks legitimate use cases
- No limit: Risk of multi-MB templates breaking UI/DB

### Consequences
- Textarea shows real-time character count
- Warning banner displays at 5,000+ chars: "⚠️ Large prompts may exceed LLM limits"
- Save button disabled at 10,000+ chars
- Database TEXT column supports up to 1GB (SQLite limit, not enforced)

---

## ADR-008: No Template Export/Import in v1
**Date:** 2026-01-01  
**Status:** ✅ Accepted  
**Decision Owner:** Product Owner

### Context
Template sharing could:
1. Export as JSON file (download button)
2. Export as shareable URL (cloud storage required)
3. Copy template text to clipboard (manual sharing)
4. No export (defer to v2)

### Decision
**No export/import in v1. Users can manually copy SystemPrompt text.**

### Rationale
**Pros:**
- Simpler v1 scope (no file I/O, no format versioning)
- Workaround exists (copy SystemPrompt text, send via email)
- Low user demand (most users won't share templates)

**Cons:**
- Manual sharing is cumbersome (must recreate TemplateKey, Name, DisplayOrder)
- No backup mechanism (users must back up entire database)

**Alternatives Rejected:**
- JSON export: Adds complexity (versioning, file format validation, import UI)
- Shareable URL: Requires cloud storage (out of scope)

### Consequences
- Documentation includes SQL commands for advanced users (INSERT template manually)
- v2 can add JSON export/import if users request it
- Database backup recommended in docs (backup entire TrackYourDayGeneric.db)

---

## ADR-009: Prevent Deleting Last Active Template (Safeguard)
**Date:** 2026-01-01  
**Status:** ✅ Accepted  
**Decision Owner:** Systems Analyst

### Context
If user deletes all active templates:
1. Allow deletion (LLM Analysis page breaks)
2. Block deletion with error message
3. Auto-reactivate another template

### Decision
**Block deletion of last active template. Show error: "Cannot deactivate the last active template."**

### Rationale
**Pros:**
- Prevents LLM Analysis page from breaking (empty dropdown)
- Forces user to have at least 1 usable template
- Clear error message explains constraint

**Cons:**
- Slightly less flexible (user must create new template before deleting last one)

**Alternatives Rejected:**
- Allow deletion: Breaks UI, poor UX
- Auto-reactivate: Confusing (which template to reactivate?)

### Consequences
- Repository method `GetActiveTemplateCount()` checks before soft delete
- Settings UI shows error dialog if count would drop to 0
- Users must create new template OR restore old template before deleting last one

---

## ADR-010: No Concurrent Edit Locking (Optimistic Strategy)
**Date:** 2026-01-01  
**Status:** ✅ Accepted  
**Decision Owner:** Systems Analyst

### Context
Concurrent template edits could be handled by:
1. Pessimistic locking (lock row during edit, release on save/cancel)
2. Optimistic locking (check UpdatedAt timestamp on save, fail if changed)
3. No locking (last write wins, risk of data loss)

### Decision
**Use optimistic locking with UpdatedAt timestamp comparison.**

### Rationale
**Pros:**
- Simpler implementation (no lock management)
- Better UX (no "locked by user X" messages)
- SQLite supports optimistic locking naturally (no row-level locks)

**Cons:**
- Concurrent edits fail (user must reload and re-apply changes)
- Risk of user frustration if conflict occurs

**Alternatives Rejected:**
- Pessimistic locking: Complex in SQLite, requires lock timeout logic
- No locking: Data loss risk (unacceptable)

### Consequences
- Repository Save() method checks: `WHERE UpdatedAt = @lastKnownUpdatedAt`
- If 0 rows affected → throw exception: "Template modified by another process"
- UI displays error + "Reload Template" button to fetch latest version

---

## Open Decisions (To Be Resolved)

### OD-001: Localization Strategy
**Question:** Should template management UI support multiple languages?  
**Options:**
1. English-only for v1 (defer localization to v2)
2. Full localization (resource files for all UI text)

**Recommendation:** Full localization from v1 (avoids technical debt). Use `.resx` files.

### OD-002: Template Testing with Real Data
**Question:** Should users test templates against real activity data?  
**Impact:** Requires date picker + data loading in preview modal (complexity increase).  
**Recommendation:** Defer to v1.5 (add "Test with Real Data" button alongside sample preview).

### OD-003: Drag-and-Drop Reordering
**Question:** Should v1 include drag-and-drop or wait for v2?  
**Impact:** Development time +1 day, mobile compatibility issues.  
**Recommendation:** Arrow buttons for v1, drag-and-drop in v2 if user demand exists.

---

## Revision History

| Version | Date       | Author           | Changes                              |
|---------|------------|------------------|--------------------------------------|
| 1.0     | 2026-01-01 | Systems Analyst  | Initial ADR document                 |
| 1.1     | TBD        | TBD              | Resolve open decisions               |

---

## References
- [Feature Spec](./spec.md)
- [Template Management Extension](./template-management-extension.md)
- [Implementation Checklist](./IMPLEMENTATION.md)
- [Database Migration](./migration.sql)
