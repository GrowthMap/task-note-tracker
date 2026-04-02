# Task Note Tracker — Design Spec

**Date:** 2026-04-01  
**Status:** Approved  
**Stack:** C# / .NET 8 / WPF / SQLite / EF Core

---

## Purpose

A Windows desktop app that lives in the system tray and prompts the user every 30 minutes to log what they are specifically doing. The goal is to build a timestamped record of daily activity to identify patterns and opportunities for AI-driven automation.

---

## Architecture

Single .NET 8 WPF application. Launches silently — no main window on startup. Lives entirely in the system tray.

**Key components:**

| Component | Responsibility |
|-----------|---------------|
| `App.xaml.cs` | Entry point. Suppresses default main window. Initializes tray. Runs EF Core migrations on startup. Writes Windows startup registry key. |
| `TrayManager` | Owns the `NotifyIcon` (WinForms interop). Manages context menu and tray tooltip state. |
| `TrackingTimer` | 30-minute countdown, relative to when tracking starts. Fires an event consumed by `TrayManager` to show the popup. |
| `TrackingPopupWindow` | Always-on-top WPF form. Shown every 30 minutes while tracking is active. |
| `HistoryWindow` | Filterable/searchable data grid with CSV and Excel export. |
| `TaskTypeManagerWindow` | Simple dialog to add and remove task type labels. |
| `AppDbContext` | EF Core + SQLite. Code-first migrations. Single local DB file. |

**Windows startup:** On first launch, writes a registry key to `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` so the app auto-starts with Windows.

**DB file location:** `%AppData%\TaskNoteTracker\tracker.db`

**Timestamps:** All entries stored as UTC. BDT (UTC+6) and CDT (auto-DST) are computed at read time via `TimeZoneInfo.FindSystemTimeZoneById`. This keeps historical data correct if timezone rules change.

- Bangladesh: `TimeZoneInfo` ID `"Bangladesh Standard Time"` (UTC+6, no DST)
- Central: `TimeZoneInfo` ID `"Central Standard Time"` (UTC−6/−5, auto-DST)

---

## Data Model

### `TaskTypes` table

| Column | Type | Notes |
|--------|------|-------|
| `Id` | int (PK) | Auto-increment |
| `Name` | string | e.g. "Deep Work", "Meeting", "Admin", "Break" |

### `TaskEntries` table

| Column | Type | Notes |
|--------|------|-------|
| `Id` | int (PK) | Auto-increment |
| `TimestampUtc` | DateTime | Stored as UTC |
| `Activity` | string | Required. What the user is specifically doing. |
| `Notes` | string | Optional. Free-text messages or notes. |
| `TaskTypeId` | int (FK) | References `TaskTypes.Id` |

BDT and CDT timestamps are **not stored** — computed on read from `TimestampUtc`.

---

## UI Components

### TrackingPopupWindow
- Appears every 30 minutes while tracking is active
- Always on top, centered on screen, steals focus
- Fields:
  - **Task Type** — dropdown of existing types, with inline "Add new type…" option
  - **Activity** — required text field ("What are you specifically doing?")
  - **Notes** — optional multiline text area
- **Submit** — saves entry, closes window, resets 30-min timer
- **Skip** — closes window without saving, resets 30-min timer

### HistoryWindow
- Opened from tray context menu
- Filter bar: date range picker, task type dropdown, free-text search
- Data grid columns: BDT Time, CDT Time, Task Type, Activity, Notes
- Default sort: newest first
- Export buttons: **Export CSV** and **Export Excel** (ClosedXML)
- Grid updates live as filters change
- **Edit** button per row — opens a small edit dialog pre-filled with the entry's fields (Activity, Notes, Task Type); saves on confirm
- **Delete** button per row — prompts for confirmation before deleting

### TaskTypeManagerWindow
- Simple dialog opened from tray context menu
- List of existing task types
- Text field + "Add" button to create new types
- Delete button per row — disabled if that type has existing entries (protects data integrity)

### Tray Context Menu
```
Start Tracking          (toggles to "Stop Tracking" when active)
─────────────────────
Open History
Manage Task Types
─────────────────────
Exit
```

Tray tooltip: `"Task Note Tracker — Not tracking"` / `"Task Note Tracker — Tracking"`

---

## Behavior & Flows

### App Startup
1. App launches silently (no window shown)
2. Tray icon appears with tooltip "Task Note Tracker — Not tracking"
3. Registry auto-start key is written if not already present
4. EF Core runs any pending migrations

### Start Tracking
1. User clicks tray → "Start Tracking"
2. 30-minute timer starts (relative to click time)
3. Tray tooltip updates to "Task Note Tracker — Tracking"
4. Menu item toggles to "Stop Tracking"

### Popup Fires
1. `TrackingPopupWindow` opens — always on top, steals focus
2. **Submit** → entry saved to DB → window closes → timer resets for next 30 min
3. **Skip** → window closes (no entry saved) → timer resets for next 30 min
4. If popup is already open when next interval fires → no second popup; existing popup stays open

### Stop Tracking
1. User clicks tray → "Stop Tracking"
2. Timer stops
3. Any open popup is closed without saving
4. Tray tooltip resets to "Not tracking"

### Exit
1. Stops tracking if active
2. Disposes tray icon cleanly
3. Application exits

---

## Testing

**Framework:** xUnit + FluentAssertions

### Unit Tests
- `TrackingTimer` — fires at correct interval, resets after submission, does not double-fire when popup is already open
- Timezone conversion — BDT and CDT computed correctly from UTC samples including DST boundary cases
- `TaskType` management — add, list, delete (blocked when entries reference the type)
- Entry filtering logic — by date range, task type, free-text search across Activity and Notes
- Entry edit — updates correct fields, leaves other entries unchanged
- Entry delete — removes entry from DB, does not affect task types

### Integration Tests
- EF Core + SQLite (in-memory SQLite provider) — save entry, retrieve with filters, FK constraint enforcement
- Export — CSV and Excel output contain correct columns and correct row data

### Manual Verification (not automated)
- WPF UI rendering and layout
- Tray icon appearance and context menu behavior
- Always-on-top popup behavior
- Windows registry write and auto-start behavior

---

## Dependencies

| Package | Purpose |
|---------|---------|
| `Microsoft.EntityFrameworkCore.Sqlite` | ORM + SQLite driver |
| `Microsoft.EntityFrameworkCore.Tools` | EF Core migrations |
| `ClosedXML` | Excel export (.xlsx) |
| `xunit` | Test framework |
| `FluentAssertions` | Readable test assertions |

> Integration tests use `Microsoft.EntityFrameworkCore.Sqlite` with `DataSource=:memory:` — no separate InMemory provider needed.

---

## Out of Scope

- Cloud sync or backup
- Multi-user support
- Mobile or web interface
- Notifications beyond the tray popup
