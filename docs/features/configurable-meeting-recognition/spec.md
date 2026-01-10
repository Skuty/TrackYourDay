# Feature: Configurable Meeting Recognition Rules

## Problem Statement
The current MS Teams meeting recognition logic is hardcoded in `ProcessBasedMeetingRecognizingStrategy`, preventing users from adapting to different Teams versions (classic vs new), localized window titles, or custom meeting detection scenarios. Users cannot modify recognition patterns without code changes.

## User Stories
- **US1:** As a TrackYourDay user, I want to configure multiple meeting recognition rules so that I can track meetings across different Teams versions or localized installations
- **US2:** As a user, I want to define custom inclusion and exclusion patterns so that I can avoid false positives (e.g., tracking chat windows as meetings)
- **US3:** As a user, I want rules to apply immediately without restarting the application so that I can test and iterate on configurations quickly
- **US4:** As a user upgrading from a previous version, I want the default configuration to match previous behavior so that my meeting tracking continues uninterrupted

## Acceptance Criteria

### AC1: Multiple Rules with Priority Order
**Given** the user has configured 3 meeting recognition rules  
**When** the recognition engine evaluates running processes  
**Then** rules are evaluated in priority order (Rule 1, Rule 2, Rule 3)  
**And** the first rule that matches starts/continues the meeting  
**And** subsequent rules are not evaluated once a match is found

### AC2: Per-Rule Matching Criteria Selection
**Given** the user is creating a new recognition rule  
**When** configuring the rule  
**Then** the user can select one of three matching modes:
- Match by process name only
- Match by window title only  
- Match by both process name AND window title (both must match)

### AC3: Multiple Pattern Matching Modes
**Given** the user is defining a pattern for process name or window title  
**When** entering the pattern  
**Then** the user can select from these matching modes via dropdown:
- Contains (substring match)
- Starts with (prefix match)
- Ends with (suffix match)
- Exact match (full string equality)
- Regular expression

### AC4: Inclusion and Exclusion Patterns
**Given** the user is configuring a rule  
**When** defining matching criteria  
**Then** the user can specify:
- Inclusion pattern(s) that MUST match
- Exclusion pattern(s) that MUST NOT match  
**And** both inclusion and exclusion patterns support all 5 matching modes independently

### AC5: First Match Within Rule Wins
**Given** a rule matches 2 running processes: Process A and Process B  
**When** the recognition engine evaluates the rule  
**Then** the first process returned by the OS (`Process.GetProcesses()` order) is selected  
**And** the second process is ignored  
**And** no secondary criteria are applied

### AC6: Settings Storage via IGenericSettingsService
**Given** the user saves meeting recognition rules  
**When** rules are persisted  
**Then** they are stored using `IGenericSettingsService` (same mechanism as GitLab/Jira settings)  
**And** rules are serialized to JSON  
**And** no new database tables are created

### AC7: New "Meeting Recognition" Settings Tab
**Given** the user opens Settings page  
**When** viewing available tabs  
**Then** a new tab "Meeting Recognition" is visible  
**And** it is positioned between existing tabs (placement TBD by implementation)  
**And** the tab contains:
- List of rules with priority order controls (up/down arrows or drag-drop)
- Add/Edit/Delete rule buttons
- Test/Preview button
- Match count statistics per rule

### AC8: Test/Preview Functionality
**Given** the user clicks "Test/Preview" button  
**When** the preview executes  
**Then** the system displays:
- All currently running MS Teams processes (process name + window title)
- Which rule (if any) matches each process
- Validation warnings for rules that would match 0 processes  
**And** preview does NOT start/stop actual meeting tracking

### AC9: Regex Validation Before Save
**Given** the user enters a regular expression pattern  
**When** attempting to save the rule  
**Then** the regex is validated for syntax correctness  
**And** if invalid, an error message is displayed  
**And** the save operation is blocked until the pattern is valid  
**And** the error message indicates which pattern field contains the error

### AC10: Default Rule Auto-Creation
**Given** a user starts the application for the first time OR upgrades from a previous version  
**When** the meeting recognition settings are initialized  
**Then** a default rule is automatically created with these exact settings:
- **Priority:** 1 (highest)
- **Matching mode:** Both process name AND window title
- **Process name inclusion:** Contains "ms-teams"
- **Window title inclusion:** Contains "Microsoft Teams"
- **Window title exclusions:**
  - Does NOT start with "Czat |"
  - Does NOT start with "Aktywność |"
  - Does NOT exactly match "Microsoft Teams"
- **All patterns case-insensitive**

### AC11: Immediate Rule Application (Breaking Change from Other Settings)
**Given** the user modifies meeting recognition rules and clicks Save  
**When** the save completes  
**Then** the new rules are applied on the next poll cycle (within seconds)  
**And** no application restart is required  
**And** the UI displays a message: "Rules applied immediately (no restart required)"  
**And** this behavior differs from Breaks/GitLab/Jira settings which require restart

### AC12: Ongoing Meeting Continuity with Title Changes
**Given** a meeting is currently being tracked (started by Rule #2)  
**When** the window title changes (e.g., screen sharing control bar)  
**And** the new window title ALSO matches Rule #2 (same rule that started the meeting)  
**Then** the meeting continues without ending  
**And** the meeting's original title is preserved  
**And** no new meeting is started

**Given** a meeting is currently being tracked (started by Rule #2)  
**When** the window title changes  
**And** the new window title does NOT match Rule #2 but matches Rule #3  
**Then** the original meeting ends  
**And** a new meeting starts (tracked by Rule #3)

### AC13: No Historical Data Migration
**Given** a user upgrades from a version without configurable rules  
**When** the upgrade completes  
**Then** existing `EndedMeeting` records remain unchanged  
**And** no database migration is executed  
**And** ongoing meetings in memory continue tracking normally

### AC14: Match Count Statistics Display
**Given** rules have been active and evaluating processes  
**When** the user views the "Meeting Recognition" settings tab  
**Then** each rule displays:
- Total number of times it has matched (lifetime count)
- Last match timestamp  
**And** this data persists across application restarts  
**And** match counts are stored via `IGenericSettingsService`

### AC15: Silent Failure for Non-Matching Rules
**Given** a rule is configured but never matches any running processes  
**When** recognition cycles execute  
**Then** no error is logged  
**And** no warning is displayed to the user  
**And** the rule continues to evaluate normally

### AC16: False Positive Discovery Post-Facto Only
**Given** a rule incorrectly matches non-meeting windows (e.g., "Chat | Microsoft Teams")  
**When** the false positive occurs  
**Then** a meeting is tracked (no real-time detection of false positives)  
**And** the user discovers the issue by reviewing tracked meetings in the UI  
**And** no automated false-positive prevention exists

### AC17: Scope Limited to MS Teams Only
**Given** the configurable rules feature  
**When** implemented  
**Then** it applies ONLY to `MsTeamsMeetingTracker`  
**And** does NOT extend to other meeting applications (Zoom, Google Meet, etc.)  
**And** does NOT support generic process/window tracking beyond meetings  
**And** the feature remains application-level tracking (not system-level)

## Out of Scope

### Explicitly Excluded from This Feature:
1. **Multi-application support** - No tracking for Zoom, Google Meet, Webex, or other meeting platforms
2. **Automatic false-positive detection** - No ML or heuristics to identify incorrectly tracked windows
3. **Process ID tracking** - Meeting identity determined by title match only, not process lifecycle
4. **Rule performance optimization** - No intelligent reordering or caching of frequently-matched rules
5. **Rule complexity limits** - No maximum on number of rules, regex complexity, or evaluation time
6. **Secondary conflict resolution** - No "prefer longest title" or "most recently focused" logic when multiple processes match
7. **Concurrent meeting tracking** - Only one meeting can be active at a time (existing constraint preserved)
8. **Rule templates/presets** - No built-in library of common patterns for different Teams versions
9. **Import/export rules** - No sharing rules between users or machines
10. **Rule versioning/history** - No undo capability or audit log of rule changes
11. **Conditional logic between rules** - No AND/OR operators combining multiple rules
12. **Time-based rules** - No "apply Rule A during work hours, Rule B otherwise"
13. **User prompts for ambiguity** - System never asks user to choose between matching processes at runtime

## Edge Cases & Risks

### Identified Risks:
1. **Breaking change in settings behavior:** Meeting rules apply immediately without restart, unlike all other settings. Users may expect restart requirement.
2. **OS-dependent process order:** "First match wins" behavior depends on `Process.GetProcesses()` order, which is not guaranteed stable across Windows versions.
3. **Regex performance unbounded:** Users can create catastrophically backtracking regex patterns (e.g., `(a+)+b`) that freeze the app during evaluation.
4. **Rule conflicts after upgrade:** If user had customized workarounds (e.g., registry hacks), default rule may conflict with their expectations.
5. **No rollback mechanism:** If user misconfigures rules and breaks meeting tracking, only manual reconfiguration can fix it (no "restore defaults" button specified).
6. **Localization assumptions:** Default rule exclusions ("Czat |", "Aktywność |") are Polish-language specific. Users with other locales may need different exclusions.
7. **Singleton strategy injection:** Current `MsTeamsMeetingTracker` has strategy injected in constructor (Singleton lifecycle). Immediate rule application requires strategy resolution on each call or refactoring to Scoped lifecycle.
8. **Test/preview race conditions:** Preview shows current processes, but by the time user configures the rule, process state may have changed.
9. **JSON serialization complexity:** Rules with complex objects (multiple patterns, modes, exclusions) must serialize/deserialize reliably via `IGenericSettingsService`.
10. **No validation for exclusion-only rules:** User could create a rule with only exclusions and no inclusions (undefined behavior).

### Edge Cases:
- **Empty process name or window title:** Process has empty string for `MainWindowTitle` - should null/empty checks exist?
- **Rule with no patterns:** User saves rule without any inclusion or exclusion patterns - should this be blocked?
- **All rules disabled/deleted:** User deletes default rule and adds no others - what happens? (Spec says default rule auto-created, but can user delete it?)
- **Regex timeout:** If regex takes >5 seconds to evaluate, does evaluation thread hang or timeout?
- **Match count overflow:** After 2 billion matches, `int` counter overflows - use `long`?
- **Simultaneous rule edits:** User edits rules in Settings UI while recognition job is evaluating - thread safety?
- **Case sensitivity per-pattern:** Default rule is case-insensitive, but should each pattern have a case-sensitive toggle?
- **Unicode in patterns:** Window titles may contain emoji or non-Latin characters - are regex flags set correctly?
- **Process name vs MainModule.FileName:** `ProcessName` returns "ms-teams" but full path is "C:\...\ms-teams.exe" - which is checked?

## Data Requirements
### Migration Considerations:
- **First run detection:** Check if `"MeetingRecognitionRules"` key exists. If not, create default rule.
- **Schema versioning:** Store version number to handle future schema changes.
- **No database changes:** All data stored in existing settings table used by `IGenericSettingsService`.

## UI/UX Requirements

### Meeting Recognition Tab Layout:

**Top Section - Rule List:**
- Table/DataGrid displaying rules in priority order:
  - Columns: Priority, Criteria (icon/badge), Process Pattern, Window Pattern, Exclusions (count), Match Count, Last Matched, Actions
  - Drag-drop rows to reorder priority OR up/down arrow buttons
  - Edit (pencil icon) and Delete (trash icon) buttons per row
- "Add New Rule" button (primary CTA)

**Middle Section - Rule Editor (shown when Add/Edit clicked):**
- **Matching Criteria** dropdown: "Process Name Only" | "Window Title Only" | "Both"
- **Process Name Pattern** (if applicable):
  - Text input
  - Dropdown for Match Mode (Contains/Starts/Ends/Exact/Regex)
  - Checkbox: Case Sensitive
- **Window Title Pattern** (if applicable):
  - Same fields as Process Name Pattern
- **Exclusions** section:
  - List of exclusion patterns (collapsible)
  - "Add Exclusion" button
  - Each exclusion has: Pattern text input, Match Mode dropdown, Case Sensitive checkbox, Remove button
- **Save** and **Cancel** buttons

**Bottom Section - Testing:**
- "Test Current Rules" button
- Results panel (appears after test):
  - "Currently Running MS Teams Processes: X found"
  - List showing: Process Name, Window Title, Matched by Rule (or "No match")
  - Warning icon for rules that matched 0 processes

**Validation Rules:**
- Block save if regex pattern is syntactically invalid
- Show inline error message next to invalid pattern field
- Cannot create rule with neither process name nor window title pattern (if Criteria is "Both", both must be filled)
- Cannot have two rules with same priority (auto-adjust on reorder)

**Interaction Patterns:**
- Save button triggers immediate application (no restart)
- Test button does NOT save - only previews with current configuration
- Match count updates displayed in real-time if Settings page is open during recognition cycles (optional: requires SignalR or polling)
- Confirmation dialog when deleting a rule: "Are you sure? This cannot be undone."

## Dependencies

### Existing Features Affected:
1. **MsTeamsMeetingTracker** - Strategy must be resolved dynamically or accept rule set parameter
2. **ProcessBasedMeetingRecognizingStrategy** - May be replaced or refactored to use configurable rules
3. **MsTeamsMeetingsTrackerJob** - Must reload rules periodically or subscribe to change events
4. **ServiceCollections.AddTrackers()** - Singleton registration may need to become Scoped or Transient
5. **Settings.razor** - Add new tab, potentially refactor tab structure for scalability
6. **IGenericSettingsService** - No changes, but heavily relied upon for persistence

### External Integrations:
- **None** - Feature is fully internal, no external APIs or services

### System/Application/Insights Level Integration:
- **Application Level:** Feature operates at application-level tracking (MS Teams specific)
- **Does NOT affect System Level:** No changes to `ActivityTracker`, mouse/keyboard monitoring, or window focus tracking
- **Does NOT affect Insights Level:** Meeting data format (`EndedMeeting`) remains unchanged, so analytics/reports continue working

### Performance Considerations:
- **Polling frequency unknown:** Specification assumes existing poll interval is acceptable. If `MsTeamsMeetingsTrackerJob` runs every 1 second and user has 10 rules with complex regex, evaluation could take 100ms+ per cycle.
- **UI responsiveness:** Test/Preview must not block UI thread (run on background thread).
- **Settings save latency:** JSON serialization of rules should complete in <100ms to avoid UI freeze.

## Non-Functional Requirements

### Performance:
- No specific performance SLA defined (per Q12 answer)
- Rule evaluation should not noticeably degrade application responsiveness (subjective)
- Test/Preview should complete within 5 seconds for typical scenarios (<50 processes)

### Security:
- No credential storage (unlike GitLab/Jira settings)
- Regex patterns are user-controlled - risk of ReDoS (Regular Expression Denial of Service) accepted
- No input sanitization beyond regex validation

### Accessibility:
- Settings UI must follow existing MudBlazor accessibility patterns
- Error messages must be screen-reader friendly
- Keyboard navigation must work for rule reordering (not just drag-drop)

### Usability:
- Default rule should make feature "invisible" to users who don't need customization
- Test/Preview button critical for discoverability and reducing trial-and-error
- Match count statistics help users understand which rules are active

### Reliability:
- Invalid regex must never crash the application (catch `RegexMatchTimeoutException`, `ArgumentException`)
- Rule evaluation failure must not stop the background job (catch, log, continue)
- Settings corruption (invalid JSON) must not prevent application startup (fallback to default rule)

### Maintainability:
- Rule schema versioned to support future enhancements
- Separation of concerns: rule storage (IGenericSettingsService), evaluation logic (strategy), UI (Settings.razor)

---

## Open Questions for Implementation Phase
*(Not requirements, but areas requiring architectural decisions)*

1. Should `IMeetingDiscoveryStrategy` interface change to accept rules parameter, or should strategy resolve rules internally?
2. How to handle thread safety when rules are modified while recognition job is reading them?
3. Should match count statistics be atomic (Interlocked.Increment) or is eventual consistency acceptable?
4. Should there be a "Reset to Default" button that recreates the default rule?
5. Should exclusions be part of the same PatternDefinition or a separate list?
6. How to handle case sensitivity in default rule - explicitly set or rely on string comparison defaults?
7. Should rule editor support bulk import/export for power users (marked out of scope but may be requested)?
8. Should there be a "Disable Meeting Tracking" global toggle (separate from deleting all rules)?
