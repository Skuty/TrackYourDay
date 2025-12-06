# Logging Configuration

## Overview
TrackYourDay supports advanced logging configuration that allows you to control log levels and organize logs into separate files for each component.

## Features

### Per-Class Logging
When enabled, each tracker and service writes to its own dedicated log file in the `PerClass/` subdirectory:

- `ActivityTracker_.log` - System activity tracking
- `BreakTracker_.log` - Break detection and tracking
- `MsTeamsMeetingTracker_.log` - Microsoft Teams meeting tracking
- `GitLabTracker_.log` - GitLab integration
- `JiraTracker_.log` - Jira integration
- Analytics strategies (MLNet, JiraEnriched, HybridContextual, etc.)
- Persistence handlers

### Log Levels
Choose the minimum log level to control verbosity:

- **Verbose** - All logs including detailed traces (most verbose)
- **Debug** - Detailed debugging information
- **Information** - General informational messages (default)
- **Warning** - Warning messages only
- **Error** - Error messages only
- **Fatal** - Fatal errors only (least verbose)

## Configuration

### Via UI (Recommended)
1. Open the application
2. Navigate to **Settings** page
3. Click on the **Logging** tab
4. Configure:
   - **Minimum Log Level**: Select desired verbosity
   - **Enable Per-Class Logging**: Toggle to enable/disable separate log files
   - **Log Directory**: Change where logs are stored
5. Click **Save**
6. **Restart the application** for changes to take effect

### Default Paths
Logs are stored in platform-specific locations:

- **Windows**: `C:\Logs\TrackYourDay\`
- **macOS**: `~/Library/Logs/TrackYourDay/`
- **Linux**: `~/.local/share/TrackYourDay/logs/`

### Log Files
- Main log: `TrackYourDay_YYYYMMDD.log` (daily rotation)
- Per-class logs: `PerClass/[ClassName]_YYYYMMDD.log` (daily rotation)

## Troubleshooting

### Logs Not Appearing
- Ensure the log directory exists and is writable
- Check that the application has been restarted after changing settings
- Verify the log level is set appropriately (Information or lower for most logs)

### Finding Specific Information
1. Enable per-class logging
2. Set log level to Debug or Verbose
3. Navigate to the PerClass subdirectory
4. Open the relevant component's log file

### Performance Considerations
- Verbose and Debug levels can generate large log files
- Consider using Information level for production use
- Per-class logging has minimal performance impact
- Log files automatically rotate daily to manage disk space

## Example Log Structure
```
C:\Logs\TrackYourDay\
├── TrackYourDay_20231206.log          # Main application log
└── PerClass\
    ├── ActivityTracker_20231206.log
    ├── BreakTracker_20231206.log
    ├── JiraTracker_20231206.log
    └── ...
```

## Development

### Adding New Components to Per-Class Logging
To add a new component to per-class logging, edit `src/TrackYourDay.MAUI/Infrastructure/LoggingConfiguration.cs` and add:

```csharp
AddPerClassLogger(loggerConfig, logDirectory, "YourClassName", logLevel);
```

The component must use `ILogger<YourClassName>` for dependency injection.
