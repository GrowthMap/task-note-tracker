# AI Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add OpenAI-powered summaries and pattern analysis to the Task Note Tracker using the official OpenAI .NET SDK, with a user-provided API key stored in `%APPDATA%\TaskNoteTracker\settings.json`.

**Architecture:** A new `SettingsService` manages the plain-text JSON config file; a new `AiService` wraps the OpenAI `ChatClient` and returns string results, reading fresh settings on each call. Two new windows (`SettingsWindow`, `AiInsightsWindow`) and a "Generate Summary" panel added to `HistoryWindow` surface the features; `TrayManager` and `App.xaml.cs` are updated to wire everything together.

**Tech Stack:** .NET 8 WPF, EF Core + SQLite (existing), `OpenAI` NuGet v2.1.0, `System.Text.Json` (in-box), xunit + FluentAssertions for tests.

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `TaskNoteTracker/Models/AppSettings.cs` | Data record: ApiKey + Model with defaults |
| Create | `TaskNoteTracker/Services/SettingsService.cs` | Load/save `settings.json` |
| Create | `TaskNoteTracker/Services/AiService.cs` | Prompt building + OpenAI ChatClient calls |
| Create | `TaskNoteTracker/Windows/SettingsWindow.xaml` | API key + model selection UI |
| Create | `TaskNoteTracker/Windows/SettingsWindow.xaml.cs` | Settings window logic |
| Create | `TaskNoteTracker/Windows/AiInsightsWindow.xaml` | Pattern analysis UI |
| Create | `TaskNoteTracker/Windows/AiInsightsWindow.xaml.cs` | Insights window logic |
| Modify | `TaskNoteTracker/Windows/HistoryWindow.xaml` | Add "Generate Summary" button + result panel |
| Modify | `TaskNoteTracker/Windows/HistoryWindow.xaml.cs` | Wire AiService, handle summary click |
| Modify | `TaskNoteTracker/Tray/TrayManager.cs` | Accept AiService + SettingsService, add menu items |
| Modify | `TaskNoteTracker/App.xaml.cs` | Init SettingsService + AiService, pass to TrayManager |
| Modify | `TaskNoteTracker/TaskNoteTracker.csproj` | Add OpenAI NuGet reference |
| Create | `TaskNoteTracker.Tests/Services/SettingsServiceTests.cs` | Tests for SettingsService |
| Create | `TaskNoteTracker.Tests/Services/AiServiceTests.cs` | Tests for AiService guard clauses |

---

## Task 1: Add OpenAI NuGet Package

**Files:**
- Modify: `TaskNoteTracker/TaskNoteTracker.csproj`

- [ ] **Step 1: Add the package reference**

Replace the closing `</ItemGroup>` of the package references section with the new entry:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <RootNamespace>TaskNoteTracker</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="ClosedXML" Version="0.102.3" />
    <PackageReference Include="OpenAI" Version="2.1.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Restore and build to verify**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet build TaskNoteTracker/TaskNoteTracker.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add TaskNoteTracker/TaskNoteTracker.csproj
git commit -m "chore: add OpenAI NuGet package"
```

---

## Task 2: AppSettings Model

**Files:**
- Create: `TaskNoteTracker/Models/AppSettings.cs`

- [ ] **Step 1: Create the model**

```csharp
using System.Text.Json.Serialization;

namespace TaskNoteTracker.Models;

public record AppSettings
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; init; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; init; } = "gpt-4o-mini";
}
```

- [ ] **Step 2: Build to verify**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet build TaskNoteTracker/TaskNoteTracker.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add TaskNoteTracker/Models/AppSettings.cs
git commit -m "feat: add AppSettings model"
```

---

## Task 3: SettingsService (TDD)

**Files:**
- Create: `TaskNoteTracker/Services/SettingsService.cs`
- Test: `TaskNoteTracker.Tests/Services/SettingsServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `TaskNoteTracker.Tests/Services/SettingsServiceTests.cs`:

```csharp
using System.IO;
using System.Text.Json;
using FluentAssertions;
using TaskNoteTracker.Models;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _filePath;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "settings.json");
        _service = new SettingsService(_filePath);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var settings = _service.Load();

        settings.ApiKey.Should().BeEmpty();
        settings.Model.Should().Be("gpt-4o-mini");
    }

    [Fact]
    public void Load_ReturnsSettings_WhenFileExists()
    {
        File.WriteAllText(_filePath, "{\"apiKey\":\"sk-abc\",\"model\":\"gpt-4o\"}");

        var settings = _service.Load();

        settings.ApiKey.Should().Be("sk-abc");
        settings.Model.Should().Be("gpt-4o");
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var original = new AppSettings { ApiKey = "sk-xyz", Model = "gpt-4o" };
        _service.Save(original);

        var loaded = _service.Load();

        loaded.ApiKey.Should().Be("sk-xyz");
        loaded.Model.Should().Be("gpt-4o");
    }

    [Fact]
    public void Save_WritesCamelCaseJson()
    {
        _service.Save(new AppSettings { ApiKey = "sk-test", Model = "gpt-4o-mini" });

        var json = File.ReadAllText(_filePath);

        json.Should().Contain("\"apiKey\"");
        json.Should().Contain("\"model\"");
        json.Should().Contain("sk-test");
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
```

- [ ] **Step 2: Run to confirm failure**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet test TaskNoteTracker.Tests/TaskNoteTracker.Tests.csproj --filter "FullyQualifiedName~SettingsServiceTests" -v minimal
```

Expected: build error — `SettingsService` does not exist yet.

- [ ] **Step 3: Implement SettingsService**

Create `TaskNoteTracker/Services/SettingsService.cs`:

```csharp
using System.IO;
using System.Text.Json;
using TaskNoteTracker.Models;

namespace TaskNoteTracker.Services;

public class SettingsService
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsService() : this(DefaultPath()) { }

    public SettingsService(string filePath) => _filePath = filePath;

    public AppSettings Load()
    {
        if (!File.Exists(_filePath))
            return new AppSettings();

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static string DefaultPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskNoteTracker",
            "settings.json");
}
```

- [ ] **Step 4: Run to confirm all pass**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet test TaskNoteTracker.Tests/TaskNoteTracker.Tests.csproj --filter "FullyQualifiedName~SettingsServiceTests" -v minimal
```

Expected: `Passed! - Failed: 0, Passed: 4`

- [ ] **Step 5: Commit**

```bash
git add TaskNoteTracker/Services/SettingsService.cs TaskNoteTracker.Tests/Services/SettingsServiceTests.cs
git commit -m "feat: add SettingsService with load/save for settings.json"
```

---

## Task 4: AiService (TDD — guard clauses)

**Files:**
- Create: `TaskNoteTracker/Services/AiService.cs`
- Test: `TaskNoteTracker.Tests/Services/AiServiceTests.cs`

> Note: Tests cover only the guard-clause paths (empty API key, empty entries). The live API call paths require a real OpenAI key and are verified manually.

- [ ] **Step 1: Write the failing tests**

Create `TaskNoteTracker.Tests/Services/AiServiceTests.cs`:

```csharp
using System.IO;
using FluentAssertions;
using TaskNoteTracker.Models;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Tests.Services;

public class AiServiceTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly SettingsService _settingsService;
    private readonly AiService _service;

    public AiServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
        _settingsService = new SettingsService(Path.Combine(_tempDir, "settings.json"));
        _service = new AiService(_settingsService);
    }

    [Fact]
    public async Task GenerateSummaryAsync_ReturnsApiKeyError_WhenKeyIsEmpty()
    {
        var result = await _service.GenerateSummaryAsync([]);

        result.Should().Be("Please configure your OpenAI API key in Settings.");
    }

    [Fact]
    public async Task GenerateSummaryAsync_ReturnsNoEntriesMessage_WhenEntriesEmptyAndKeySet()
    {
        _settingsService.Save(new AppSettings { ApiKey = "sk-test", Model = "gpt-4o-mini" });

        var result = await _service.GenerateSummaryAsync([]);

        result.Should().Be("No entries to analyze. Try adjusting the filters or date range.");
    }

    [Fact]
    public async Task AnalyzePatternsAsync_ReturnsApiKeyError_WhenKeyIsEmpty()
    {
        var result = await _service.AnalyzePatternsAsync([]);

        result.Should().Be("Please configure your OpenAI API key in Settings.");
    }

    [Fact]
    public async Task AnalyzePatternsAsync_ReturnsNoEntriesMessage_WhenEntriesEmptyAndKeySet()
    {
        _settingsService.Save(new AppSettings { ApiKey = "sk-test", Model = "gpt-4o-mini" });

        var result = await _service.AnalyzePatternsAsync([]);

        result.Should().Be("No entries to analyze. Try adjusting the filters or date range.");
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
```

- [ ] **Step 2: Run to confirm failure**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet test TaskNoteTracker.Tests/TaskNoteTracker.Tests.csproj --filter "FullyQualifiedName~AiServiceTests" -v minimal
```

Expected: build error — `AiService` does not exist yet.

- [ ] **Step 3: Implement AiService**

Create `TaskNoteTracker/Services/AiService.cs`:

```csharp
using System.Text;
using OpenAI.Chat;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Models;

namespace TaskNoteTracker.Services;

public class AiService(SettingsService settingsService)
{
    private const string SystemPrompt =
        "You are a productivity analyst reviewing time-tracking data. " +
        "Be concise, specific, and actionable. " +
        "Focus on patterns and insights that help the user understand how they spend their time.";

    private const string ApiKeyError =
        "Please configure your OpenAI API key in Settings.";

    private const string NoEntriesError =
        "No entries to analyze. Try adjusting the filters or date range.";

    public async Task<string> GenerateSummaryAsync(IEnumerable<TaskEntry> entries)
    {
        var settings = settingsService.Load();
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return ApiKeyError;

        var list = entries.ToList();
        if (list.Count == 0)
            return NoEntriesError;

        var client = new ChatClient(model: settings.Model, apiKey: settings.ApiKey);
        var response = await client.CompleteChatAsync(
        [
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(BuildSummaryPrompt(list))
        ]);
        return response.Value.Content[0].Text;
    }

    public async Task<string> AnalyzePatternsAsync(IEnumerable<TaskEntry> entries)
    {
        var settings = settingsService.Load();
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return ApiKeyError;

        var list = entries.ToList();
        if (list.Count == 0)
            return NoEntriesError;

        var client = new ChatClient(model: settings.Model, apiKey: settings.ApiKey);
        var response = await client.CompleteChatAsync(
        [
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(BuildPatternsPrompt(list))
        ]);
        return response.Value.Content[0].Text;
    }

    private static string BuildSummaryPrompt(List<TaskEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "Summarize the following work entries, grouped by task type. " +
            "Highlight key activities and time distribution:");
        sb.AppendLine();
        foreach (var e in entries)
        {
            sb.AppendLine($"[{e.TimestampUtc:yyyy-MM-dd HH:mm} UTC] [{e.TaskType.Name}] {e.Activity}");
            if (!string.IsNullOrWhiteSpace(e.Notes))
                sb.AppendLine($"  Notes: {e.Notes}");
        }
        return sb.ToString();
    }

    private static string BuildPatternsPrompt(List<TaskEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze the following work entries and identify patterns. Include:");
        sb.AppendLine("- Which task types dominate (by entry count)");
        sb.AppendLine("- Time-of-day trends");
        sb.AppendLine("- Recurring activities");
        sb.AppendLine("- Actionable suggestions (e.g. scheduling recommendations)");
        sb.AppendLine();
        foreach (var e in entries)
        {
            sb.AppendLine($"[{e.TimestampUtc:yyyy-MM-dd HH:mm} UTC] [{e.TaskType.Name}] {e.Activity}");
            if (!string.IsNullOrWhiteSpace(e.Notes))
                sb.AppendLine($"  Notes: {e.Notes}");
        }
        return sb.ToString();
    }
}
```

- [ ] **Step 4: Run to confirm all pass**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet test TaskNoteTracker.Tests/TaskNoteTracker.Tests.csproj --filter "FullyQualifiedName~AiServiceTests" -v minimal
```

Expected: `Passed! - Failed: 0, Passed: 4`

- [ ] **Step 5: Run the full test suite to check nothing broke**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet test TaskNoteTracker.Tests/TaskNoteTracker.Tests.csproj -v minimal
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add TaskNoteTracker/Services/AiService.cs TaskNoteTracker.Tests/Services/AiServiceTests.cs
git commit -m "feat: add AiService with OpenAI SDK integration and guard-clause tests"
```

---

## Task 5: SettingsWindow

**Files:**
- Create: `TaskNoteTracker/Windows/SettingsWindow.xaml`
- Create: `TaskNoteTracker/Windows/SettingsWindow.xaml.cs`

- [ ] **Step 1: Create SettingsWindow.xaml**

```xml
<Window x:Class="TaskNoteTracker.Windows.SettingsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Settings" Height="230" Width="420"
        WindowStartupLocation="CenterOwner" ResizeMode="NoResize">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="OpenAI API Key:" Margin="0,0,0,4"/>

        <Grid Grid.Row="1" Margin="0,0,0,12">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <PasswordBox x:Name="ApiKeyBox" Grid.Column="0" Margin="0,0,8,0"/>
            <TextBox x:Name="ApiKeyPlain" Grid.Column="0" Margin="0,0,8,0" Visibility="Collapsed"/>
            <Button x:Name="ShowHideBtn" Content="Show" Grid.Column="1" Width="50"
                    Click="ShowHide_Click"/>
        </Grid>

        <TextBlock Grid.Row="2" Text="Model:" Margin="0,0,0,4"/>

        <ComboBox x:Name="ModelBox" Grid.Row="3" Margin="0,0,0,16">
            <ComboBoxItem Content="gpt-4o-mini"/>
            <ComboBoxItem Content="gpt-4o"/>
        </ComboBox>

        <Button Grid.Row="4" Content="Save" Width="80" HorizontalAlignment="Left"
                Click="Save_Click" Margin="0,0,0,8"/>

        <TextBlock Grid.Row="5"
                   Text="API key is stored as plain text in AppData\Roaming\TaskNoteTracker\settings.json"
                   FontSize="10" Foreground="Gray" TextWrapping="Wrap"/>
    </Grid>
</Window>
```

- [ ] **Step 2: Create SettingsWindow.xaml.cs**

```csharp
using System.Windows;
using System.Windows.Controls;
using TaskNoteTracker.Models;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Windows;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;

    public SettingsWindow(SettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();
        ApiKeyBox.Password = settings.ApiKey;

        var match = ModelBox.Items
            .Cast<ComboBoxItem>()
            .FirstOrDefault(i => i.Content.ToString() == settings.Model);
        ModelBox.SelectedItem = match ?? ModelBox.Items[0];
    }

    private void ShowHide_Click(object sender, RoutedEventArgs e)
    {
        if (ApiKeyBox.Visibility == Visibility.Visible)
        {
            ApiKeyPlain.Text = ApiKeyBox.Password;
            ApiKeyBox.Visibility = Visibility.Collapsed;
            ApiKeyPlain.Visibility = Visibility.Visible;
            ShowHideBtn.Content = "Hide";
        }
        else
        {
            ApiKeyBox.Password = ApiKeyPlain.Text;
            ApiKeyPlain.Visibility = Visibility.Collapsed;
            ApiKeyBox.Visibility = Visibility.Visible;
            ShowHideBtn.Content = "Show";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var apiKey = ApiKeyBox.Visibility == Visibility.Visible
            ? ApiKeyBox.Password
            : ApiKeyPlain.Text;
        var model = (ModelBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "gpt-4o-mini";

        _settingsService.Save(new AppSettings { ApiKey = apiKey, Model = model });
        MessageBox.Show("Settings saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }
}
```

- [ ] **Step 3: Build to verify**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet build TaskNoteTracker/TaskNoteTracker.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add TaskNoteTracker/Windows/SettingsWindow.xaml TaskNoteTracker/Windows/SettingsWindow.xaml.cs
git commit -m "feat: add SettingsWindow for API key and model configuration"
```

---

## Task 6: AiInsightsWindow

**Files:**
- Create: `TaskNoteTracker/Windows/AiInsightsWindow.xaml`
- Create: `TaskNoteTracker/Windows/AiInsightsWindow.xaml.cs`

- [ ] **Step 1: Create AiInsightsWindow.xaml**

```xml
<Window x:Class="TaskNoteTracker.Windows.AiInsightsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="AI Insights" Height="520" Width="700"
        WindowStartupLocation="CenterScreen">
    <Grid Margin="12">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <WrapPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,8">
            <TextBlock Text="Time range:" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <ComboBox x:Name="RangeBox" Width="130" Margin="0,0,12,0" SelectedIndex="0">
                <ComboBoxItem Content="Last 30 days"/>
                <ComboBoxItem Content="Last 90 days"/>
                <ComboBoxItem Content="All time"/>
            </ComboBox>
            <Button x:Name="AnalyzeBtn" Content="Analyze Patterns" Width="130"
                    Click="Analyze_Click"/>
        </WrapPanel>

        <TextBox Grid.Row="1" x:Name="ResultBox"
                 IsReadOnly="True" TextWrapping="Wrap"
                 VerticalScrollBarVisibility="Auto" AcceptsReturn="True"
                 FontFamily="Segoe UI" FontSize="13" Padding="8"
                 Text="Select a time range and click 'Analyze Patterns' to identify trends in your tracked entries."/>
    </Grid>
</Window>
```

- [ ] **Step 2: Create AiInsightsWindow.xaml.cs**

```csharp
using System.Windows;
using TaskNoteTracker.Data;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Windows;

public partial class AiInsightsWindow : Window
{
    private readonly EntryService _entryService;
    private readonly AiService _aiService;

    public AiInsightsWindow(AppDbContext db, AiService aiService)
    {
        _entryService = new EntryService(db);
        _aiService = aiService;
        InitializeComponent();
    }

    private async void Analyze_Click(object sender, RoutedEventArgs e)
    {
        AnalyzeBtn.IsEnabled = false;
        AnalyzeBtn.Content = "Analyzing...";
        ResultBox.Text = string.Empty;

        try
        {
            var filter = BuildFilter();
            var entries = await _entryService.GetFilteredAsync(filter);
            ResultBox.Text = await _aiService.AnalyzePatternsAsync(entries);
        }
        catch (Exception ex)
        {
            ResultBox.Text = $"OpenAI error: {ex.Message}";
        }
        finally
        {
            AnalyzeBtn.IsEnabled = true;
            AnalyzeBtn.Content = "Analyze Patterns";
        }
    }

    private EntryFilter BuildFilter()
    {
        var fromUtc = RangeBox.SelectedIndex switch
        {
            0 => (DateTime?)DateTime.UtcNow.AddDays(-30),
            1 => (DateTime?)DateTime.UtcNow.AddDays(-90),
            _ => null
        };
        return new EntryFilter(FromUtc: fromUtc);
    }
}
```

- [ ] **Step 3: Build to verify**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet build TaskNoteTracker/TaskNoteTracker.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add TaskNoteTracker/Windows/AiInsightsWindow.xaml TaskNoteTracker/Windows/AiInsightsWindow.xaml.cs
git commit -m "feat: add AiInsightsWindow for pattern analysis"
```

---

## Task 7: Update TrayManager + App.xaml.cs

**Files:**
- Modify: `TaskNoteTracker/Tray/TrayManager.cs`
- Modify: `TaskNoteTracker/App.xaml.cs`

- [ ] **Step 1: Update TrayManager.cs**

Replace the full file content:

```csharp
using System.Drawing;
using System.Windows.Forms;
using TaskNoteTracker.Data;
using TaskNoteTracker.Services;
using TaskNoteTracker.Windows;

namespace TaskNoteTracker.Tray;

public sealed class TrayManager : IDisposable
{
    private readonly AppDbContext _db;
    private readonly AiService _aiService;
    private readonly SettingsService _settingsService;
    private readonly TrackingTimer _timer;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _toggleItem;
    private TrackingPopupWindow? _popup;
    private bool _isTracking;

    public event EventHandler? ExitRequested;

    public TrayManager(AppDbContext db, AiService aiService, SettingsService settingsService)
    {
        _db = db;
        _aiService = aiService;
        _settingsService = settingsService;
        _timer = new TrackingTimer(TimeSpan.FromMinutes(30));
        _timer.IntervalElapsed += OnTimerElapsed;

        _toggleItem = new ToolStripMenuItem("Start Tracking", null, OnToggleTracking);
        var menu = new ContextMenuStrip();
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Open History", null, OnOpenHistory);
        menu.Items.Add("Manage Task Types", null, OnManageTaskTypes);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("AI Insights", null, OnOpenAiInsights);
        menu.Items.Add("Settings", null, OnOpenSettings);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, OnExit);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Task Note Tracker — Not tracking",
            ContextMenuStrip = menu,
            Visible = true
        };
    }

    private void OnToggleTracking(object? sender, EventArgs e)
    {
        if (_isTracking) StopTracking();
        else StartTracking();
    }

    private void StartTracking()
    {
        _isTracking = true;
        _toggleItem.Text = "Stop Tracking";
        _notifyIcon.Text = "Task Note Tracker — Tracking";
        _timer.Start();
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_popup != null) return;
            _popup = new TrackingPopupWindow(_db);
            _popup.Closed += OnPopupClosed;
            _popup.Show();
        });
    }

    private void StopTracking()
    {
        _isTracking = false;
        _toggleItem.Text = "Start Tracking";
        _notifyIcon.Text = "Task Note Tracker — Not tracking";
        _timer.Stop();
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _popup?.Close();
            _popup = null;
        });
    }

    private void OnTimerElapsed(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_popup != null) return;
            _popup = new TrackingPopupWindow(_db);
            _popup.Closed += OnPopupClosed;
            _popup.Show();
        });
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _popup = null;
        if (_isTracking) _timer.Reset();
    }

    private void OnOpenHistory(object? sender, EventArgs e) =>
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
            new HistoryWindow(_db, _aiService).Show());

    private void OnManageTaskTypes(object? sender, EventArgs e) =>
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
            new TaskTypeManagerWindow(_db).ShowDialog());

    private void OnOpenAiInsights(object? sender, EventArgs e) =>
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
            new AiInsightsWindow(_db, _aiService).Show());

    private void OnOpenSettings(object? sender, EventArgs e) =>
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
            new SettingsWindow(_settingsService).ShowDialog());

    private void OnExit(object? sender, EventArgs e) =>
        ExitRequested?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        _timer.Dispose();
        _notifyIcon.Dispose();
    }
}
```

- [ ] **Step 2: Update App.xaml.cs**

Replace the full file content:

```csharp
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using TaskNoteTracker.Data;
using TaskNoteTracker.Services;
using TaskNoteTracker.Tray;

namespace TaskNoteTracker;

public partial class App : Application
{
    private TrayManager? _trayManager;
    private AppDbContext? _db;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskNoteTracker");
        Directory.CreateDirectory(dataDir);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"DataSource={Path.Combine(dataDir, "tracker.db")}")
            .Options;
        _db = new AppDbContext(options);
        _db.Database.Migrate();

        new StartupService().EnsureEnabled();

        var settingsService = new SettingsService();
        var aiService = new AiService(settingsService);

        _trayManager = new TrayManager(_db, aiService, settingsService);
        _trayManager.ExitRequested += (_, _) => Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayManager?.Dispose();
        _db?.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 3: Build to verify**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet build TaskNoteTracker/TaskNoteTracker.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add TaskNoteTracker/Tray/TrayManager.cs TaskNoteTracker/App.xaml.cs
git commit -m "feat: wire SettingsService and AiService into TrayManager and App startup"
```

---

## Task 8: HistoryWindow Modifications

**Files:**
- Modify: `TaskNoteTracker/Windows/HistoryWindow.xaml`
- Modify: `TaskNoteTracker/Windows/HistoryWindow.xaml.cs`

- [ ] **Step 1: Update HistoryWindow.xaml**

Replace the full file content. Key changes: add a "Generate Summary" button to the toolbar row, a new `<RowDefinition>` for the summary panel, and the panel itself.

```xml
<Window x:Class="TaskNoteTracker.Windows.HistoryWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Activity History" Height="600" Width="900"
        WindowStartupLocation="CenterScreen">
    <Grid Margin="12">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Filter bar -->
        <WrapPanel Grid.Row="0" Margin="0,0,0,8" Orientation="Horizontal">
            <TextBlock Text="From:" VerticalAlignment="Center" Margin="0,0,4,0"/>
            <DatePicker x:Name="FromDate" Width="130" Margin="0,0,12,0"/>
            <TextBlock Text="To:" VerticalAlignment="Center" Margin="0,0,4,0"/>
            <DatePicker x:Name="ToDate" Width="130" Margin="0,0,12,0"/>
            <TextBlock Text="Type:" VerticalAlignment="Center" Margin="0,0,4,0"/>
            <ComboBox x:Name="TypeFilter" Width="140" Margin="0,0,12,0"
                      SelectionChanged="TypeFilter_SelectionChanged"/>
            <TextBlock Text="Search:" VerticalAlignment="Center" Margin="0,0,4,0"/>
            <TextBox x:Name="SearchBox" Width="180" Margin="0,0,12,0"
                     TextChanged="SearchBox_TextChanged"/>
            <Button Content="Apply" Width="60" Click="Apply_Click"
                    Margin="0,0,8,0"/>
            <Button Content="Clear" Width="60" Click="Clear_Click"/>
        </WrapPanel>

        <!-- Toolbar: export + AI summary -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,8">
            <Button Content="Export CSV" Width="100" Margin="0,0,8,0"
                    Click="ExportCsv_Click"/>
            <Button Content="Export Excel" Width="110" Margin="0,0,16,0"
                    Click="ExportExcel_Click"/>
            <Button x:Name="GenerateSummaryBtn" Content="Generate Summary" Width="140"
                    Click="GenerateSummary_Click"/>
        </StackPanel>

        <!-- Data grid -->
        <DataGrid Grid.Row="2" x:Name="EntriesGrid"
                  AutoGenerateColumns="False" IsReadOnly="True"
                  CanUserAddRows="False" CanUserDeleteRows="False"
                  SelectionMode="Single" GridLinesVisibility="Horizontal">
            <DataGrid.Columns>
                <DataGridTextColumn Header="BDT Time" Binding="{Binding BdtTime}" Width="130"/>
                <DataGridTextColumn Header="CDT Time" Binding="{Binding CdtTime}" Width="130"/>
                <DataGridTextColumn Header="Task Type" Binding="{Binding TaskType}" Width="120"/>
                <DataGridTextColumn Header="Activity" Binding="{Binding Activity}" Width="*"/>
                <DataGridTextColumn Header="Notes" Binding="{Binding Notes}" Width="160"/>
                <DataGridTemplateColumn Header="Actions" Width="130">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Button Content="Edit" Width="55" Margin="0,1,4,1"
                                        Tag="{Binding}" Click="Edit_Click"/>
                                <Button Content="Delete" Width="55" Margin="0,1,0,1"
                                        Tag="{Binding}" Click="Delete_Click"/>
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

        <!-- AI summary panel (hidden until first generation) -->
        <Border Grid.Row="3" x:Name="SummaryPanel" Visibility="Collapsed"
                BorderBrush="#DDDDDD" BorderThickness="1"
                Margin="0,8,0,0" Padding="8">
            <TextBox x:Name="SummaryBox"
                     IsReadOnly="True" TextWrapping="Wrap"
                     VerticalScrollBarVisibility="Auto" AcceptsReturn="True"
                     MaxHeight="180" FontSize="12"
                     Background="Transparent" BorderThickness="0"/>
        </Border>

        <!-- Row count -->
        <TextBlock Grid.Row="4" x:Name="RowCountLabel"
                   Margin="0,6,0,0" Foreground="Gray" FontSize="11"/>
    </Grid>
</Window>
```

- [ ] **Step 2: Update HistoryWindow.xaml.cs**

Replace the full file content:

```csharp
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TaskNoteTracker.Data;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Windows;

public partial class HistoryWindow : Window
{
    private readonly AppDbContext _db;
    private readonly EntryService _entryService;
    private readonly TaskTypeService _taskTypeService;
    private readonly ExportService _exportService = new();
    private readonly AiService _aiService;

    public HistoryWindow(AppDbContext db, AiService aiService)
    {
        _db = db;
        _aiService = aiService;
        _entryService = new EntryService(db);
        _taskTypeService = new TaskTypeService(db);
        InitializeComponent();
        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        var types = new List<TaskType> { new() { Id = 0, Name = "All types" } };
        types.AddRange(await _taskTypeService.GetAllAsync());
        TypeFilter.ItemsSource = types;
        TypeFilter.DisplayMemberPath = "Name";
        TypeFilter.SelectedIndex = 0;
        await RefreshGridAsync();
    }

    private async Task RefreshGridAsync()
    {
        var filter = BuildFilter();
        var entries = await _entryService.GetFilteredAsync(filter);
        EntriesGrid.ItemsSource = entries.Select(e => new EntryRow
        {
            Id = e.Id,
            BdtTime = TimezoneService.ToBdt(e.TimestampUtc).ToString("yyyy-MM-dd HH:mm"),
            CdtTime = TimezoneService.ToCdt(e.TimestampUtc).ToString("yyyy-MM-dd HH:mm"),
            TaskType = e.TaskType.Name,
            Activity = e.Activity,
            Notes = e.Notes ?? string.Empty,
            TaskTypeId = e.TaskTypeId,
            TimestampUtc = e.TimestampUtc
        }).ToList();
        RowCountLabel.Text = $"{entries.Count} entry(ies)";
    }

    private EntryFilter BuildFilter()
    {
        var fromUtc = FromDate.SelectedDate.HasValue
            ? (DateTime?)FromDate.SelectedDate.Value.ToUniversalTime() : null;
        var toUtc = ToDate.SelectedDate.HasValue
            ? (DateTime?)ToDate.SelectedDate.Value.AddDays(1).ToUniversalTime() : null;
        var taskTypeId = TypeFilter.SelectedItem is TaskType { Id: > 0 } t
            ? (int?)t.Id : null;
        var search = string.IsNullOrWhiteSpace(SearchBox.Text) ? null : SearchBox.Text;
        return new EntryFilter(fromUtc, toUtc, taskTypeId, search);
    }

    private async void Apply_Click(object sender, RoutedEventArgs e) =>
        await RefreshGridAsync();

    private async void Clear_Click(object sender, RoutedEventArgs e)
    {
        FromDate.SelectedDate = null;
        ToDate.SelectedDate = null;
        TypeFilter.SelectedIndex = 0;
        SearchBox.Text = string.Empty;
        await RefreshGridAsync();
    }

    private async void TypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        await RefreshGridAsync();

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e) =>
        await RefreshGridAsync();

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not EntryRow row) return;
        var dialog = new EditEntryDialog(_db, row.Id) { Owner = this };
        if (dialog.ShowDialog() == true)
            await RefreshGridAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not EntryRow row) return;
        var confirm = MessageBox.Show(
            $"Delete \"{row.Activity}\"?", "Confirm Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;
        await _entryService.DeleteAsync(row.Id);
        await RefreshGridAsync();
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var entries = await _entryService.GetFilteredAsync(BuildFilter());
        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"activity-log-{DateTime.Now:yyyy-MM-dd}.csv"
        };
        if (dialog.ShowDialog() != true) return;
        await File.WriteAllBytesAsync(dialog.FileName, _exportService.ExportToCsv(entries));
        MessageBox.Show("CSV exported.", "Done", MessageBoxButton.OK);
    }

    private async void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var entries = await _entryService.GetFilteredAsync(BuildFilter());
        var dialog = new SaveFileDialog
        {
            Filter = "Excel files (*.xlsx)|*.xlsx",
            FileName = $"activity-log-{DateTime.Now:yyyy-MM-dd}.xlsx"
        };
        if (dialog.ShowDialog() != true) return;
        await File.WriteAllBytesAsync(dialog.FileName, _exportService.ExportToExcel(entries));
        MessageBox.Show("Excel file exported.", "Done", MessageBoxButton.OK);
    }

    private async void GenerateSummary_Click(object sender, RoutedEventArgs e)
    {
        GenerateSummaryBtn.IsEnabled = false;
        GenerateSummaryBtn.Content = "Generating...";
        SummaryPanel.Visibility = Visibility.Visible;
        SummaryBox.Text = string.Empty;

        try
        {
            var entries = await _entryService.GetFilteredAsync(BuildFilter());
            SummaryBox.Text = await _aiService.GenerateSummaryAsync(entries);
        }
        catch (Exception ex)
        {
            SummaryBox.Text = $"OpenAI error: {ex.Message}";
        }
        finally
        {
            GenerateSummaryBtn.IsEnabled = true;
            GenerateSummaryBtn.Content = "Generate Summary";
        }
    }
}

public class EntryRow
{
    public int Id { get; init; }
    public string BdtTime { get; init; } = string.Empty;
    public string CdtTime { get; init; } = string.Empty;
    public string TaskType { get; init; } = string.Empty;
    public string Activity { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public int TaskTypeId { get; init; }
    public DateTime TimestampUtc { get; init; }
}
```

- [ ] **Step 3: Build to verify**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet build TaskNoteTracker/TaskNoteTracker.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Run all tests**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet test TaskNoteTracker.Tests/TaskNoteTracker.Tests.csproj -v minimal
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add TaskNoteTracker/Windows/HistoryWindow.xaml TaskNoteTracker/Windows/HistoryWindow.xaml.cs
git commit -m "feat: add Generate Summary button and AI result panel to HistoryWindow"
```

---

## Manual Smoke Test Checklist

After all tasks are complete, manually verify:

- [ ] App launches — tray icon appears
- [ ] "Settings" tray menu item opens `SettingsWindow`
- [ ] Entering and saving an API key persists across app restarts (check `%APPDATA%\TaskNoteTracker\settings.json`)
- [ ] "AI Insights" tray menu item opens `AiInsightsWindow`
- [ ] Without an API key: clicking "Analyze Patterns" shows "Please configure your OpenAI API key in Settings."
- [ ] With a valid API key + entries: "Analyze Patterns" returns a real AI response
- [ ] In `HistoryWindow`: "Generate Summary" button appears in toolbar
- [ ] Without entries matching filters: shows "No entries to analyze..."
- [ ] With entries + valid key: summary panel appears below grid with AI-generated text
- [ ] Model selection in Settings works (`gpt-4o-mini` vs `gpt-4o`)
