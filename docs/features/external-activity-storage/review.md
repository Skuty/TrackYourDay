# Quality Gate Review: External Activity Storage & Resilience

## Defects Found

### Critical (Must Fix)
- **C1:** External activity jobs never execute because there is no way to set the `Enabled` flags required by the scheduler.
  - **Location:** `src/TrackYourDay.MAUI/ServiceRegistration/ServiceCollections.cs:52-80`, `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabSettingsService.cs:17-41`, `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraSettingsService.cs:17-41`, `src/TrackYourDay.Web/Pages/Settings.razor:22-37`.
  - **Violation:** Spec AC1–AC5 rely on the jobs running; Dependency Injection/configuration best practices.
  - **Fix:** Add enable toggles per integration, persist `GitLab.Enabled`/`Jira.Enabled`, and wire Quartz scheduling to the persisted values without spinning up ad-hoc service providers.

- **C2:** Append-only logs cannot deduplicate because each `GitLabActivity`/`JiraActivity` instance generates a new random `Guid`, so every poll is treated as “new”.
  - **Location:** `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabActivityService.cs:5-8`, `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraActivityService.cs:5-8`, `src/TrackYourDay.MAUI/BackgroundJobs/ExternalActivities/GitLabFetchJob.cs:35-45`, `src/TrackYourDay.MAUI/BackgroundJobs/ExternalActivities/JiraFetchJob.cs:47-54`.
  - **Violation:** AC1 & AC2 (“duplicate activities ignored”) and append-only log requirements.
  - **Fix:** Use deterministic upstream identifiers (GitLab event IDs, Jira history/worklog IDs) for repository keys so `INSERT OR IGNORE` actually suppresses duplicates.

- **C3:** Circuit breaker and throttling features promised in AC4/AC5 are completely absent—there are no Polly policies, no failure thresholds, no cooldown or probe logic, and the interval settings are never persisted.
  - **Location:** `src/TrackYourDay.MAUI/ServiceRegistration/ExternalActivitiesServiceCollectionExtensions.cs:38-65`, `src/TrackYourDay.MAUI/BackgroundJobs/ExternalActivities/GitLabFetchJob.cs:29-54`, `src/TrackYourDay.MAUI/BackgroundJobs/ExternalActivities/JiraFetchJob.cs:35-66`, `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabSettingsService.cs:17-46`, `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraSettingsService.cs:17-46`.
  - **Violation:** AC4 & AC5 plus the architecture doc’s Polly requirement.
  - **Fix:** Add Polly circuit breaker/retry handlers to the named HttpClients, track consecutive failures with a configurable cooldown/probe, and persist the request interval/threshold/duration settings so jobs respect them.

- **C4:** `GitLabActivityService` performs sync-over-async calls (`.GetAwaiter().GetResult()`) for every HTTP request, explicitly violating the “no blocking .Result/.Wait” rejection trigger and risking UI deadlocks.
  - **Location:** `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabActivityService.cs:38-69` and `129-189`.
  - **Violation:** Async best practices and the stated rejection trigger.
  - **Fix:** Make the fetch pipeline fully asynchronous (await the HttpClient calls, return `Task<List<GitLabActivity>>`, and update the jobs to await it).

### Major (Should Fix)
- **M1:** Fetch intervals, circuit breaker thresholds/durations, and enablement controls are missing from both the Settings UI and the settings services, so users cannot satisfy AC5 or the documented UI requirements even if the runtime logic existed.
  - **Location:** `src/TrackYourDay.Web/Pages/Settings.razor:22-37 & 226-238`, `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabSettingsService.cs:17-46`, `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraSettingsService.cs:17-46`.
  - **Fix:** Add the “External Integrations” section (toggle + numeric interval inputs), persist the new values, and reload them on startup.

- **M2:** `ConfigureExternalActivityJobs` calls `services.BuildServiceProvider()`, instantiating a second container, duplicating singletons (settings repo, loggers) and freezing settings at startup.
  - **Location:** `src/TrackYourDay.MAUI/ServiceRegistration/ServiceCollections.cs:52-80`.
  - **Violation:** Dependency Inversion / DI lifetime rules.
  - **Fix:** Use Quartz configuration APIs with `IOptions`/`IConfiguration` instead of spinning up a throwaway provider.

- **M3:** SQLite files for GitLab/Jira data are written to a hard-coded relative path (`TrackYourDay.db`) with no per-user directory or encryption, despite the security checklist’s “sensitive data encrypted at rest” requirement.
  - **Location:** `src/TrackYourDay.MAUI/ServiceRegistration/ExternalActivitiesServiceCollectionExtensions.cs:13-33`, `src/TrackYourDay.MAUI/Infrastructure/Persistence/*.cs`.
  - **Fix:** Store databases under `%AppData%`/`FileSystem.AppDataDirectory`, secure them per-user, and encrypt or otherwise protect the contents.

- **M4:** Jira ingestion always queries `DateTime.Today`, so any activity older than the current day is lost and AC2 (“historical analysis”) is impossible.
  - **Location:** `src/TrackYourDay.MAUI/BackgroundJobs/ExternalActivities/JiraFetchJob.cs:41-58`, `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraActivityService.cs:29-88`.
  - **Fix:** Persist and reuse a “last successful sync” timestamp and fetch deltas based on that watermark rather than midnight each day.

- **M5:** `GitLabActivityService` and `JiraActivityService` are singletons holding mutable state (`gitlabActivities`, `currentUser`, `stopFetching...`), so multiple consumers (trackers + Quartz jobs) race on shared data and the classes violate SRP.
  - **Location:** `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabActivityService.cs:14-26`, `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraActivityService.cs:18-33`.
  - **Fix:** Remove mutable state (or change lifetimes), keep per-call data local, and split fetching from stateful caching.

### Minor (Consider)
- **m1:** `JiraFetchJob` injects `IPublisher` but never uses it (`src/TrackYourDay.MAUI/BackgroundJobs/ExternalActivities/JiraFetchJob.cs:12-33`), leaving dead code and indicating unfinished event propagation.
- **m2:** Polly packages and namespaces are referenced but never used (`TrackYourDay.MAUI/ServiceRegistration/ExternalActivitiesServiceCollectionExtensions.cs:1-4`, `src/TrackYourDay.MAUI/TrackYourDay.MAUI.csproj:61`), creating misleading dependencies.
- **m3:** Both fetch jobs swallow exceptions after logging, so Quartz believes the job succeeded and cannot trigger any recovery workflow, masking operational failures.

## Missing Tests
- No unit or integration tests cover `GitLabActivityRepository`, `JiraActivityRepository`, or `JiraIssueRepository` (dedupe guarantees, transactions, schema creation).
- No tests exercise `GitLabFetchJob`/`JiraFetchJob` (publishing events, updating current-state tables, honoring settings).
- No tests validate the promised circuit-breaker/throttling behavior or that duplicate activities are ignored.
- No tests ensure the new settings (intervals, thresholds, enable toggles) are persisted and reloaded correctly.

## Performance Concerns
- `GitLabActivityService.GetTodayActivities` refetches the entire event history plus per-event commit lookups on every run (`GitLabActivityService.cs:35-199`), resulting in O(n²) HTTP chatter and making throttling impossible.
- `JiraActivityService` issues sequential worklog requests for every issue (`JiraActivityService.cs:64-77`) without any delay or batching, guaranteeing rate-limit violations once AC5 is honored.
- Lack of pagination/watermarking for Jira activities (always querying “today”) causes redundant processing and accelerates database bloat.

## Security Issues
- GitLab/Jira payloads (issue summaries, comments, worklogs) are stored as raw JSON inside `TrackYourDay.db` in the application directory with no encryption, per-user isolation, or hardened ACLs (`ExternalActivitiesServiceCollectionExtensions.cs:13-33`, `Infrastructure/Persistence/*.cs`), violating the security checklist item “Sensitive data encrypted at rest”.

## Final Verdict
**Status:** ❌ REJECTED

**Justification:** Core acceptance criteria (storage, resilience, throttling) are unmet—the jobs never run, duplicates flood the “append-only” tables, circuit breaker logic is missing, and the implementation blocks on async calls while introducing security regressions.

**Conditions (if applicable):**
- Implement real enable/interval settings, deterministic identifiers, and the specified circuit breaker/throttling behavior with accompanying tests before requesting re-review.
- Address the async/thread-safety issues and move persisted data to a secure, per-user location.
