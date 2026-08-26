# Feature: Reliable GitLab Commit Tracking

## Problem Statement
GitLab tracker currently maps push events to commit activities, but commit visibility is not fully reliable in real-world scenarios. Commits can be skipped due to strict identity matching, missing SHA ranges in push payloads, or ref/query encoding issues.

## User Stories
- As a **User**, I want commits I push to GitLab to always appear in my tracked GitLab activities.
- As a **User**, I want branch names with `/` and other special characters to be handled correctly.
- As a **System**, I want commit fetching to work even when push events contain incomplete SHA range data.
- As a **System**, I want commit retrieval to handle paged API responses so that no commits are silently missed.

## Acceptance Criteria

### AC1: Commit Activities Are Created for Push Events
- **Given** a GitLab push event with commits
- **When** the tracker processes the event
- **Then** one `GitLabActivity` is created per discovered commit
- **And** each activity uses deterministic upstream id `gitlab-commit-{projectId}-{commitSha}` for deduplication

### AC2: Missing SHA Range Falls Back to Branch Query
- **Given** a push event with missing `commit_from` or `commit_to`
- **When** the tracker processes the event
- **Then** commit discovery falls back to `GetCommits(project, ref, startDate)`
- **And** the fallback uses tracker sync window start date (not current day only)

### AC3: Identity Matching Is Resilient
- **Given** commit metadata does not exactly match authenticated user email/name
- **When** strict filtering would return zero commits
- **Then** tracker falls back to returned commits instead of dropping them

### AC4: Ref Name and SHA Query Safety
- **Given** branch/ref names or SHA values contain characters requiring encoding
- **When** REST API requests are built
- **Then** query parameters are URL encoded correctly

### AC5: Commit Pagination
- **Given** repository commits span multiple API pages
- **When** commits are fetched by branch
- **Then** all pages are fetched and combined

## Out of Scope
- Showing commits in "assigned work items" snapshot widget (Issues/MRs-only view).
- Changing GitLab webhook/event model (polling remains).
- Implementing new persistence schema for commits (existing `GitLabActivity` persistence remains).

## Edge Cases & Risks
- Compare API can fail or return unexpected results; fallback branch query must remain available.
- Branch with very large history may return many commits; deduplication and sync window continue to guard duplicates.
- Email aliases and renamed users can make identity matching brittle; fallback behavior is required to avoid data loss.

## Data Requirements
- No database schema change required.
- Existing append-only historical storage for `GitLabActivity` remains source of truth.

## UI/UX Requirements
- No new UI components required.
- Commit activities continue appearing in existing GitLab tracker views (Trackers tab and prompt data sources).

## Dependencies
- Existing GitLab REST API integration (`IGitLabRestApiClient`)
- Existing historical repository (`IHistoricalDataRepository<GitLabActivity>`)
