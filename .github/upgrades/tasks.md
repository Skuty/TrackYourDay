# TrackYourDay .NET 9.0 Upgrade Tasks

## Overview

This document tracks the execution of the TrackYourDay solution upgrade from .NET 8.0 to .NET 9.0. All 4 projects will be upgraded simultaneously in a single atomic operation, followed by comprehensive testing and validation.

**Progress**: 0/3 tasks complete (0%) ![0%](https://progress-bar.xyz/0)

---

## Tasks

### [▶] TASK-001: Verify prerequisites
**References**: Plan §Executive Summary

- [▶] (1) Verify .NET 9.0 SDK is installed
- [ ] (2) .NET 9.0 SDK version meets minimum requirements (**Verify**)

---

### [ ] TASK-002: Atomic framework and dependency upgrade with compilation fixes
**References**: Plan §Project-by-Project Migration Plans, Plan §Package Update Reference, Plan §Breaking Changes Catalog

- [ ] (1) Update TargetFramework in all 4 project files: TrackYourDay.Core (net8.0→net9.0), TrackYourDay.Web (net8.0→net9.0), TrackYourDay.MAUI (net8.0-windows10.0.19041.0→net9.0-windows10.0.22000.0), TrackYourDay.Tests (net8.0→net9.0)
- [ ] (2) All project TargetFramework properties updated (**Verify**)
- [ ] (3) Update package references per Plan §Package Update Reference: Core (3 packages), Web (3 packages), MAUI (1 package)
- [ ] (4) All package references updated to target versions (**Verify**)
- [ ] (5) Restore all NuGet packages
- [ ] (6) All packages restored successfully (**Verify**)
- [ ] (7) Add `#pragma warning disable SYSLIB0041` and `#pragma warning restore SYSLIB0041` around Rfc2898DeriveBytes constructor calls in src\TrackYourDay.Core\IEncryptionService.cs (lines 36, 67)
- [ ] (8) Build entire solution and fix any remaining compilation errors per Plan §Breaking Changes Catalog
- [ ] (9) Solution builds with 0 errors (**Verify**)
- [ ] (10) Commit changes with message: "TASK-002: Upgrade TrackYourDay solution to .NET 9.0"

---

### [ ] TASK-003: Run full test suite and validate upgrade
**References**: Plan §Testing Strategy

- [ ] (1) Run all tests in Tests\TrackYourDay.Tests\TrackYourDay.Tests.csproj
- [ ] (2) Fix any test failures (reference Plan §Breaking Changes Catalog for TimeSpan API issues if needed)
- [ ] (3) Re-run tests after fixes
- [ ] (4) All tests pass with 0 failures (**Verify**)
- [ ] (5) Commit test fixes with message: "TASK-003: Complete .NET 9.0 upgrade testing and validation"

---
