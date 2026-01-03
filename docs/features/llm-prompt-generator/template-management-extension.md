# Feature Extension: LLM Prompt Template Management UI

## Problem Statement
Users need ability to create, edit, and delete custom LLM prompt templates without direct SQL access. Current spec only provides hardcoded templates editable via SQL, creating barrier for non-technical users and making template iteration slow.

---

## User Stories

**Primary Persona:** Power User / Admin who wants to customize LLM analysis behavior

- **US8:** As a power user, I want to create new custom prompt templates so that I can tailor LLM output to my specific workflow
- **US9:** As a user, I want to edit existing prompt templates so that I can fix errors or improve prompts based on LLM results
- **US10:** As an admin, I want to deactivate (soft delete) prompt templates so that I can remove outdated templates without losing them permanently
- **US11:** As a user, I want to reorder templates so that my most-used templates appear first in the dropdown
- **US12:** As a user, I want to preview how activity data will be injected into my template so that I can validate placeholder usage before saving
- **US13:** As a user, I want to duplicate an existing template so that I can create variations without starting from scratch
- **US14:** As a user, I want to restore accidentally deleted templates so that I can recover from mistakes

---

## Acceptance Criteria

### AC11: Settings Page Navigation
**Given** the user is on the Settings page  
**When** the page loads  
**Then** a new tab labeled "LLM Prompt Templates" appears alongside "Breaks", "GitLab Integration", "Jira Integration", "Database Management"  
**And** the tab icon is `@Icons.Material.Filled.TextSnippet`

### AC12: Template List Display
**Given** the user clicks the "LLM Prompt Templates" tab  
**When** the tab loads  
**Then** a MudDataGrid displays all templates (both active and inactive)  
**And** columns are: Name, TemplateKey, IsActive (checkbox), DisplayOrder, UpdatedAt, Actions  
**And** rows are sortable by all columns  
**And** inactive templates are visually distinguished (gray text or strikethrough)  
**And** a "Create New Template" button is visible above the grid

### AC13: Create New Template
**Given** the user clicks "Create New Template" button  
**When** a dialog opens  
**Then** the dialog contains:
- TemplateKey input (required, 3-50 chars, lowercase alphanumeric + hyphens, regex: `^[a-z0-9-]+$`)
- Name input (required, 5-100 chars)
- SystemPrompt textarea (required, 100-10,000 chars, must contain `{ACTIVITY_DATA_PLACEHOLDER}`)
- DisplayOrder numeric input (required, positive integer, default = max existing order + 1)
- IsActive checkbox (default = checked)
- "Preview with Sample Data" button
- "Save" and "Cancel" buttons  
**And** validation errors display inline below each field  
**And** Save is disabled until all validations pass

### AC14: Template Key Uniqueness Validation
**Given** the user enters a TemplateKey that already exists in the database  
**When** they attempt to save  
**Then** an error displays: "Template key '{key}' already exists. Choose a unique key."  
**And** the save operation is blocked  
**And** the TemplateKey input is highlighted in red

### AC15: Placeholder Validation
**Given** the user enters a SystemPrompt without `{ACTIVITY_DATA_PLACEHOLDER}`  
**When** they attempt to save  
**Then** an error displays: "SystemPrompt must contain {ACTIVITY_DATA_PLACEHOLDER} for activity data injection."  
**And** the save operation is blocked  
**And** the SystemPrompt textarea is highlighted in red

### AC16: Edit Existing Template
**Given** the user clicks "Edit" action on a template row  
**When** the edit dialog opens  
**Then** all fields are pre-populated with current values  
**And** TemplateKey is read-only (cannot change after creation)  
**And** UpdatedAt timestamp updates on save  
**And** validation rules match AC13-AC15

### AC17: Soft Delete Template
**Given** the user clicks "Delete" action on an active template  
**When** a confirmation dialog appears: "Deactivate template '{Name}'? It will be hidden from LLM Analysis page but preserved in database."  
**And** the user confirms  
**Then** the template's `IsActive` flag sets to 0  
**And** the template remains visible in Settings grid (grayed out)  
**And** the template disappears from LLM Analysis dropdown  
**And** UpdatedAt timestamp updates

### AC18: Prevent Deleting Last Active Template
**Given** only 1 active template remains in the database  
**When** the user attempts to delete it  
**Then** an error displays: "Cannot deactivate the last active template. At least one template must remain available."  
**And** the delete operation is blocked

### AC19: Restore Deleted Template
**Given** the user views an inactive template in the grid  
**When** they click "Restore" action  
**Then** the template's `IsActive` flag sets to 1  
**And** the template becomes available in LLM Analysis dropdown  
**And** UpdatedAt timestamp updates  
**And** no confirmation dialog required (low-risk operation)

### AC20: Reorder Templates (Drag-and-Drop)
**Given** the user enables "Reorder Mode" toggle above the grid  
**When** reorder mode is active  
**Then** each row displays a drag handle icon  
**And** the user can drag rows to new positions  
**And** on drop, DisplayOrder values update automatically (1, 2, 3...)  
**And** UpdatedAt timestamps update for all affected rows  
**And** changes persist immediately (no explicit save button)

### AC21: Preview Template with Sample Data
**Given** the user clicks "Preview with Sample Data" button in create/edit dialog  
**When** the preview modal opens  
**Then** the system generates sample activity data (hardcoded 5 activities with varied descriptions/durations)  
**And** replaces `{ACTIVITY_DATA_PLACEHOLDER}` with serialized sample data  
**And** displays the full rendered prompt in a read-only textarea  
**And** displays character count below preview  
**And** user can close preview without saving template

### AC22: Duplicate Template
**Given** the user clicks "Duplicate" action on a template row  
**When** the create dialog opens  
**Then** all fields pre-populate with source template values  
**And** TemplateKey auto-appends "-copy" suffix (e.g., "detailed" → "detailed-copy")  
**And** Name auto-prepends "Copy of " (e.g., "Copy of Detailed Time Breakdown")  
**And** DisplayOrder sets to max existing order + 1  
**And** IsActive defaults to checked  
**And** user can modify all fields before saving

### AC23: Display Order Conflict Resolution
**Given** two templates have the same DisplayOrder value (data corruption scenario)  
**When** the Settings page loads  
**Then** the system auto-corrects DisplayOrder by re-sequencing (1, 2, 3...) based on current sort order  
**And** UpdatedAt timestamps do NOT change (silent correction)  
**And** a warning toast displays: "Template display order corrected due to conflicts."

### AC24: Concurrent Edit Detection
**Given** User A opens edit dialog for template "detailed"  
**And** User B edits and saves the same template  
**When** User A attempts to save their changes  
**Then** an error displays: "Template was modified by another process. Your changes cannot be saved. Please reload and try again."  
**And** the save operation is blocked  
**And** a "Reload Template" button appears to fetch latest version

### AC25: Template Character Count Warnings
**Given** the user types in the SystemPrompt textarea  
**When** the character count exceeds 5,000 characters  
**Then** a warning displays: "⚠️ Large prompts may exceed LLM context limits. Consider shortening."  
**And** the warning updates in real-time as user types  
**And** save is still allowed (warning, not error)

---

## Out of Scope (Extended)

1. **No Template Version History** - UpdatedAt tracks last change, but no rollback to previous versions
2. **No Template Import/Export** - Users cannot export templates as JSON/XML or import from file
3. **No Template Sharing** - No mechanism to share templates between users or across installations
4. **No Markdown Preview** - SystemPrompt textarea displays raw text, no rich text formatting
5. **No Syntax Highlighting** - No color-coding for `{ACTIVITY_DATA_PLACEHOLDER}` or other placeholders
6. **No Template Testing** - No ability to test template against real activity data before saving
7. **No Bulk Operations** - Cannot delete/activate/deactivate multiple templates at once
8. **No Template Categories/Tags** - All templates in flat list, no folder structure
9. **No Access Control** - All users have full CRUD permissions (no admin-only templates)

---

## Edge Cases & Risks (Extended)

### UI/UX Risks

11. **What if user creates 50+ custom templates?**  
    - Risk: Settings grid becomes unmanageable, LLM Analysis dropdown too long.  
    - Mitigation: Add pagination to Settings grid (10 per page). Add search box above grid to filter by Name/TemplateKey.

12. **What if user accidentally saves template with malformed placeholder (typo)?**  
    - Risk: Prompt generation fails silently or produces broken output.  
    - Mitigation: Validate exact string match: `if (!SystemPrompt.Contains("{ACTIVITY_DATA_PLACEHOLDER}"))`. Highlight typos in preview.

13. **What if SystemPrompt contains multiple placeholders (user expects repeated injection)?**  
    - Risk: Only first placeholder gets replaced, others remain as literal text.  
    - Mitigation: Documentation clarifies: "Use placeholder once. It will be replaced with full data table."

14. **What if user deletes all templates including defaults?**  
    - Risk: LLM Analysis page breaks (no templates in dropdown).  
    - Mitigation: AC18 prevents deleting last active template. Add "Restore Defaults" button to re-seed original 3 templates.

15. **What if DisplayOrder values become negative or zero?**  
    - Risk: Sorting breaks, templates disappear from UI.  
    - Mitigation: Validate `DisplayOrder > 0` on save. Auto-correct on load if negative values detected.

16. **What if template Name contains special characters (&, <, >, ", ')?**  
    - Risk: XSS attack if Name rendered without escaping.  
    - Mitigation: Blazor auto-escapes by default. Add explicit validation: reject `<script>` tags, limit to safe chars.

17. **What if user holds "Reorder Mode" toggle on while another user saves?**  
    - Risk: DisplayOrder conflicts, lost reorder changes.  
    - Mitigation: Display warning: "Reorder mode locks templates. Exit to allow concurrent edits."

### Data Integrity Risks

18. **What if database transaction fails mid-reorder (power loss)?**  
    - Risk: Half of DisplayOrder values updated, half stale → grid shows wrong order.  
    - Mitigation: Wrap reorder in SQLite transaction. Rollback on failure. Add retry logic.

19. **What if UpdatedAt timestamp overflows or becomes corrupted?**  
    - Risk: Concurrent edit detection fails (always allows overwrite).  
    - Mitigation: Use ISO 8601 TEXT type (no overflow risk). Validate timestamp format on read.

20. **What if TemplateKey contains spaces despite validation (race condition)?**  
    - Risk: Breaks repository GetByKey() queries.  
    - Mitigation: Double-validate on server side before SQL insert. Trim whitespace. Reject if validation fails.

### Performance Risks

21. **What if SystemPrompt is 9,999 characters and user types fast?**  
    - Risk: Textarea lags, character count updates slowly.  
    - Mitigation: Debounce character count update (300ms delay). Use `@bind-Value:event="oninput"` for real-time validation.

22. **What if loading 100+ templates from database takes >2 seconds?**  
    - Risk: Settings tab freezes on load.  
    - Mitigation: Add loading spinner. Load templates asynchronously (`await repository.GetAllTemplates()`). Paginate grid.

---

## Data Requirements (Extended)

### No New Tables Required
All template management uses existing `llm_prompt_templates` table defined in main spec.

### New Repository Methods

#### **ILlmPromptTemplateRepository (Extended)**
```csharp
public interface ILlmPromptTemplateRepository
{
    // Existing methods from main spec
    IReadOnlyList<LlmPromptTemplate> GetActiveTemplates();
    LlmPromptTemplate? GetByKey(string templateKey);
    void Save(LlmPromptTemplate template);  // Upsert (INSERT or UPDATE)
    void Delete(string templateKey);        // Soft delete
    void SeedDefaultTemplates();
    
    // NEW methods for template management UI
    IReadOnlyList<LlmPromptTemplate> GetAllTemplates(bool includeInactive = true);  // For Settings grid
    void Restore(string templateKey);                                               // Set IsActive = 1
    void UpdateDisplayOrder(string templateKey, int newOrder);                      // Reorder
    void BulkUpdateDisplayOrder(Dictionary<string, int> keyOrderMap);              // Reorder multiple
    bool TemplateKeyExists(string templateKey);                                    // Uniqueness check
    int GetMaxDisplayOrder();                                                       // For new template default
    int GetActiveTemplateCount();                                                   // For last-template deletion check
    LlmPromptTemplate? GetByKeyIncludingInactive(string templateKey);              // For restore operation
}
```

### Validation Rules (Enforced in UI + Repository)

| Field | Validation Rule | Error Message |
|-------|----------------|---------------|
| TemplateKey | Required, 3-50 chars, regex: `^[a-z0-9-]+$` | "Template key must be 3-50 lowercase alphanumeric characters or hyphens." |
| TemplateKey | Unique (case-insensitive) | "Template key '{key}' already exists. Choose a unique key." |
| Name | Required, 5-100 chars | "Name must be between 5 and 100 characters." |
| Name | No leading/trailing whitespace | "Name cannot start or end with spaces." |
| SystemPrompt | Required, 100-10,000 chars | "Prompt must be between 100 and 10,000 characters." |
| SystemPrompt | Contains `{ACTIVITY_DATA_PLACEHOLDER}` exactly | "Prompt must contain {ACTIVITY_DATA_PLACEHOLDER} for data injection." |
| DisplayOrder | Positive integer (> 0) | "Display order must be a positive number." |
| DisplayOrder | Unique per active template (enforced on save) | "Display order {order} is already used. Choose a different number." |

---

## UI/UX Requirements (Extended)

### Settings Page - New Tab: "LLM Prompt Templates"

**Layout:**
```
┌─────────────────────────────────────────────────────────────────────┐
│ [+ Create New Template]  [🔄 Restore Defaults]  [Toggle: Reorder]  │
│ Search: [_______________________________________________]            │
├─────────────────────────────────────────────────────────────────────┤
│ MudDataGrid                                                         │
│ ┌──────┬────────────────┬──────────┬────────┬─────────┬──────────┐│
│ │Handle│ Name           │ Key      │ Active │ Order   │ Actions  ││
│ ├──────┼────────────────┼──────────┼────────┼─────────┼──────────┤│
│ │ ≡    │ Detailed...    │ detailed │ ☑      │ 1       │ [E][D][C]││
│ │ ≡    │ Concise...     │ concise  │ ☑      │ 2       │ [E][D][C]││
│ │ ≡    │ Task-Oriented  │ task-... │ ☐      │ 3       │ [E][R][C]││
│ └──────┴────────────────┴──────────┴────────┴─────────┴──────────┘│
│ Showing 1-3 of 3 templates                          [<] 1 [>]      │
└─────────────────────────────────────────────────────────────────────┘

Legend:
[E] = Edit button
[D] = Delete button (active templates)
[R] = Restore button (inactive templates)
[C] = Duplicate button
```

### Create/Edit Template Dialog

**Size:** Large (width: 80vw, max 1200px)  
**Sections:**
1. **Basic Info** (left column):
   - TemplateKey input (read-only if editing)
   - Name input
   - IsActive checkbox
   - DisplayOrder numeric input

2. **Prompt Editor** (right column):
   - SystemPrompt textarea (height: 400px, monospace font)
   - Character count: "X / 10,000 characters"
   - Warning banner (if >5,000 chars)

3. **Actions** (bottom):
   - "Preview with Sample Data" button (secondary)
   - "Cancel" button (text)
   - "Save" button (primary, disabled until valid)

### Preview Modal

**Triggered by:** "Preview with Sample Data" button  
**Content:**
- **Header:** "Prompt Preview - {Template Name}"
- **Sample Data Section:** (collapsible)
  ```
  Sample Activities:
  | Date       | Description               | Duration |
  |------------|---------------------------|----------|
  | 2026-01-01 | Visual Studio Code        | 02:15:00 |
  | 2026-01-01 | PROJ-123: Code Review     | 01:30:00 |
  | 2026-01-01 | Email and Slack           | 00:45:00 |
  | 2026-01-01 | Meeting: Sprint Planning  | 01:00:00 |
  | 2026-01-01 | PROJ-456: Bug Investigation | 00:30:00 |
  ```
- **Rendered Prompt Section:** (read-only textarea, height: 500px)
  - Full prompt with placeholder replaced by sample data
- **Character Count:** "Final prompt: X characters"
- **Actions:** "Close" button

### Reorder Mode Toggle

**State:** OFF by default  
**When ON:**
- Grid rows display drag handle (≡ icon) on left
- Row hover shows grab cursor
- Drag-and-drop enabled with visual feedback (shadow, placeholder row)
- "Save Order" button appears (or auto-saves on drop)
- Other actions (Edit, Delete) disabled during reorder

### Confirmation Dialogs

**Delete Template:**
```
Title: ⚠️ Deactivate Template
Body:  Deactivate template "Detailed Time Breakdown"?
       It will be hidden from the LLM Analysis dropdown but
       preserved in the database. You can restore it later.
Actions: [Cancel] [Deactivate]
```

**Restore Defaults:**
```
Title: ℹ️ Restore Default Templates
Body:  This will re-add the original 3 default templates if deleted:
       - Detailed Time Breakdown
       - Concise Summary
       - Task-Oriented Log
       
       Existing custom templates will NOT be affected.
Actions: [Cancel] [Restore]
```

**Last Template Deletion:**
```
Title: ❌ Cannot Deactivate Template
Body:  Cannot deactivate the last active template.
       At least one template must remain available for
       the LLM Analysis feature to function.
Actions: [OK]
```

### Error States

| Error Scenario | UI Behavior |
|----------------|-------------|
| Validation fails on save | Inline error below invalid field, field highlighted red |
| Template key already exists | Error banner above form: "Template key 'xyz' already exists." |
| Database save fails | Toast notification: "Failed to save template. Please try again." |
| Concurrent edit conflict | Error banner: "Template modified by another user. [Reload] to fetch latest." |
| Load failure | Settings tab shows error banner: "Failed to load templates. [Retry]" |

### Responsive Behavior

- **Desktop (>1200px):** Create/Edit dialog uses 2-column layout
- **Tablet (768-1199px):** Dialog switches to single column, SystemPrompt textarea width 100%
- **Mobile (<768px):** 
  - Grid columns collapse (hide TemplateKey, show only Name + Actions)
  - Reorder mode disabled (no drag-and-drop on touch devices)
  - Create/Edit dialog full-screen modal

---

## Dependencies (Extended)

### Existing Features Affected
1. **Settings.razor** - Add new MudTabPanel for template management
2. **ILlmPromptTemplateRepository** - Add 8 new methods (GetAllTemplates, Restore, UpdateDisplayOrder, etc.)
3. **ServiceRegistration** - Register repository as singleton (already done in main spec)
4. **LLM Analysis Page** - Dropdown refresh required after template CRUD operations (reactive update or page reload)

### New UI Components Required
- **TemplateManagementTab.razor** - New component for Settings tab content
- **TemplateEditorDialog.razor** - Reusable component for Create/Edit operations
- **TemplatePreviewModal.razor** - Preview dialog with sample data injection
- **DraggableRow.razor** (optional) - Custom MudDataGrid row with drag handle

### MudBlazor Components Used
- `MudDataGrid<LlmPromptTemplate>` with sorting, pagination, filtering
- `MudDialog` for Create/Edit/Preview/Confirmations
- `MudTextField` for inputs (TemplateKey, Name)
- `MudNumericField<int>` for DisplayOrder
- `MudCheckBox` for IsActive
- `MudExpansionPanels` for collapsible sample data preview
- `MudButton` (Primary, Secondary, Error colors)
- `MudAlert` for inline validation errors
- `MudSwitch` for Reorder Mode toggle

### JavaScript Interop (if needed)
- **Drag-and-Drop:** Use `@ondrop`, `@ondragstart` events (native Blazor)
- **Textarea Auto-Resize:** Optional JS interop for dynamic height adjustment

---

## Non-Functional Requirements (Extended)

### Performance
- Template list load must complete in <1 second for 100 templates
- Preview generation must render in <500ms (hardcoded sample data, no DB query)
- Reorder drag-and-drop must feel responsive (<100ms latency)
- Character count update must debounce to avoid lag during fast typing

### Security
- **Input Sanitization:** Escape all user-provided text before rendering (Blazor default)
- **SQL Injection Prevention:** Use parameterized queries in repository (already enforced)
- **XSS Prevention:** Validate SystemPrompt for `<script>` tags, reject if found
- **No Secrets in Templates:** Warn users: "Do not include API keys or credentials in templates"

### Accessibility
- All inputs must have `aria-label` attributes
- Dialog focus trap (Tab/Shift+Tab cycles through dialog elements only)
- Error messages must be announced by screen readers (`aria-live="polite"`)
- Drag handles must have keyboard alternative (Up/Down arrow keys to reorder)

### Usability
- **Undo Support:** No undo for saves (user must manually revert). Document in help text.
- **Auto-Save:** No auto-save in edit dialog (explicit Save button required)
- **Unsaved Changes Warning:** If user closes dialog with unsaved changes, show confirmation: "Discard unsaved changes?"

---

## Test Scenarios (Extended)

### Template Management Tests

1. **Create Valid Template:**
   - Fill all fields with valid data
   - Verify `{ACTIVITY_DATA_PLACEHOLDER}` validation passes
   - Verify template appears in Settings grid and LLM Analysis dropdown

2. **Create Duplicate TemplateKey:**
   - Enter existing key (e.g., "detailed")
   - Verify error: "Template key 'detailed' already exists"
   - Verify save is blocked

3. **Edit Template SystemPrompt:**
   - Open edit dialog for "detailed" template
   - Modify SystemPrompt (preserve placeholder)
   - Save → Verify UpdatedAt timestamp changes
   - Verify LLM Analysis page reflects changes

4. **Edit Template - Remove Placeholder:**
   - Delete `{ACTIVITY_DATA_PLACEHOLDER}` from SystemPrompt
   - Attempt save → Verify error: "Prompt must contain {ACTIVITY_DATA_PLACEHOLDER}"
   - Re-add placeholder → Verify save succeeds

5. **Soft Delete Last Active Template:**
   - Deactivate all templates except one
   - Attempt to delete remaining template
   - Verify error: "Cannot deactivate the last active template"

6. **Restore Deleted Template:**
   - Soft delete "concise" template
   - Verify it disappears from LLM Analysis dropdown
   - Click "Restore" in Settings grid
   - Verify it reappears in dropdown

7. **Reorder Templates:**
   - Enable Reorder Mode
   - Drag "Task-Oriented" to position 1
   - Verify DisplayOrder updates: Task-Oriented=1, Detailed=2, Concise=3
   - Verify LLM Analysis dropdown shows new order

8. **Duplicate Template:**
   - Click "Duplicate" on "detailed" template
   - Verify create dialog pre-fills with:
     - TemplateKey: "detailed-copy"
     - Name: "Copy of Detailed Time Breakdown"
   - Save → Verify new template exists separately

9. **Preview with Sample Data:**
   - Create new template with custom prompt
   - Click "Preview with Sample Data"
   - Verify sample activities table appears
   - Verify placeholder replaced in rendered prompt

10. **Character Count Warning:**
    - Type 5,001 characters in SystemPrompt
    - Verify warning: "⚠️ Large prompts may exceed LLM context limits"
    - Verify save is still allowed (warning, not error)

11. **Concurrent Edit Conflict:**
    - User A opens edit dialog for "detailed"
    - User B edits and saves "detailed" via SQL
    - User A attempts save
    - Verify error: "Template modified by another process"
    - Click "Reload Template" → Verify latest version loads

12. **Restore Defaults:**
    - Soft delete all 3 default templates
    - Click "Restore Defaults" button
    - Verify original 3 templates re-appear (active, original SystemPrompt text)

13. **DisplayOrder Auto-Correction:**
    - Manually set two templates to DisplayOrder=1 via SQL
    - Reload Settings page
    - Verify warning toast: "Template display order corrected"
    - Verify DisplayOrder re-sequenced: 1, 2, 3

14. **Search/Filter Templates:**
    - Create 10 templates with varied names
    - Type "Detailed" in search box
    - Verify grid filters to show only matching templates

15. **Pagination:**
    - Create 25 templates
    - Verify grid shows 10 per page (default)
    - Navigate to page 2 → Verify next 10 templates display

### Unit Test Coverage (New)

**Repository Layer:**
- `GetAllTemplates(includeInactive: false)` → Returns only IsActive=1
- `GetAllTemplates(includeInactive: true)` → Returns all templates
- `Restore(templateKey)` → Sets IsActive=1, updates UpdatedAt
- `UpdateDisplayOrder(key, order)` → Updates single template's DisplayOrder
- `BulkUpdateDisplayOrder(map)` → Updates multiple templates in transaction
- `TemplateKeyExists(key)` → Case-insensitive uniqueness check
- `GetActiveTemplateCount()` → Returns count where IsActive=1
- `GetMaxDisplayOrder()` → Returns highest DisplayOrder value

**UI Layer:**
- `TemplateEditorDialog.ValidateTemplateKey()` → Rejects invalid chars, checks uniqueness
- `TemplateEditorDialog.ValidatePlaceholder()` → Ensures `{ACTIVITY_DATA_PLACEHOLDER}` exists
- `TemplateManagementTab.OnReorderComplete()` → Calls BulkUpdateDisplayOrder correctly
- `TemplatePreviewModal.RenderPreview()` → Replaces placeholder with sample data

---

## Migration & Rollout (Extended)

### No Additional Database Changes
Template management uses existing `llm_prompt_templates` table. No new migrations required.

### Service Registration
```csharp
// In ServiceRegistration/ServiceCollections.cs
services.AddSingleton<ILlmPromptTemplateRepository, SqliteLlmPromptTemplateRepository>();
```

### Feature Flag (Optional)
```json
{
  "FeatureFlags": {
    "EnableLlmPromptTemplateManagement": true  // Set to false to hide Settings tab
  }
}
```

### Backward Compatibility
- If feature flag is OFF, Settings tab hidden but LLM Analysis page still works (reads DB templates)
- Existing SQL-edited templates remain functional
- Users can upgrade from v1 (SQL-only) to v2 (UI management) without data loss

---

## Open Questions (Extended)

### Critical Decisions Needed

1. **Reorder Mode Implementation:**  
   - Should reorder use drag-and-drop (complex, no mobile support)?  
   - OR use Up/Down arrow buttons per row (simpler, mobile-friendly)?  
   - **Recommendation:** Arrow buttons for v1. Drag-and-drop in v2 if users request it.

2. **Template Testing:**  
   - Should users test templates against real activity data before saving?  
   - OR only preview with hardcoded sample data?  
   - **Recommendation:** Sample data only for v1. Real data testing requires date picker + data loading (scope creep).

3. **Template Import/Export:**  
   - Should v1 include JSON export for backup/sharing?  
   - OR defer to v2?  
   - **Recommendation:** Defer to v2. Workaround: Users can manually copy SystemPrompt text.

4. **Concurrent Edit Strategy:**  
   - Should system use optimistic locking (UpdatedAt comparison) or pessimistic locking (row lock)?  
   - **Recommendation:** Optimistic locking (check UpdatedAt on save). Pessimistic locking complex in SQLite.

5. **Character Limit Enforcement:**  
   - Should SystemPrompt hard-cap at 10,000 chars (block typing)?  
   - OR allow exceeding with warning (soft limit)?  
   - **Recommendation:** Soft limit (allow 10,001+ but warn). Hard cap breaks copy-paste workflows.

6. **Restore Defaults Behavior:**  
   - Should "Restore Defaults" overwrite edited default templates?  
   - OR only re-add if missing?  
   - **Recommendation:** Only re-add if missing. Use `INSERT OR IGNORE` to prevent overwrites.

---

## Definition of Done (Extended)

- [ ] **Settings Page:**
  - [ ] "LLM Prompt Templates" tab added to Settings.razor MudTabs
  - [ ] Tab icon: `@Icons.Material.Filled.TextSnippet`
  - [ ] MudDataGrid displays all templates (active + inactive)
  - [ ] Search box filters templates by Name/TemplateKey

- [ ] **Template CRUD:**
  - [ ] "Create New Template" button opens TemplateEditorDialog
  - [ ] Create dialog validates: TemplateKey uniqueness, placeholder existence, character limits
  - [ ] Edit dialog pre-populates fields, makes TemplateKey read-only
  - [ ] Soft delete sets IsActive=0, shows confirmation dialog
  - [ ] Prevent deleting last active template (AC18)
  - [ ] Restore button sets IsActive=1, no confirmation

- [ ] **Reorder:**
  - [ ] Reorder Mode toggle shows/hides drag handles OR arrow buttons
  - [ ] DisplayOrder updates correctly on reorder
  - [ ] Changes persist immediately or on "Save Order" button click

- [ ] **Preview:**
  - [ ] "Preview with Sample Data" button opens TemplatePreviewModal
  - [ ] Sample data hardcoded (5 activities with Jira keys, varied durations)
  - [ ] Placeholder replaced with Markdown table in preview

- [ ] **Duplicate:**
  - [ ] "Duplicate" button opens create dialog with pre-filled fields
  - [ ] TemplateKey auto-appends "-copy", Name prepends "Copy of "

- [ ] **Restore Defaults:**
  - [ ] "Restore Defaults" button re-seeds original 3 templates if missing
  - [ ] Existing custom templates unaffected

- [ ] **Repository Methods:**
  - [ ] All 8 new methods implemented in SqliteLlmPromptTemplateRepository
  - [ ] Methods use parameterized queries (no SQL injection risk)

- [ ] **Validation:**
  - [ ] Client-side validation on all inputs (real-time feedback)
  - [ ] Server-side validation in repository before SQL execution
  - [ ] All validation error messages match spec

- [ ] **Testing:**
  - [ ] Unit tests for all new repository methods (80%+ coverage)
  - [ ] UI component tests for TemplateEditorDialog validation
  - [ ] Manual tests for all 15 extended test scenarios
  - [ ] Concurrent edit test (simulate multi-user scenario)

- [ ] **Documentation:**
  - [ ] README updated with "Manage LLM Prompt Templates" section
  - [ ] Screenshots of Settings tab added to docs
  - [ ] SQL commands for advanced users documented

- [ ] **Code Review:**
  - [ ] No hardcoded strings (use resource files for localization)
  - [ ] Error handling for all DB operations
  - [ ] No breaking changes to existing Settings tabs

---

## Risk Register (Extended)

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **User creates malformed template, breaks LLM Analysis page** | Medium | High | Validate placeholder on save. Add "Test Template" button to verify before activation. |
| **Reorder mode causes DisplayOrder conflicts (concurrent users)** | Low | Medium | Wrap reorder in transaction. Retry on conflict. Show error if persist fails. |
| **User deletes all templates, cannot use LLM Analysis** | Low | High | Block deletion of last active template (AC18). Add "Restore Defaults" failsafe. |
| **Preview modal with large SystemPrompt (9,999 chars) lags** | Low | Low | Debounce preview generation (500ms delay). Show loading spinner. |
| **Drag-and-drop breaks on mobile/touch devices** | High | Medium | Use arrow buttons instead of drag-and-drop for v1 (mobile-friendly). |
| **Template Name contains HTML/JS injection** | Low | Medium | Blazor auto-escapes by default. Add explicit validation: reject `<script>` tags. |
| **Database write fails mid-reorder (power loss)** | Low | Medium | Wrap BulkUpdateDisplayOrder in transaction. Rollback on failure. |
| **UpdatedAt timestamp used for locking is stale** | Low | Low | Use ISO 8601 with milliseconds: `datetime('now', 'subsec')`. Compare exact string match. |
| **User edits template while LLM Analysis page is open** | Medium | Low | LLM Analysis dropdown caches templates on load. Add "Refresh Templates" button or auto-refresh every 5 min. |
| **Character count lags during fast typing** | Low | Low | Debounce character count update (300ms). Use `@bind-Value:event="oninput"` for immediate validation. |

---

## Future Enhancements (v3+)

1. **Template Version History** - Store previous versions in `llm_prompt_template_history` table, allow rollback
2. **Template Import/Export** - Export as JSON, import from file, share via clipboard
3. **Template Testing with Real Data** - Select date range, test template, preview output before saving
4. **Template Categories** - Organize templates into folders (e.g., "Jira-Focused", "GitLab-Focused")
5. **Syntax Highlighting** - Color-code `{ACTIVITY_DATA_PLACEHOLDER}` in SystemPrompt textarea
6. **Markdown Preview** - Rich text preview of SystemPrompt formatting
7. **Template Sharing** - Export template as URL, import from shared link
8. **Access Control** - Mark templates as "Admin Only" or "Public"
9. **Template Analytics** - Track usage count, last used date, success rate
10. **AI-Assisted Template Creation** - Suggest templates based on user's workflow patterns

---

## Appendix: Sample Template Data for Preview

**Hardcoded Sample Activities (used in Preview Modal):**
```markdown
| Date       | Activity Description                          | Duration  |
|------------|-----------------------------------------------|-----------|
| 2026-01-01 | Visual Studio Code - TrackYourDay project     | 02:15:30  |
| 2026-01-01 | PROJ-123: Code Review - Authentication Module | 01:30:00  |
| 2026-01-01 | Email and Slack - Team Communication          | 00:45:20  |
| 2026-01-01 | Meeting: Sprint Planning                      | 01:00:00  |
| 2026-01-01 | PROJ-456: Bug Investigation - Database Timeout| 00:30:15  |
```

**Total Duration:** 6 hours 1 minute 5 seconds  
**Jira Keys Present:** PROJ-123, PROJ-456  
**Non-Jira Activities:** Visual Studio Code, Email and Slack, Meeting

This sample data enables users to test how their template handles:
- Activities with Jira keys
- Activities without Jira keys
- Varied duration formats
- Mixed activity types (coding, meetings, communication)
