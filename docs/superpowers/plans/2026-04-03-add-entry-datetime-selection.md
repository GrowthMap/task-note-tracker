# Add Entry with Datetime Selection — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an "Add Entry…" tray menu item that opens a new window where the user can log an entry at the current moment or at a custom BDT date/time.

**Architecture:** A new `AddEntryWindow` (XAML + code-behind) handles the form. `TimezoneService` gains a `ToUtc` helper for BDT→UTC conversion. `TrayManager` gets a new "Add Entry…" menu item wired to open the window. All existing files (`TrackingPopupWindow`, `EntryService`, data models) are untouched.

**Tech Stack:** C# 12, .NET 8, WPF, xUnit, FluentAssertions, SQLite in-memory (tests)

---

## File Map

| Action | File |
|--------|------|
| Modify | `TaskNoteTracker/Services/TimezoneService.cs` |
| Modify | `TaskNoteTracker.Tests/Services/TimezoneServiceTests.cs` |
| Modify | `TaskNoteTracker.Tests/Services/EntryServiceTests.cs` |
| Create | `TaskNoteTracker/Windows/AddEntryWindow.xaml` |
| Create | `TaskNoteTracker/Windows/AddEntryWindow.xaml.cs` |
| Modify | `TaskNoteTracker/Tray/TrayManager.cs` |

---

## Task 1: Add `ToUtc` helper to `TimezoneService` (TDD)

**Files:**
- Modify: `TaskNoteTracker.Tests/Services/TimezoneServiceTests.cs`
- Modify: `TaskNoteTracker/Services/TimezoneService.cs`

- [ ] **Step 1: Write the failing test**

Open `TaskNoteTracker.Tests/Services/TimezoneServiceTests.cs` and add this test at the bottom of the class (before the closing `}`):

```csharp
[Fact]
public void ToUtc_ConvertsBdtToCorrectUtc()
{
    // BDT is UTC+6, no DST — midday BDT should be 06:00 UTC
    var bdt = new DateTime(2026, 4, 3, 12, 0, 0);
    TimezoneService.ToUtc(bdt).Should().Be(new DateTime(2026, 4, 3, 6, 0, 0, DateTimeKind.Utc));
}
```

- [ ] **Step 2: Run test to verify it fails**

```
cd "F:/Trojar Test Dev/Task Note Tracker"
dotnet test --filter "FullyQualifiedName~TimezoneServiceTests.ToUtc_ConvertsBdtToCorrectUtc"
```

Expected: FAIL — `'TimezoneService' does not contain a definition for 'ToUtc'`

- [ ] **Step 3: Implement `ToUtc` in `TimezoneService`**

Open `TaskNoteTracker/Services/TimezoneService.cs`. Add this method after `ToCdt`:

```csharp
public static DateTime ToUtc(DateTime bdt) =>
    TimeZoneInfo.ConvertTimeToUtc(
        DateTime.SpecifyKind(bdt, DateTimeKind.Unspecified), BdtZone);
```

The full file should now look like:

```csharp
namespace TaskNoteTracker.Services;

public static class TimezoneService
{
    private static readonly TimeZoneInfo BdtZone =
        TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");

    private static readonly TimeZoneInfo CentralZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

    public static DateTime ToBdt(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), BdtZone);

    public static DateTime ToCdt(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), CentralZone);

    public static DateTime ToUtc(DateTime bdt) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(bdt, DateTimeKind.Unspecified), BdtZone);
}
```

- [ ] **Step 4: Run test to verify it passes**

```
dotnet test --filter "FullyQualifiedName~TimezoneServiceTests.ToUtc_ConvertsBdtToCorrectUtc"
```

Expected: PASS

- [ ] **Step 5: Run full test suite**

```
dotnet test
```

Expected: All tests pass (no regressions).

- [ ] **Step 6: Commit**

```
git add TaskNoteTracker/Services/TimezoneService.cs TaskNoteTracker.Tests/Services/TimezoneServiceTests.cs
git commit -m "feat: add TimezoneService.ToUtc for BDT-to-UTC conversion"
```

---

## Task 2: Test BDT-to-UTC round-trip through `EntryService`

**Files:**
- Modify: `TaskNoteTracker.Tests/Services/EntryServiceTests.cs`

- [ ] **Step 1: Write the failing test**

Open `TaskNoteTracker.Tests/Services/EntryServiceTests.cs` and add this test inside the class (before `public void Dispose()`):

```csharp
[Fact]
public async Task CreateAsync_WithBdtConversion_StoresCorrectUtc()
{
    // BDT 2026-04-03 12:00 should be stored as UTC 2026-04-03 06:00
    var bdt = new DateTime(2026, 4, 3, 12, 0, 0);
    var timestampUtc = TimezoneService.ToUtc(bdt);

    var entry = await _service.CreateAsync(timestampUtc, "BDT entry", null, _taskType.Id);

    entry.TimestampUtc.Should().Be(new DateTime(2026, 4, 3, 6, 0, 0, DateTimeKind.Utc));
}
```

You will also need to add the `TimezoneService` namespace at the top of the file. The existing usings are:

```csharp
using FluentAssertions;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Services;
using TaskNoteTracker.Tests.Helpers;
```

`TimezoneService` is already in `TaskNoteTracker.Services` — no new `using` needed.

- [ ] **Step 2: Run test to verify it passes immediately**

This test exercises the `ToUtc` helper (just added) + existing `CreateAsync`. It should pass without any further changes:

```
dotnet test --filter "FullyQualifiedName~EntryServiceTests.CreateAsync_WithBdtConversion_StoresCorrectUtc"
```

Expected: PASS

- [ ] **Step 3: Run full test suite**

```
dotnet test
```

Expected: All tests pass.

- [ ] **Step 4: Commit**

```
git add TaskNoteTracker.Tests/Services/EntryServiceTests.cs
git commit -m "test: verify BDT-to-UTC conversion stores correct timestamp"
```

---

## Task 3: Create `AddEntryWindow` (XAML)

**Files:**
- Create: `TaskNoteTracker/Windows/AddEntryWindow.xaml`

- [ ] **Step 1: Create the XAML file**

Create `TaskNoteTracker/Windows/AddEntryWindow.xaml` with this content:

```xml
<Window x:Class="TaskNoteTracker.Windows.AddEntryWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Add Entry" Height="450" Width="400"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize" ShowInTaskbar="False">
    <StackPanel Margin="20">

        <TextBlock Text="When?" FontWeight="SemiBold" Margin="0,0,0,6"/>
        <RadioButton x:Name="RightNowRadio" Content="Right Now"
                     IsChecked="True" Margin="0,0,0,4"
                     Checked="TimeMode_Changed"/>
        <RadioButton x:Name="CustomTimeRadio" Content="Custom time"
                     Margin="0,0,0,6"
                     Checked="TimeMode_Changed"/>

        <StackPanel x:Name="CustomTimePanel" Orientation="Horizontal"
                    Visibility="Collapsed" Margin="16,0,0,14">
            <TextBlock Text="Date:" VerticalAlignment="Center" Margin="0,0,4,0"/>
            <DatePicker x:Name="DatePicker" Width="130" Margin="0,0,12,0"/>
            <TextBlock Text="Hour:" VerticalAlignment="Center" Margin="0,0,4,0"/>
            <TextBox x:Name="HourBox" Width="36" Margin="0,0,8,0"/>
            <TextBlock Text="Min:" VerticalAlignment="Center" Margin="0,0,4,0"/>
            <TextBox x:Name="MinuteBox" Width="36" Margin="0,0,8,0"/>
            <TextBlock Text="(BDT)" VerticalAlignment="Center" Foreground="Gray"/>
        </StackPanel>

        <TextBlock Text="Task Type" FontWeight="SemiBold" Margin="0,0,0,4"/>
        <ComboBox x:Name="TaskTypeCombo" Margin="0,0,0,14"
                  SelectionChanged="TaskTypeCombo_SelectionChanged"/>

        <TextBlock Text="What are you specifically doing? *"
                   FontWeight="SemiBold" Margin="0,0,0,4"/>
        <TextBox x:Name="ActivityBox" Height="70"
                 TextWrapping="Wrap" AcceptsReturn="True"
                 VerticalScrollBarVisibility="Auto" Margin="0,0,0,14"/>

        <TextBlock Text="Notes" FontWeight="SemiBold" Margin="0,0,0,4"/>
        <TextBox x:Name="NotesBox" Height="80"
                 TextWrapping="Wrap" AcceptsReturn="True"
                 VerticalScrollBarVisibility="Auto" Margin="0,0,0,18"/>

        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="Cancel" Width="80" Margin="0,0,8,0"
                    Click="Cancel_Click"/>
            <Button Content="Submit" Width="80"
                    Click="Submit_Click" IsDefault="True"/>
        </StackPanel>

    </StackPanel>
</Window>
```

No build step needed yet — the code-behind comes next.

---

## Task 4: Create `AddEntryWindow` code-behind

**Files:**
- Create: `TaskNoteTracker/Windows/AddEntryWindow.xaml.cs`

- [ ] **Step 1: Create the code-behind file**

Create `TaskNoteTracker/Windows/AddEntryWindow.xaml.cs` with this content:

```csharp
using System.Windows;
using System.Windows.Controls;
using TaskNoteTracker.Data;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Windows;

public partial class AddEntryWindow : Window
{
    private readonly EntryService _entryService;
    private readonly TaskTypeService _taskTypeService;
    private List<TaskType> _taskTypes = [];
    private bool _loading;

    public AddEntryWindow(AppDbContext db)
    {
        _entryService = new EntryService(db);
        _taskTypeService = new TaskTypeService(db);
        InitializeComponent();
        Loaded += async (_, _) => await LoadTaskTypesAsync();
    }

    private async Task LoadTaskTypesAsync(int? selectId = null)
    {
        _loading = true;
        _taskTypes = [.. await _taskTypeService.GetAllAsync()];
        _taskTypes.Add(new TaskType { Id = -1, Name = "＋ Add new type…" });
        TaskTypeCombo.ItemsSource = _taskTypes;
        TaskTypeCombo.DisplayMemberPath = "Name";

        if (selectId.HasValue)
            TaskTypeCombo.SelectedItem = _taskTypes.FirstOrDefault(t => t.Id == selectId.Value);
        else if (_taskTypes.Count > 1)
            TaskTypeCombo.SelectedIndex = 0;

        _loading = false;
    }

    private async void TaskTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (TaskTypeCombo.SelectedItem is not TaskType { Id: -1 }) return;

        var dialog = new AddTaskTypeDialog { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            var newType = await _taskTypeService.AddAsync(dialog.TypeName);
            await LoadTaskTypesAsync(selectId: newType.Id);
        }
        else
        {
            _loading = true;
            TaskTypeCombo.SelectedIndex = 0;
            _loading = false;
        }
    }

    private void TimeMode_Changed(object sender, RoutedEventArgs e)
    {
        if (CustomTimePanel is null) return;

        if (CustomTimeRadio.IsChecked == true)
        {
            var now = TimezoneService.ToBdt(DateTime.UtcNow);
            DatePicker.SelectedDate = now.Date;
            HourBox.Text = now.Hour.ToString();
            MinuteBox.Text = now.Minute.ToString();
            CustomTimePanel.Visibility = Visibility.Visible;
        }
        else
        {
            CustomTimePanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        if (TaskTypeCombo.SelectedItem is not TaskType { Id: > 0 } taskType)
        {
            MessageBox.Show("Please select a task type.", "Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(ActivityBox.Text))
        {
            MessageBox.Show("Activity is required.", "Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DateTime timestampUtc;

        if (CustomTimeRadio.IsChecked == true)
        {
            if (DatePicker.SelectedDate is not { } selectedDate)
            {
                MessageBox.Show("Please select a date.", "Required",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(HourBox.Text, out var hour) || hour < 0 || hour > 23)
            {
                MessageBox.Show("Hour must be between 0 and 23.", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(MinuteBox.Text, out var minute) || minute < 0 || minute > 59)
            {
                MessageBox.Show("Minute must be between 0 and 59.", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var bdt = new DateTime(
                selectedDate.Year, selectedDate.Month, selectedDate.Day,
                hour, minute, 0);
            timestampUtc = TimezoneService.ToUtc(bdt);
        }
        else
        {
            timestampUtc = DateTime.UtcNow;
        }

        await _entryService.CreateAsync(timestampUtc, ActivityBox.Text, NotesBox.Text, taskType.Id);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
```

- [ ] **Step 2: Build to verify it compiles**

```
cd "F:/Trojar Test Dev/Task Note Tracker"
dotnet build TaskNoteTracker/TaskNoteTracker.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Run full test suite**

```
dotnet test
```

Expected: All tests pass.

- [ ] **Step 4: Commit**

```
git add TaskNoteTracker/Windows/AddEntryWindow.xaml TaskNoteTracker/Windows/AddEntryWindow.xaml.cs
git commit -m "feat: add AddEntryWindow with Right Now / Custom BDT time selection"
```

---

## Task 5: Wire "Add Entry…" into `TrayManager`

**Files:**
- Modify: `TaskNoteTracker/Tray/TrayManager.cs`

- [ ] **Step 1: Add the menu item and handler**

Open `TaskNoteTracker/Tray/TrayManager.cs`.

In the constructor, insert the new menu item after the first `ToolStripSeparator` is added. The current sequence is:

```csharp
_menu.Items.Add(_toggleItem);
_menu.Items.Add(new ToolStripSeparator());
_menu.Items.Add("Open History", null, OnOpenHistory);
```

Change it to:

```csharp
_menu.Items.Add(_toggleItem);
_menu.Items.Add(new ToolStripSeparator());
_menu.Items.Add("Add Entry…", null, OnAddEntry);
_menu.Items.Add(new ToolStripSeparator());
_menu.Items.Add("Open History", null, OnOpenHistory);
```

Then add the handler method alongside the other `On…` methods (e.g. after `OnToggleTracking`):

```csharp
private void OnAddEntry(object? sender, EventArgs e) =>
    System.Windows.Application.Current.Dispatcher.Invoke(() =>
        new AddEntryWindow(_db).ShowDialog());
```

- [ ] **Step 2: Build to verify it compiles**

```
dotnet build TaskNoteTracker/TaskNoteTracker.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Run full test suite**

```
dotnet test
```

Expected: All tests pass.

- [ ] **Step 4: Commit**

```
git add TaskNoteTracker/Tray/TrayManager.cs
git commit -m "feat: add 'Add Entry…' tray menu item opening AddEntryWindow"
```

---

## Done

The feature is complete. Manual smoke-test checklist:

- [ ] Right-click tray icon → "Add Entry…" opens the new window
- [ ] "Right Now" selected by default; date/time row is hidden
- [ ] Switching to "Custom time" reveals Date/Hour/Minute fields pre-filled with current BDT
- [ ] Submitting with "Right Now" creates entry with timestamp ≈ `DateTime.UtcNow`
- [ ] Submitting with a custom BDT time creates entry with the correct UTC timestamp (verify in History window — it should show the BDT time you entered)
- [ ] Validation: empty activity, invalid hour/minute, no date selected each show the correct warning
- [ ] "＋ Add new type…" in the task type dropdown opens `AddTaskTypeDialog` and the new type is selected
