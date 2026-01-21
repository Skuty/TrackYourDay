# GitLab/Jira Tracker Consolidation - Implementation Summary

## Overview
Successfully consolidated GitLabTracker/GitLabFetchJob and JiraTracker/JiraFetchJob into unified tracker implementations with dual deduplication validation.

## Changes Implemented

### 1. Shared Abstractions (`src/TrackYourDay.Core/ApplicationTrackers/Shared/`)
- **IExternalActivityTracker<TActivity>**: Unified interface for external activity trackers
- **IHasDeterministicGuid**: Marker interface for activities with GUID generation
- **IHasOccurrenceDate**: Marker interface for activities with timestamps

### 2. Activity Models Updated
- **GitLabActivity**: Implements `IHasDeterministicGuid`, `IHasOccurrenceDate`
- **JiraActivity**: Implements `IHasDeterministicGuid`, `IHasOccurrenceDate`

### 3. GitLabTracker Refactored
**Before:**
- In-memory list of published activities
- Timestamp-based deduplication only
- Manual watermark tracking with `IGenericSettingsService`

**After:**
- Repository-based persistence (`IGitLabActivityRepository`)
- **Primary**: GUID-based deduplication (database-enforced)
- **Secondary**: Timestamp validation (consistency check)
- Watermark tracking via `IGitLabSettingsService`
- Publishes `GitLabActivityDiscoveredEvent` for new activities
- Logs warnings when deduplication strategies disagree

**Key Method:**
```csharp
Task<int> RecognizeActivitiesAsync(CancellationToken ct)
```

### 4. JiraTracker Refactored
**Before:**
- In-memory list with manual add
- Time-based polling logic (5-minute intervals)
- No persistence

**After:**
- Repository-based persistence (`IJiraActivityRepository`)
- **Primary**: GUID-based deduplication (database-enforced)
- **Secondary**: Timestamp validation (consistency check)
- Watermark tracking via `IJiraSettingsService`
- No event publishing (consistent with original)
- Logs warnings when deduplication strategies disagree

**Key Method:**
```csharp
Task<int> RecognizeActivitiesAsync(CancellationToken ct)
```

### 5. Jobs Simplified
**GitLabFetchJob:**
```csharp
// Before: 67 lines with full logic
// After: 29 lines - thin Quartz wrapper
public async Task Execute(IJobExecutionContext context)
{
    var newActivityCount = await _tracker.RecognizeActivitiesAsync(context.CancellationToken);
    // Logging only
}
```

**JiraFetchJob:**
```csharp
// Before: 78 lines with activity + issue logic
// After: 56 lines - delegates activity tracking to JiraTracker, handles issues separately
```

### 6. Service Registrations Updated
- `GitLabTracker`: Now requires `IGitLabActivityRepository`, `IGitLabSettingsService`
- `JiraTracker`: Now requires `IJiraActivityRepository`, `IJiraSettingsService`
- Jobs inject trackers instead of raw services

### 7. JiraEnrichedSummaryStrategy Updated
- Uses `GetActivitiesAsync(fromDate, toDate)` instead of `GetJiraActivities()`
- Fetches only relevant date range from repository

## Dual Validation Logic

### Primary: GUID-Based (Database-Enforced)
```sql
CREATE TABLE GitLabActivities (
    Guid TEXT PRIMARY KEY,  -- Enforces uniqueness
    ...
);

INSERT OR IGNORE INTO GitLabActivities (...) VALUES (...);
-- Returns 0 rows affected if Guid exists
```

### Secondary: Timestamp-Based (Validation)
```csharp
var isNewByGuid = await _repository.TryAppendAsync(activity, ct);
var isNewByTimestamp = activity.OccurrenceDate > watermark;

if (isNewByGuid != isNewByTimestamp)
{
    _logger.LogWarning("Deduplication mismatch...");
}
```

### Warning Scenarios
1. **GUID=NEW, Timestamp=OLD**: Activity inserted but occurred before watermark (possible clock skew, event reprocessing)
2. **GUID=DUPLICATE, Timestamp=NEW**: Activity blocked but occurred after watermark (watermark drift, delayed sync)

## Benefits

### 1. Single Source of Truth
- All activity tracking logic in `*Tracker` classes
- Jobs are thin orchestration wrappers

### 2. Consistency
- Both trackers follow same pattern
- Easier to maintain and extend

### 3. Observability
- Mismatch warnings highlight data integrity issues
- Can monitor watermark drift
- Can detect event reprocessing

### 4. Testability
- Trackers can be unit tested independently
- Jobs have minimal logic to test

### 5. Repository-Based Storage
- All activities persisted immediately
- No in-memory state loss on crashes
- Query by date range via `GetActivitiesAsync()`

## Breaking Changes

### API Changes
- `GitLabTracker.RecognizeActivity()` → `RecognizeActivitiesAsync(CancellationToken)` (returns `Task<int>`)
- `GitLabTracker.GetGitLabActivities()` → `GetActivitiesAsync(DateOnly, DateOnly, CancellationToken)`
- `JiraTracker.RecognizeActivity()` → `RecognizeActivitiesAsync(CancellationToken)` (returns `Task<int>`)
- `JiraTracker.GetJiraActivities()` → `GetActivitiesAsync(DateOnly, DateOnly, CancellationToken)`

### Constructor Changes
```csharp
// GitLabTracker - OLD
GitLabTracker(IGitLabActivityService, IClock, IPublisher, IGenericSettingsService, ILogger)

// GitLabTracker - NEW
GitLabTracker(IGitLabActivityService, IGitLabActivityRepository, IGitLabSettingsService, IPublisher, ILogger)

// JiraTracker - OLD
JiraTracker(IJiraActivityService, IClock)

// JiraTracker - NEW
JiraTracker(IJiraActivityService, IJiraActivityRepository, IJiraSettingsService, ILogger)
```

## Test Updates Required

### GitLabTrackerTests
- [ ] Update constructor calls with new dependencies (mock `IGitLabActivityRepository`, `IGitLabSettingsService`)
- [ ] Replace `RecognizeActivity()` with `RecognizeActivitiesAsync()`
- [ ] Replace `GetGitLabActivities()` with `GetActivitiesAsync(fromDate, toDate)`
- [ ] Add tests for deduplication mismatch warnings
- [ ] Add tests for watermark updates

### JiraTrackerTests
- [ ] Update constructor calls with new dependencies (mock `IJiraActivityRepository`, `IJiraSettingsService`)
- [ ] Replace `RecognizeActivity()` with `RecognizeActivitiesAsync()`
- [ ] Replace `GetJiraActivities()` with `GetActivitiesAsync(fromDate, toDate)`
- [ ] Remove tests for 5-minute polling logic (now handled by Quartz)

## Next Steps

1. **Update GitLabTrackerTests** (11 tests failing)
2. **Update JiraTrackerTests** (3 tests failing)
3. **Run full test suite** to verify functionality
4. **Test deduplication mismatch scenarios** (manual/integration)
5. **Update documentation** if needed

## Files Modified

### Created
- `src/TrackYourDay.Core/ApplicationTrackers/Shared/IExternalActivityTracker.cs`
- `src/TrackYourDay.Core/ApplicationTrackers/Shared/IHasDeterministicGuid.cs`
- `src/TrackYourDay.Core/ApplicationTrackers/Shared/IHasOccurrenceDate.cs`

### Modified
- `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabActivityService.cs`
- `src/TrackYourDay.Core/ApplicationTrackers/GitLab/GitLabTracker.cs`
- `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraActivityService.cs`
- `src/TrackYourDay.Core/ApplicationTrackers/Jira/JiraTracker.cs`
- `src/TrackYourDay.MAUI/BackgroundJobs/ExternalActivities/GitLabFetchJob.cs`
- `src/TrackYourDay.MAUI/BackgroundJobs/ExternalActivities/JiraFetchJob.cs`
- `src/TrackYourDay.Core/ServiceRegistration/ServiceCollections.cs`
- `src/TrackYourDay.Core/Insights/Analytics/JiraEnrichedSummaryStrategy.cs`

### Tests Requiring Updates
- `Tests/TrackYourDay.Tests/ApplicationTrackers/GitLab/GitLabTrackerTests.cs`
- `Tests/TrackYourDay.Tests/ApplicationTrackers/Jira/JiraTrackerTests.cs`

## Build Status
✅ **Production code compiles successfully**  
⚠️ **Test code requires updates** (expected)
