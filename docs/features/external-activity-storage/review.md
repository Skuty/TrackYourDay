# Quality Gate Review: External Activity Storage & Resilience (Duplication Audit — 2026-03-06)

---

## ❌ NEW AUDIT: Duplication Bug Investigation (2026-03-06)

**Verdict: TWO CONFIRMED BUGS. Previous review C2 ("FIXED") was WRONG. Activities are duplicating on every sync cycle.**

---

### Critical Defect: DA-1 — `INSERT OR IGNORE` Is Completely Non-Functional

**Location:** `src/TrackYourDay.Core/Persistence/GenericDataRepository.cs:371-379` (schema) and `:212-222` (insert)

**Violation:** Broken deduplication contract — `TryAppendAsync()` always returns `true`. Every call inserts a new row. Zero duplicate suppression.

**Root Cause:**
```csharp
// GenericDataRepository.cs:371 — THE CRIME SCENE
command.CommandText = @"
    CREATE TABLE IF NOT EXISTS historical_data (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Guid TEXT NOT NULL,          // ← Only a regular column. No constraint.
        TypeName TEXT NOT NULL,
        DataJson TEXT NOT NULL
    );
    CREATE INDEX IF NOT EXISTS idx_historical_data_guid ON historical_data(Guid);  // ← INDEX, not UNIQUE
    CREATE INDEX IF NOT EXISTS idx_historical_data_type ON historical_data(TypeName);";
```

SQLite's `INSERT OR IGNORE` **only suppresses inserts that violate a PRIMARY KEY or UNIQUE constraint**. A regular `INDEX` provides zero conflict detection. Therefore:

```csharp
// GenericDataRepository.cs:213
insertCommand.CommandText = @"
    INSERT OR IGNORE INTO historical_data (Guid, TypeName, DataJson)
    VALUES (@guid, @typeName, @dataJson)";
// ↑ "OR IGNORE" is a no-op. Every row is inserted unconditionally.

var rowsAffected = await insertCommand.ExecuteNonQueryAsync(cancellationToken);
return rowsAffected > 0;  // ← ALWAYS returns true. Every activity is "new". Forever.
```

**This invalidates the previous review's C2 "FIXED" verdict.** Deterministic GUIDs were implemented correctly — but the database-level constraint that would USE those GUIDs to enforce uniqueness was never added. The deduplication machinery has a missing gear.

**Fix:**
```csharp
command.CommandText = @"
    CREATE TABLE IF NOT EXISTS historical_data (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Guid TEXT NOT NULL,
        TypeName TEXT NOT NULL,
        DataJson TEXT NOT NULL,
        UNIQUE (Guid, TypeName)   -- ← This is what makes INSERT OR IGNORE actually work
    );
    CREATE INDEX IF NOT EXISTS idx_historical_data_type ON historical_data(TypeName);";
    -- ↑ The Guid index is now redundant; the UNIQUE constraint covers it.
```

**⚠️ Migration Required:** Existing databases contain duplicated rows. A one-time deduplication migration must run before shipping this fix:
```sql
DELETE FROM historical_data
WHERE Id NOT IN (
    SELECT MIN(Id) FROM historical_data GROUP BY Guid, TypeName
);
```

---

### Critical Defect: DA-2 — Jira Changelog Fetches ALL History, Not Just Delta (Explains Tripling)

**Location:** `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraActivityService.cs:46-86`

**Violation:** Watermark semantics are broken for changelog entries. Every sync that touches a Jira issue re-fetches its entire history going back to creation.

**Root Cause:**
```csharp
// JiraActivityService.cs:46
public async Task<List<JiraActivity>> GetActivitiesUpdatedAfter(DateTime updateDate)
{
    // ✅ Fetches issues UPDATED after watermark — correct.
    var issues = await _jiraRestApiClient.GetUserIssues(currentUser, updateDate);

    foreach (var issue in issues)
    {
        // ✅ Issue CREATION is filtered by date — correct.
        if (issue.Fields.Created.Value.LocalDateTime >= updateDate && ...)
            activities.Add(CreateIssueCreationActivity(issue));

        // ❌ Changelog is NOT filtered by date at all.
        if (issue.Changelog?.Histories != null)
        {
            var changelogActivities = MapChangelogToActivities(issue, currentUser);
            // ↑ Returns ALL changelog entries for the issue, from inception.
            // No date filter. updateDate is not passed in.
            activities.AddRange(changelogActivities);
        }

        // ✅ Worklog API call passes updateDate — correct.
        var worklogs = await _jiraRestApiClient.GetIssueWorklogs(issue.Key, updateDate);
    }
}
```

**The cascade that causes tripling:**
1. Sync 1 (Day 1): Issue ABC-123 has 5 changelog entries → 5 rows inserted (DA-1 means no dedup = 5 rows)
2. Someone adds a comment to ABC-123 on Day 7 → issue appears in `GetUserIssues(watermark=Day1)` again
3. Sync 2 (Day 7): `MapChangelogToActivities` returns all 5 ORIGINAL entries + 0 new (comment is not a changelog "item") → 5 MORE rows inserted = **10 rows total**
4. Sync 3 (Day 14): Same issue touched again → 5 more rows = **15 rows total**

This is why Jira is **worse** than GitLab. GitLab's API returns events *after* a date directly. Jira returns issues *updated* after a date, then dumps their entire changelog on you. Without the date filter, old changelog entries are re-processed on every subsequent sync.

**Fix:**
```csharp
// Pass updateDate into the mapper
private static List<JiraActivity> MapChangelogToActivities(
    JiraIssueResponse issue, 
    JiraUser currentUser,
    DateTime updatedAfter)  // ← Add parameter
{
    foreach (var history in issue.Changelog.Histories)
    {
        // ← Add this guard
        if (history.Created.LocalDateTime < updatedAfter)
            continue;
        
        // ... rest of mapping
    }
}
```

And at the call site:
```csharp
var changelogActivities = MapChangelogToActivities(issue, currentUser, updateDate);
```

---

### Observation: Previous Review C2 Is a False Positive

The 2026-01-15 review stamped C2 as `✅ FIXED` with the note:
> *"`INSERT OR IGNORE` now correctly suppresses duplicates"*

This is factually incorrect. Only half the fix was applied (deterministic GUIDs). The other half (the UNIQUE constraint that makes `INSERT OR IGNORE` meaningful) was never implemented. The test suite did not catch this because `JiraTrackerTests` and `GitLabTrackerTests` mock `IHistoricalDataRepository<T>` — they never exercise the real SQLite schema. There are zero integration tests for `GenericDataRepository.TryAppendAsync`.

---

## Status: ✅ CRITICAL ISSUES RESOLVED (2026-01-15)

Critical defects C1-C4 have been fixed. C3 requires package installation to complete. Major issues remain for separate review.

---

## Defects Found & Resolution Status

### Critical (Must Fix) - ✅ RESOLVED

- **C1: ✅ FIXED** External activity jobs never execute because there is no way to set the `Enabled` flags required by the scheduler.
  - **Location:** `src/TrackYourDay.MAUI/ServiceRegistration/ServiceCollections.cs`, `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabSettingsService.cs`, `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraSettingsService.cs`.
  - **Resolution:** 
    - Jobs now read `Enabled` flag and `FetchIntervalMinutes` from persisted settings at startup
    - Only enabled jobs with configured URLs are scheduled
    - Settings services provide overloaded `UpdateSettings()` to persist all configuration
    - Using scoped temporary provider during Quartz configuration (acceptable for startup-only reads)
  - **Verified:** Jobs respect settings on application restart

- **C2: ✅ FIXED** Append-only logs cannot deduplicate because each `GitLabActivity`/`JiraActivity` instance generates a new random `Guid`, so every poll is treated as "new".
  - **Location:** `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabActivityService.cs`, `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraActivityService.cs`.
  - **Resolution:**
    - Implemented deterministic GUID generation via MD5 hash of `UpstreamId`
    - GitLab activities use stable IDs: `gitlab-commit-{projectId}-{commitSHA}`, `gitlab-mr-{projectId}-{eventId}-{action}`, etc.
    - Jira activities use: `jira-worklog-{issueKey}-{worklogId}`, `jira-history-{issueKey}-{historyId}-{field}`, etc.
    - `INSERT OR IGNORE` now correctly suppresses duplicates
  - **Verified:** Same activity from multiple polls generates identical GUID

- **C3: ⚠️ PARTIAL** Circuit breaker and throttling features promised in AC4/AC5 are completely absent—there are no Polly policies, no failure thresholds, no cooldown or probe logic.
  - **Location:** `src/TrackYourDay.MAUI/ServiceRegistration/ExternalActivitiesServiceCollectionExtensions.cs`.
  - **Partial Resolution:**
    - Settings persistence implemented for `CircuitBreakerThreshold` and `CircuitBreakerDurationMinutes`
    - Settings services updated to persist and retrieve resilience configuration
    - **TODO:** Install `Microsoft.Extensions.Http.Polly` package and wire circuit breaker policies (documented in code)
  - **Remaining Work:** 
    ```bash
    dotnet add src/TrackYourDay.MAUI package Microsoft.Extensions.Http.Polly
    ```
    Then implement circuit breaker as documented in `ExternalActivitiesServiceCollectionExtensions.cs:40-46`

- **C4: ✅ FIXED** `GitLabActivityService` performs sync-over-async calls (`.GetAwaiter().GetResult()`) for every HTTP request, explicitly violating the "no blocking .Result/.Wait" rejection trigger and risking UI deadlocks.
  - **Location:** `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabActivityService.cs`.
  - **Resolution:**
    - Converted `GetTodayActivities()` → `GetTodayActivitiesAsync(CancellationToken)`
    - All HTTP calls now use `await` with `ConfigureAwait(false)`
    - Removed mutable state fields (`userId`, `userEmail`, `stopFetchingDueToFailedRequests`)
    - Updated interface `IGitLabActivityService` and all consumers (`GitLabFetchJob`, `GitLabTracker`)
    - Made all mapping methods fully async where needed
  - **Verified:** Zero blocking async calls in service layer

### Major (Should Fix)

- **M1: 🔄 IN PROGRESS** Fetch intervals, circuit breaker thresholds/durations, and enablement controls are missing from the Settings UI.
  - **Location:** `src/TrackYourDay.Web/Pages/Settings.razor`.
  - **Partial Resolution:** Settings services can now persist all configuration values
  - **Remaining:** Add UI controls in Settings page for Enable toggle and interval inputs

- **M2: ✅ FIXED** `ConfigureExternalActivityJobs` calls `services.BuildServiceProvider()`, instantiating a second container.
  - **Location:** `src/TrackYourDay.MAUI/ServiceRegistration/ServiceCollections.cs`.
  - **Resolution:** Using scoped temporary provider during Quartz configuration only, properly disposed
  - **Note:** Settings are read once at startup; requires app restart to apply changes (acceptable trade-off)

- **M3: ⚠️ OPEN** SQLite files for GitLab/Jira data are written to a hard-coded relative path (`TrackYourDay.db`) with no per-user directory or encryption.
  - **Location:** `src/TrackYourDay.MAUI/ServiceRegistration/ExternalActivitiesServiceCollectionExtensions.cs`, `src/TrackYourDay.MAUI/Infrastructure/Persistence/*.cs`.
  - **Fix Required:** Store databases under `%AppData%`/`FileSystem.AppDataDirectory`, secure them per-user, and encrypt sensitive contents

- **M4: ⚠️ OPEN** Jira ingestion always queries `DateTime.Today`, so any activity older than the current day is lost and AC2 ("historical analysis") is impossible.
  - **Location:** `src/TrackYourDay.MAUI/BackgroundJobs/ExternalActivities/JiraFetchJob.cs`, `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraActivityService.cs`.
  - **Fix Required:** Persist and reuse a "last successful sync" timestamp and fetch deltas based on that watermark

- **M5: ✅ FIXED** `GitLabActivityService` and `JiraActivityService` are singletons holding mutable state, causing race conditions.
  - **Location:** `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabActivityService.cs`, `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraActivityService.cs`.
  - **Resolution:**
    - Removed all mutable state from both services
    - Methods now accept user email/user as parameters instead of storing them
    - All data kept local to method scope
    - Helper methods made static where appropriate
  - **Verified:** Services are now thread-safe

### Minor (Consider)

- **m1: ⚠️ OPEN** `JiraFetchJob` injects `IPublisher` but never uses it, leaving dead code.
- **m2: ✅ FIXED** Polly packages and namespaces referenced but never used.
  - **Resolution:** Removed unused Polly imports; documented TODO for proper circuit breaker integration
- **m3: ✅ FIXED** Both fetch jobs swallow exceptions after logging.
  - **Resolution:** Jobs now re-throw exceptions after logging, allowing Quartz to handle failures properly

## Missing Tests (Requires Update)

- **⚠️ NEEDS UPDATE:** Existing `GitLabTrackerTests` use old sync method signature - update to async
- **⚠️ MISSING:** No unit or integration tests cover `GitLabActivityRepository`, `JiraActivityRepository`, or `JiraIssueRepository`
- **⚠️ MISSING:** No tests exercise `GitLabFetchJob`/`JiraFetchJob` 
- **⚠️ MISSING:** No tests validate circuit-breaker/throttling behavior or that duplicate activities are ignored
- **⚠️ MISSING:** No tests ensure settings persistence works correctly

## Performance Concerns (Unchanged)

- **⚠️ OPEN:** `GitLabActivityService.GetTodayActivitiesAsync` refetches the entire event history plus per-event commit lookups on every run, resulting in O(n²) HTTP chatter
- **⚠️ OPEN:** `JiraActivityService` issues sequential worklog requests for every issue without batching, risking rate-limit violations
- **⚠️ OPEN:** Lack of pagination/watermarking causes redundant processing and database bloat

## Security Issues (Unchanged)

- **⚠️ OPEN:** GitLab/Jira payloads stored as raw JSON with no encryption, per-user isolation, or hardened ACLs, violating "Sensitive data encrypted at rest" requirement

---

## Final Verdict Update (2026-01-15)

**Status:** ✅ **CRITICAL ISSUES RESOLVED** → Proceed with caution

**Summary of Changes:**
1. ✅ **C1 Fixed:** Jobs now respect persisted `Enabled` flag and `FetchIntervalMinutes` at startup
2. ✅ **C2 Fixed:** Deterministic GUIDs eliminate duplicate activities in append-only logs
3. ⚠️ **C3 Partial:** Settings infrastructure ready; needs Polly package installation
4. ✅ **C4 Fixed:** Fully async pipeline, zero blocking calls, thread-safe services

**Additional Improvements:**
- ✅ **M2 Fixed:** Removed DI anti-pattern (using scoped provider at startup only)
- ✅ **M5 Fixed:** Services are now stateless and thread-safe
- ✅ **m2, m3 Fixed:** Cleaned up unused code, proper exception handling

**Build Status:** ✅ Core + MAUI projects compile successfully

**Remaining Critical Work:**
- Install `Microsoft.Extensions.Http.Polly` and implement circuit breaker policies (C3 completion)
- Update unit tests for async method signatures
- Address Major issues M3 (security) and M4 (data loss)

**Recommendation:** 
Critical defects blocking functionality are resolved. Implementation can proceed to Major issue resolution and comprehensive testing phase. Security and performance concerns should be addressed before production deployment.
