# AI Integration Design

**Date:** 2026-04-02
**Feature:** OpenAI-powered summaries, reports, and pattern detection
**Status:** Approved

---

## Overview

Add AI capabilities to the Task Note Tracker using the official OpenAI .NET SDK. The feature provides two AI functions — on-demand summaries of filtered entries and pattern analysis of work history — surfaced through a new AI Insights window and a button in the existing History window. Users provide their own OpenAI API key, stored in a plain-text settings file.

---

## Architecture

Five new components, all following existing service/window conventions:

| Component | Type | Purpose |
|---|---|---|
| `AppSettings` | Model | Holds `ApiKey` and `Model` |
| `SettingsService` | Service | Reads/writes `settings.json` |
| `AiService` | Service | Builds prompts, calls OpenAI, returns text |
| `AiInsightsWindow` | Window | Pattern analysis UI, opened from tray |
| `SettingsWindow` | Window | API key entry form, opened from tray |

Two existing components receive minor additions:
- **`HistoryWindow`** — "Generate Summary" button + collapsible result panel
- **`TrayManager`** — two new menu items: "AI Insights" and "Settings"

---

## Settings & API Key Storage

**File location:** `%APPDATA%\TaskNoteTracker\settings.json`

**Schema:**
```json
{
  "apiKey": "sk-...",
  "model": "gpt-4o-mini"
}
```

- `SettingsService.Load()` reads the file on app start; returns defaults if file is missing
- `SettingsService.Save(AppSettings)` writes the file when user clicks Save in SettingsWindow
- `AiService` receives settings via constructor injection
- If `ApiKey` is empty, `AiService` returns an error string immediately without making an API call
- The UI notes that the key is stored as plain text in AppData

**`AppSettings` model:**
```
ApiKey: string  (default: "")
Model: string   (default: "gpt-4o-mini")
```

---

## AiService

Wraps the `OpenAI` NuGet package's `ChatClient`. Two public async methods returning `string`.

### `GenerateSummaryAsync(IEnumerable<TaskEntry> entries)`

Formats entries as a readable block (UTC timestamp, task type, activity, notes) and sends to the model with a system prompt establishing it as a productivity analyst. The model returns a concise summary grouped by task type, highlighting key activities and time distribution.

### `AnalyzePatternsAsync(IEnumerable<TaskEntry> entries)`

`AiInsightsWindow` filters entries for the selected time range via `EntryService.GetFilteredAsync()` before calling this method. The method receives already-filtered entries and asks the model to identify:
- Which task types dominate (by entry count)
- Time-of-day trends
- Recurring activities
- Actionable suggestions (e.g. "You spend 60% of time on Bug Fixes — consider dedicated focus blocks")

**Both methods:**
- Use `ChatClient` with the user-configured model
- Return the response content as a plain `string`
- Are `async` throughout — no blocking calls
- Return an error string immediately (no API call) if `ApiKey` is empty
- Throw a descriptive exception on API failure; callers catch and display the message inline

**System prompt (both methods):**
> "You are a productivity analyst reviewing time-tracking data. Be concise, specific, and actionable. Focus on patterns and insights that help the user understand how they spend their time."

---

## UI

### HistoryWindow changes

- Add "Generate Summary" button in the toolbar alongside existing Export buttons
- Clicking the button:
  1. Disables the button, sets label to "Generating..."
  2. Calls `AiService.GenerateSummaryAsync()` with current filtered entries
  3. Shows result in a panel below the DataGrid (hidden until first use, scrollable read-only `TextBox`)
  4. Re-enables button when complete
- Errors and "no entries" messages appear in the same panel

### AiInsightsWindow (new)

- Opened from tray "AI Insights" menu item
- Time range selector: Last 30 days / Last 90 days / All time
- "Analyze Patterns" button
- Scrollable read-only text area for the AI report
- Same loading/error state pattern as HistoryWindow

### SettingsWindow (new)

- Opened from tray "Settings" menu item; opens modally
- Password-style text box for API key with show/hide toggle
- Dropdown: model selection (`gpt-4o-mini` default, `gpt-4o` option)
- Save button
- Small notice: "Your API key is stored as plain text in AppData\Roaming\TaskNoteTracker\settings.json"

### TrayManager changes

Two new context menu items added after "Open History":
- "AI Insights" → opens `AiInsightsWindow`
- "Settings" → opens `SettingsWindow`

---

## Error Handling

All errors display inline in the result panel/text area. No separate error dialogs. Button re-enables after any error.

| Failure | Message shown |
|---|---|
| API key empty | "Please configure your OpenAI API key in Settings." |
| API call fails (auth, rate limit, network) | "OpenAI error: {exception message}" |
| No entries to analyze | "No entries to analyze. Try adjusting the filters or date range." |

---

## Dependencies

- **`OpenAI`** NuGet package (official .NET SDK by OpenAI) — add to `TaskNoteTracker.csproj`
- No other new dependencies

---

## Out of Scope

- AI suggestions in the tracking popup
- Scheduled/automatic AI analysis
- Multiple AI provider support (Azure OpenAI, etc.)
- Streaming responses (full response returned at once)
- Storing or exporting AI-generated reports
