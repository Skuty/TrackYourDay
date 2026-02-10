# Critical Defect: Empty Data Handling Returns Orphaned Markdown Headers

## Defect ID: C11

**Location:** `LlmPromptService.cs:81-112, 114-145, 147-202`  
**Violation:** UX Anti-Pattern, Fail Silent  
**Severity:** CRITICAL

---

## Problem Description

When backend services return no data, the current implementation has two failure modes:

### Failure Mode 1: Silent Empty String
```csharp
if (activities.Count == 0)
    return string.Empty; // Line 90, 123
```

**Issue:** Placeholder `{JIRA_ACTIVITIES}` vanishes from prompt. LLM receives incomplete context with no indication data was requested.

**Example Output:**
```
Analyze my work today:



Suggest time logs.
```

The double newlines are the only evidence a placeholder existed. User has no idea if:
- Query returned no data
- Feature is broken
- Database is down

---

### Failure Mode 2: Orphaned Markdown Headers
```csharp
var result = sb.ToString();
return result == "## Currently Assigned Issues\n\n" ? string.Empty : result; // Line 195
```

**Issue:** If only ONE data source (Jira vs GitLab) has no data, header is emitted but no table follows.

**Example Output:**
```
## Currently Assigned Issues

### Jira Issues

### GitLab Work Items
| Type | Title | State | Updated |
|------|-------|-------|---------|
| MR | [Feature](https://gitlab.com/mr/123) | opened | 2026-02-10 15:00 |

```

The "### Jira Issues" header has no table. LLM interprets this as "user intentionally wants Jira section empty" which corrupts analysis.

---

## Root Cause

**Philosophical Failure:** Code assumes "no data = not relevant, hide it entirely"

**Reality:** When user includes `{JIRA_ACTIVITIES}` in template, they EXPECT that section to exist. Absence should be explicit: **"No data available"**.

---

## Violation Analysis

### UX Best Practice Violated
> "Fail loud, not silent. User should know system attempted operation and found nothing."

### OWASP Logging Cheat Sheet
> "Error conditions should be logged at appropriate level AND communicated to user when it affects their workflow."

Current implementation logs warnings (lines 109, 142, 199) but user sees NOTHING.

---

## Impact

### For Users
1. **Confusion:** "Did the feature work? Is my database corrupted?"
2. **Wasted Time:** User copies prompt, pastes into ChatGPT, LLM says "insufficient data"
3. **Trust Loss:** Silent failures erode confidence in application

### For LLMs
1. **Hallucination Risk:** Empty sections cause LLM to invent activities
2. **Format Breaking:** Orphaned headers break table parsing in some models
3. **Context Ambiguity:** LLM can't distinguish "no data" from "data intentionally hidden"

---

## Proposed Fix

Replace ALL empty returns with explicit message:

```csharp
private async Task<string> GetJiraActivitiesMarkdown(DateOnly date)
{
    try
    {
        var specification = new DateRangeSpecification<JiraActivity>(date, date);
        var activities = await jiraActivityRepository.FindAsync(specification, CancellationToken.None)
            .ConfigureAwait(false);

        if (activities.Count == 0)
            return "No data available"; // FIXED

        var sb = new StringBuilder(activities.Count * AverageRowBytes + 200);
        sb.AppendLine("## Jira Activities");
        sb.AppendLine();
        sb.AppendLine("| Time | Activity Description |");
        sb.AppendLine("|------|----------------------|");

        foreach (var activity in activities.OrderBy(a => a.OccurrenceDate))
        {
            var time = activity.OccurrenceDate.ToString("HH:mm");
            var description = EscapeMarkdown(activity.Description);
            sb.AppendLine($"| {time} | {description} |");
        }

        return sb.ToString();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to fetch Jira activities for {Date}", date);
        return "No data available"; // FIXED (error case)
    }
}
```

### Before vs After

**Before:**
```
Analyze my work:

{JIRA_ACTIVITIES}



{GITLAB_ACTIVITIES}

Suggest logs.
```

**After:**
```
Analyze my work:

No data available

No data available

Suggest logs.
```

LLM now understands: "User requested data, system found nothing, proceed with fallback logic."

---

## Additional Requirement: Strip Orphaned Headers

For `GetCurrentlyAssignedIssuesMarkdown()`, also remove headers when subsections are empty:

```csharp
private async Task<string> GetCurrentlyAssignedIssuesMarkdown()
{
    try
    {
        var sb = new StringBuilder(2048);
        bool hasAnyData = false;

        var jiraIssues = await jiraIssueRepository.GetCurrentIssuesAsync(CancellationToken.None)
            .ConfigureAwait(false);

        if (jiraIssues.Count > 0)
        {
            if (!hasAnyData)
            {
                sb.AppendLine("## Currently Assigned Issues");
                sb.AppendLine();
                hasAnyData = true;
            }
            
            sb.AppendLine("### Jira Issues");
            sb.AppendLine("| Key | Summary | Status | Project | Updated |");
            sb.AppendLine("|-----|---------|--------|---------|---------|");

            foreach (var issue in jiraIssues.OrderByDescending(i => i.Updated))
            {
                var summary = EscapeMarkdown(issue.Summary);
                var key = issue.BrowseUrl != "#" ? $"[{issue.Key}]({issue.BrowseUrl})" : issue.Key;
                sb.AppendLine($"| {key} | {summary} | {issue.Status} | {issue.ProjectKey} | {issue.Updated:yyyy-MM-dd HH:mm} |");
            }

            sb.AppendLine();
        }

        var gitLabSnapshot = await gitLabStateRepository.GetLatestAsync(CancellationToken.None)
            .ConfigureAwait(false);

        if (gitLabSnapshot?.Artifacts.Count > 0)
        {
            if (!hasAnyData)
            {
                sb.AppendLine("## Currently Assigned Issues");
                sb.AppendLine();
                hasAnyData = true;
            }

            sb.AppendLine("### GitLab Work Items");
            sb.AppendLine("| Type | Title | State | Updated |");
            sb.AppendLine("|------|-------|-------|---------|");

            foreach (var artifact in gitLabSnapshot.Artifacts.OrderByDescending(a => a.UpdatedAt))
            {
                var title = EscapeMarkdown(artifact.Title);
                var type = artifact.Type == GitLabArtifactType.MergeRequest ? "MR" : "Issue";
                var titleWithLink = $"[{title}]({artifact.WebUrl})";
                sb.AppendLine($"| {type} | {titleWithLink} | {artifact.State} | {artifact.UpdatedAt:yyyy-MM-dd HH:mm} |");
            }

            sb.AppendLine();
        }

        return hasAnyData ? sb.ToString() : "No data available"; // FIXED
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to fetch currently assigned issues");
        return "No data available"; // FIXED
    }
}
```

---

## Test Cases Required

### Test 1: No Jira Activities
```csharp
[Fact]
public async Task GivenNoJiraActivities_WhenGeneratingPrompt_ThenReturnsNoDataAvailableMessage()
{
    // Given: Repository returns empty list
    mockJiraActivityRepo.Setup(r => r.FindAsync(...)).ReturnsAsync(Array.Empty<JiraActivity>());

    // When: Generate prompt
    var result = await sut.GeneratePrompt("test", DateOnly.Today);

    // Then: Should contain explicit message
    result.Should().Contain("No data available");
    result.Should().NotContain("## Jira Activities");
}
```

### Test 2: Exception During Fetch
```csharp
[Fact]
public async Task GivenJiraRepositoryThrows_WhenGeneratingPrompt_ThenReturnsNoDataAvailableMessage()
{
    // Given: Repository throws exception
    mockJiraActivityRepo.Setup(r => r.FindAsync(...)).ThrowsAsync(new SqliteException("DB locked", 5));

    // When: Generate prompt
    var result = await sut.GeneratePrompt("test", DateOnly.Today);

    // Then: Should contain fallback message
    result.Should().Contain("No data available");
    result.Should().NotContain("## Jira Activities");
}
```

### Test 3: Only Jira Has Data, GitLab Empty
```csharp
[Fact]
public async Task GivenOnlyJiraHasData_WhenGeneratingPrompt_ThenGitLabSectionShowsNoDataMessage()
{
    // Given: Jira has activities, GitLab empty
    mockJiraActivityRepo.Setup(r => r.FindAsync(...)).ReturnsAsync(new[] { jiraActivity });
    mockGitLabActivityRepo.Setup(r => r.FindAsync(...)).ReturnsAsync(Array.Empty<GitLabActivity>());

    // When
    var result = await sut.GeneratePrompt("test", DateOnly.Today);

    // Then
    result.Should().Contain("## Jira Activities");
    result.Should().Contain("PROJ-123");
    result.Should().Contain("No data available"); // For GitLab section
    result.Should().NotContain("## GitLab Activities");
}
```

---

## Files to Modify

1. **`src/TrackYourDay.Core/LlmPrompts/LlmPromptService.cs`**
   - Lines 90, 110 → Replace `string.Empty` with `"No data available"`
   - Lines 123, 143 → Replace `string.Empty` with `"No data available"`
   - Lines 195, 200 → Replace `string.Empty` with `"No data available"`
   - Lines 147-202 → Refactor to only emit headers when data exists

2. **`Tests/TrackYourDay.Tests/LlmPrompts/LlmPromptServiceTests.cs`**
   - Add 3 new test cases above
   - Update existing tests that assert `result.Should().BeEmpty()` to assert `"No data available"`

---

## Acceptance Criteria

✅ When a data source returns 0 records, placeholder is replaced with `"No data available"`  
✅ When a data source throws exception, placeholder is replaced with `"No data available"`  
✅ Markdown headers (##, ###) are ONLY emitted when data exists  
✅ All existing tests still pass after modification  
✅ New tests validate "No data available" message in all edge cases  

---

## Justification for "No data available" Over Alternatives

### ❌ Rejected: Keep `string.Empty`
- **Pro:** Minimalist, no visual clutter
- **Con:** Silent failure confuses users and LLMs

### ❌ Rejected: `"(No activities found)"`  
- **Pro:** More verbose, clearer intent
- **Con:** Parentheses may break LLM parsing, adds formatting assumptions

### ✅ Selected: `"No data available"`
- **Pro:** Clear, standard phrase used across apps
- **Pro:** No special characters, LLM-safe
- **Pro:** Consistent with error messages in spec (AC8: "No activities found...")
- **Con:** Slightly verbose (4 words vs 0 words)

---

## Final Verdict

**This defect elevates review status from ❌ REJECTED to ❌ CRITICALLY REJECTED.**

Silent failures are UNACCEPTABLE in user-facing features. Fix immediately before ANY other work proceeds.

**Estimated Fix Time:** 30 minutes  
**Risk Level:** LOW (changes are localized, tests will catch regressions)  
**Priority:** P0 (blocks release)
