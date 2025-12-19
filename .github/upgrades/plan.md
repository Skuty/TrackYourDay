# .NET 9.0 Upgrade Plan

## Table of Contents

- [Executive Summary](#executive-summary)
  - [Discovered Metrics](#discovered-metrics)
  - [Complexity Classification](#complexity-classification)
  - [Selected Strategy](#selected-strategy)
- [Migration Strategy](#migration-strategy)
  - [Approach Selection](#approach-selection)
  - [Dependency-Based Ordering](#dependency-based-ordering)
- [Detailed Dependency Analysis](#detailed-dependency-analysis)
  - [Dependency Graph Summary](#dependency-graph-summary)
  - [Project Groupings by Migration Phase](#project-groupings-by-migration-phase)
  - [Critical Path](#critical-path)
- [Project-by-Project Migration Plans](#project-by-project-migration-plans)
  - [TrackYourDay.Core](#trackyourdaycore)
  - [TrackYourDay.Web](#trackyourdayweb)
  - [TrackYourDay.MAUI](#trackyourdaymaui)
  - [TrackYourDay.Tests](#trackyourdaytests)
- [Package Update Reference](#package-update-reference)
  - [Common Package Updates](#common-package-updates)
  - [Project-Specific Updates](#project-specific-updates)
- [Breaking Changes Catalog](#breaking-changes-catalog)
  - [Framework Breaking Changes](#framework-breaking-changes)
  - [API Changes](#api-changes)
  - [Legacy Cryptography](#legacy-cryptography)
- [Risk Management](#risk-management)
  - [Risk Assessment](#risk-assessment)
  - [Mitigation Strategies](#mitigation-strategies)
  - [Contingency Plans](#contingency-plans)
- [Testing Strategy](#testing-strategy)
  - [Phase Testing](#phase-testing)
  - [Full Solution Validation](#full-solution-validation)
- [Complexity & Effort Assessment](#complexity--effort-assessment)
  - [Per-Project Complexity](#per-project-complexity)
  - [Phase Complexity](#phase-complexity)
- [Source Control Strategy](#source-control-strategy)
  - [Branching Strategy](#branching-strategy)
  - [Commit Strategy](#commit-strategy)
- [Success Criteria](#success-criteria)
  - [Technical Criteria](#technical-criteria)
  - [Quality Criteria](#quality-criteria)

---

## Executive Summary

This plan outlines the upgrade of the **TrackYourDay** solution from **.NET 8.0** to **.NET 9.0**. The solution consists of 4 projects including a .NET MAUI desktop application, Blazor WebAssembly components, a core class library, and a test project.

### Discovered Metrics

| Metric | Value |
|--------|-------|
| Total Projects | 4 |
| Total NuGet Packages | 30 |
| Packages Requiring Update | 6 (20%) |
| Total Lines of Code | 13,570 |
| Estimated LOC to Modify | 422+ (3.1%) |
| Dependency Depth | 2 levels |
| Circular Dependencies | None |
| Security Vulnerabilities | None ? |
| Binary Incompatible APIs | 23 |
| Source Incompatible APIs | 399 |

### Complexity Classification

**Classification: Simple**

| Criterion | Value | Threshold |
|-----------|-------|-----------|
| Project Count | 4 | ?5 ? |
| Dependency Depth | 2 | ?2 ? |
| High-Risk Projects | 0 | 0 ? |
| Security Vulnerabilities | 0 | 0 ? |
| Circular Dependencies | None | None ? |

**Justification**: This is a small, well-structured solution with clear dependency relationships. All projects are SDK-style and currently on .NET 8.0. Package compatibility is excellent (80% already compatible). The majority of API issues are "source incompatible" warnings related to .NET MAUI APIs that may require re-compilation but typically don't require code changes.

### Selected Strategy

**All-At-Once Strategy** - All 4 projects upgraded simultaneously in a single atomic operation.

**Rationale**:
- Small solution with only 4 projects
- All projects currently on .NET 8.0 (homogeneous)
- Clear, simple dependency structure (no cycles)
- All 6 packages requiring updates have known target versions
- Low overall difficulty rating across all projects (?? Low)
- No security vulnerabilities blocking upgrade
- Single commit approach maintains clean git history

**Expected Iterations**: 3-4 detail iterations (fast batch approach)

---

## Migration Strategy

### Approach Selection

**Selected: All-At-Once (Atomic Upgrade)**

This approach updates all project files, package references, and fixes compilation errors in a single coordinated operation. The solution will be in a temporarily non-building state during the upgrade, but will return to a fully working state once the atomic operation completes.

**Why All-At-Once is appropriate:**
- **Small scope**: 4 projects can be reasoned about together
- **Homogeneous stack**: All projects on net8.0 (MAUI has Windows platform variant)
- **No blocking issues**: All packages have clear upgrade paths
- **Fast completion**: Minimizes time in "broken" state
- **Clean history**: Single commit captures entire upgrade

**Trade-offs accepted:**
- Cannot incrementally validate individual project upgrades
- If issues arise, must troubleshoot across entire solution
- Requires focused attention to complete in one session

### Dependency-Based Ordering

While all projects are updated atomically, the **order of specification** respects dependency hierarchy for clarity:

```
Level 0 (Leaf):     TrackYourDay.Core      ? No project dependencies
Level 1:            TrackYourDay.Web       ? Depends on Core
Level 1:            TrackYourDay.Tests     ? Depends on Core
Level 2 (Root):     TrackYourDay.MAUI      ? Depends on Core + Web
```

**Update Sequence within Atomic Operation:**
1. Update all TargetFramework properties (all projects simultaneously)
2. Update all package references (all projects simultaneously)
3. Restore dependencies
4. Build and fix all compilation errors
5. Verify solution builds with 0 errors

---

## Detailed Dependency Analysis

### Dependency Graph Summary

```
TrackYourDay.Core (net8.0 ? net9.0)
    ?
    ??? TrackYourDay.Web (net8.0 ? net9.0)
    ?       ?
    ?       ??? TrackYourDay.MAUI (net8.0-windows10.0.19041.0 ? net9.0-windows10.0.22000.0)
    ?
    ??? TrackYourDay.Tests (net8.0 ? net9.0)
    ?
    ??? TrackYourDay.MAUI (also depends directly on Core)
```

**Key Observations:**
- **TrackYourDay.Core** is the foundation - all other projects depend on it
- **TrackYourDay.Web** is shared Blazor component library used by MAUI
- **TrackYourDay.MAUI** is the root application (top of dependency tree)
- **TrackYourDay.Tests** depends only on Core (test isolation)

### Project Groupings by Migration Phase

**Phase 1: Atomic Upgrade** (Single coordinated operation)

All projects updated simultaneously:

| Project | Current Framework | Target Framework | Type |
|---------|------------------|------------------|------|
| TrackYourDay.Core | net8.0 | net9.0 | ClassLibrary |
| TrackYourDay.Web | net8.0 | net9.0 | AspNetCore (Blazor WASM) |
| TrackYourDay.MAUI | net8.0-windows10.0.19041.0 | net9.0-windows10.0.22000.0 | MAUI Windows App |
| TrackYourDay.Tests | net8.0 | net9.0 | xUnit Test Project |

### Critical Path

The critical path for this upgrade is straightforward:

1. **Framework Update**: All 4 project files updated with new TargetFramework
2. **Package Updates**: 6 packages across 3 projects
3. **Compilation Fixes**: Address any build errors (primarily TimeSpan API changes, Legacy Cryptography)
4. **Test Validation**: Run TrackYourDay.Tests to verify functionality

**No blocking dependencies** - all projects can be processed in single atomic operation.

---

## Project-by-Project Migration Plans

### TrackYourDay.Core

**Current State:**
- **Target Framework**: net8.0
- **Project Kind**: ClassLibrary
- **Dependencies**: 0 project references
- **Dependants**: 3 projects (Web, MAUI, Tests)
- **Files**: 114 | **LOC**: 6,501
- **Package Issues**: 3 | **API Issues**: 12
- **Risk Level**: ?? Low

**Target State:**
- **Target Framework**: net9.0
- **Updated Packages**: 3

**Migration Steps:**

1. **Update TargetFramework**
   - Change `<TargetFramework>net8.0</TargetFramework>` to `<TargetFramework>net9.0</TargetFramework>`

2. **Update Package References**
   | Package | Current | Target |
   |---------|---------|--------|
   | Microsoft.Data.Sqlite | 8.0.1 | 9.0.11 |
   | Microsoft.Extensions.Logging.Abstractions | 8.0.0 | 9.0.11 |
   | Newtonsoft.Json | 13.0.3 | 13.0.4 |

3. **Expected Breaking Changes**
   - **Legacy Cryptography** (SKIPPED): `Rfc2898DeriveBytes` constructor in `IEncryptionService.cs` (lines 36, 67) uses deprecated overload. **Decision: Suppress warning and defer update** - The existing code will continue to function. Add `#pragma warning disable SYSLIB0041` to suppress the obsolete warning.
   - **TimeSpan APIs**: Multiple files use `TimeSpan.FromMinutes()`, `TimeSpan.FromHours()`, `TimeSpan.FromSeconds()` with `double` parameters. In .NET 9, these have new overloads; existing code will compile but may produce warnings about ambiguous calls if integer literals are used.

4. **Files Requiring Review**
   - `src\TrackYourDay.Core\IEncryptionService.cs` - Add warning suppression for legacy cryptography (lines 36, 67)
   - `src\TrackYourDay.Core\ApplicationTrackers\Breaks\BreaksSettings.cs` - TimeSpan API
   - `src\TrackYourDay.Core\ApplicationTrackers\Breaks\BreaksSettingsService.cs` - TimeSpan API
   - `src\TrackYourDay.Core\SystemTrackers\ActivitiesSettings.cs` - TimeSpan API
   - `src\TrackYourDay.Core\SystemTrackers\ActivitiesSettingsService.cs` - TimeSpan API
   - `src\TrackYourDay.Core\Insights\Workdays\WorkdayDefinition.cs` - TimeSpan API

5. **Validation Checklist**
   - [ ] Project builds without errors
   - [ ] No new compiler warnings introduced (except suppressed SYSLIB0041)
   - [ ] All dependent projects still reference correctly

---

### TrackYourDay.Web

**Current State:**
- **Target Framework**: net8.0
- **Project Kind**: AspNetCore (Blazor WebAssembly)
- **Dependencies**: 1 project reference (Core)
- **Dependants**: 1 project (MAUI)
- **Files**: 56 | **LOC**: 187
- **Package Issues**: 3 | **API Issues**: 2
- **Risk Level**: ?? Low

**Target State:**
- **Target Framework**: net9.0
- **Updated Packages**: 3

**Migration Steps:**

1. **Update TargetFramework**
   - Change `<TargetFramework>net8.0</TargetFramework>` to `<TargetFramework>net9.0</TargetFramework>`

2. **Update Package References**
   | Package | Current | Target |
   |---------|---------|--------|
   | Microsoft.AspNetCore.Components.WebAssembly | 8.0.10 | 9.0.11 |
   | Microsoft.AspNetCore.Components.WebAssembly.DevServer | 8.0.10 | 9.0.11 |
   | Microsoft.Data.Sqlite | 8.0.1 | 9.0.11 |

3. **Expected Breaking Changes**
   - **ServiceCollectionExtensions**: Binary incompatible type in `ServiceCollections.cs` (line 9). The `AddMediatR` extension method location may have changed - verify compilation.
   - **TimeSpan API**: `Settings.razor` uses `TimeSpan.FromMinutes()` - same pattern as Core project.

4. **Files Requiring Review**
   - `src\TrackYourDay.Web\ServiceRegistration\ServiceCollections.cs` - ServiceCollectionExtensions (line 9)
   - `src\TrackYourDay.Web\Pages\Settings.razor` - TimeSpan API

5. **Validation Checklist**
   - [ ] Project builds without errors
   - [ ] Blazor components render correctly (verify at runtime)
   - [ ] Dependency injection still works

---

### TrackYourDay.MAUI

**Current State:**
- **Target Framework**: net8.0-windows10.0.19041.0
- **Project Kind**: MAUI Windows Application
- **Dependencies**: 2 project references (Core, Web)
- **Dependants**: 0 (root application)
- **Files**: 38 | **LOC**: 1,226
- **Package Issues**: 1 | **API Issues**: 333
- **Risk Level**: ?? Low (most issues are in generated files)

**Target State:**
- **Target Framework**: net9.0-windows10.0.22000.0
- **Updated Packages**: 1

**Migration Steps:**

1. **Update TargetFramework**
   - Change `<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>` to `<TargetFramework>net9.0-windows10.0.22000.0</TargetFramework>`
   - Note: Windows SDK version changes from 19041 to 22000

2. **Update Package References**
   | Package | Current | Target |
   |---------|---------|--------|
   | Microsoft.Extensions.Logging.Debug | 8.0.0 | 9.0.11 |

3. **Expected Breaking Changes**
   - **Generated Files**: Most API issues (333) are in `obj\` generated files (`XamlTypeInfo.g.cs`). These will be regenerated automatically after rebuild with .NET 9.
   - **MauiWinUIApplication**: Binary incompatible constructor - regenerated by build process.
   - **MauiNavigationView**: Property changes - handled by MAUI framework update.

4. **Important Notes**
   - The high issue count (333) is misleading - these are in generated XAML type info files
   - Clean build recommended after framework update to regenerate all platform-specific code
   - No manual code changes expected in source files

5. **Files Requiring Review**
   - No source files require manual changes
   - Generated files in `obj\` folder will be regenerated

6. **Validation Checklist**
   - [ ] Clean and rebuild succeeds
   - [ ] Application launches correctly
   - [ ] Window management works (title, size, etc.)
   - [ ] Navigation functions correctly

---

### TrackYourDay.Tests

**Current State:**
- **Target Framework**: net8.0
- **Project Kind**: xUnit Test Project
- **Dependencies**: 1 project reference (Core)
- **Dependants**: 0
- **Files**: 48 | **LOC**: 5,656
- **Package Issues**: 0 | **API Issues**: 75
- **Risk Level**: ?? Low

**Target State:**
- **Target Framework**: net9.0
- **Updated Packages**: 0 (all compatible)

**Migration Steps:**

1. **Update TargetFramework**
   - Change `<TargetFramework>net8.0</TargetFramework>` to `<TargetFramework>net9.0</TargetFramework>`

2. **No Package Updates Required**
   All test packages are compatible:
   - coverlet.collector 6.0.0 ?
   - FluentAssertions 6.12.0 ?
   - Microsoft.NET.Test.Sdk 17.7.2 ?
   - Moq 4.20.69 ?
   - xunit 2.5.1 ?
   - xunit.runner.visualstudio 2.5.1 ?

3. **Expected Breaking Changes**
   - **TimeSpan APIs**: 75 source incompatible issues - all related to `TimeSpan.FromMinutes()`, `TimeSpan.FromHours()`, etc. in test assertions. These are typically false positives that will compile without issues.

4. **Validation Checklist**
   - [ ] Project builds without errors
   - [ ] All tests pass
   - [ ] No test framework compatibility issues

---

## Package Update Reference

### Common Package Updates

These packages are used across multiple projects and require version alignment:

| Package | Current | Target | Projects Affected | Update Reason |
|---------|---------|--------|-------------------|---------------|
| Microsoft.Data.Sqlite | 8.0.1 | 9.0.11 | Core, Web | Framework alignment |

### Project-Specific Updates

**TrackYourDay.Core (3 packages)**
| Package | Current | Target |
|---------|---------|--------|
| Microsoft.Data.Sqlite | 8.0.1 | 9.0.11 |
| Microsoft.Extensions.Logging.Abstractions | 8.0.0 | 9.0.11 |
| Newtonsoft.Json | 13.0.3 | 13.0.4 |

**TrackYourDay.Web (3 packages)**
| Package | Current | Target |
|---------|---------|--------|
| Microsoft.AspNetCore.Components.WebAssembly | 8.0.10 | 9.0.11 |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer | 8.0.10 | 9.0.11 |
| Microsoft.Data.Sqlite | 8.0.1 | 9.0.11 |

**TrackYourDay.MAUI (1 package)**
| Package | Current | Target |
|---------|---------|--------|
| Microsoft.Extensions.Logging.Debug | 8.0.0 | 9.0.11 |

**TrackYourDay.Tests (0 packages)**
- All packages are .NET 9.0 compatible, no updates required

### Packages Remaining Unchanged (24)

These packages are already compatible with .NET 9.0:

| Package | Version | Status |
|---------|---------|--------|
| coverlet.collector | 6.0.0 | ? Compatible |
| FluentAssertions | 6.12.0 | ? Compatible |
| MediatR | 12.1.1 | ? Compatible |
| Microsoft.AspNetCore.Components.WebView.Maui | 8.0.3 | ? Compatible |
| Microsoft.Maui.Controls | 8.0.3 | ? Compatible |
| Microsoft.Maui.Controls.Compatibility | 8.0.3 | ? Compatible |
| Microsoft.ML | 3.0.0 | ? Compatible |
| Microsoft.ML.FastTree | 3.0.0 | ? Compatible |
| Microsoft.ML.TensorFlow | 3.0.0 | ? Compatible |
| Microsoft.ML.TimeSeries | 3.0.0 | ? Compatible |
| Microsoft.NET.Test.Sdk | 17.7.2 | ? Compatible |
| Moq | 4.20.69 | ? Compatible |
| MudBlazor | 6.10.0 | ? Compatible |
| Quartz | 3.7.0 | ? Compatible |
| Quartz.Extensions.DependencyInjection | 3.7.0 | ? Compatible |
| Quartz.Extensions.Hosting | 3.7.0 | ? Compatible |
| Serilog | 3.0.1 | ? Compatible |
| Serilog.Extensions.Logging | 7.0.0 | ? Compatible |
| Serilog.Sinks.Console | 4.1.0 | ? Compatible |
| Serilog.Sinks.Debug | 2.0.0 | ? Compatible |
| Serilog.Sinks.File | 5.0.0 | ? Compatible |
| Serilog.Sinks.Map | 1.0.2 | ? Compatible |
| xunit | 2.5.1 | ? Compatible |
| xunit.runner.visualstudio | 2.5.1 | ? Compatible |

---

## Breaking Changes Catalog

### Framework Breaking Changes

#### TimeSpan Factory Methods (.NET 9)

**Impact**: Source Incompatible (potential ambiguous call warnings)

In .NET 9, `TimeSpan` factory methods have new overloads accepting `int` parameters:
- `TimeSpan.FromMinutes(int)`
- `TimeSpan.FromHours(int)`
- `TimeSpan.FromSeconds(int)`
- `TimeSpan.FromMilliseconds(int)`

**Affected Code Pattern**:
```csharp
// May produce CS0121 ambiguous call warning with integer literals
TimeSpan.FromMinutes(5)    // Was double, now ambiguous with int overload
TimeSpan.FromHours(8)      // Same issue
```

**Resolution Options**:
1. **No action needed** if code compiles without errors (most common)
2. If ambiguity errors occur, explicitly cast: `TimeSpan.FromMinutes((double)5)`
3. Or use the new `int` overload explicitly (preferred for performance)

**Files Affected**: 
- Core: 7 files (12 occurrences)
- Web: 1 file (1 occurrence)
- Tests: 18 files (75 occurrences)

### API Changes

#### ServiceCollectionExtensions Namespace Change

**Impact**: Binary Incompatible

**Location**: `src\TrackYourDay.Web\ServiceRegistration\ServiceCollections.cs` (line 9)

**Issue**: The `ServiceCollectionExtensions` type location may have changed between framework versions.

**Resolution**: Verify that `AddMediatR` extension method resolves correctly after package restore. The MediatR package (12.1.1) is compatible, so this is likely a false positive that will resolve with recompilation.

#### MAUI Platform Types (Generated Code)

**Impact**: Binary Incompatible (auto-resolved)

**Affected Types**:
- `Microsoft.Maui.Platform.MauiNavigationView`
- `Microsoft.Maui.Platform.RootNavigationView`
- `Microsoft.Maui.Controls.Platform.ShellView`
- `Microsoft.Maui.MauiWinUIApplication`

**Resolution**: These are in generated `obj\` files. Clean build will regenerate compatible versions. **No manual action required**.

### Legacy Cryptography

**Impact**: Source Incompatible (compiler warning)

**Location**: `src\TrackYourDay.Core\IEncryptionService.cs` (lines 36, 67)

**Issue**: `Rfc2898DeriveBytes` constructor using `(string password, byte[] salt)` is obsolete in .NET 9. This constructor uses SHA1 by default which is not recommended.

**Current Code**:
```csharp
var key = new Rfc2898DeriveBytes(salt, Encoding.UTF8.GetBytes(salt)).GetBytes(32);
```

**Decision: SKIP UPDATE - Suppress Warning**

The legacy cryptography update is being deferred. To maintain backward compatibility with existing encrypted data and minimize scope of this upgrade:

1. Add warning suppression to `IEncryptionService.cs`:
```csharp
#pragma warning disable SYSLIB0041 // Rfc2898DeriveBytes obsolete constructor
var key = new Rfc2898DeriveBytes(salt, Encoding.UTF8.GetBytes(salt)).GetBytes(32);
#pragma warning restore SYSLIB0041
```

**Rationale for Skipping**:
- Changing the hash algorithm would break existing encrypted data
- The code is functional and will continue to work
- Security improvement can be addressed in a separate, dedicated effort
- Reduces risk and scope of .NET 9 upgrade

**Future Consideration**: A separate task should be created to:
- Migrate to SHA256 with proper iteration count
- Handle data migration for existing encrypted values
- Implement backward compatibility layer if needed

?? **Note**: This is documented technical debt. The warning suppression makes the decision explicit and traceable.

---

## Risk Management

### Risk Assessment

| Project | Risk Level | Description | Mitigation |
|---------|------------|-------------|------------|
| TrackYourDay.Core | ?? Low | Legacy crypto needs attention but is isolated to one file | Fix during build phase; test encryption/decryption |
| TrackYourDay.Web | ?? Low | Minimal changes, well-structured Blazor project | Standard verification |
| TrackYourDay.MAUI | ?? Low | High issue count but all in generated files | Clean rebuild resolves issues |
| TrackYourDay.Tests | ?? Low | No package changes, TimeSpan warnings only | Run full test suite |

### Mitigation Strategies

1. **Legacy Cryptography Risk** (DEFERRED)
   - Add `#pragma warning disable SYSLIB0041` around affected code
   - Document as technical debt for future remediation
   - Existing functionality preserved - no data migration needed
   - Creates follow-up task for proper cryptography update

2. **MAUI Generated Code Risk**
   - Clean `obj` and `bin` folders before rebuild
   - Use `dotnet clean` followed by `dotnet build`
   - Windows SDK version change (19041 ? 22000) is handled by framework

3. **Blazor WebAssembly Risk**
   - Verify component rendering after upgrade
   - Check browser console for any JavaScript interop issues
   - MudBlazor compatibility already confirmed

### Contingency Plans

| Issue | Contingency |
|-------|-------------|
| TimeSpan ambiguity errors | Add explicit casts `(double)` to affected calls |
| Crypto compilation errors | Warning suppression already planned with `#pragma warning disable SYSLIB0041` |
| MAUI build failures | Delete `obj/bin`, restore packages, clean rebuild |
| Test failures | Investigate individually; likely TimeSpan-related |
| Package restore failures | Clear NuGet cache, verify .NET 9 SDK installed |

---

## Testing Strategy

### Phase Testing

Since this is an All-At-Once upgrade, testing occurs after the complete atomic operation:

**Post-Upgrade Build Verification**:
1. Restore all NuGet packages: `dotnet restore`
2. Build entire solution: `dotnet build`
3. Expected outcome: 0 errors, minimal warnings

**Unit Test Execution**:
1. Run test project: `dotnet test`
2. Project: `Tests\TrackYourDay.Tests\TrackYourDay.Tests.csproj`
3. Expected outcome: All tests pass

### Full Solution Validation

**Build Validation Checklist**:
- [ ] `dotnet restore` completes successfully
- [ ] `dotnet build` completes with 0 errors
- [ ] No unexpected new warnings introduced
- [ ] All project references resolve correctly

**Test Validation Checklist**:
- [ ] All unit tests pass
- [ ] No test framework compatibility issues
- [ ] Test coverage maintained

**Runtime Validation** (Manual, post-execution):
- [ ] MAUI application launches
- [ ] Blazor components render correctly
- [ ] Core functionality works as expected

---

## Complexity & Effort Assessment

### Per-Project Complexity

| Project | Complexity | Reasoning |
|---------|------------|-----------|
| TrackYourDay.Core | ?? Low | 3 package updates, 1 crypto fix, leaf node |
| TrackYourDay.Web | ?? Low | 3 package updates, minimal code changes |
| TrackYourDay.MAUI | ?? Low | 1 package update, generated code auto-fixes |
| TrackYourDay.Tests | ?? Low | No package updates, just framework change |

### Phase Complexity

**Phase 1: Atomic Upgrade** - ?? Low Complexity
- 4 project file updates
- 6 unique package version updates (some shared)
- Mostly automated changes
- One manual review item (crypto)

**Phase 2: Test Validation** - ?? Low Complexity  
- Run existing test suite
- No test modifications expected

---

## Source Control Strategy

### Branching Strategy

- **Source Branch**: `refactor-upgrade-to-net-9` (current working branch)
- **Upgrade Branch**: Work continues on current branch
- **Merge Target**: `main` (after validation)

### Commit Strategy

**Single Commit Approach** (Recommended for All-At-Once):

Since all changes are interdependent and form a single atomic upgrade, use one commit:

```
feat: Upgrade solution to .NET 9.0

- Update all projects to target net9.0
- Update 6 NuGet packages to .NET 9 compatible versions
- Suppress legacy cryptography warning (SYSLIB0041) - deferred to future update
- Update MAUI Windows SDK from 19041 to 22000

Projects updated:
- TrackYourDay.Core: net8.0 ? net9.0
- TrackYourDay.Web: net8.0 ? net9.0  
- TrackYourDay.MAUI: net8.0-windows10.0.19041.0 ? net9.0-windows10.0.22000.0
- TrackYourDay.Tests: net8.0 ? net9.0

Packages updated:
- Microsoft.AspNetCore.Components.WebAssembly: 8.0.10 ? 9.0.11
- Microsoft.AspNetCore.Components.WebAssembly.DevServer: 8.0.10 ? 9.0.11
- Microsoft.Data.Sqlite: 8.0.1 ? 9.0.11
- Microsoft.Extensions.Logging.Abstractions: 8.0.0 ? 9.0.11
- Microsoft.Extensions.Logging.Debug: 8.0.0 ? 9.0.11
- Newtonsoft.Json: 13.0.3 ? 13.0.4

Note: Legacy cryptography (Rfc2898DeriveBytes) update deferred - warning suppressed.
```

---

## Success Criteria

### Technical Criteria

- [ ] All 4 projects target .NET 9.0
- [ ] All 6 package updates applied
- [ ] Solution builds with 0 errors
- [ ] All unit tests pass
- [ ] No security vulnerabilities (maintained)
- [ ] MAUI Windows SDK updated to 22000
- [ ] Legacy cryptography warning suppressed (SYSLIB0041)

### Quality Criteria

- [ ] No regression in functionality
- [ ] Code quality maintained (warning suppression documented with justification)
- [ ] Legacy cryptography deferred (warning suppressed, documented as technical debt)
- [ ] Clean git history with descriptive commit message
- [ ] Documentation updated (this plan serves as record)
