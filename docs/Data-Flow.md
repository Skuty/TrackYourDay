# Data Flow

## Overview

This document describes how data flows through TrackYourDay from low-level system events to high-level insights and UI updates.

## Complete Data Flow

```mermaid
flowchart TB
    subgraph OS["Operating System"]
        OSE[OS Events]
        OSE --> WF[Window Focus]
        OSE --> MP[Mouse Position]
        OSE --> KB[Keyboard Input]
        OSE --> SL[System Lock]
    end
    
    subgraph SysTrack["System Trackers Layer"]
        AT[Activity Tracker]
        RS[Recognition Strategies]
        
        WF --> RS
        MP --> RS
        KB --> RS
        SL --> RS
        RS --> AT
        AT --> SE[System Events]
    end
    
    subgraph AppTrack["Application Trackers Layer"]
        BT[Break Tracker]
        MT[MS Teams Tracker]
        JT[Jira Tracker]
        GT[GitLab Tracker]
        
        SE --> BT
        SE --> MT
        JT -.API Calls.-> JAPI[Jira API]
        GT -.API Calls.-> GAPI[GitLab API]
    end
    
    subgraph Insights["Insights Layer"]
        WD[Workday Manager]
        AA[Activities Analyser]
        SUM[Summary Strategies]
        
        BT --> WD
        SE --> WD
        SE --> AA
        AA --> SUM
    end
    
    subgraph Persist["Persistence Layer"]
        DB[(SQLite Database)]
        CACHE[Cache]
        
        WD --> DB
        AA --> DB
        JT --> CACHE
        GT --> CACHE
    end
    
    subgraph UI["User Interface"]
        DASH[Dashboard]
        NOTIF[Notifications]
        REP[Reports]
        
        WD --> DASH
        WD --> NOTIF
        SUM --> REP
    end
```

## Detailed Flow Scenarios

### Scenario 1: User Switches Application

This scenario shows what happens when a user switches from one application to another.

```mermaid
sequenceDiagram
    participant OS as Operating System
    participant Job as Background Job
    participant AT as ActivityTracker
    participant Strat as FocusedWindowStrategy
    participant Pub as MediatR Publisher
    participant BT as BreakTracker
    participant WD as Workday
    participant UI as User Interface
    
    Note over OS: User switches from<br/>VSCode to Chrome
    
    Job->>AT: RecognizeActivity() [Scheduled Poll]
    AT->>Strat: RecognizeActivity()
    Strat->>OS: GetForegroundWindow()
    OS-->>Strat: Chrome Window
    Strat-->>AT: FocusOnApplicationState("Chrome")
    
    Note over AT: Different from current activity
    
    AT->>AT: End current activity<br/>(VSCode)
    AT->>Pub: Publish PeriodicActivityEndedEvent<br/>(VSCode, 2h duration)
    
    AT->>AT: Start new activity<br/>(Chrome)
    AT->>Pub: Publish PeriodicActivityStartedEvent<br/>(Chrome)
    
    par Process in Break Tracker
        Pub->>BT: PeriodicActivityEndedEvent
        BT->>BT: AddActivityToProcess(VSCode activity)
        BT->>BT: ProcessActivities()
        Note over BT: Check for breaks<br/>between activities
        BT->>BT: No break detected<br/>(activities close together)
    and Process in Workday
        Pub->>WD: PeriodicActivityEndedEvent
        WD->>WD: AddActivity(VSCode activity)
        WD->>WD: RecalculateMetrics()
        WD->>UI: Update Dashboard
        UI->>UI: Show VSCode: 2h
    end
```

### Scenario 2: Break Detection and End

This scenario demonstrates how breaks are detected and ended.

```mermaid
sequenceDiagram
    participant AT as ActivityTracker
    participant Pub as MediatR Publisher
    participant BT as BreakTracker
    participant Clock as IClock
    participant WD as Workday
    participant UI as User Interface
    
    Note over AT: Last activity at 11:00
    Note over Clock: Current time: 11:25<br/>(25 min gap)
    
    AT->>Pub: InstantActivityOccuredEvent(Mouse moved, 11:25)
    Pub->>BT: InstantActivityOccuredEvent
    BT->>BT: AddActivityToProcess(11:25)
    BT->>Clock: Now()
    Clock-->>BT: 11:25
    
    Note over BT: Calculate gap:<br/>11:25 - 11:00 = 25 min<br/>Gap > Threshold (5 min)
    
    BT->>BT: Create StartedBreak(11:00)
    BT->>Pub: Publish BreakStartedEvent
    
    par Notify UI
        Pub->>UI: BreakStartedEvent
        UI->>UI: Show "Break Started" notification
    and Update Workday
        Pub->>WD: BreakStartedEvent
        WD->>WD: Mark break in progress
    end
    
    Note over AT: User continues working
    Note over Clock: Current time: 11:26
    
    AT->>Pub: PeriodicActivityEndedEvent(VSCode, 11:26)
    Pub->>BT: PeriodicActivityEndedEvent
    BT->>BT: AddActivityToProcess(11:26)
    
    Note over BT: Activity detected<br/>while break in progress
    
    BT->>BT: End current break<br/>Duration: 11:00-11:25 = 25 min
    BT->>Pub: Publish BreakEndedEvent(25 min)
    
    par Notify UI
        Pub->>UI: BreakEndedEvent
        UI->>UI: Show "Break Ended: 25 min"
    and Update Workday
        Pub->>WD: BreakEndedEvent
        WD->>WD: AddBreak(25 min break)
        WD->>WD: RecalculateMetrics()
        WD->>UI: Update Dashboard<br/>BreakTime: 25 min<br/>ActiveWork: recalculated
    end
```

### Scenario 3: Jira Integration Flow

This scenario shows how Jira integration enriches activity tracking.

```mermaid
sequenceDiagram
    participant User
    participant BG as Background Job
    participant JT as JiraTracker
    participant API as Jira API Client
    participant Cache as Local Cache
    participant AT as ActivityTracker
    participant Sum as JiraEnrichedStrategy
    participant UI as User Interface
    
    Note over User: Opens VSCode with<br/>"PROJ-123: Fix Bug"
    
    par Jira Data Sync
        BG->>JT: FetchAssignedIssues() [Scheduled]
        JT->>API: GetCurrentUser()
        API-->>JT: JiraUser(email)
        JT->>API: GetAssignedIssues(user)
        API-->>JT: List[Issues]
        JT->>Cache: Store issues with TTL
    and Activity Tracking
        AT->>AT: Detect VSCode window
        AT->>AT: Extract title: "PROJ-123: Fix Bug"
    end
    
    Note over User: Works for 2 hours
    
    AT->>Sum: Generate summary
    Sum->>Sum: Extract Jira key "PROJ-123"<br/>from window title
    Sum->>Cache: GetIssue("PROJ-123")
    Cache-->>Sum: Issue(title: "Fix login error",<br/>type: Bug, priority: High)
    
    Sum->>Sum: Group activities by Jira issue<br/>PROJ-123: 2h
    Sum->>UI: Display Summary
    UI->>UI: Show: "PROJ-123 [Bug] Fix login error: 2h"
    
    Note over User: End of day - log time
    
    User->>UI: Click "Log to Jira"
    UI->>JT: LogWorkTime("PROJ-123", 2h)
    JT->>API: CreateWorklog("PROJ-123", 7200 seconds)
    API-->>JT: Success
    JT->>UI: Confirm time logged
    UI->>User: Show "2h logged to PROJ-123"
```

### Scenario 4: System Lock and Break

This scenario shows immediate break detection when system is locked.

```mermaid
sequenceDiagram
    participant User
    participant OS as Operating System
    participant AT as ActivityTracker
    participant Strat as Recognition Strategy
    participant Pub as MediatR Publisher
    participant BT as BreakTracker
    participant WD as Workday
    participant UI as User Interface
    
    Note over User: Locks computer<br/>(Win+L)
    
    OS->>OS: System Locked
    
    Note over AT: Next polling cycle
    
    AT->>Strat: RecognizeActivity()
    Strat->>OS: GetSystemState()
    OS-->>Strat: System Locked
    Strat-->>AT: SystemLockedState
    
    AT->>Pub: Publish PeriodicActivityEndedEvent
    AT->>Pub: Publish PeriodicActivityStartedEvent<br/>(SystemLockedState)
    
    Pub->>BT: PeriodicActivityStartedEvent(Locked)
    
    Note over BT: SystemLockedState detected
    
    BT->>BT: Immediately start break<br/>Reason: "System Locked"
    BT->>Pub: Publish BreakStartedEvent
    
    Pub->>WD: BreakStartedEvent
    WD->>UI: Update Dashboard<br/>"Break in progress"
    
    Note over User: Returns and unlocks<br/>30 minutes later
    
    OS->>OS: System Unlocked
    AT->>Strat: RecognizeActivity()
    Strat->>OS: GetSystemState()
    OS-->>Strat: System Active (VSCode)
    Strat-->>AT: FocusOnApplicationState("VSCode")
    
    AT->>Pub: Publish PeriodicActivityEndedEvent<br/>(SystemLockedState)
    Pub->>BT: PeriodicActivityEndedEvent
    
    BT->>BT: End current break<br/>Duration: 30 min
    BT->>Pub: Publish BreakEndedEvent(30 min)
    
    Pub->>WD: BreakEndedEvent
    WD->>WD: AddBreak(30 min)
    WD->>WD: RecalculateMetrics()
    WD->>UI: Update Dashboard<br/>Break: 30 min added
```

### Scenario 5: End of Workday Summary

This scenario shows how the end-of-day summary is generated.

```mermaid
sequenceDiagram
    participant Clock as IClock
    participant Job as Scheduled Job
    participant WD as Workday
    participant AA as ActivitiesAnalyser
    participant Strat as Summary Strategy
    participant Cache as Jira/GitLab Cache
    participant NS as Notification Service
    participant UI as User Interface
    participant User
    
    Note over Clock: 17:00 - End of workday
    
    Job->>WD: GetCurrentWorkday()
    WD-->>Job: Workday(8h worked, 45m breaks)
    
    Job->>AA: GenerateDailySummary()
    AA->>AA: Get all activities from workday
    AA->>Strat: Summarize(activities)
    
    Strat->>Strat: Group by application
    Strat->>Strat: Extract Jira keys from titles
    Strat->>Cache: GetJiraIssues(keys)
    Cache-->>Strat: Issue details
    
    Strat->>Strat: Enrich groups with Jira data
    Strat-->>AA: GroupedActivities[]
    
    AA->>AA: Sort by duration
    AA->>AA: Format summary
    AA-->>Job: Summary Report
    
    Job->>NS: CheckWorkdayComplete(workday)
    NS->>NS: Calculate: 8h worked >= 8h required
    NS->>NS: Create EndOfWorkDayNotification
    NS->>UI: ShowNotification(summary)
    
    UI->>User: Display Notification:<br/>"Workday Complete!<br/>PROJ-123: 4h<br/>PROJ-124: 3h<br/>Meetings: 1h"
    
    User->>UI: Click notification
    UI->>UI: Open detailed report
    UI->>User: Show:<br/>- Time breakdown<br/>- Break analysis<br/>- Productivity metrics<br/>- Export options
```

## Background Job Scheduling

TrackYourDay uses Quartz.NET for scheduling recurring jobs:

```mermaid
gantt
    title Background Job Schedule
    dateFormat mm:ss
    axisFormat %M:%S
    
    section Activity Tracking
    Recognize Activity :active, a1, 00:00, 10s
    Recognize Activity :active, a2, 00:10, 10s
    Recognize Activity :active, a3, 00:20, 10s
    
    section Break Processing
    Process Activities :b1, 00:30, 5s
    Process Activities :b2, 01:00, 5s
    
    section Meeting Discovery
    Check Teams :c1, 00:15, 5s
    Check Teams :c2, 01:15, 5s
    
    section Jira Sync
    Fetch Issues :d1, 05:00, 30s
```

**Job Types**:

1. **ActivityEventTrackerJob**: Every 10 seconds
   - Polls system for activity changes
   - Most frequent job

2. **MsTeamsMeetingsTrackerJob**: Every 60 seconds
   - Checks for Teams meetings
   - Less frequent as meetings don't change quickly

3. **NotificationsProcessorJob**: Every 60 seconds
   - Evaluates notification rules
   - Triggers UI notifications

4. **JiraDataSyncJob**: Every 5 minutes (configurable)
   - Fetches assigned Jira issues
   - Updates local cache

5. **GitLabDataSyncJob**: Every 5 minutes (configurable)
   - Fetches commits and MRs
   - Updates local cache

## Event Flow Patterns

### Fire and Forget

Used for logging and non-critical operations:

```mermaid
graph LR
    A[Publisher] -->|Publish Event| B[Event Bus]
    B --> C[Handler 1: Log]
    B --> D[Handler 2: Metric]
    B --> E[Handler 3: Cache]
```

### Request/Response

Used for queries and synchronous operations:

```mermaid
graph LR
    A[UI] -->|Request| B[Service]
    B -->|Query| C[Repository]
    C -->|Result| B
    B -->|Response| A
```

### Publish/Subscribe

Used for event-driven updates:

```mermaid
graph TB
    A[Activity Tracker] -->|Publish| B[Event Bus]
    B --> C[Break Tracker Handler]
    B --> D[Workday Handler]
    B --> E[Persistence Handler]
    B --> F[Notification Handler]
    
    C --> G[Break Tracker State]
    D --> H[Workday State]
    E --> I[Database]
    F --> J[UI]
```

## Data Consistency

### Eventually Consistent

Read models are eventually consistent with events:

1. Event published
2. Handler processes event (async)
3. Read model updated
4. UI refreshed

**Acceptable delay**: < 1 second for UI updates

### Transactional Consistency

Critical operations use transactions:

- Break revocation: Must atomically remove break and recalculate workday
- Time logging to Jira: Must update both local and remote state
- Settings changes: Must be persisted before taking effect

## Error Handling in Data Flow

```mermaid
graph TB
    A[Event Published] --> B{Handler Execution}
    B -->|Success| C[Update State]
    B -->|Failure| D[Log Error]
    D --> E{Retry Policy}
    E -->|Transient| F[Retry with Backoff]
    E -->|Permanent| G[Dead Letter Queue]
    F --> B
    G --> H[Manual Intervention]
    C --> I[Continue Flow]
```

**Error Strategies**:
- **Transient Errors**: Retry with exponential backoff
- **Permanent Errors**: Log and notify user
- **Critical Errors**: Graceful degradation (use cached data)

## Performance Optimizations

### Batching

Multiple events can be batched for processing:

```
Events: [E1, E2, E3, E4, E5]
Batch Size: 3
Batches: [E1, E2, E3], [E4, E5]
```

### Debouncing

Rapid events are debounced to reduce processing:

```
Events: E1, E2, E3, E4 (within 100ms)
Result: Only E4 processed
```

### Caching

Frequently accessed data is cached:

- Current workday: In-memory cache
- Jira issues: 5-minute TTL
- GitLab data: 5-minute TTL
- Settings: In-memory until changed

## Monitoring and Observability

```mermaid
graph LR
    A[Data Flow] --> B[Structured Logging]
    A --> C[Metrics]
    A --> D[Tracing]
    
    B --> E[Serilog]
    C --> F[Custom Metrics]
    D --> G[Correlation IDs]
    
    E --> H[Log Files]
    F --> I[Performance Data]
    G --> J[Debug Info]
```

**Logged Information**:
- Event publication and handling
- Job execution times
- API call latencies
- Error stack traces
- User actions

**Correlation**: Each event has a correlation ID for tracing through the system.
