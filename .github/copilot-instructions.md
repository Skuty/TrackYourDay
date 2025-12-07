# GitHub Copilot Instructions for TrackYourDay

## Project Overview

TrackYourDay is a Windows-only time tracking application built with .NET MAUI and Blazor. It tracks user activities by monitoring system events, application-level events, and providing insights through analysis and aggregation.

**Key Characteristics:**
- Windows 10+ desktop application
- .NET 8.0 target framework
- MAUI for UI with Blazor components (MudBlazor)
- Real-time activity tracking with ML.NET for insights
- SQLite for local data persistence

## Architecture

The application is organized in three main levels:

1. **System Level** - Low-level event tracking:
   - Application focus events
   - Mouse/keyboard activity
   - Window title changes

2. **Application Level** - Application-specific integrations:
   - Teams meeting detection
   - GitLab/Jira activity tracking
   - Manual task entries

3. **Insights Level** - Data analysis and presentation:
   - Activity aggregation and interpretation
   - Break detection
   - Work day summaries
   - Time analysis using ML.NET

## Project Structure

```
TrackYourDay/
├── src/
│   ├── TrackYourDay.Core/          # Core business logic, domain models, services
│   ├── TrackYourDay.MAUI/          # MAUI application, UI, background jobs
│   └── TrackYourDay.Web/           # Blazor web components
└── Tests/
    └── TrackYourDay.Tests/         # Unit tests using xUnit
```

## Development Environment

### Requirements
- Windows 10 Build 19041.0 or higher (required for development and testing)
- .NET 8.0 SDK or later
- Visual Studio 2022 recommended (for MAUI development)

### Build and Test Commands

**Restore dependencies:**
```bash
dotnet restore
```

**Build the solution:**
```bash
dotnet build --configuration Release
```

**Run tests:**
```bash
dotnet test --configuration Release --filter "Category!=Integration"
```

**Build for Windows deployment:**
```bash
dotnet build src/TrackYourDay.MAUI/TrackYourDay.MAUI.csproj --configuration Release --runtime win-x64
```

**Note:** The application is Windows-specific and uses Windows APIs (WindowsIdentity, Windows notifications, etc.). Tests that depend on Windows features will fail on non-Windows platforms.

## Coding Standards

### General Conventions
- **Language:** C# 12 with .NET 8.0
- **Nullable reference types:** Enabled (`<Nullable>enable</Nullable>`)
- **Implicit usings:** Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- **Async/await:** Use `async`/`await` for I/O operations
- **Dependency injection:** Constructor injection for dependencies

### Naming Conventions
- **Classes/Interfaces:** PascalCase (e.g., `ActivityTracker`, `IEncryptionService`)
- **Methods/Properties:** PascalCase (e.g., `GetCurrentActivity`, `StartDate`)
- **Private fields:** camelCase (e.g., `_logger`, `_clock`)
- **Local variables:** camelCase (e.g., `startDate`, `activityEvent`)
- **Constants:** PascalCase (e.g., `DefaultTimeout`)

### Code Organization
- One class per file
- File names match the class name
- Group related classes in folders by feature
- Internal classes marked with `InternalsVisibleTo` for testing when needed

## Testing Practices

### Framework and Tools
- **xUnit** for unit tests
- **FluentAssertions** for assertions (preferred over Assert.Equal)
- **Moq** for mocking dependencies

### Test Naming Convention
Use descriptive names following the pattern: `Given[Context]_When[Action]_Then[Result]`

Examples:
- `GivenStartDateIsAfterEndDate_WhenCreatingTimePeriod_ThenThrowsArgumentException`
- `WhenCreatingTimePeriod_ThenDurationIsEqualToTimeBetweenStartDateAndEndDate`

### Test Structure
Use Given-When-Then pattern with comments:

```csharp
[Fact]
public void GivenValidInput_WhenProcessing_ThenReturnsExpectedResult()
{
    // Given
    var input = CreateTestInput();
    var sut = new SystemUnderTest();

    // When
    var result = sut.Process(input);

    // Then
    result.Should().Be(expectedValue);
}
```

### Test Categories
- Default tests run on all platforms
- Use `[Trait("Category", "Integration")]` for integration tests that should be skipped in CI

## Key Dependencies

### Core Libraries
- **MediatR** (12.1.1) - CQRS pattern, command/query handling
- **Microsoft.ML** (3.0.0) - Machine learning for activity analysis
- **Microsoft.Data.Sqlite** (8.0.1) - Local data persistence
- **Newtonsoft.Json** (13.0.3) - JSON serialization

### UI and Framework
- **Microsoft.Maui.Controls** (8.0.3) - MAUI framework
- **MudBlazor** (6.10.0) - Blazor component library
- **Microsoft.AspNetCore.Components.WebView.Maui** (8.0.3) - Blazor in MAUI

### Background Jobs and Logging
- **Quartz** (3.7.0) - Background job scheduling
- **Serilog** (3.0.1) - Structured logging with file and console sinks

## Common Patterns

### Service Registration
Services are registered in `ServiceRegistration` classes using extension methods:
```csharp
public static IServiceCollection AddCoreServices(this IServiceCollection services)
{
    services.AddSingleton<IClock, Clock>();
    // ... more registrations
    return services;
}
```

### MediatR Handlers
Use MediatR for commands and queries:
```csharp
public class MyCommandHandler : IRequestHandler<MyCommand, MyResult>
{
    public Task<MyResult> Handle(MyCommand request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

### Background Jobs
Quartz jobs for periodic tasks:
```csharp
public class MyTrackerJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // Job implementation
    }
}
```

## Security Considerations

- Sensitive data (credentials, tokens) should be encrypted using `IEncryptionService`
- User-specific encryption uses Windows user account SID as salt
- Never log sensitive information
- Validate all external inputs (window titles, URLs, etc.)

## File Locations and Conventions

### Configuration
- Application settings: `appsettings.json` in MAUI project
- User settings stored in SQLite database

### Data Storage
- SQLite database in user's local AppData folder
- Logs in application directory

### Resources
- Icons and images in `Resources/` folders
- MAUI resources follow standard MAUI structure

## Pull Request Guidelines

When submitting PRs:
1. Use conventional commits with prefixes: `feat:` for features, `fix:` for bug fixes, `chore:` for maintenance
2. Ensure all tests pass (`dotnet test`)
3. Build succeeds in Release configuration
4. No new warnings introduced
5. Add tests for new functionality
6. Update documentation if adding new features
7. Follow existing code style and patterns

## Additional Notes

- This is primarily a learning project (see README.md)
- Pull requests will mostly be accepted
- For feature requests or bugs, create an issue first
- The application is licensed under BSD-style license
