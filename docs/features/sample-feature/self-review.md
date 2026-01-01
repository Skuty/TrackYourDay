# Self-Audit: Activity Export to CSV

## Implementation Risks

### Risk 1: CSV Injection Vulnerability
**Description:** Malicious window titles starting with `=`, `+`, `@`, or `-` could be interpreted as formulas in Excel, leading to CSV injection attacks.

**Example Scenario:**
```
WindowTitle: "=cmd|'/c calc'!A1"
```

**Impact:** High - Code execution when user opens CSV in Excel

**Mitigation Required:**
- Prefix potentially dangerous cells with single quote `'` or tab character
- Add unit test with injection payloads
- Consider adding security warning in UI

**Test Coverage:** Missing - Add to `CsvExportServiceTests`

---

### Risk 2: Timezone Confusion
**Description:** Activities are stored in UTC but users expect local time in exports. If not clearly documented, users may misinterpret timestamps.

**Example Scenario:**
- User in PST (UTC-8) sees activity at 2:00 AM that actually occurred at 10:00 PM previous day

**Impact:** Medium - User confusion, inaccurate time analysis

**Mitigation Required:**
- Convert all timestamps to local time before export
- Add timezone offset to CSV header comment: `# Exported in Pacific Standard Time (UTC-8)`
- Include test for timezone conversion

**Test Coverage:** Partial - Add timezone-specific test case

---

### Risk 3: File Handle Leak on Cancellation
**Description:** If export is cancelled mid-operation, file stream may not be properly disposed, leaving file locked.

**Example Scenario:**
```csharp
await using var writer = new StreamWriter(filePath);
await WriteDataAsync(writer, cancellationToken); // Cancelled here
// Dispose may not be called if exception thrown
```

**Impact:** Medium - File remains locked until app restart

**Mitigation Required:**
- Ensure `await using` pattern is used consistently
- Add explicit cancellation handling with proper cleanup
- Test cancellation scenario: `[Fact] public async Task GivenExportInProgress_WhenCancelled_ThenFileIsDisposedProperly()`

**Test Coverage:** Missing - Add cancellation test

---

### Risk 4: Large Dataset OutOfMemoryException
**Description:** Despite streaming intent, accidental materialization (e.g., `.ToList()`) could load entire dataset into memory.

**Example Scenario:**
```csharp
// BAD - materializes entire query
var activities = await repo.GetActivitiesAsync(start, end).ToListAsync();

// GOOD - streams one at a time
await foreach (var activity in repo.GetActivitiesAsync(start, end))
```

**Impact:** High - App crash for users with 50K+ activities

**Mitigation Required:**
- Code review to ensure no accidental materialization
- Add integration test with 10K+ records
- Monitor memory usage during export in telemetry

**Test Coverage:** Missing - Add large dataset integration test (marked with `[Trait("Category", "Integration")]`)

---

### Risk 5: Progress Reporting Overhead
**Description:** Reporting progress after every single record could overwhelm UI thread with too many updates.

**Example Scenario:**
- 100K records × progress update = 100K UI thread marshalling calls

**Impact:** Medium - UI sluggishness, reduced export performance

**Mitigation Required:**
- Batch progress updates (report every 100 records, not every 1)
- Debounce progress notifications in state container
- Add performance test: `[Fact] public async Task GivenLargeExport_WhenReportingProgress_ThenCompletesWithin10Seconds()`

**Test Coverage:** Missing - Add performance benchmark

---

## Missing Test Coverage

### Critical Tests Missing
1. **CSV Injection:** Test window titles with formula characters (`=`, `+`, `@`, `-`, `|`)
2. **Special Characters:** Test commas, quotes, newlines, Unicode characters in data
3. **Timezone Conversion:** Verify timestamps are converted to local time
4. **Cancellation:** Test proper cleanup when CancellationToken is triggered
5. **Large Datasets:** Integration test with 10K+ records
6. **Disk Space:** Test behavior when insufficient disk space available
7. **File Permissions:** Test handling of read-only or inaccessible Downloads folder

### Edge Cases to Test
```csharp
[Theory]
[InlineData("=cmd|'/c calc'!A1")] // CSV injection
[InlineData("\"Quoted, with comma\"")] // Embedded quotes and commas
[InlineData("Line1\nLine2")] // Newlines
[InlineData("🚀 Unicode emoji")] // Unicode
[InlineData("")] // Empty string
[InlineData(null)] // Null (if allowed)
[InlineData("String with 1000 characters...")] // Very long strings
public async Task GivenWindowTitleWithSpecialCharacters_WhenExporting_ThenCsvIsValid(string windowTitle)
{
    // Test implementation
}
```

---

## Performance Bottlenecks

### Bottleneck 1: Synchronous File I/O
**Location:** `CsvExportService.WriteActivitiesAsync()`

**Issue:** If using synchronous `File.WriteAllText()` or `StreamWriter.Write()` without async variants

**Fix:** Use `StreamWriter.WriteAsync()` and `FlushAsync()` consistently

**Verification:** Profile with dotTrace or PerfView

---

### Bottleneck 2: Database Query Not Using Index
**Location:** `ActivityRepository.GetActivitiesAsync()`

**Issue:** Query may perform full table scan if no index on `StartDate`

**Fix:** 
```sql
CREATE INDEX IF NOT EXISTS idx_activities_daterange 
ON EndedActivities(StartDate, EndDate);
```

**Verification:** Run `EXPLAIN QUERY PLAN` on the query

---

### Bottleneck 3: String Concatenation in Loop
**Location:** CSV row building (if manually concatenating)

**Issue:** Using `string +=` in loop allocates new string each iteration

**Fix:** Use CsvHelper library or `StringBuilder` if manual implementation

**Verification:** Memory profiler showing excessive Gen0 collections

---

## Security Vulnerabilities

### Vulnerability 1: Path Traversal
**Location:** File path construction

**Issue:** If filename is derived from user input without sanitization:
```csharp
// BAD
var filename = $"{userInput}.csv";
var path = Path.Combine(downloads, filename);
```

**Fix:** Sanitize filename or use fixed pattern:
```csharp
var filename = $"activities_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}.csv";
```

**Test:** Verify rejection of paths like `../../../etc/passwd.csv`

---

### Vulnerability 2: Sensitive Data Exposure
**Location:** Window titles may contain passwords from password managers

**Issue:** Window title "1Password - Password: MySecretP@ssw0rd" exported to plain CSV

**Fix:** 
- Add warning dialog before export
- Consider regex filtering of known sensitive patterns
- Document in user guide

**Test:** Manual testing with password manager open

---

### Vulnerability 3: CSV Injection (Critical)
**Location:** Any user-generated content in CSV cells

**Issue:** Covered in Risk 1 above - potential RCE vulnerability

**Fix:** Sanitize all cells or use CsvHelper's built-in escaping

**Test:** Unit test with OWASP CSV injection payloads

---

## Code Quality Issues

### Issue 1: Insufficient Null Checks
Ensure all query parameters are validated:
```csharp
ArgumentNullException.ThrowIfNull(query);
ArgumentOutOfRangeException.ThrowIfGreaterThan(query.StartDate, query.EndDate);
```

### Issue 2: Missing XML Documentation
All public APIs must have XML doc comments explaining parameters and behavior.

### Issue 3: Magic Numbers
Replace hardcoded values:
```csharp
// BAD
const int BufferSize = 8192;

// GOOD
private const int FileBufferSizeInBytes = 8 * 1024; // 8 KB
```

---

## Recommendations Before Approval

1. **Add Critical Tests:**
   - CSV injection test
   - Cancellation test
   - Large dataset integration test

2. **Security Review:**
   - Implement CSV injection mitigation
   - Add user warning about sensitive data

3. **Performance Validation:**
   - Profile with 100K records
   - Verify streaming is working (no materialization)
   - Confirm progress batching is implemented

4. **Code Review:**
   - Add XML docs to all public members
   - Eliminate magic numbers
   - Ensure proper null checks

5. **Documentation:**
   - Update user guide with export feature
   - Document CSV format and column meanings
   - Add troubleshooting section for common issues
