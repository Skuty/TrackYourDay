# Self-Audit: AI Prompt Generator

## Risk 1: Regex DoS with Malicious Titles
**Description:** `JiraKeyExtractor` uses `[GeneratedRegex]` with pattern `[A-Z]{2,10}-\d+`. If an attacker creates activity events with extremely long strings of uppercase letters followed by hyphens, the regex engine could catastrophically backtrack.

**Mitigation:**
- Current pattern has bounded quantifier (`{2,10}`), reducing risk
- Consider adding input length validation (reject titles >500 chars)
- .NET 7+ `GeneratedRegex` includes automatic optimization

**Status:** Low risk—acceptable for local-only app with bounded quantifiers

---

## Risk 2: PII Leakage in Prompts
**Description:** `GetDailyActivitiesForPromptQueryHandler` does not filter window titles containing emails, passwords, or sensitive URLs. If user works with login forms or email clients, these values could leak into prompts copied to external LLMs.

**Mitigation Options:**
1. Add PII detection regex in handler (email patterns, `password=`, etc.)
2. Provide UI toggle "Exclude sensitive titles" with configurable patterns
3. Warn user in UI: "Review prompt before sharing externally"

**Status:** High risk—should add basic PII filtering before production release

---

## Risk 3: Memory Pressure with Large Activity Sets
**Description:** `GetDailyActivitiesForPromptQueryHandler` loads all activities for a date into memory, then performs grouping/aggregation in-process. For power users with >1000 activities/day, this could consume significant memory and delay prompt generation.

**Mitigation:**
- Add activity cap (500 max per query)
- Repository already filters in SQL via specification pattern
- Current implementation adequate for typical usage (50-200 activities/day)

**Status:** Medium risk—monitor performance with real-world data

---

## Risk 4: No Validation for Missing Template Keys
**Description:** `PromptTemplateProvider` stores templates as hardcoded strings. If a template is modified and forgets to include `{DATE}`, `{ACTIVITY_LIST}`, or `{JIRA_KEYS}`, the generated prompt will have raw placeholder text.

**Mitigation:**
- Unit tests verify all templates contain required placeholders (implemented)
- Runtime validation in `PromptGeneratorService.GeneratePrompt()` could check result for leftover `{...}`

**Status:** Low risk—unit tests provide adequate coverage

---

## Risk 5: Clipboard API Failure Handling
**Description:** `AiPromptGenerator.razor` uses MAUI's `Clipboard.SetTextAsync()`. On some Windows configurations (virtualized environments, RDP sessions), clipboard access may fail with `PlatformNotSupportedException`.

**Mitigation:**
- Wrapped in try-catch with user-friendly error message (implemented)
- Fallback: prompt is already displayed in read-only textbox (user can select manually)
- Log exception details for diagnostics

**Status:** Low risk—current error handling adequate

---

## Missing Tests
1. **Edge case:** Activity with null or empty `GetDescription()` result
2. **Edge case:** Activities spanning midnight boundary (start/end on different dates)
3. **Performance test:** 500+ activities in single query (measure <500ms target)
4. **Validation test:** All templates contain required placeholders (partially covered)

---

## Performance Bottlenecks
- **Database query:** Repository uses `ActivityByDateSpecification` with JSON extraction—indexes may not optimize well
- **Regex compilation:** `JiraKeyExtractor` uses `[GeneratedRegex]` (optimal for .NET 7+)
- **String concatenation:** `FormatActivityList()` uses `StringBuilder` (correct approach)
- **In-memory grouping:** LINQ `.GroupBy()` on activities—acceptable for <500 records

**Optimization Path:**
1. Add index on extracted `StartDate` field in SQLite if query performance degrades
2. Consider caching deduplicated activities for "today" with 5-minute TTL

---

## Security Vulnerabilities
- **No authentication:** Any user with app access can generate prompts (acceptable for single-user desktop app)
- **No audit log:** Prompt generation not logged (consider adding for enterprise version)
- **External LLM risk:** User manually copies prompt—no control over destination (document in help text)
- **PII in window titles:** No filtering of sensitive data (HIGH PRIORITY—see Risk 2)

---

## Code Quality Issues
1. **Nullable warnings:** All public APIs have XML docs and proper null handling
2. **Async consistency:** Handler uses `Task.FromResult` (synchronous operation)—acceptable for repository pattern
3. **Dependency injection:** All services properly registered in `ServiceCollections.cs`
4. **Test coverage:** Core business logic covered—UI interaction tests missing (acceptable)

---

## Production Readiness Checklist
- [x] Unit tests for all service classes
- [x] MediatR query/handler implementation
- [x] Blazor UI with error handling
- [x] Service registration in DI container
- [x] Navigation menu integration
- [ ] PII filtering for sensitive window titles
- [ ] Performance testing with 500+ activities
- [ ] User documentation/help text
- [ ] Add logging for prompt generation events

---

## Recommended Next Steps
1. **Immediate:** Add PII filtering regex to exclude common patterns (email, password fields)
2. **Short-term:** Add performance monitoring/logging for slow query detection
3. **Long-term:** Consider UI toggle for "include/exclude" specific applications in prompt
4. **Documentation:** Add tooltip/help text explaining external LLM workflow
