# Add Entry with Datetime Selection — Design Spec

**Date:** 2026-04-03
**Status:** Approved

---

## Overview

Add a "Add Entry…" option to the system tray context menu that opens a dedicated window for manually logging a task entry. The window lets the user either log at the current moment or specify a custom date and time in BDT (Bangladesh Standard Time, UTC+6).

---

## Architecture

### New file: `AddEntryWindow.xaml` + `AddEntryWindow.xaml.cs`

A new dedicated WPF window for manual entry. The existing `TrackingPopupWindow` is left untouched — it remains the timer-driven prompt. Separation of concerns: timer nudge vs. deliberate manual backfill.

### Modified files

- `TimezoneService.cs` — add `ToUtc(DateTime bdt)` helper
- `TrayManager.cs` — add "Add Entry…" menu item and handler
- `TimezoneServiceTests.cs` — add test for `ToUtc`
- `EntryServiceTests.cs` — add test for BDT-to-UTC conversion on create

---

## Window Layout

`AddEntryWindow` — approximately 400×450px, `ResizeMode="NoResize"`, `WindowStartupLocation="CenterScreen"`, `ShowInTaskbar="False"`.

```
[ When? ]
  (•) Right Now
  ( ) Custom time
      Date: [DatePicker]   Hour: [TextBox 0–23]   Minute: [TextBox 0–59]   (BDT)

[ Task Type ]
  [ComboBox ▼]   (includes "+ Add new type…" sentinel, same pattern as TrackingPopupWindow)

[ What are you specifically doing? * ]
  [TextBox — multiline, height ~70]

[ Notes ]
  [TextBox — multiline, height ~80]

                              [Cancel]  [Submit]
```

**Datetime row behaviour:**
- Hidden when "Right Now" is selected.
- Visible when "Custom time" is selected; pre-filled with current BDT date/time so minor adjustments are easy.
- Hour and Minute are plain `TextBox` controls — user types numerals.

---

## Data Flow

### Right Now path
```
Submit → DateTime.UtcNow → EntryService.CreateAsync(timestampUtc, ...)
```

### Custom time path
```
User picks BDT date + types hour + minute
→ Construct DateTime(date.Year, date.Month, date.Day, hour, minute, 0) as unspecified kind
→ TimezoneService.ToUtc(bdtDateTime) → DateTime (UTC)
→ EntryService.CreateAsync(timestampUtc, ...)
```

### `TimezoneService.ToUtc` implementation
```csharp
public static DateTime ToUtc(DateTime bdt) =>
    TimeZoneInfo.ConvertTimeToUtc(
        DateTime.SpecifyKind(bdt, DateTimeKind.Unspecified), BdtZone);
```

---

## Validation (on Submit)

| Field | Rule | Message |
|-------|------|---------|
| Task Type | Must have Id > 0 | "Please select a task type." |
| Activity | Not blank | "Activity is required." |
| Date | Must be selected (custom time only) | "Please select a date." |
| Hour | Integer 0–23 (custom time only) | "Hour must be between 0 and 23." |
| Minute | Integer 0–59 (custom time only) | "Minute must be between 0 and 59." |

Validation shown via `MessageBox` (same pattern as existing windows). Hour/Minute validation only runs when "Custom time" is selected.

---

## TrayManager Change

New menu item inserted in the first group, after the tracking toggle separator:

```
Start Tracking
─────────────────
Add Entry…         ← new (OnAddEntry handler)
─────────────────
Open History
Manage Task Types
...
```

Handler:
```csharp
private void OnAddEntry(object? sender, EventArgs e) =>
    System.Windows.Application.Current.Dispatcher.Invoke(() =>
        new AddEntryWindow(_db).ShowDialog());
```

---

## Testing

### `TimezoneServiceTests.cs`
- `ToUtc_ConvertsBdtToCorrectUtc` — e.g. BDT 2026-04-03 12:00 → UTC 2026-04-03 06:00

### `EntryServiceTests.cs`
- `CreateAsync_WithCustomBdtTime_StoresCorrectUtc` — construct a BDT time, convert, create entry, assert `TimestampUtc` matches expected UTC value.

`AddEntryWindow` (WPF) has no unit tests — consistent with all other windows in this project.

---

## Out of Scope

- Modifying `TrackingPopupWindow` in any way.
- Seconds or timezone selection (BDT only).
- Editing an existing entry's timestamp (handled by `EditEntryDialog`, which is a separate concern).
