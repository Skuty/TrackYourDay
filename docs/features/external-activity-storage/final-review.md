# Quality Gate Review: External Activity Storage & Resilience

## Final Verdict

**Status:** ❌ **REJECTED** - Critical defects remain unresolved

**Build Status:** ✅ Compiles (154 warnings, 0 errors)  
**Test Status:** ❌ 6 failures, 23 passed  

---

## Critical Issues (Auto-Fail Triggers)

### C1: ❌ INCOMPLETE - Circuit Breaker Implementation Half-Baked
- **Location:** `src/TrackYourDay.MAUI/ServiceRegistration/ExternalActivitiesServiceCollectionExtensions.cs:71-76`
- **Violation:** AC4 promises "Circuit Breaker stops requests after 5 consecutive failures" but implementation BUILDS A SECOND DI CONTAINER (`BuildServiceProvider()` at line 40) during startup to read settings, then registers Polly policies that will never be used because `GitLabRestApiClient` and `JiraRestApiClient` directly instantiate `HttpClient` instead of accepting `IHttpClientFactory`.
- **Evidence:** Search codebase for `new HttpClient()` in REST clients.
- **Fix:** 
  1. Refactor `*RestApiClient` to accept `IHttpClientFactory` in constructor
  2. Remove `BuildServiceProvider()` anti-pattern
  3. Add integration tests verifying circuit opens after threshold failures

### C2: ❌ CRITICAL DATA LOSS - "Today Only" Fetching Violates AC2
- **Location:** `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabActivityService.cs:39`, `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraActivityService.cs`
- **Violation:** AC2 promises "historical analysis" of append-only logs, but **ALL fetching is hard-coded to `DateTime.Today`**, meaning any activity older than 24 hours is permanently lost and AC2 is impossible.
- **Evidence:** 
  - `GetTodayActivitiesAsync()` only fetches `since: DateTime.Today`
  - No watermark persistence exists (M4 in review.md)
  - On app restart after 2 days, you lose 1 day of history
- **Fix:** Implement M4 watermark strategy or rename feature to "Daily Activity Snapshot" and remove AC2 from spec.

### C3: ❌ SECURITY - Plaintext Sensitive Data Violates OWASP
- **Location:** `src/TrackYourDay.MAUI/Infrastructure/Persistence/*.cs`
- **Violation:** GitLab/Jira payloads containing **usernames, project names, issue descriptions, commit messages** stored as JSON with:
  - Zero encryption (violates "Sensitive data encrypted at rest")
  - Hard-coded path `TrackYourDay.db` in working directory (not `%AppData%`)
  - No ACLs or per-user isolation
- **Evidence:** `GitLabActivityRepository.cs:34` - `JsonSerializer.Serialize(activity)` stored plaintext
- **OWASP:** A02:2021 – Cryptographic Failures
- **Fix:** 
  1. Move DB to `FileSystem.AppDataDirectory`
  2. Encrypt `DataJson` column using Windows DPAPI or AES-GCM
  3. Add integration tests verifying encryption at rest

---

## Major Defects (Must Fix Before Approval)

### M1: ❌ ZERO TEST COVERAGE for New Repositories
- **Location:** `Tests/`
- **Missing:**
  - No tests for `GitLabActivityRepository.TryAppendAsync()` duplicate detection
  - No tests for `JiraIssueRepository.UpdateCurrentStateAsync()` transactional update
  - No tests for `*FetchJob` retry logic after exception re-throw
  - No tests verifying deterministic GUID generation prevents duplicates
- **Required:** Minimum 80% coverage for business logic per project standards

### M2: ❌ PERFORMANCE - O(n²) HTTP Overhead in GitLab Fetching
- **Location:** `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabActivityService.cs`
- **Issue:** For each event (n), service makes additional HTTP call to fetch commit details, resulting in O(n²) API calls on every poll
- **Impact:** Will hit rate limits immediately on active repositories
- **Fix:** Batch commit lookups or use GraphQL endpoint to fetch events+commits in single request

### M3: ❌ UI CONFIGURATION MISSING - AC5 Impossible to Test
- **Location:** `src/TrackYourDay.Web/Pages/Settings.razor`
- **Violation:** AC5 promises "Settings menu with Request Interval input" but UI has no controls for:
  - `FetchIntervalMinutes` slider/input
  - `CircuitBreakerThreshold` input
  - Enable/Disable toggles for GitLab/Jira
- **Evidence:** Backend services can persist settings (C1 fixed) but frontend cannot set them
- **Fix:** Add MudBlazor `<MudNumericField>` components to Settings page per architecture.md mockup

---

## Minor Defects (Consider)

### m1: Build ServiceProvider Anti-Pattern Still Present
- **Location:** `ExternalActivitiesServiceCollectionExtensions.cs:40`
- **Issue:** `using var tempProvider = services.BuildServiceProvider()` instantiates second DI container during startup
- **Mitigation:** Review claims "acceptable for startup-only reads" but violates MS guidelines (CA1816)
- **Fix:** Pass settings via `IOptions<T>` or delay HttpClient registration to first use

### m2: Exception Swallowing Inconsistency
- **Location:** `JiraFetchJob.cs:52`, `GitLabFetchJob.cs:52`
- **Issue:** Review claims "m3 FIXED: Jobs re-throw exceptions" but catch blocks only `_logger.LogError()` then re-throw with no additional context
- **Best Practice:** Either handle gracefully (update last-run timestamp, set alarm) or don't catch at all

### m3: Deterministic GUID Implementation Weak
- **Location:** `GitLabActivityService.cs` (review claims C2 fixed)
- **Issue:** Using MD5 hash of `UpstreamId` string is collision-prone if IDs are non-unique across projects
- **Example:** `"gitlab-commit-123-abc123"` could collide between Project A and Project B if both use commit SHA "abc123"
- **Fix:** Include project/workspace namespace in hash input

---

## Test Failures Blocking Approval

```
FAILED: GitLabRestApiClientTests.GivenUserIsAuthenticated_WhenGettingGitLabProject_ThenProjectIsSerializedProperly
FAILED: GitLabRestApiClientTests.GivenUserIsAuthenticated_WhenGettingGitLabCommits_ThenCommitsAreSerializedProperly
ERROR: Response status code does not indicate success: 401 (Unauthorized)
```

**Root Cause:** Tests assume live GitLab credentials but settings service returns empty/invalid API key  
**Fix Required:** Mock `IGitLabRestApiClient` in tests OR use integration test category trait

---

## Alignment to Specification

| Requirement | Status | Notes |
|------------|--------|-------|
| **AC1: GitLab Append-Only** | ⚠️ Partial | Deduplication works but only for `DateTime.Today` data |
| **AC2: Jira Event Sourcing** | ❌ Failed | "Historical analysis" impossible with daily-only fetching |
| **AC3: Jira Current State** | ✅ Pass | `JiraIssueRepository.UpdateCurrentStateAsync()` transaction implemented |
| **AC4: Circuit Breaker** | ❌ Failed | Polly registered but never used by REST clients |
| **AC5: Throttling Config** | ❌ Failed | Backend ready, UI missing, end-to-end untested |

**Score: 1.5/5 acceptance criteria met**

---

## Breaking Changes Introduced

1. ✅ **Documented:** `IGitLabActivityService.GetTodayActivities()` → `GetTodayActivitiesAsync(CancellationToken)`
2. ❌ **Undocumented:** `GitLabRestApiClient` constructor signature unchanged despite architecture.md promising HttpClientFactory refactor
3. ❌ **Breaking:** Settings now require `CircuitBreakerThreshold` and `CircuitBreakerDurationMinutes` fields but no migration/defaults provided

---

## Final Justification

**This implementation violates THREE auto-fail triggers:**

1. **Blocking async calls** (C4) - CLAIMED fixed but untested
2. **Hardcoded secrets** (C3) - Plaintext sensitive data in SQLite
3. **Zero unit tests for business logic** (M1) - New repositories untested

**Additionally:**
- AC2's core promise ("historical analysis") is architecturally impossible with current `DateTime.Today` fetching
- AC4's circuit breaker is registered but never invoked (dead code)
- AC5's UI is missing, making feature untestable by users

**Recommendation:** 
Return to implementation phase. Address C1-C3 critical defects, add repository test coverage, and implement watermark strategy OR descope AC2 from specification.
