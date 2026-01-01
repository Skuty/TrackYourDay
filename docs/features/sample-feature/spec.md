# Feature: Activity Export to CSV

## Problem Statement
Users need to export their tracked activities to CSV format for analysis in external tools (Excel, Power BI) or for archival purposes. Currently, data is locked within the application database with no export capability.

## User Stories
- As a user, I want to export my activities to CSV so that I can analyze my time tracking data in Excel
- As a user, I want to filter the export by date range so that I only export relevant data
- As a power user, I want to include meeting data and user tasks in the export so that I get a complete picture of my work

## Acceptance Criteria

### AC1: Export Activities to CSV
**Given** the user has tracked activities in the database  
**When** the user clicks the "Export to CSV" button  
**Then** a CSV file is generated with columns: StartTime, EndTime, Duration, ApplicationName, WindowTitle, Category  
**And** the file is saved to the user's Downloads folder  
**And** a success notification is shown

### AC2: Date Range Filter
**Given** the user is on the export page  
**When** the user selects a start date and end date  
**And** clicks "Export to CSV"  
**Then** only activities within the selected date range are exported  
**And** the filename includes the date range (e.g., `activities_2026-01-01_to_2026-01-31.csv`)

### AC3: Include Meeting and Task Data
**Given** the user has MS Teams meetings and user tasks in the database  
**When** the user checks "Include Meetings" and "Include Tasks" options  
**And** clicks "Export to CSV"  
**Then** the CSV includes additional rows for meetings and tasks with appropriate columns  
**And** a "Type" column differentiates between Activity, Meeting, and Task entries

### AC4: Empty Data Handling
**Given** there are no activities in the selected date range  
**When** the user attempts to export  
**Then** an informational message is shown: "No data to export for the selected date range"  
**And** no file is created

### AC5: Large Dataset Performance
**Given** the user has more than 10,000 activity records  
**When** the user exports to CSV  
**Then** the export completes within 5 seconds  
**And** the UI remains responsive during export

## Out of Scope
- Export to formats other than CSV (JSON, Excel, PDF)
- Scheduled/automatic exports
- Cloud storage integration (OneDrive, Google Drive)
- Custom column selection
- Data aggregation/summarization in export (raw data only)
- Export of insights/analytics data

## Edge Cases & Risks

### Edge Cases
- **Very long window titles:** Truncate to 500 characters with ellipsis
- **Special characters in data:** Properly escape commas, quotes, and newlines per CSV RFC 4180
- **Timezone handling:** Export times in user's local timezone with ISO 8601 format
- **Concurrent exports:** Prevent multiple simultaneous exports from same user
- **Disk space:** Check available disk space before export (minimum 10 MB required)

### Risks
- **Performance degradation:** Large exports (50K+ rows) may cause UI freezing if not properly async
- **Data privacy:** Exported CSV contains sensitive window titles (passwords, credentials visible)
- **File system permissions:** Download folder may not be writable on some systems
- **Memory consumption:** Loading all data into memory before export could cause OutOfMemoryException

## Data Requirements

### Existing Entities Used
- `EndedActivity` (System level)
- `EndedMeeting` (Application level)
- `UserTask` (Application level)

### New Properties/Entities
None required - uses existing data

### Query Considerations
- Efficient date range query with index on `StartDate`
- Streaming results to avoid loading entire dataset into memory
- Consider pagination for large datasets

## UI/UX Requirements

### UI Location
New tab/page: "Export Data" in the main navigation menu

### Interaction Pattern
1. Date range picker (default: last 30 days)
2. Checkboxes: "Include Meetings", "Include Tasks" (both checked by default)
3. Export button (primary action)
4. Progress indicator during export
5. Success/error notification with file path

### Validation Rules
- Start date must be before or equal to end date
- Date range cannot exceed 1 year
- At least one data type (Activities, Meetings, or Tasks) must be selected

### Accessibility
- Keyboard navigation support (Tab, Enter)
- ARIA labels for all controls
- Screen reader announcements for export status

## Dependencies

### Existing Features Affected
None - purely additive feature

### External Integrations
- File system access (Windows API for save dialog)
- CsvHelper library for CSV generation

## Non-Functional Requirements

### Performance
- Export up to 10K records in < 2 seconds
- Export up to 100K records in < 10 seconds
- UI must remain responsive (async operation with progress reporting)

### Security
- Warn user about sensitive data in export
- No automatic upload/sharing of exported files
- Export feature respects user data encryption settings

### Usability
- Clear progress indication
- Ability to cancel long-running exports
- Open exported file location after successful export
