-- ============================================================================
-- Migration: Add LLM Prompt Templates Feature
-- Version: V1_AddLlmPromptTemplates
-- Target Database: TrackYourDayGeneric.db (SQLite)
-- Execution: Runs automatically on application startup via LlmPromptTemplateRepository
-- ============================================================================

-- Create table for storing LLM prompt templates
CREATE TABLE IF NOT EXISTS llm_prompt_templates (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TemplateKey TEXT NOT NULL UNIQUE,      -- Unique identifier (e.g., "detailed", "concise")
    Name TEXT NOT NULL,                     -- Display name shown in UI dropdown
    SystemPrompt TEXT NOT NULL,             -- Full prompt template with {ACTIVITY_DATA_PLACEHOLDER}
    IsActive INTEGER NOT NULL DEFAULT 1,    -- Soft delete flag: 1 = active, 0 = deleted
    DisplayOrder INTEGER NOT NULL,          -- Sort order in UI (ascending: 1, 2, 3...)
    CreatedAt TEXT NOT NULL,                -- ISO 8601 timestamp: YYYY-MM-DD HH:MM:SS
    UpdatedAt TEXT NOT NULL                 -- ISO 8601 timestamp: YYYY-MM-DD HH:MM:SS
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_llm_templates_active ON llm_prompt_templates(IsActive, DisplayOrder);
CREATE UNIQUE INDEX IF NOT EXISTS idx_llm_templates_key ON llm_prompt_templates(TemplateKey);

-- ============================================================================
-- Seed default templates (INSERT OR IGNORE ensures idempotency)
-- ============================================================================

-- Template 1: Detailed Time Breakdown
INSERT OR IGNORE INTO llm_prompt_templates (TemplateKey, Name, SystemPrompt, IsActive, DisplayOrder, CreatedAt, UpdatedAt)
VALUES (
    'detailed',
    'Detailed Time Breakdown',
    'You are a time tracking assistant. Analyze the following activity log and generate a detailed time log report.

REQUIREMENTS:
- Generate between 3 and 9 time log entries
- Group similar activities together
- Identify Jira ticket keys using pattern: [A-Z][A-Z0-9]+-\d+
- If no Jira key found, use "N/A" for Jira Key field
- Sum durations for grouped activities
- Each entry must include: Description, Duration (decimal hours), Jira Key
- Note: Durations already exclude break periods

ACTIVITY DATA:
{ACTIVITY_DATA_PLACEHOLDER}

OUTPUT FORMAT:
| Description | Duration (hours) | Jira Key |
|-------------|------------------|----------|
| ... | ... | ... |

Generate the report now.',
    1,
    1,
    datetime('now'),
    datetime('now')
);

-- Template 2: Concise Summary
INSERT OR IGNORE INTO llm_prompt_templates (TemplateKey, Name, SystemPrompt, IsActive, DisplayOrder, CreatedAt, UpdatedAt)
VALUES (
    'concise',
    'Concise Summary',
    'Summarize the following workday into 3-9 time log entries suitable for Jira worklog submission.

Rules:
1. Merge similar activities
2. Extract Jira keys (format: ABC-123) from descriptions
3. If no key found, write "No ticket" in Jira Key column
4. Convert durations to decimal hours (e.g., 1h 30m = 1.5)
5. Break time already excluded from durations

Data:
{ACTIVITY_DATA_PLACEHOLDER}

Output as table with columns: Task, Hours, Jira Ticket',
    1,
    2,
    datetime('now'),
    datetime('now')
);

-- Template 3: Task-Oriented Log
INSERT OR IGNORE INTO llm_prompt_templates (TemplateKey, Name, SystemPrompt, IsActive, DisplayOrder, CreatedAt, UpdatedAt)
VALUES (
    'task-oriented',
    'Task-Oriented Log',
    'Act as a project manager reviewing an engineer''s workday. Group the activities below into distinct tasks (minimum 3, maximum 9).

For each task:
- Write a clear description
- Sum total time spent (already excludes breaks)
- Identify associated Jira ticket (if mentioned in activity names)
- If no ticket, indicate "Administrative" or "Untracked"

Activities:
{ACTIVITY_DATA_PLACEHOLDER}

Format output as:
1. [Jira Key or "N/A"] - Description (X.X hours)
2. ...',
    1,
    3,
    datetime('now'),
    datetime('now')
);

-- ============================================================================
-- Verification Query (run after migration to confirm success)
-- ============================================================================
-- SELECT * FROM llm_prompt_templates WHERE IsActive = 1 ORDER BY DisplayOrder;
-- Expected: 3 rows with TemplateKey = 'detailed', 'concise', 'task-oriented'

-- ============================================================================
-- Rollback (if needed)
-- ============================================================================
-- DROP TABLE IF EXISTS llm_prompt_templates;
-- DROP INDEX IF EXISTS idx_llm_templates_active;
-- DROP INDEX IF EXISTS idx_llm_templates_key;

-- ============================================================================
-- Admin Maintenance Commands
-- ============================================================================

-- Update existing template (example: modify "detailed" template)
-- UPDATE llm_prompt_templates 
-- SET SystemPrompt = '[updated prompt text here]', 
--     UpdatedAt = datetime('now')
-- WHERE TemplateKey = 'detailed';

-- Soft delete a template (hides from UI but preserves in DB)
-- UPDATE llm_prompt_templates SET IsActive = 0, UpdatedAt = datetime('now') WHERE TemplateKey = 'concise';

-- Reactivate a deleted template
-- UPDATE llm_prompt_templates SET IsActive = 1, UpdatedAt = datetime('now') WHERE TemplateKey = 'concise';

-- Add a new custom template
-- INSERT INTO llm_prompt_templates (TemplateKey, Name, SystemPrompt, IsActive, DisplayOrder, CreatedAt, UpdatedAt)
-- VALUES ('custom', 'Custom Template', '[prompt text with {ACTIVITY_DATA_PLACEHOLDER}]', 1, 4, datetime('now'), datetime('now'));

-- View all templates (including soft-deleted)
-- SELECT TemplateKey, Name, IsActive, DisplayOrder, UpdatedAt FROM llm_prompt_templates ORDER BY DisplayOrder;
