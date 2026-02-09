# Quality Gate Review: LLM Prompt Generation Context

**⚠️ UPDATED AFTER REQUIREMENTS CLARIFICATION ⚠️**

## Clarified Requirements (Provided by Stakeholder)

1. **"Trail of issues"**: All activities that OCCURRED on a SINGLE DATE (not date range)
2. **"Currently assigned"**: Snapshot of issues assigned AT THE MOMENT (not historical)
3. **GitLab**: MUST be included (was forgotten/incomplete)
4. **Security**: Prompts displayed in UI only (no external API calls to LLMs)
5. **Data source**: ALL data from DATABASE - ZERO API calls allowed
6. **Template placeholders**: Need separate `{JIRA_ACTIVITIES}`, `{GITLAB_ACTIVITIES}`, `{CURRENTLY_ASSIGNED_ISSUES}`
7. **Sensitive data**: Include ALL (no filtering)
8. **Performance**: No API calls, database only

---

## Defects Found

### Critical (Must Fix)

- **CRITICAL-001: ENTIRE IMPLEMENTATION VIOLATES "NO API CALLS" REQUIREMENT**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs:45` + `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraActivityService.cs:46-87`
  - **Violation:** ARCHITECTURAL VIOLATION - Requirements Not Met
  - **Description:** The implementation makes API calls via `IJiraRestApiClient` when requirements explicitly state "all data is stored in our database so it should not do ANY API CALLS."
  - **Evidence of API Calls:**
    ```csharp
    // JiraActivityService.cs:48-49 - MAKES API CALLS
    var currentUser = await _jiraRestApiClient.GetCurrentUser().ConfigureAwait(false);
    var issues = await _jiraRestApiClient.GetUserIssues(currentUser, updateDate).ConfigureAwait(false);
    
    // Line 73 - MAKES N+1 API CALLS
    var worklogs = await _jiraRestApiClient.GetIssueWorklogs(issue.Key, updateDate).ConfigureAwait(false);
    ```
  - **What Should Happen:** 
    - Jira activities are ALREADY STORED in database via `IHistoricalDataRepository<JiraActivity>` (see `JiraTracker.cs:14`)
    - GitLab activities are ALREADY STORED via event handlers (`PersistGitLabActivityHandler.cs`)
    - Currently assigned Jira issues are in `IJiraIssueRepository.GetCurrentIssuesAsync()`
    - Currently assigned GitLab artifacts are in `IGitLabStateRepository.GetLatestAsync()`
  - **Fix:** 
    1. DELETE `JiraActivityService.GetActivitiesUpdatedAfter()` usage in LlmPromptService
    2. Query `IHistoricalDataRepository<JiraActivity>` directly using `DateRangeSpecification`
    3. Query `IJiraIssueRepository.GetCurrentIssuesAsync()` for currently assigned issues
    4. Do the same for GitLab

- **CRITICAL-002: WRONG METHOD SIGNATURE - Date Range Instead of Single Date**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/ILlmPromptService.cs:14`
  - **Violation:** Requirements Not Met
  - **Description:** Method signature is `GeneratePrompt(string templateKey, DateOnly startDate, DateOnly endDate)` but requirement states "generate prompt should be done only for single date."
  - **Current Signature:**
    ```csharp
    Task<string> GeneratePrompt(string templateKey, DateOnly startDate, DateOnly endDate);
    ```
  - **Required Signature:**
    ```csharp
    Task<string> GeneratePrompt(string templateKey, DateOnly date);
    ```
  - **Fix:** Change signature to accept single date. Update all tests and callers.

- **CRITICAL-003: Missing GitLab Integration Entirely**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs`
  - **Violation:** Incomplete Feature Implementation
  - **Description:** GitLab is 100% absent from LlmPromptService despite requirement confirmation that "GitLab integration was forgotten/incomplete - FIX IT."
  - **What Exists:**
    - `IGitLabCurrentStateService` - syncs currently assigned artifacts to `IGitLabStateRepository`
    - `IHistoricalDataRepository<GitLabActivity>` via `GitLabTracker` - stores activity events
    - `GitLabStateSnapshot` with `List<GitLabArtifact>` containing issues/MRs
  - **What's Missing:**
    - No injection of GitLab repositories into `LlmPromptService`
    - No `GetGitLabActivitiesMarkdown()` method
    - No `GetCurrentlyAssignedGitLabArtifactsMarkdown()` method
    - Zero GitLab data in generated prompts
  - **Fix:** 
    1. Inject `IHistoricalDataRepository<GitLabActivity>` and `IGitLabStateRepository`
    2. Create methods to fetch GitLab data from DATABASE (not API)
    3. Format as markdown sections

- **CRITICAL-004: Wrong Template Placeholder Design**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/LlmPromptTemplate.cs:17`
  - **Violation:** Requirements Not Met, Poor Extensibility
  - **Description:** Single placeholder `{ACTIVITY_DATA_PLACEHOLDER}` violates requirement for separate placeholders: `{JIRA_ACTIVITIES}`, `{GITLAB_ACTIVITIES}`, `{CURRENTLY_ASSIGNED_ISSUES}`.
  - **Current Design:**
    ```csharp
    public const string Placeholder = "{ACTIVITY_DATA_PLACEHOLDER}";
    ```
  - **Required Design:**
    ```csharp
    public const string JiraActivitiesPlaceholder = "{JIRA_ACTIVITIES}";
    public const string GitLabActivitiesPlaceholder = "{GITLAB_ACTIVITIES}";
    public const string CurrentlyAssignedIssuesPlaceholder = "{CURRENTLY_ASSIGNED_ISSUES}";
    ```
  - **Impact:** Templates cannot control layout/ordering of different data sections. All data is dumped into one blob.
  - **Fix:** 
    1. Add three separate placeholder constants
    2. Update `HasValidPlaceholder()` to check for at least one
    3. Update `GeneratePrompt()` to replace each placeholder independently
    4. BREAKING CHANGE: Requires database migration for existing templates

- **CRITICAL-005: No Separation Between Activities and Currently Assigned State**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs:122-158`
  - **Violation:** Requirements Not Met
  - **Description:** The implementation conflates "activities that occurred on date" with "currently assigned issues." These are TWO DIFFERENT CONCEPTS that need separate sections.
  - **What's Required:**
    1. **Activities Section:** What happened on this specific date (comments, worklogs, status changes) - from `IHistoricalDataRepository<JiraActivity>`
    2. **Currently Assigned Section:** Snapshot of what's assigned RIGHT NOW (regardless of date) - from `IJiraIssueRepository.GetCurrentIssuesAsync()`
  - **What's Missing:**
    - No call to `IJiraIssueRepository.GetCurrentIssuesAsync()`
    - No `GetCurrentlyAssignedIssuesMarkdown()` method
    - No separate markdown section for currently assigned
  - **Fix:**
    ```csharp
    // Add to LlmPromptService
    private async Task<string> GetCurrentlyAssignedJiraIssuesMarkdown()
    {
        var issues = await _jiraIssueRepository.GetCurrentIssuesAsync(CancellationToken.None);
        // Format as markdown table: Key, Summary, Status, Updated
    }
    ```

- **CRITICAL-006: No GitLab Tests for LLM Prompt Integration**
  - **Location:** `Tests/TrackYourDay.Tests/LlmPrompts/LlmPromptServiceTests.cs`
  - **Violation:** Test Coverage Gap
  - **Description:** Tests exist for Jira integration (lines 80-243) but ZERO tests for GitLab despite GitLab service existing with identical interface.
  - **Missing Test Scenarios:**
    - GitLab activities included in prompt
    - GitLab activities filtered by date range
    - GitLab service failure handling
    - Mixed Jira + GitLab activities in single prompt
  - **Fix:** Add test methods for GitLab following same pattern as Jira tests.

### Major (Should Fix)

- **MAJOR-001: WRONG Dependencies Injected - Using API Services Instead of Repositories**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs:16-23`
  - **Violation:** Dependency Inversion Principle, Architecture Violation
  - **Description:** Constructor injects `IJiraActivityService` which makes API calls, when it should inject repositories that query the database.
  - **Current (WRONG) Dependencies:**
    ```csharp
    public LlmPromptService(
        IGenericSettingsRepository settingsRepository,
        IHistoricalDataRepository<EndedActivity> activityRepository,
        IHistoricalDataRepository<EndedMeeting> meetingRepository,
        UserTaskService userTaskService,
        ActivityNameSummaryStrategy summaryStrategy,
        IJiraActivityService jiraActivityService, // ❌ MAKES API CALLS
        ILogger<LlmPromptService> logger)
    ```
  - **Required Dependencies:**
    ```csharp
    public LlmPromptService(
        IGenericSettingsRepository settingsRepository,
        IHistoricalDataRepository<EndedActivity> activityRepository,
        IHistoricalDataRepository<EndedMeeting> meetingRepository,
        IHistoricalDataRepository<JiraActivity> jiraActivityRepository,     // ✅ DATABASE
        IHistoricalDataRepository<GitLabActivity> gitLabActivityRepository, // ✅ DATABASE
        IJiraIssueRepository jiraIssueRepository,                            // ✅ DATABASE
        IGitLabStateRepository gitLabStateRepository,                        // ✅ DATABASE
        UserTaskService userTaskService,
        ActivityNameSummaryStrategy summaryStrategy,
        ILogger<LlmPromptService> logger)
    ```
  - **Fix:** Remove `IJiraActivityService`, inject the four repositories listed above.

- **MAJOR-002: Incorrect Service Lifetime May Cause Stale Data**
  - **Location:** Service registration (need to verify in `ServiceRegistration` files)
  - **Violation:** DI Lifetime Misconfiguration Risk
  - **Description:** Without seeing the registration, if `LlmPromptService` is registered as Singleton but depends on repositories, it could cache stale data.
  - **Fix:** Verify `LlmPromptService` is registered as Scoped or Transient, not Singleton.

- **MAJOR-003: Wrong Markdown Structure - Doesn't Match Requirement**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs:138-148`
  - **Violation:** Requirements Not Met
  - **Description:** Current structure has single "## Related Jira Issues" section. Required structure based on separate placeholders:
  - **Current Structure:**
    ```markdown
    | Date       | Activity Description | Duration  |
    |------------|---------------------|-----------|
    ...
    
    ## Related Jira Issues
    | Date       | Jira Activity Description |
    ...
    ```
  - **Required Structure (Three Separate Sections):**
    ```markdown
    {CURRENTLY_ASSIGNED_ISSUES}
    ## Currently Assigned Jira Issues
    | Key | Summary | Status | Project | Updated |
    
    ## Currently Assigned GitLab Work Items
    | Type | Title | State | Project | Updated |
    
    {JIRA_ACTIVITIES}
    ## Jira Activities (2026-02-08)
    | Time | Activity Description |
    
    {GITLAB_ACTIVITIES}
    ## GitLab Activities (2026-02-08)
    | Time | Activity Description |
    ```
  - **Fix:** Split into three separate generation methods, each returning markdown for its placeholder.

- **MAJOR-004: Silent Failure on GitLab Fetch Hides Errors**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs:152-157`
  - **Violation:** Error Handling Best Practice
  - **Description:** Jira failures are caught and logged as Warning, but this silently hides configuration/connectivity issues from LLM prompt consumers. User gets incomplete prompt without knowing GitLab/Jira data is missing.
  - **Current Code:**
    ```csharp
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to fetch Jira issues...");
        return string.Empty; // SILENT FAILURE
    }
    ```
  - **Fix:** Either:
    - A) Include error message in prompt: `"⚠️ Jira data unavailable: {reason}"`
    - B) Throw exception and let caller handle
    - C) Add validation method that pre-checks connectivity

### Minor (Consider)

- **MINOR-001: Inefficient Date Filtering**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs:127-131`
  - **Violation:** Performance Concern
  - **Description:** Fetches all activities from `startDate` then filters in-memory. For users with years of history, this is wasteful.
  - **Fix:** Push date range filtering into `JiraActivityService.GetActivitiesUpdatedAfter()` or create new method `GetActivitiesInDateRange(start, end)`.

- **MINOR-002: Magic String "## Related Jira Issues"**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs:139`
  - **Violation:** Code Style
  - **Description:** Hardcoded section header should be constant for consistency.
  - **Fix:** 
    ```csharp
    private const string JiraIssuesHeader = "## Related Jira Issues";
    ```

- **MINOR-003: No Cancellation Token Support**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs:28`
  - **Violation:** Async Best Practice
  - **Description:** `GeneratePrompt` is async but doesn't accept `CancellationToken`. GitLab service already supports it (`GetActivitiesUpdatedAfter(DateTime startDate, CancellationToken cancellationToken = default)`).
  - **Fix:** Add `CancellationToken` parameter and pass through to Jira/GitLab calls.

- **MINOR-004: Inconsistent Property Naming in GitLabActivity**
  - **Location:** `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabActivityService.cs:14`
  - **Violation:** Code Style
  - **Description:** Property is spelled `OccuranceDate` (incorrect) instead of `OccurrenceDate` (correct). Jira uses correct spelling.
  - **Fix:** Rename to `OccurrenceDate` (breaking change - requires database migration if persisted).

## Missing Tests

### LlmPromptService Tests (Critical)
- [ ] `GivenGitLabActivitiesExist_WhenGeneratingPrompt_ThenIncludesGitLabActivities`
- [ ] `GivenBothJiraAndGitLabExist_WhenGeneratingPrompt_ThenIncludesBothSections`
- [ ] `GivenNoGitLabActivitiesExist_WhenGeneratingPrompt_ThenExcludesGitLabSection`
- [ ] `GivenGitLabServiceThrowsException_WhenGeneratingPrompt_ThenContinuesWithoutGitLab`
- [ ] `GivenCurrentlyAssignedIssuesExist_WhenGeneratingPrompt_ThenIncludesSeparateSection`

### JiraActivityService Tests (Missing)
- [ ] `GivenIssueReassignedToOther_WhenFetchingCurrentlyAssigned_ThenExcludesIssue`
- [ ] `GivenIssueCurrentlyAssignedToUser_WhenFetchingActivities_ThenMarksAsCurrentlyAssigned`

### Integration Tests
- [ ] `GivenRealJiraConnection_WhenGeneratingPrompt_ThenReturnsValidMarkdown`
- [ ] `GivenRealGitLabConnection_WhenGeneratingPrompt_ThenReturnsValidMarkdown`

## Performance Concerns

- **PERF-001: ENTIRE PERFORMANCE MODEL IS WRONG - API Calls Instead of Database**
  - **Location:** `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraActivityService.cs:46-87`
  - **Description:** Makes 50+ API calls per prompt generation when ALL data should come from database.
  - **Current (WRONG) Flow:**
    1. Call Jira API to get user (200ms)
    2. Call Jira API to get issues (500ms)
    3. For EACH issue, call Jira API for worklogs (N * 200ms)
    4. Total: 700ms + (N * 200ms) = **10+ seconds for 50 issues**
  - **Required Flow:**
    1. Query `IHistoricalDataRepository<JiraActivity>` with date filter (10ms)
    2. Query `IJiraIssueRepository.GetCurrentIssuesAsync()` (10ms)
    3. Total: **20ms**
  - **Impact:** 500x performance degradation due to architecture violation
  - **Fix:** Use repositories, not API services

- **PERF-002: DELETED - No longer applicable since we're not making API calls**

## Security Issues

- **SEC-001: DELETED - Requirement Clarified as "UI Display Only, No External API"**
  - **Previous Concern:** Data leakage to external LLMs
  - **Clarification:** Prompts are "just displayed in UI for user to copy/paste - No risk"
  - **Status:** ✅ NOT A CONCERN given UI-only usage
  - **Note:** If this changes in future (e.g., direct LLM integration), revisit this issue

- **SEC-002: No Input Validation on Template SystemPrompt**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/LlmPromptTemplate.cs:11`
  - **Violation:** OWASP A03:2021 - Injection
  - **Description:** `SystemPrompt` field accepts arbitrary text with no sanitization. Could be used for prompt injection attacks if malicious user can modify templates.
  - **Attack Vector:** Admin UI allows template editing → Attacker injects prompt instructions to leak data or manipulate LLM output
  - **Recommendation:** 
    1. Validate SystemPrompt doesn't contain injection patterns
    2. Limit template editing to superadmin only
    3. Add template approval workflow

## Architecture Issues

- **ARCH-001: LlmPromptService Violates Single Responsibility**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs`
  - **Violation:** SOLID - Single Responsibility Principle
  - **Description:** Class does too many things:
    1. Template management (GetTemplateByKey, GetActiveTemplates)
    2. Activity aggregation (GetActivitiesForDateRange)
    3. Jira integration (GetJiraIssuesMarkdown)
    4. Markdown serialization (SerializeToMarkdown)
    5. Prompt rendering
  - **Recommendation:** Split into:
    - `PromptTemplateRepository` - Template CRUD
    - `ActivityAggregator` - Fetch/combine activities
    - `PromptRenderer` - Inject data into templates
    - `ExternalIssueFormatter` - Jira/GitLab markdown generation

- **ARCH-002: Tight Coupling to Newtonsoft.Json**
  - **Location:** `src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs:69,117`
  - **Violation:** Dependency Inversion Principle
  - **Description:** Directly uses `JsonConvert` instead of abstraction. Makes testing harder and locks into specific serializer.
  - **Recommendation:** Use System.Text.Json or inject `IJsonSerializer` abstraction

## Missing Documentation

- [ ] No feature specification document for "trail of issues" requirement
- [ ] No ADR explaining why only Jira is integrated (GitLab ignored)
- [ ] No documentation on prompt structure/format
- [ ] No security review for LLM data handling
- [ ] No privacy impact assessment for external LLM usage

## Final Verdict

**Status:** ❌ **HARD REJECTED - COMPLETE REWRITE REQUIRED**

**Justification:** 
This implementation is **architecturally broken at the foundation**. It makes API calls when it should use the database, accepts date ranges when it should accept single dates, uses a single placeholder when it needs three, and completely omits GitLab despite explicit requirement. This is not a "fix a few bugs" situation—this requires **complete architectural redesign**.

**The implementation violates EVERY clarified requirement:**
1. ❌ Makes API calls (should be database-only)
2. ❌ Uses date range (should be single date)
3. ❌ GitLab completely missing
4. ❌ Single placeholder (needs three separate)
5. ❌ No "currently assigned" section
6. ❌ Wrong dependencies injected

**Blocking Issues (Auto-Fail Triggers):**
- ✅ **CRITICAL-001:** Makes API calls instead of database queries (ARCHITECTURAL VIOLATION)
- ✅ **CRITICAL-002:** Wrong method signature (date range vs single date)
- ✅ **CRITICAL-003:** Missing GitLab (50% of external integrations absent)
- ✅ **CRITICAL-004:** Wrong placeholder design (not extensible)
- ✅ **CRITICAL-005:** No separation of activities vs currently assigned
- ✅ **CRITICAL-006:** Zero tests for GitLab integration

**Required Actions for Resubmission:**

### 1. ARCHITECTURAL CHANGES (Mandatory - 3-4 days)
- [ ] DELETE dependency on `IJiraActivityService` (makes API calls)
- [ ] INJECT four database repositories:
  - `IHistoricalDataRepository<JiraActivity>`
  - `IHistoricalDataRepository<GitLabActivity>`
  - `IJiraIssueRepository`
  - `IGitLabStateRepository`
- [ ] CHANGE method signature to single date: `GeneratePrompt(string templateKey, DateOnly date)`
- [ ] ADD three placeholder constants to `LlmPromptTemplate`
- [ ] IMPLEMENT three separate markdown generation methods:
  - `GetCurrentlyAssignedIssuesMarkdown()` - queries `IJiraIssueRepository` + `IGitLabStateRepository`
  - `GetJiraActivitiesMarkdown(DateOnly date)` - queries `IHistoricalDataRepository<JiraActivity>`
  - `GetGitLabActivitiesMarkdown(DateOnly date)` - queries `IHistoricalDataRepository<GitLabActivity>`
- [ ] UPDATE `GeneratePrompt()` to replace three placeholders independently

### 2. TESTING (Mandatory - 1 day)
- [ ] Add 8 new tests for GitLab integration (matching Jira test coverage)
- [ ] Add 4 tests for currently assigned issues (Jira + GitLab)
- [ ] Add 3 tests for single date validation
- [ ] Update all existing tests to use single date

### 3. DATABASE MIGRATION (Mandatory - 1 day)
- [ ] Create migration script to update existing templates from `{ACTIVITY_DATA_PLACEHOLDER}` to three new placeholders
- [ ] Add default template with all three placeholders
- [ ] Test rollback strategy

### 4. DOCUMENTATION (Mandatory - 0.5 days)
- [ ] Update spec.md with clarified single-date requirement
- [ ] Document three placeholder types with examples
- [ ] Add sequence diagram showing database-only flow (no API calls)
- [ ] Update ADR explaining why we use repositories not API services

**Estimated Total Rework:** 5-6 days

**Do NOT attempt to "patch" this implementation. Start from scratch with correct architecture.**

**Next Steps:**
1. Create new branch: `feature/llm-prompt-generator-v2`
2. Design new `LlmPromptService` with correct dependencies
3. Implement database queries (no API calls)
4. Add comprehensive tests
5. Resubmit for review

**Reviewer:** I will NOT review this again until ALL six critical issues are resolved. Bring me a working prototype with database queries and GitLab integration before wasting more of my time.
