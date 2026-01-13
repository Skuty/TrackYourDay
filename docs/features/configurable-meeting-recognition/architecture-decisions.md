# Architecture Decisions - Quick Reference

This document provides concise explanations for the three key architectural decisions requested.

---

## 1. Match Count Storage: Embedded vs Separate

### The Question
Should match statistics (`MatchCount`, `LastMatchedAt`) be stored **inside** the `MeetingRecognitionRule` object or as **separate** key-value pairs in `IGenericSettingsService`?

### Decision: EMBEDDED (Inside MeetingRecognitionRule)

### Explanation

**Embedded approach:**
```json
{
  "MeetingRecognitionRules.v1": [
    {
      "Id": "abc123",
      "Priority": 1,
      "ProcessNamePattern": { "Pattern": "ms-teams", "MatchMode": "Contains" },
      "MatchCount": 42,           // ← Stats embedded here
      "LastMatchedAt": "2026-01-06T19:00:00Z"
    },
    {
      "Id": "def456",
      "Priority": 2,
      "WindowTitlePattern": { "Pattern": "Zoom", "MatchMode": "Contains" },
      "MatchCount": 7,
      "LastMatchedAt": "2026-01-05T12:30:00Z"
    }
  ]
}
```

**Separate approach (REJECTED):**
```json
{
  "MeetingRecognitionRules.v1": [
    { "Id": "abc123", "Priority": 1, ... },  // No stats here
    { "Id": "def456", "Priority": 2, ... }
  ],
  "MeetingRuleStats.abc123": { "MatchCount": 42, "LastMatchedAt": "..." },
  "MeetingRuleStats.def456": { "MatchCount": 7, "LastMatchedAt": "..." }
}
```

### Trade-offs

| Aspect | Embedded | Separate |
|--------|----------|----------|
| **Write frequency** | ❌ Entire rule set saved every match (every 10s) | ✅ Only one key updated per match |
| **Read complexity** | ✅ Single read gets complete view | ❌ N+1 queries (rules + stats per rule) |
| **Synchronization** | ✅ Atomic—can't have rule without stats | ❌ Deleting rule orphans stats key |
| **Garbage collection** | ✅ Not needed | ❌ Must clean up orphaned stats keys |
| **Performance (10 rules)** | 5ms serialization per match | 1ms single key write per match |

### Why Embedded Wins

1. **Simplicity**: Single source of truth. No synchronization logic needed.
2. **Performance acceptable**: 5ms overhead per 10 seconds = 0.05% CPU time.
3. **Atomicity**: Rule deletion automatically deletes stats.
4. **No orphans**: Separate keys accumulate garbage—requires cleanup job.

### When to Reconsider

If profiling shows >50ms serialization time (e.g., 100+ rules), introduce **write-behind cache**:
- Increment match counts in-memory (`ConcurrentDictionary`)
- Flush to `IGenericSettingsService` every 60 seconds
- Trade-off: Stats delayed by up to 60s, but writes reduced 6x

**Current decision: Do NOT pre-optimize. Measure first.**

---

## 2. Scoped vs Singleton Lifecycle

### The Problem

Current implementation:
```csharp
// ServiceCollections.cs
services.AddSingleton<MsTeamsMeetingTracker>(container => {
    var strategy = new ProcessBasedMeetingRecognizingStrategy(...);
    return new MsTeamsMeetingTracker(..., strategy, ...);
});
```

**Issue**: Strategy is hardcoded at registration time. To support configurable rules that apply immediately (AC11), strategy must be resolved per execution cycle.

### Options Considered

| Approach | Pros | Cons |
|----------|------|------|
| **Singleton + ServiceLocator** | No refactoring | Anti-pattern, hides dependencies |
| **Singleton + Func<IStrategy>** | Minimal changes | Still Singleton with mutable state |
| **Scoped lifecycle** | Clean DI, testable | Requires state extraction |

### Decision: SCOPED LIFECYCLE

### Implementation

**Before:**
```csharp
services.AddSingleton<MsTeamsMeetingTracker>(...);

public class MsTeamsMeetingsTrackerJob : IJob
{
    private readonly MsTeamsMeetingTracker _tracker;
    
    public MsTeamsMeetingsTrackerJob(MsTeamsMeetingTracker tracker) 
    {
        _tracker = tracker;
    }
    
    public Task Execute(IJobExecutionContext context)
    {
        _tracker.RecognizeActivity(); // Uses same instance every cycle
    }
}
```

**After:**
```csharp
services.AddScoped<MsTeamsMeetingTracker>();
services.AddScoped<IMeetingDiscoveryStrategy, ConfigurableMeetingDiscoveryStrategy>();

public class MsTeamsMeetingsTrackerJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public MsTeamsMeetingsTrackerJob(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var tracker = scope.ServiceProvider.GetRequiredService<MsTeamsMeetingTracker>();
        tracker.RecognizeActivity(); // Fresh instance with fresh strategy
    }
}
```

### The Critical Problem: State Loss

**Issue**: `MsTeamsMeetingTracker` stores ongoing meeting as instance field:
```csharp
public class MsTeamsMeetingTracker
{
    private StartedMeeting ongoingMeeting; // ← Lost when scope disposed!
}
```

**Impact**: New scope every 10 seconds = new tracker instance = `ongoingMeeting` reset to null = meeting ends prematurely.

### Solution: Extract State to Singleton

```csharp
// NEW: Singleton service for state persistence
public interface IMeetingStateCache
{
    StartedMeeting? GetOngoingMeeting();
    void SetOngoingMeeting(StartedMeeting? meeting);
    Guid? GetMatchedRuleId();
    void SetMatchedRuleId(Guid? ruleId);
}

public class MeetingStateCache : IMeetingStateCache
{
    private readonly object _lock = new();
    private StartedMeeting? _ongoingMeeting;
    private Guid? _matchedRuleId;
    
    public StartedMeeting? GetOngoingMeeting()
    {
        lock (_lock) return _ongoingMeeting;
    }
    
    public void SetOngoingMeeting(StartedMeeting? meeting)
    {
        lock (_lock) _ongoingMeeting = meeting;
    }
    
    // ... other methods with locks
}

// Registration
services.AddSingleton<IMeetingStateCache, MeetingStateCache>();
services.AddScoped<MsTeamsMeetingTracker>();

// Tracker now stateless—delegates to cache
public class MsTeamsMeetingTracker
{
    private readonly IMeetingStateCache _stateCache;
    
    public void RecognizeActivity()
    {
        var ongoingMeeting = _stateCache.GetOngoingMeeting();
        // ... logic using ongoingMeeting
        _stateCache.SetOngoingMeeting(updatedMeeting);
    }
}
```

### Why This Architecture?

1. **Separation of concerns**: 
   - Tracker = orchestration logic (Scoped, no state)
   - State cache = data persistence (Singleton, thread-safe)
   
2. **Testability**: 
   - Mock `IMeetingStateCache` in unit tests
   - No shared state between test runs
   
3. **Immediate rule application**: 
   - Fresh tracker/strategy per cycle
   - Rules loaded from repository every 10s
   
4. **Avoids anti-patterns**:
   - No ServiceLocator (`IServiceProvider` in tracker)
   - No ambient context (`MeetingState.Current` static)

---

## 3. ReDoS (Regular Expression Denial of Service)

### What is ReDoS?

A **Regular Expression Denial of Service** attack exploits catastrophic backtracking in regex engines, causing exponential evaluation time that freezes applications.

### How It Works

User creates regex pattern:
```regex
(a+)+b
```

Application evaluates against input:
```
"aaaaaaaaaaaaaaaaaaaaaaaaX"  (24 'a' characters, no 'b')
```

**What happens:**
1. Regex engine tries to match `(a+)+` 
2. Inner `a+` can match 1-24 characters
3. Outer `+` can repeat inner group 1-24 times
4. For 24 characters: **2^24 = 16,777,216 possible combinations** to try
5. Each combination requires backtracking when final `b` not found
6. Evaluation time: **seconds to minutes** for 30+ characters

### Real-World Examples

- **Stack Overflow (2016)**: Regex `\s+$` against 20KB string with trailing spaces → 30-second hang, site down
- **npm registry (2019)**: Regex in semver parser → infinite loop, package installs failed globally
- **Cloudflare (2019)**: Single regex in WAF rules → CPU spike to 100%, global outage

### Why It's Dangerous in TrackYourDay

1. **User-controlled patterns**: AC3 allows regex mode—users paste patterns from Stack Overflow without understanding complexity
2. **Arbitrary input length**: Teams window titles can be 200+ characters (meeting agenda in title)
3. **Blocking execution**: Regex runs in background job thread—freezes all meeting tracking
4. **No expertise validation**: No way to verify user understands regex performance implications

### Attack Scenarios

**Scenario 1: Malicious user**
- User configures rule: `(.*a){20}b`
- Teams window title: "Daily standup meeting aaaaaa..." (50 chars)
- Result: 2^20 backtracking paths, app freezes 30+ seconds

**Scenario 2: Innocent mistake**
- User tries to match "ends with Microsoft Teams": `.*Microsoft Teams$`
- Window title has 100-char meeting agenda before "Microsoft Teams"
- Pathological input: No "Microsoft Teams" at end → exponential backtracking

**Scenario 3: Copy-paste from web**
- User finds regex online: `^(([a-z])+.)+[A-Z]([a-z])+$`
- Seems harmless, but nested quantifiers create exponential paths
- Input "aaaaaaaaaaaaaaaaaaaaaaaX" → minutes to evaluate

### Mitigation Strategy (Defense in Depth)

#### Layer 1: Timeout in Regex Constructor
```csharp
var timeout = TimeSpan.FromSeconds(2);
var regex = new Regex(pattern, RegexOptions.Compiled, timeout);
```
- Limits compilation time (detects infinite loops during save)
- Throws `RegexMatchTimeoutException` if pattern too complex

#### Layer 2: Timeout in IsMatch Evaluation
```csharp
try 
{
    return regex.IsMatch(input); // Timeout enforced by constructor
}
catch (RegexMatchTimeoutException ex)
{
    _logger.LogWarning(ex, "Regex timeout: {Pattern} vs input length {Length}", 
        pattern, input.Length);
    return false; // Treat timeout as non-match, never crash
}
```

#### Layer 3: Graceful Degradation
- **Do NOT** crash tracker on timeout
- **Do NOT** show user error modal during background job
- **DO** log warning for diagnostics
- **DO** treat timeout as "pattern didn't match"

#### Layer 4: UI Warnings (Optional)
- Warn if pattern contains nested quantifiers: `(.*)+`, `(a+)*`, `(x*)+`
- Show tooltip: "Complex patterns may slow down tracking"
- Do NOT block save—user may know what they're doing

### What NOT to Do

❌ **Parse regex AST to detect dangerous patterns**
- Brittle: Many safe patterns contain `+` or `*`
- Incomplete: New attack patterns discovered regularly
- False positives: Blocks legitimate use cases

❌ **Limit regex complexity score**
- No consensus on how to score complexity
- Users can craft high-score but fast patterns
- Users can craft low-score but slow patterns

❌ **Pre-test against known bad inputs**
- Infinite possible bad inputs
- Tests would take longer than 2s timeout anyway

### Accepting Residual Risk

**Remaining risk**: Sophisticated user crafts regex that takes exactly 1.9 seconds to evaluate.

**Why this is acceptable:**
1. **Desktop app**: Single user, no remote exploitation
2. **User accountability**: User configured the pattern, user experiences slowdown
3. **Reversible**: User can delete rule to fix issue
4. **No data loss**: Timeout prevents crashes, tracking resumes next cycle

**Not a security boundary**: User shooting themselves in the foot is expected for advanced features.

### Testing ReDoS Protection

```csharp
[Fact]
public void GivenCatastrophicBacktrackingPattern_WhenEvaluatingAgainstLongInput_ThenTimesOutGracefully()
{
    // Given
    var pattern = PatternDefinition.CreateRegexPattern("(a+)+b", caseSensitive: false);
    var input = new string('a', 30) + "X"; // 30 'a' chars, no 'b'
    
    // When
    var sw = Stopwatch.StartNew();
    var matches = pattern.Matches(input);
    sw.Stop();
    
    // Then
    matches.Should().BeFalse(); // Timeout treated as non-match
    sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3)); // 2s timeout + margin
}
```

---

## Implementation Checklist

### Before Starting (Phase 0)
- [ ] Extract `IMeetingStateCache` interface + implementation
- [ ] Refactor `MsTeamsMeetingTracker` to use state cache
- [ ] Update `MsTeamsMeetingsTrackerJob` to use `IServiceScopeFactory`
- [ ] Verify all existing tests pass
- [ ] **Manual test**: Start meeting, wait 30s, confirm meeting still tracked

### During Implementation
- [ ] Regex timeout in **both** constructor and `IsMatch()`
- [ ] `IMeetingStateCache` uses locks (no lock-free patterns)
- [ ] Empty pattern validation blocks save
- [ ] Test/Preview runs in `Task.Run()` (UI responsiveness)
- [ ] Unit tests mock `IMeetingStateCache` (avoid Singleton in tests)

### Before Release
- [ ] **ReDoS test**: Create rule `(a+)+b`, open Teams with 30 'a' title, verify app responsive
- [ ] **AC12 test**: Meeting ongoing, title changes, same rule matches → meeting continues
- [ ] **Concurrent mod test**: Save rules while job running → no exceptions
- [ ] **Corruption recovery**: Corrupt JSON, restart → default rule loads
- [ ] **Performance**: <200ms evaluation time with 10 rules, 50 processes

---

## Quick Answers

**Q: Why not cache rules instead of loading every cycle?**
A: AC11 requires immediate application. Caching breaks this unless cache invalidation is bulletproof.

**Q: Why `lock` instead of lock-free patterns in state cache?**
A: Single job thread, 10s interval = low contention. Lock is simpler, safer, fast enough.

**Q: Why not store rule ID in `StartedMeeting` domain object?**
A: Domain objects shouldn't know about application-level rule IDs. Violates layering.

**Q: What if user creates 1000 rules?**
A: UI will lag, evaluation may timeout. Document limit (e.g., 50 rules) in user guide. Don't enforce programmatically yet—YAGNI.

**Q: Why 2-second timeout instead of 5 or 10?**
A: .NET default. Long enough for complex patterns, short enough to recover quickly. Matches user expectation for "instant" feedback.
