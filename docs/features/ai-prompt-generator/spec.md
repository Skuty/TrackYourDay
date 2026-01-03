# AI Prompt Generator for Work Day Summarization

## Problem Statement
Users need to export their daily activity data as structured prompts to feed external LLMs (ChatGPT, Claude, etc.) for generating time-logging summaries that map work to Jira tickets.

## User Stories

### US-1: Generate Prompt from Day's Activities
**Given** I am viewing the AI Prompt Generator page  
**And** I have activity data for the selected date  
**When** I select a prompt template from the dropdown  
**Then** the system generates a prompt containing:
- Selected date
- List of activities with timestamps and durations
- Extracted Jira ticket keys (if any)
- Template-specific instructions for the LLM

**Acceptance Criteria:**
- Prompt includes activities sorted chronologically
- Jira keys extracted via regex pattern `[A-Z]{2,10}-\d+`
- Idle time (<5 min activity gaps) excluded
- Activities deduplicated by title+application

### US-2: Copy Prompt to Clipboard
**Given** a generated prompt is displayed  
**When** I click "Copy to Clipboard"  
**Then** the entire prompt text is copied  
**And** a toast notification confirms success

### US-3: Select Prompt Template
**Given** I am on the AI Prompt Generator page  
**When** I open the template dropdown  
**Then** I see 3 options:
1. "Detailed Summary with Time Allocation"
2. "Concise Bullet-Point Summary"
3. "Jira-Focused Worklog Template"

### US-4: Handle Missing Jira Keys
**Given** the selected day has no Jira-related activities  
**When** I generate a prompt  
**Then** the prompt includes instruction: "No Jira tickets detected. Provide general summary without ticket references."

## Out of Scope
- Calling LLM APIs directly (manual copy-paste workflow only)
- Editing prompt templates via UI
- Multi-day or date-range selection
- Filtering activities by application or category

## Data Requirements
- **Input:** `DateOnly` for selected day
- **Query:** Retrieve `EndedActivity` records for that day
- **Extraction:** Parse Jira keys from activity descriptions
- **Output:** Plain text prompt (2000-5000 characters)

## UI Requirements
- **Page Location:** Insights → AI Prompt Generator
- **Controls:**
  - Date picker (default: today)
  - Template dropdown (3 options)
  - "Generate Prompt" button
  - Read-only multi-line textbox (scrollable, 20+ rows)
  - "Copy to Clipboard" button
- **Component Library:** MudBlazor (`MudDatePicker`, `MudSelect`, `MudTextField`, `MudButton`)

## Non-Functional Requirements
- Prompt generation <500ms for typical day (50-200 activities)
- No external API calls
- No PII in prompts (exclude window titles with emails/passwords)
