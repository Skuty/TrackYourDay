# LLM Prompt Generator - Implementation Checklist

## 📋 Quick Reference

**Feature Status:** Ready for Development  
**Documentation:** Complete  
**Database Impact:** New table `llm_prompt_templates` (auto-created on startup)  
**UI Impact:** 2 new pages (Insights tab + Settings tab)

---

## 🎯 Implementation Order

### Phase 1: Database & Repository (Backend)
**Priority:** Critical  
**Estimated Effort:** 2-3 days

- [ ] Create `LlmPromptTemplate` entity class
- [ ] Implement `ILlmPromptTemplateRepository` interface
- [ ] Implement `SqliteLlmPromptTemplateRepository` 
  - [ ] InitializeStructure() - Create table
  - [ ] SeedDefaultTemplates() - Insert 3 defaults
  - [ ] GetActiveTemplates()
  - [ ] GetByKey()
  - [ ] Save() - Upsert logic
  - [ ] Delete() - Soft delete
  - [ ] GetAllTemplates() - For Settings UI
  - [ ] Restore()
  - [ ] UpdateDisplayOrder()
  - [ ] BulkUpdateDisplayOrder()
  - [ ] TemplateKeyExists()
  - [ ] GetMaxDisplayOrder()
  - [ ] GetActiveTemplateCount()
- [ ] Write unit tests for repository (target: 80%+ coverage)
- [ ] Register repository in ServiceRegistration/ServiceCollections.cs

**Acceptance Test:**
```bash
# Run app, check database
sqlite3 "%AppData%\TrackYourDay\TrackYourDayGeneric.db"
SELECT * FROM llm_prompt_templates WHERE IsActive = 1;
# Should return 3 rows
```

---

### Phase 2: Prompt Generation Service (Core Logic)
**Priority:** Critical  
**Estimated Effort:** 1-2 days

- [ ] Create `PromptGeneratorService` class
  - [ ] `GeneratePrompt(template, activities)` → string
  - [ ] `ValidateTemplate(template)` → throws if invalid
  - [ ] `SerializeActivitiesToMarkdown(activities)` → string
- [ ] Create `ActivityDataSerializer` helper
  - [ ] Converts `GroupedActivity[]` → Markdown table
  - [ ] Formats durations (HH:mm:ss)
  - [ ] Escapes special characters
- [ ] Write unit tests for prompt generation

**Acceptance Test:**
```csharp
var activities = new[] { /* test data */ };
var template = new LlmPromptTemplate { SystemPrompt = "Test {ACTIVITY_DATA_PLACEHOLDER}" };
var result = promptGenerator.GeneratePrompt(template, activities);
Assert.DoesNotContain("{ACTIVITY_DATA_PLACEHOLDER}", result);
Assert.Contains("| Date", result); // Markdown table header
```

---

### Phase 3: LLM Analysis Page (Insights Tab)
**Priority:** High  
**Estimated Effort:** 2-3 days

- [ ] Create `LlmAnalysis.razor` page
  - [ ] Add `@page "/llm-analysis"` route
  - [ ] Date range pickers (reuse from Analytics.razor)
  - [ ] Template dropdown (populated from repository)
  - [ ] "Generate Prompt" button
  - [ ] Prompt preview textarea (read-only, multiline)
  - [ ] Character count label
  - [ ] "Copy to Clipboard" button
  - [ ] "Download Prompt" button
  - [ ] Privacy warning banner
  - [ ] Error handling (no activities, no templates, etc.)
- [ ] Add tab to Insights section navigation
  - [ ] Update Index.razor or Main.razor (wherever Insights tabs defined)
  - [ ] Tab label: "LLM Analysis"
  - [ ] Icon: `@Icons.Material.Filled.SmartToy`
- [ ] Implement JavaScript interop for clipboard/download
  - [ ] ClipboardService.CopyToClipboard(text)
  - [ ] FileDownloadService.DownloadAsFile(text, filename)
- [ ] Write Blazor component tests

**Acceptance Test:**
1. Navigate to Insights → LLM Analysis
2. Select today's date
3. Choose "Detailed Time Breakdown" template
4. Click "Generate Prompt"
5. Verify prompt displays with activity data
6. Click "Copy to Clipboard" → Paste in Notepad → Verify content

---

### Phase 4: Template Management UI (Settings Tab)
**Priority:** Medium  
**Estimated Effort:** 3-4 days

- [ ] Create `TemplateManagementTab.razor` component
  - [ ] MudDataGrid with columns: Name, TemplateKey, IsActive, DisplayOrder, Actions
  - [ ] Search box for filtering
  - [ ] Pagination (10 per page)
  - [ ] "Create New Template" button
  - [ ] "Restore Defaults" button
  - [ ] Reorder Mode toggle (arrow buttons for v1)
- [ ] Create `TemplateEditorDialog.razor` component
  - [ ] TemplateKey input (read-only if editing)
  - [ ] Name input
  - [ ] SystemPrompt textarea (monospace font)
  - [ ] IsActive checkbox
  - [ ] DisplayOrder numeric input
  - [ ] Character count (real-time)
  - [ ] Validation errors (inline)
  - [ ] "Preview with Sample Data" button
  - [ ] "Save" and "Cancel" buttons
- [ ] Create `TemplatePreviewModal.razor` component
  - [ ] Hardcoded sample activities (5 rows)
  - [ ] Rendered prompt preview (read-only)
  - [ ] Character count
- [ ] Add tab to Settings.razor
  - [ ] New `<MudTabPanel Text="LLM Prompt Templates">`
  - [ ] Icon: `@Icons.Material.Filled.TextSnippet`
- [ ] Implement action handlers
  - [ ] OnCreateTemplate()
  - [ ] OnEditTemplate()
  - [ ] OnDeleteTemplate() - Confirmation dialog
  - [ ] OnRestoreTemplate()
  - [ ] OnDuplicateTemplate()
  - [ ] OnReorderTemplates() - Arrow up/down buttons
  - [ ] OnRestoreDefaults() - Confirmation dialog
- [ ] Write Blazor component tests

**Acceptance Test:**
1. Navigate to Settings → LLM Prompt Templates
2. Click "Create New Template"
3. Fill fields with valid data
4. Click "Preview with Sample Data" → Verify preview renders
5. Save template
6. Verify new template appears in grid
7. Navigate to Insights → LLM Analysis
8. Verify new template in dropdown

---

### Phase 5: Validation & Error Handling
**Priority:** High  
**Estimated Effort:** 1 day

- [ ] Client-side validation
  - [ ] TemplateKey regex: `^[a-z0-9-]+$`
  - [ ] Name length: 5-100 chars
  - [ ] SystemPrompt length: 100-10,000 chars
  - [ ] Placeholder existence check
  - [ ] DisplayOrder > 0
- [ ] Server-side validation (repository layer)
  - [ ] TemplateKey uniqueness check
  - [ ] Duplicate DisplayOrder detection
  - [ ] Last active template deletion prevention
- [ ] Error messages (user-friendly)
  - [ ] "No activities found for selected dates"
  - [ ] "Template key already exists"
  - [ ] "Cannot deactivate last active template"
  - [ ] "Template modified by another process" (concurrent edit)
- [ ] Database error handling
  - [ ] Wrap all SQLite operations in try-catch
  - [ ] Log errors with Serilog
  - [ ] Display user-friendly error banner
  - [ ] Graceful degradation (show hardcoded templates if DB fails)

**Acceptance Test:**
1. Attempt to create template with invalid TemplateKey (spaces) → Verify error
2. Attempt to delete last active template → Verify error
3. Manually corrupt DB file → Restart app → Verify error banner + fallback

---

### Phase 6: Testing & Documentation
**Priority:** High  
**Estimated Effort:** 2 days

- [ ] Unit tests
  - [ ] Repository methods (all CRUD operations)
  - [ ] PromptGenerator service
  - [ ] ActivityDataSerializer
  - [ ] Template validation logic
  - [ ] Target: 80%+ coverage
- [ ] Integration tests
  - [ ] Database seeding idempotency
  - [ ] Concurrent template edits
  - [ ] DisplayOrder auto-correction
- [ ] Manual test scenarios
  - [ ] All 25 scenarios from spec.md
  - [ ] All 15 scenarios from template-management-extension.md
  - [ ] Browser compatibility (Edge, Chrome)
  - [ ] Clipboard permissions test
- [ ] Documentation
  - [ ] Update README.md with feature description
  - [ ] Add screenshots to docs/features/llm-prompt-generator/
  - [ ] Document SQL maintenance commands
  - [ ] Add "What's New" entry (if release notes exist)

**Acceptance Test:**
```bash
# Run all tests
dotnet test --filter "Category!=Integration"
# Should pass with 80%+ coverage

# Manual test: Generate prompt for 100+ activities
# Verify character count warning displays
# Verify download file works (no browser crash)
```

---

## 🔍 Quality Gates

### Before Code Review
- [ ] All unit tests pass (80%+ coverage)
- [ ] No compiler warnings
- [ ] No hardcoded strings (use resource files)
- [ ] No magic numbers (use constants)
- [ ] All database queries use parameterized queries
- [ ] All user inputs sanitized/validated
- [ ] Serilog logging added for all errors

### Before Merge to Main
- [ ] Code review approved by 1+ reviewer
- [ ] All manual test scenarios pass
- [ ] Database migration tested (fresh DB + existing DB)
- [ ] No breaking changes to existing features
- [ ] Documentation updated
- [ ] Screenshots added to README
- [ ] Release notes entry added

---

## 🐛 Known Issues & Workarounds

### Issue: Clipboard API requires HTTPS
**Impact:** Local development (http://localhost) may fail clipboard copy  
**Workaround:** Use "Download Prompt" button instead  
**Fix:** Deploy app with HTTPS or use `https://localhost` in dev

### Issue: SQLite write conflicts (multi-instance)
**Impact:** If user runs 2 app instances, concurrent template edits may fail  
**Workaround:** Close other instances before editing templates  
**Fix:** Add retry logic with exponential backoff

### Issue: Large prompts (>100KB) lag UI
**Impact:** Selecting 500+ activities generates slow prompt  
**Workaround:** Limit date range to single day  
**Fix:** Add debouncing + progress spinner for generation

---

## 📊 Success Metrics

**User Adoption:**
- Target: 50% of active users try feature within 1 month
- Measure: Count of distinct users who click "Generate Prompt"

**Feature Usage:**
- Target: 10+ prompts generated per week (per user average)
- Measure: Log event when "Copy to Clipboard" / "Download" clicked

**Template Customization:**
- Target: 20% of users create custom templates
- Measure: Count of templates where TemplateKey NOT IN ('detailed', 'concise', 'task-oriented')

**Error Rate:**
- Target: <5% of prompt generations fail
- Measure: Count of exceptions thrown / total generation attempts

---

## 🚀 Deployment Checklist

### Pre-Deployment
- [ ] All tests pass in CI/CD pipeline
- [ ] Database migration script tested on staging
- [ ] Feature flag `EnableLlmPromptGenerator` = true
- [ ] Backup production database

### Deployment
- [ ] Deploy application package
- [ ] Verify database table creation on first run
- [ ] Verify 3 default templates seeded
- [ ] Test Settings tab loads without errors
- [ ] Test LLM Analysis tab loads without errors

### Post-Deployment
- [ ] Monitor Serilog logs for errors (first 24 hours)
- [ ] Check database file size increase (expected: +10KB)
- [ ] Verify no performance regression (startup time, UI responsiveness)
- [ ] Collect user feedback (GitHub Issues, support tickets)

### Rollback Plan
If critical bug found:
1. Set feature flag `EnableLlmPromptGenerator` = false
2. Restart app → Features hidden from UI
3. Fix bug, deploy patch
4. Re-enable feature flag

---

## 📞 Support

**Developer Contact:** [Your Name/Team]  
**Documentation:** `docs/features/llm-prompt-generator/README.md`  
**Specs:** 
- Core: `docs/features/llm-prompt-generator/spec.md`
- Template Management: `docs/features/llm-prompt-generator/template-management-extension.md`

**Database Migration:** `docs/features/llm-prompt-generator/migration.sql`
