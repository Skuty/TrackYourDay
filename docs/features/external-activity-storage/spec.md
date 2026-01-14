# Feature: External Activity Storage & Resilience

## Problem Statement
The application currently fetches external activities (GitLab, Jira) but lacks a robust persistence strategy for long-term analysis. Specifically, Jira data needs both historical event tracking and a "current state" view. Additionally, error handling is primitive (one-time failure stops fetching until restart), and there is no control over API request rates, risking rate limits from external providers.

## User Stories
- As a **System**, I want to store fetched GitLab activities as an append-only log so that I can replay or analyze them later without re-fetching.
- As a **System**, I want to store fetched Jira activities as an append-only log for historical analysis.
- As a **User**, I want to see a view of my currently assigned Jira issues so that I know my current workload.
- As a **User**, I want the application to stop sending requests to external services if they are failing (Circuit Breaker) to avoid wasting resources and flooding logs.
- As a **User**, I want to configure request throttling (e.g., time between requests) to avoid hitting API rate limits.

## Acceptance Criteria

### AC1: GitLab Activity Storage (Append-Only)
- **Given** the GitLab background job runs
- **When** new activities are fetched from GitLab
- **Then** each unique activity is appended to the persistent storage (e.g., SQLite table `GitLabActivities`)
- **And** activities are never updated or deleted (immutable log)
- **And** duplicate activities (same ID/signature) are ignored

### AC2: Jira Activity Storage (Event Sourcing)
- **Given** the Jira background job runs
- **When** new activities (worklogs, status changes, comments) are fetched
- **Then** each unique activity is appended to the persistent storage (e.g., SQLite table `JiraActivities`)
- **And** activities are never updated or deleted (immutable log)

### AC3: Jira Current State Storage
- **Given** the Jira background job runs
- **When** the list of currently assigned issues is fetched
- **Then** the "Current State" storage (e.g., SQLite table `JiraIssues`) is updated to reflect the latest state
- **And** issues no longer assigned are removed or marked as unassigned in this view
- **And** new issues are added

### AC4: Circuit Breaker for External Calls
- **Given** external API calls are failing (e.g., 5xx errors or timeouts)
- **When** the failure threshold is reached (e.g., 5 consecutive failures)
- **Then** the system stops sending requests to that service for a specified cooldown period
- **And** after the cooldown, a "probe" request is allowed to check if the service is back

### AC5: Configurable Throttling
- **Given** the user is in the Settings menu
- **When** they set a "Request Interval" (e.g., 5 minutes) for GitLab or Jira
- **Then** this setting is saved
- **And** upon the next application restart, the background jobs respect this interval between fetch cycles

## Out of Scope
- UI for viewing the raw activity logs (internal storage only).
- Complex analysis/reporting logic (this feature is just about *collecting* and *storing* data).
- Real-time updates (WebHooks) - strictly polling based.
- Two-way sync (writing back to Jira/GitLab).

## Edge Cases & Risks
- **API Rate Limits:** Even with throttling, we might hit limits if the interval is too short.
- **Large Payloads:** Initial fetch might bring thousands of historical activities.
- **Data Consistency:** "Current State" view might be slightly out of sync with "Event Log" if processing fails midway.
- **Credentials:** Invalid credentials should pause the job indefinitely (different from transient Circuit Breaker).

## UI/UX Requirements
- **Settings Page:**
    - New section: "External Integrations" or specific tabs for Jira/GitLab.
    - Input: "Fetch Interval (minutes)" (Numeric, min 1).
    - Toggle: "Enable Integration" (already exists, verify).

## Dependencies
- **Polly:** Library for Circuit Breaker and Retry policies.
- **Quartz.NET:** Existing job scheduler to apply throttling intervals.
- **SQLite:** For new tables (`JiraActivities`, `JiraIssues`, `GitLabActivities`).
