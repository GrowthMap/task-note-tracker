# Task Note Tracker Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows system-tray app in C#/.NET 8/WPF that prompts every 30 minutes to log current activity, storing entries in a local SQLite database with BDT and CDT timestamps, with a filterable history view and CSV/Excel export.

**Architecture:** Single WPF app with no main window — lives in the system tray via WinForms `NotifyIcon` interop. All business logic lives in pure service classes (`TimezoneService`, `EntryService`, `TaskTypeService`, `ExportService`, `TrackingTimer`), independently tested with xUnit. WPF windows hold only UI code. EF Core + SQLite stores data in `%AppData%\TaskNoteTracker\tracker.db`. No DI framework — services are manually instantiated and passed by constructor.

**Tech Stack:** .NET 8, C#, WPF + WinForms interop, EF Core 8 + SQLite, ClosedXML, xUnit + FluentAssertions

---

## Prerequisites

- .NET 8 SDK installed (`dotnet --version` should show `8.x.x`)
- Visual Studio 2022 or VS Code with C# extension (optional — all commands use `dotnet` CLI)

---

## File Map

### App project: `TaskNoteTracker/`

| File | Responsibility |
|------|---------------|
| `App.xaml` | Application definition — no `StartupUri` |
| `App.xaml.cs` | Silent startup; DB init; registry; creates TrayManager |
| `Data/Models/TaskType.cs` | TaskType EF entity |
| `Data/Models/TaskEntry.cs` | TaskEntry EF entity |
| `Data/AppDbContext.cs` | EF Core DbContext with FK configuration |
| `Data/AppDbContextFactory.cs` | Design-time factory for `dotnet ef migrations` |
| `Services/TimezoneService.cs` | Static: UTC→BDT and UTC→CDT |
| `Services/TaskTypeService.cs` | Add / list / delete task types |
| `Services/EntryService.cs` | Create / update / delete / filter entries |
| `Services/ExportService.cs` | CSV and Excel (.xlsx) export |
| `Services/TrackingTimer.cs` | 30-min countdown; fires event once; resets |
| `Services/StartupService.cs` | Windows registry auto-start |
| `Tray/TrayManager.cs` | NotifyIcon; context menu; popup orchestration |
| `Windows/TrackingPopupWindow.xaml(.cs)` | 30-min form popup |
| `Windows/AddTaskTypeDialog.xaml(.cs)` | Inline "add new type" modal |
| `Windows/HistoryWindow.xaml(.cs)` | Filter / export / edit / delete view |
| `Windows/EditEntryDialog.xaml(.cs)` | Edit a single entry |
| `Windows/TaskTypeManagerWindow.xaml(.cs)` | Manage task type list |

### Test project: `TaskNoteTracker.Tests/`

| File | Tests |
|------|-------|
| `Helpers/DbFactory.cs` | Shared in-memory SQLite context factory |
| `Services/TimezoneServiceTests.cs` | UTC→BDT, UTC→CDT including DST |
| `Services/TaskTypeServiceTests.cs` | Add, list, delete, blocked-delete |
| `Services/EntryServiceTests.cs` | Create, update, delete, filter combos |
| `Services/ExportServiceTests.cs` | CSV and Excel output correctness |
| `Services/TrackingTimerTests.cs` | Fires, stops, resets, fires exactly once per interval |

---

## Task 1: Solution Scaffold

**Files:**
- Create: `TaskNoteTracker.sln`
- Create: `TaskNoteTracker/TaskNoteTracker.csproj`
- Create: `TaskNoteTracker.Tests/TaskNoteTracker.Tests.csproj`

- [ ] **Step 1: Create solution and projects**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet new sln -n TaskNoteTracker
dotnet new wpf -n TaskNoteTracker -o TaskNoteTracker --framework net8.0-windows
dotnet new xunit -n TaskNoteTracker.Tests -o TaskNoteTracker.Tests --framework net8.0-windows
dotnet sln add TaskNoteTracker/TaskNoteTracker.csproj
dotnet sln add TaskNoteTracker.Tests/TaskNoteTracker.Tests.csproj
cd TaskNoteTracker.Tests && dotnet add reference ../TaskNoteTracker/TaskNoteTracker.csproj && cd ..
```

- [ ] **Step 2: Delete default generated files not needed**

```bash
rm TaskNoteTracker/MainWindow.xaml
rm TaskNoteTracker/MainWindow.xaml.cs
```

- [ ] **Step 3: Replace `TaskNoteTracker/TaskNoteTracker.csproj` with this content**

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
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Replace `TaskNoteTracker.Tests/TaskNoteTracker.Tests.csproj` with this content**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.6" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\TaskNoteTracker\TaskNoteTracker.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Verify solution restores and builds**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet restore
dotnet build
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 6: Commit**

```bash
git init
git add TaskNoteTracker.sln TaskNoteTracker/ TaskNoteTracker.Tests/
git commit -m "feat: scaffold solution with WPF app and test projects"
```

---

## Task 2: Domain Models + DbContext

**Files:**
- Create: `TaskNoteTracker/Data/Models/TaskType.cs`
- Create: `TaskNoteTracker/Data/Models/TaskEntry.cs`
- Create: `TaskNoteTracker/Data/AppDbContext.cs`
- Create: `TaskNoteTracker/Data/AppDbContextFactory.cs`
- Create: `TaskNoteTracker.Tests/Helpers/DbFactory.cs`
- Create: `TaskNoteTracker.Tests/Services/AppDbContextTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `TaskNoteTracker.Tests/Helpers/DbFactory.cs`:

```csharp
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskNoteTracker.Data;

namespace TaskNoteTracker.Tests.Helpers;

public static class DbFactory
{
    public static (AppDbContext db, SqliteConnection conn) Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return (db, connection);
    }
}
```

Create `TaskNoteTracker.Tests/Services/AppDbContextTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Tests.Helpers;

namespace TaskNoteTracker.Tests.Services;

public class AppDbContextTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _conn;
    private readonly TaskNoteTracker.Data.AppDbContext _db;

    public AppDbContextTests()
    {
        (_db, _conn) = DbFactory.Create();
    }

    [Fact]
    public void CanSaveAndRetrieveTaskType()
    {
        _db.TaskTypes.Add(new TaskType { Name = "Deep Work" });
        _db.SaveChanges();

        _db.TaskTypes.Single().Name.Should().Be("Deep Work");
    }

    [Fact]
    public void CanSaveAndRetrieveTaskEntry()
    {
        var taskType = new TaskType { Name = "Meeting" };
        _db.TaskTypes.Add(taskType);
        _db.SaveChanges();

        _db.TaskEntries.Add(new TaskEntry
        {
            TimestampUtc = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc),
            Activity = "Sprint planning",
            Notes = "Q2 goals",
            TaskTypeId = taskType.Id
        });
        _db.SaveChanges();

        var entry = _db.TaskEntries.Include(e => e.TaskType).Single();
        entry.Activity.Should().Be("Sprint planning");
        entry.TaskType.Name.Should().Be("Meeting");
    }

    [Fact]
    public void DeleteTaskType_WhenEntriesExist_Throws()
    {
        var taskType = new TaskType { Name = "Work" };
        _db.TaskTypes.Add(taskType);
        _db.SaveChanges();

        _db.TaskEntries.Add(new TaskEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Activity = "Coding",
            TaskTypeId = taskType.Id
        });
        _db.SaveChanges();

        _db.TaskTypes.Remove(taskType);
        var act = () => _db.SaveChanges();
        act.Should().Throw<Exception>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure (types don't exist yet)**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet test TaskNoteTracker.Tests --no-build 2>&1 | head -20
```

Expected: Build errors referencing missing `TaskType`, `TaskEntry`, `AppDbContext`.

- [ ] **Step 3: Create domain models**

Create `TaskNoteTracker/Data/Models/TaskType.cs`:

```csharp
namespace TaskNoteTracker.Data.Models;

public class TaskType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<TaskEntry> Entries { get; set; } = [];
}
```

Create `TaskNoteTracker/Data/Models/TaskEntry.cs`:

```csharp
namespace TaskNoteTracker.Data.Models;

public class TaskEntry
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int TaskTypeId { get; set; }
    public TaskType TaskType { get; set; } = null!;
}
```

Create `TaskNoteTracker/Data/AppDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TaskNoteTracker.Data.Models;

namespace TaskNoteTracker.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaskType> TaskTypes => Set<TaskType>();
    public DbSet<TaskEntry> TaskEntries => Set<TaskEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskEntry>()
            .HasOne(e => e.TaskType)
            .WithMany(t => t.Entries)
            .HasForeignKey(e => e.TaskTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

Create `TaskNoteTracker/Data/AppDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskNoteTracker.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=design-time.db")
            .Options;
        return new AppDbContext(options);
    }
}
```

- [ ] **Step 4: Run tests — expect all pass**

```bash
dotnet test TaskNoteTracker.Tests
```

Expected: `Passed! - 3 tests`.

- [ ] **Step 5: Create the initial EF migration**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet tool install --global dotnet-ef --version 8.0.0
dotnet ef migrations add Initial --project TaskNoteTracker --startup-project TaskNoteTracker
```

Expected: `Done. To undo this action, use 'ef migrations remove'`. Creates `Migrations/` folder in `TaskNoteTracker/`.

- [ ] **Step 6: Commit**

```bash
git add TaskNoteTracker/ TaskNoteTracker.Tests/
git commit -m "feat: add domain models, DbContext, and initial EF migration"
```

---

## Task 3: TimezoneService

**Files:**
- Create: `TaskNoteTracker/Services/TimezoneService.cs`
- Create: `TaskNoteTracker.Tests/Services/TimezoneServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `TaskNoteTracker.Tests/Services/TimezoneServiceTests.cs`:

```csharp
using FluentAssertions;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Tests.Services;

public class TimezoneServiceTests
{
    [Fact]
    public void ToBdt_ConvertsUtcCorrectly()
    {
        // BDT is UTC+6, no DST
        var utc = new DateTime(2026, 4, 1, 6, 0, 0, DateTimeKind.Utc);
        TimezoneService.ToBdt(utc).Should().Be(new DateTime(2026, 4, 1, 12, 0, 0));
    }

    [Fact]
    public void ToBdt_MidnightUtcIsEarlyMorningBdt()
    {
        var utc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        TimezoneService.ToBdt(utc).Should().Be(new DateTime(2026, 1, 15, 6, 0, 0));
    }

    [Fact]
    public void ToCdt_ConvertsUtcDuringDaylightSavingTime()
    {
        // CDT (summer) = UTC-5
        var utc = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        TimezoneService.ToCdt(utc).Should().Be(new DateTime(2026, 7, 1, 7, 0, 0));
    }

    [Fact]
    public void ToCdt_ConvertsUtcDuringStandardTime()
    {
        // CST (winter) = UTC-6
        var utc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        TimezoneService.ToCdt(utc).Should().Be(new DateTime(2026, 1, 15, 6, 0, 0));
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure**

```bash
dotnet test TaskNoteTracker.Tests
```

Expected: Compile error: `TimezoneService does not exist`.

- [ ] **Step 3: Implement TimezoneService**

Create `TaskNoteTracker/Services/TimezoneService.cs`:

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
}
```

- [ ] **Step 4: Run tests — expect all pass**

```bash
dotnet test TaskNoteTracker.Tests
```

Expected: `Passed! - 7 tests` (3 from Task 2 + 4 new).

- [ ] **Step 5: Commit**

```bash
git add TaskNoteTracker/Services/TimezoneService.cs TaskNoteTracker.Tests/Services/TimezoneServiceTests.cs
git commit -m "feat: add TimezoneService with BDT and CDT conversion"
```

---

## Task 4: TaskTypeService

**Files:**
- Create: `TaskNoteTracker/Services/TaskTypeService.cs`
- Create: `TaskNoteTracker.Tests/Services/TaskTypeServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `TaskNoteTracker.Tests/Services/TaskTypeServiceTests.cs`:

```csharp
using FluentAssertions;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Services;
using TaskNoteTracker.Tests.Helpers;

namespace TaskNoteTracker.Tests.Services;

public class TaskTypeServiceTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _conn;
    private readonly TaskNoteTracker.Data.AppDbContext _db;
    private readonly TaskTypeService _service;

    public TaskTypeServiceTests()
    {
        (_db, _conn) = DbFactory.Create();
        _service = new TaskTypeService(_db);
    }

    [Fact]
    public async Task AddAsync_StoresTaskType()
    {
        await _service.AddAsync("Deep Work");
        var all = await _service.GetAllAsync();
        all.Should().ContainSingle(t => t.Name == "Deep Work");
    }

    [Fact]
    public async Task AddAsync_TrimsWhitespace()
    {
        await _service.AddAsync("  Meeting  ");
        var all = await _service.GetAllAsync();
        all.Should().ContainSingle(t => t.Name == "Meeting");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSortedByName()
    {
        await _service.AddAsync("Zeta");
        await _service.AddAsync("Alpha");
        var all = await _service.GetAllAsync();
        all.Select(t => t.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task CanDeleteAsync_ReturnsTrueWhenNoEntries()
    {
        var type = await _service.AddAsync("Admin");
        (await _service.CanDeleteAsync(type.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task CanDeleteAsync_ReturnsFalseWhenEntriesExist()
    {
        var type = await _service.AddAsync("Break");
        _db.TaskEntries.Add(new TaskEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Activity = "Coffee",
            TaskTypeId = type.Id
        });
        await _db.SaveChangesAsync();

        (await _service.CanDeleteAsync(type.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovesTaskType_WhenNoEntries()
    {
        var type = await _service.AddAsync("Research");
        await _service.DeleteAsync(type.Id);
        (await _service.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenEntriesExist()
    {
        var type = await _service.AddAsync("Work");
        _db.TaskEntries.Add(new TaskEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Activity = "Coding",
            TaskTypeId = type.Id
        });
        await _db.SaveChangesAsync();

        var act = async () => await _service.DeleteAsync(type.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*entries*");
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure**

```bash
dotnet test TaskNoteTracker.Tests
```

Expected: Compile error: `TaskTypeService does not exist`.

- [ ] **Step 3: Implement TaskTypeService**

Create `TaskNoteTracker/Services/TaskTypeService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TaskNoteTracker.Data;
using TaskNoteTracker.Data.Models;

namespace TaskNoteTracker.Services;

public class TaskTypeService(AppDbContext db)
{
    public async Task<IReadOnlyList<TaskType>> GetAllAsync() =>
        await db.TaskTypes.OrderBy(t => t.Name).ToListAsync();

    public async Task<TaskType> AddAsync(string name)
    {
        var taskType = new TaskType { Name = name.Trim() };
        db.TaskTypes.Add(taskType);
        await db.SaveChangesAsync();
        return taskType;
    }

    public async Task<bool> CanDeleteAsync(int id) =>
        !await db.TaskEntries.AnyAsync(e => e.TaskTypeId == id);

    public async Task DeleteAsync(int id)
    {
        var taskType = await db.TaskTypes.FindAsync(id)
            ?? throw new InvalidOperationException($"TaskType {id} not found.");

        if (await db.TaskEntries.AnyAsync(e => e.TaskTypeId == id))
            throw new InvalidOperationException(
                "Cannot delete a task type that has entries.");

        db.TaskTypes.Remove(taskType);
        await db.SaveChangesAsync();
    }
}
```

- [ ] **Step 4: Run tests — expect all pass**

```bash
dotnet test TaskNoteTracker.Tests
```

Expected: `Passed! - 14 tests`.

- [ ] **Step 5: Commit**

```bash
git add TaskNoteTracker/Services/TaskTypeService.cs TaskNoteTracker.Tests/Services/TaskTypeServiceTests.cs
git commit -m "feat: add TaskTypeService with add/list/delete"
```

---

## Task 5: EntryService

**Files:**
- Create: `TaskNoteTracker/Services/EntryService.cs`
- Create: `TaskNoteTracker.Tests/Services/EntryServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `TaskNoteTracker.Tests/Services/EntryServiceTests.cs`:

```csharp
using FluentAssertions;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Services;
using TaskNoteTracker.Tests.Helpers;

namespace TaskNoteTracker.Tests.Services;

public class EntryServiceTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _conn;
    private readonly TaskNoteTracker.Data.AppDbContext _db;
    private readonly EntryService _service;
    private readonly TaskType _taskType;

    public EntryServiceTests()
    {
        (_db, _conn) = DbFactory.Create();
        _service = new EntryService(_db);
        _taskType = new TaskType { Name = "Work" };
        _db.TaskTypes.Add(_taskType);
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_SavesEntry()
    {
        var utc = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        var entry = await _service.CreateAsync(utc, "Writing tests", "notes here", _taskType.Id);

        entry.Id.Should().BeGreaterThan(0);
        entry.Activity.Should().Be("Writing tests");
        entry.Notes.Should().Be("notes here");
        entry.TaskTypeId.Should().Be(_taskType.Id);
    }

    [Fact]
    public async Task CreateAsync_TrimsActivity()
    {
        var entry = await _service.CreateAsync(DateTime.UtcNow, "  Coding  ", null, _taskType.Id);
        entry.Activity.Should().Be("Coding");
    }

    [Fact]
    public async Task UpdateAsync_ChangesFields()
    {
        var entry = await _service.CreateAsync(DateTime.UtcNow, "Old activity", null, _taskType.Id);
        var otherType = new TaskType { Name = "Break" };
        _db.TaskTypes.Add(otherType);
        await _db.SaveChangesAsync();

        await _service.UpdateAsync(entry.Id, "New activity", "new notes", otherType.Id);

        var updated = await _db.TaskEntries.FindAsync(entry.Id);
        updated!.Activity.Should().Be("New activity");
        updated.Notes.Should().Be("new notes");
        updated.TaskTypeId.Should().Be(otherType.Id);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntry()
    {
        var entry = await _service.CreateAsync(DateTime.UtcNow, "Delete me", null, _taskType.Id);
        await _service.DeleteAsync(entry.Id);
        _db.TaskEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByDateRange()
    {
        await _service.CreateAsync(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), "March entry", null, _taskType.Id);
        await _service.CreateAsync(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), "April entry", null, _taskType.Id);

        var results = await _service.GetFilteredAsync(new EntryFilter(
            FromUtc: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)));

        results.Should().ContainSingle(e => e.Activity == "April entry");
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByTaskType()
    {
        var otherType = new TaskType { Name = "Break" };
        _db.TaskTypes.Add(otherType);
        await _db.SaveChangesAsync();

        await _service.CreateAsync(DateTime.UtcNow, "Work entry", null, _taskType.Id);
        await _service.CreateAsync(DateTime.UtcNow, "Break entry", null, otherType.Id);

        var results = await _service.GetFilteredAsync(new EntryFilter(TaskTypeId: otherType.Id));
        results.Should().ContainSingle(e => e.Activity == "Break entry");
    }

    [Fact]
    public async Task GetFilteredAsync_SearchesActivityAndNotes()
    {
        await _service.CreateAsync(DateTime.UtcNow, "Reviewing PRs", null, _taskType.Id);
        await _service.CreateAsync(DateTime.UtcNow, "Coding", "see the PR for context", _taskType.Id);
        await _service.CreateAsync(DateTime.UtcNow, "Lunch", null, _taskType.Id);

        var results = await _service.GetFilteredAsync(new EntryFilter(SearchText: "PR"));
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFilteredAsync_ReturnsNewestFirst()
    {
        await _service.CreateAsync(new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc), "First", null, _taskType.Id);
        await _service.CreateAsync(new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc), "Second", null, _taskType.Id);

        var results = await _service.GetFilteredAsync(new EntryFilter());
        results[0].Activity.Should().Be("Second");
        results[1].Activity.Should().Be("First");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectEntry()
    {
        var entry = await _service.CreateAsync(DateTime.UtcNow, "Find me", null, _taskType.Id);
        var found = await _service.GetByIdAsync(entry.Id);
        found!.Activity.Should().Be("Find me");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var found = await _service.GetByIdAsync(9999);
        found.Should().BeNull();
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure**

```bash
dotnet test TaskNoteTracker.Tests
```

Expected: Compile error: `EntryService`, `EntryFilter` do not exist.

- [ ] **Step 3: Implement EntryService**

Create `TaskNoteTracker/Services/EntryService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TaskNoteTracker.Data;
using TaskNoteTracker.Data.Models;

namespace TaskNoteTracker.Services;

public record EntryFilter(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int? TaskTypeId = null,
    string? SearchText = null
);

public class EntryService(AppDbContext db)
{
    public async Task<TaskEntry> CreateAsync(
        DateTime timestampUtc, string activity, string? notes, int taskTypeId)
    {
        var entry = new TaskEntry
        {
            TimestampUtc = timestampUtc,
            Activity = activity.Trim(),
            Notes = notes?.Trim(),
            TaskTypeId = taskTypeId
        };
        db.TaskEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    public async Task UpdateAsync(int id, string activity, string? notes, int taskTypeId)
    {
        var entry = await db.TaskEntries.FindAsync(id)
            ?? throw new InvalidOperationException($"Entry {id} not found.");
        entry.Activity = activity.Trim();
        entry.Notes = notes?.Trim();
        entry.TaskTypeId = taskTypeId;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entry = await db.TaskEntries.FindAsync(id)
            ?? throw new InvalidOperationException($"Entry {id} not found.");
        db.TaskEntries.Remove(entry);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<TaskEntry>> GetFilteredAsync(EntryFilter filter)
    {
        var query = db.TaskEntries.Include(e => e.TaskType).AsQueryable();

        if (filter.FromUtc.HasValue)
            query = query.Where(e => e.TimestampUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            query = query.Where(e => e.TimestampUtc <= filter.ToUtc.Value);
        if (filter.TaskTypeId.HasValue)
            query = query.Where(e => e.TaskTypeId == filter.TaskTypeId.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var text = $"%{filter.SearchText.Trim()}%";
            query = query.Where(e =>
                EF.Functions.Like(e.Activity, text) ||
                (e.Notes != null && EF.Functions.Like(e.Notes, text)));
        }

        return await query.OrderByDescending(e => e.TimestampUtc).ToListAsync();
    }

    public async Task<TaskEntry?> GetByIdAsync(int id) =>
        await db.TaskEntries.Include(e => e.TaskType).FirstOrDefaultAsync(e => e.Id == id);
}
```

- [ ] **Step 4: Run tests — expect all pass**

```bash
dotnet test TaskNoteTracker.Tests
```

Expected: `Passed! - 23 tests`.

- [ ] **Step 5: Commit**

```bash
git add TaskNoteTracker/Services/EntryService.cs TaskNoteTracker.Tests/Services/EntryServiceTests.cs
git commit -m "feat: add EntryService with create/update/delete/filter"
```

---

## Task 6: ExportService

**Files:**
- Create: `TaskNoteTracker/Services/ExportService.cs`
- Create: `TaskNoteTracker.Tests/Services/ExportServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `TaskNoteTracker.Tests/Services/ExportServiceTests.cs`:

```csharp
using ClosedXML.Excel;
using FluentAssertions;
using System.Text;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Tests.Services;

public class ExportServiceTests
{
    private static IReadOnlyList<TaskEntry> SampleEntries() =>
    [
        new TaskEntry
        {
            Id = 1,
            TimestampUtc = new DateTime(2026, 4, 1, 6, 0, 0, DateTimeKind.Utc), // BDT 12:00, CDT 01:00
            Activity = "Writing tests",
            Notes = "TDD cycle",
            TaskTypeId = 1,
            TaskType = new TaskType { Id = 1, Name = "Deep Work" }
        },
        new TaskEntry
        {
            Id = 2,
            TimestampUtc = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc), // BDT 14:00, CDT 03:00
            Activity = "Code review",
            Notes = null,
            TaskTypeId = 2,
            TaskType = new TaskType { Id = 2, Name = "Collaboration" }
        }
    ];

    [Fact]
    public void ExportToCsv_IncludesHeaderRow()
    {
        var csv = Encoding.UTF8.GetString(new ExportService().ExportToCsv(SampleEntries()));
        csv.Split('\n')[0].Should().Contain("BDT Time").And.Contain("CDT Time")
            .And.Contain("Task Type").And.Contain("Activity").And.Contain("Notes");
    }

    [Fact]
    public void ExportToCsv_IncludesEntryData()
    {
        var csv = Encoding.UTF8.GetString(new ExportService().ExportToCsv(SampleEntries()));
        csv.Should().Contain("Writing tests").And.Contain("Deep Work").And.Contain("TDD cycle");
    }

    [Fact]
    public void ExportToCsv_EscapesCommasInValues()
    {
        var entries = new List<TaskEntry>
        {
            new TaskEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Activity = "Planning, review, retrospective",
                TaskTypeId = 1,
                TaskType = new TaskType { Id = 1, Name = "Meeting" }
            }
        };
        var csv = Encoding.UTF8.GetString(new ExportService().ExportToCsv(entries));
        csv.Should().Contain("\"Planning, review, retrospective\"");
    }

    [Fact]
    public void ExportToExcel_HasCorrectHeaders()
    {
        var bytes = new ExportService().ExportToExcel(SampleEntries());
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);
        sheet.Cell(1, 1).Value.ToString().Should().Be("BDT Time");
        sheet.Cell(1, 2).Value.ToString().Should().Be("CDT Time");
        sheet.Cell(1, 3).Value.ToString().Should().Be("Task Type");
        sheet.Cell(1, 4).Value.ToString().Should().Be("Activity");
        sheet.Cell(1, 5).Value.ToString().Should().Be("Notes");
    }

    [Fact]
    public void ExportToExcel_HasCorrectRowCount()
    {
        var bytes = new ExportService().ExportToExcel(SampleEntries());
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);
        sheet.LastRowUsed()!.RowNumber().Should().Be(3); // header + 2 data rows
    }

    [Fact]
    public void ExportToExcel_RowDataMatchesEntries()
    {
        var bytes = new ExportService().ExportToExcel(SampleEntries());
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);
        sheet.Cell(2, 4).Value.ToString().Should().Be("Writing tests");
        sheet.Cell(2, 3).Value.ToString().Should().Be("Deep Work");
        sheet.Cell(2, 5).Value.ToString().Should().Be("TDD cycle");
        sheet.Cell(3, 4).Value.ToString().Should().Be("Code review");
        sheet.Cell(3, 5).Value.ToString().Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure**

```bash
dotnet test TaskNoteTracker.Tests
```

Expected: Compile error: `ExportService does not exist`.

- [ ] **Step 3: Implement ExportService**

Create `TaskNoteTracker/Services/ExportService.cs`:

```csharp
using System.Text;
using ClosedXML.Excel;
using TaskNoteTracker.Data.Models;

namespace TaskNoteTracker.Services;

public class ExportService
{
    public byte[] ExportToCsv(IReadOnlyList<TaskEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BDT Time,CDT Time,Task Type,Activity,Notes");
        foreach (var e in entries)
        {
            var bdt = TimezoneService.ToBdt(e.TimestampUtc).ToString("yyyy-MM-dd HH:mm");
            var cdt = TimezoneService.ToCdt(e.TimestampUtc).ToString("yyyy-MM-dd HH:mm");
            sb.AppendLine(string.Join(",",
                Escape(bdt), Escape(cdt),
                Escape(e.TaskType.Name), Escape(e.Activity), Escape(e.Notes ?? "")));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] ExportToExcel(IReadOnlyList<TaskEntry> entries)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Activity Log");

        sheet.Cell(1, 1).Value = "BDT Time";
        sheet.Cell(1, 2).Value = "CDT Time";
        sheet.Cell(1, 3).Value = "Task Type";
        sheet.Cell(1, 4).Value = "Activity";
        sheet.Cell(1, 5).Value = "Notes";

        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            var row = i + 2;
            sheet.Cell(row, 1).Value = TimezoneService.ToBdt(e.TimestampUtc).ToString("yyyy-MM-dd HH:mm");
            sheet.Cell(row, 2).Value = TimezoneService.ToCdt(e.TimestampUtc).ToString("yyyy-MM-dd HH:mm");
            sheet.Cell(row, 3).Value = e.TaskType.Name;
            sheet.Cell(row, 4).Value = e.Activity;
            sheet.Cell(row, 5).Value = e.Notes ?? "";
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
```

- [ ] **Step 4: Run tests — expect all pass**

```bash
dotnet test TaskNoteTracker.Tests
```

Expected: `Passed! - 29 tests`.

- [ ] **Step 5: Commit**

```bash
git add TaskNoteTracker/Services/ExportService.cs TaskNoteTracker.Tests/Services/ExportServiceTests.cs
git commit -m "feat: add ExportService for CSV and Excel export"
```

---

## Task 7: TrackingTimer

**Files:**
- Create: `TaskNoteTracker/Services/TrackingTimer.cs`
- Create: `TaskNoteTracker.Tests/Services/TrackingTimerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `TaskNoteTracker.Tests/Services/TrackingTimerTests.cs`:

```csharp
using FluentAssertions;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Tests.Services;

public class TrackingTimerTests
{
    [Fact]
    public async Task Start_FiresIntervalElapsedAfterInterval()
    {
        using var timer = new TrackingTimer(TimeSpan.FromMilliseconds(100));
        var fired = false;
        timer.IntervalElapsed += (_, _) => fired = true;

        timer.Start();
        await Task.Delay(250);

        fired.Should().BeTrue();
    }

    [Fact]
    public async Task Stop_PreventsEventFromFiring()
    {
        using var timer = new TrackingTimer(TimeSpan.FromMilliseconds(100));
        var fired = false;
        timer.IntervalElapsed += (_, _) => fired = true;

        timer.Start();
        timer.Stop();
        await Task.Delay(250);

        fired.Should().BeFalse();
    }

    [Fact]
    public async Task IntervalElapsed_FiresExactlyOnce_WithoutReset()
    {
        using var timer = new TrackingTimer(TimeSpan.FromMilliseconds(100));
        var fireCount = 0;
        timer.IntervalElapsed += (_, _) => fireCount++;

        timer.Start();
        await Task.Delay(400);

        fireCount.Should().Be(1);
    }

    [Fact]
    public async Task Reset_AllowsTimerToFireAgain()
    {
        using var timer = new TrackingTimer(TimeSpan.FromMilliseconds(100));
        var fireCount = 0;
        timer.IntervalElapsed += (_, _) => { fireCount++; timer.Reset(); };

        timer.Start();
        await Task.Delay(450);

        fireCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void IsRunning_FalseBeforeStart()
    {
        using var timer = new TrackingTimer(TimeSpan.FromMinutes(30));
        timer.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void IsRunning_TrueAfterStart()
    {
        using var timer = new TrackingTimer(TimeSpan.FromMinutes(30));
        timer.Start();
        timer.IsRunning.Should().BeTrue();
        timer.Stop();
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure**

```bash
dotnet test TaskNoteTracker.Tests
```

Expected: Compile error: `TrackingTimer does not exist`.

- [ ] **Step 3: Implement TrackingTimer**

Create `TaskNoteTracker/Services/TrackingTimer.cs`:

```csharp
namespace TaskNoteTracker.Services;

public sealed class TrackingTimer(TimeSpan interval) : IDisposable
{
    private System.Timers.Timer? _timer;

    public event EventHandler? IntervalElapsed;
    public bool IsRunning => _timer?.Enabled ?? false;

    public void Start()
    {
        _timer?.Dispose();
        _timer = new System.Timers.Timer(interval.TotalMilliseconds) { AutoReset = false };
        _timer.Elapsed += OnElapsed;
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    public void Reset()
    {
        _timer?.Stop();
        _timer?.Start();
    }

    private void OnElapsed(object? sender, System.Timers.ElapsedEventArgs e) =>
        IntervalElapsed?.Invoke(this, EventArgs.Empty);

    public void Dispose() => Stop();
}
```

- [ ] **Step 4: Run tests — expect all pass**

```bash
dotnet test TaskNoteTracker.Tests
```

Expected: `Passed! - 35 tests`.

- [ ] **Step 5: Commit**

```bash
git add TaskNoteTracker/Services/TrackingTimer.cs TaskNoteTracker.Tests/Services/TrackingTimerTests.cs
git commit -m "feat: add TrackingTimer with start/stop/reset and single-fire per interval"
```

---

## Task 8: StartupService

**Files:**
- Create: `TaskNoteTracker/Services/StartupService.cs`

No automated tests — this writes to the Windows registry. Verified manually after wiring.

- [ ] **Step 1: Implement StartupService**

Create `TaskNoteTracker/Services/StartupService.cs`:

```csharp
using Microsoft.Win32;

namespace TaskNoteTracker.Services;

public class StartupService
{
    private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "TaskNoteTracker";

    public void EnsureEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
        if (key?.GetValue(AppName) is null)
            key?.SetValue(AppName, Environment.ProcessPath ?? string.Empty);
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        return key?.GetValue(AppName) is not null;
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }
}
```

- [ ] **Step 2: Verify build still passes**

```bash
dotnet build TaskNoteTracker
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add TaskNoteTracker/Services/StartupService.cs
git commit -m "feat: add StartupService for Windows registry auto-start"
```

---

## Task 9: AddTaskTypeDialog

**Files:**
- Create: `TaskNoteTracker/Windows/AddTaskTypeDialog.xaml`
- Create: `TaskNoteTracker/Windows/AddTaskTypeDialog.xaml.cs`

- [ ] **Step 1: Create XAML**

Create `TaskNoteTracker/Windows/AddTaskTypeDialog.xaml`:

```xml
<Window x:Class="TaskNoteTracker.Windows.AddTaskTypeDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Add Task Type" Height="140" Width="360"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize" ShowInTaskbar="False">
    <StackPanel Margin="16">
        <TextBlock Text="Task type name:" Margin="0,0,0,8"/>
        <TextBox x:Name="NameBox" Margin="0,0,0,12"
                 KeyDown="NameBox_KeyDown"/>
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="Cancel" Width="70" Margin="0,0,8,0"
                    Click="Cancel_Click"/>
            <Button Content="Add" Width="70"
                    Click="Add_Click" IsDefault="True"/>
        </StackPanel>
    </StackPanel>
</Window>
```

- [ ] **Step 2: Create code-behind**

Create `TaskNoteTracker/Windows/AddTaskTypeDialog.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Input;

namespace TaskNoteTracker.Windows;

public partial class AddTaskTypeDialog : Window
{
    public string TypeName { get; private set; } = string.Empty;

    public AddTaskTypeDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => NameBox.Focus();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Please enter a name.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        TypeName = name;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) =>
        DialogResult = false;

    private void NameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) DialogResult = false;
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build TaskNoteTracker
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add TaskNoteTracker/Windows/AddTaskTypeDialog.xaml TaskNoteTracker/Windows/AddTaskTypeDialog.xaml.cs
git commit -m "feat: add AddTaskTypeDialog"
```

---

## Task 10: TrackingPopupWindow

**Files:**
- Create: `TaskNoteTracker/Windows/TrackingPopupWindow.xaml`
- Create: `TaskNoteTracker/Windows/TrackingPopupWindow.xaml.cs`

- [ ] **Step 1: Create XAML**

Create `TaskNoteTracker/Windows/TrackingPopupWindow.xaml`:

```xml
<Window x:Class="TaskNoteTracker.Windows.TrackingPopupWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="What are you doing?" Height="380" Width="480"
        WindowStartupLocation="CenterScreen"
        Topmost="True" ResizeMode="NoResize" ShowInTaskbar="False">
    <StackPanel Margin="20">
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
            <Button Content="Skip" Width="80" Margin="0,0,8,0"
                    Click="Skip_Click"/>
            <Button Content="Submit" Width="80"
                    Click="Submit_Click"/>
        </StackPanel>
    </StackPanel>
</Window>
```

- [ ] **Step 2: Create code-behind**

Create `TaskNoteTracker/Windows/TrackingPopupWindow.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Controls;
using TaskNoteTracker.Data;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Windows;

public partial class TrackingPopupWindow : Window
{
    private readonly EntryService _entryService;
    private readonly TaskTypeService _taskTypeService;
    private List<TaskType> _taskTypes = [];
    private bool _loading;

    public TrackingPopupWindow(AppDbContext db)
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
        await _entryService.CreateAsync(
            DateTime.UtcNow, ActivityBox.Text, NotesBox.Text, taskType.Id);
        Close();
    }

    private void Skip_Click(object sender, RoutedEventArgs e) => Close();
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build TaskNoteTracker
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add TaskNoteTracker/Windows/TrackingPopupWindow.xaml TaskNoteTracker/Windows/TrackingPopupWindow.xaml.cs
git commit -m "feat: add TrackingPopupWindow"
```

---

## Task 11: EditEntryDialog

**Files:**
- Create: `TaskNoteTracker/Windows/EditEntryDialog.xaml`
- Create: `TaskNoteTracker/Windows/EditEntryDialog.xaml.cs`

- [ ] **Step 1: Create XAML**

Create `TaskNoteTracker/Windows/EditEntryDialog.xaml`:

```xml
<Window x:Class="TaskNoteTracker.Windows.EditEntryDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Edit Entry" Height="360" Width="480"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize" ShowInTaskbar="False">
    <StackPanel Margin="20">
        <TextBlock Text="Task Type" FontWeight="SemiBold" Margin="0,0,0,4"/>
        <ComboBox x:Name="TaskTypeCombo" Margin="0,0,0,14"/>

        <TextBlock Text="Activity *" FontWeight="SemiBold" Margin="0,0,0,4"/>
        <TextBox x:Name="ActivityBox" Height="70"
                 TextWrapping="Wrap" AcceptsReturn="True"
                 VerticalScrollBarVisibility="Auto" Margin="0,0,0,14"/>

        <TextBlock Text="Notes" FontWeight="SemiBold" Margin="0,0,0,4"/>
        <TextBox x:Name="NotesBox" Height="70"
                 TextWrapping="Wrap" AcceptsReturn="True"
                 VerticalScrollBarVisibility="Auto" Margin="0,0,0,18"/>

        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="Cancel" Width="80" Margin="0,0,8,0"
                    Click="Cancel_Click"/>
            <Button Content="Save" Width="80"
                    Click="Save_Click" IsDefault="True"/>
        </StackPanel>
    </StackPanel>
</Window>
```

- [ ] **Step 2: Create code-behind**

Create `TaskNoteTracker/Windows/EditEntryDialog.xaml.cs`:

```csharp
using System.Windows;
using TaskNoteTracker.Data;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Windows;

public partial class EditEntryDialog : Window
{
    private readonly EntryService _entryService;
    private readonly TaskTypeService _taskTypeService;
    private readonly int _entryId;
    private List<TaskType> _taskTypes = [];

    public EditEntryDialog(AppDbContext db, int entryId)
    {
        _entryService = new EntryService(db);
        _taskTypeService = new TaskTypeService(db);
        _entryId = entryId;
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _taskTypes = [.. await _taskTypeService.GetAllAsync()];
        TaskTypeCombo.ItemsSource = _taskTypes;
        TaskTypeCombo.DisplayMemberPath = "Name";

        var entry = await _entryService.GetByIdAsync(_entryId);
        if (entry is null) { Close(); return; }

        ActivityBox.Text = entry.Activity;
        NotesBox.Text = entry.Notes ?? string.Empty;
        TaskTypeCombo.SelectedItem = _taskTypes.FirstOrDefault(t => t.Id == entry.TaskTypeId);
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (TaskTypeCombo.SelectedItem is not TaskType taskType)
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
        await _entryService.UpdateAsync(
            _entryId, ActivityBox.Text, NotesBox.Text, taskType.Id);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) =>
        DialogResult = false;
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build TaskNoteTracker
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add TaskNoteTracker/Windows/EditEntryDialog.xaml TaskNoteTracker/Windows/EditEntryDialog.xaml.cs
git commit -m "feat: add EditEntryDialog"
```

---

## Task 12: HistoryWindow

**Files:**
- Create: `TaskNoteTracker/Windows/HistoryWindow.xaml`
- Create: `TaskNoteTracker/Windows/HistoryWindow.xaml.cs`

- [ ] **Step 1: Create XAML**

Create `TaskNoteTracker/Windows/HistoryWindow.xaml`:

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

        <!-- Export buttons -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,8">
            <Button Content="Export CSV" Width="100" Margin="0,0,8,0"
                    Click="ExportCsv_Click"/>
            <Button Content="Export Excel" Width="110"
                    Click="ExportExcel_Click"/>
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

        <!-- Row count -->
        <TextBlock Grid.Row="3" x:Name="RowCountLabel"
                   Margin="0,6,0,0" Foreground="Gray" FontSize="11"/>
    </Grid>
</Window>
```

- [ ] **Step 2: Create code-behind**

Create `TaskNoteTracker/Windows/HistoryWindow.xaml.cs`:

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

    public HistoryWindow(AppDbContext db)
    {
        _db = db;
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

- [ ] **Step 3: Verify build**

```bash
dotnet build TaskNoteTracker
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add TaskNoteTracker/Windows/HistoryWindow.xaml TaskNoteTracker/Windows/HistoryWindow.xaml.cs
git commit -m "feat: add HistoryWindow with filtering, edit, delete, and export"
```

---

## Task 13: TaskTypeManagerWindow

**Files:**
- Create: `TaskNoteTracker/Windows/TaskTypeManagerWindow.xaml`
- Create: `TaskNoteTracker/Windows/TaskTypeManagerWindow.xaml.cs`

- [ ] **Step 1: Create XAML**

Create `TaskNoteTracker/Windows/TaskTypeManagerWindow.xaml`:

```xml
<Window x:Class="TaskNoteTracker.Windows.TaskTypeManagerWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Manage Task Types" Height="400" Width="360"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize" ShowInTaskbar="False">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <ListBox Grid.Row="0" x:Name="TypesList" Margin="0,0,0,12"
                 DisplayMemberPath="Name"/>

        <StackPanel Grid.Row="1">
            <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                <TextBox x:Name="NewTypeBox" Width="220" Margin="0,0,8,0"/>
                <Button Content="Add" Width="80" Click="Add_Click"/>
            </StackPanel>
            <Button Content="Delete Selected" Width="120"
                    HorizontalAlignment="Left" Click="Delete_Click"/>
        </StackPanel>
    </Grid>
</Window>
```

- [ ] **Step 2: Create code-behind**

Create `TaskNoteTracker/Windows/TaskTypeManagerWindow.xaml.cs`:

```csharp
using System.Windows;
using TaskNoteTracker.Data;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Windows;

public partial class TaskTypeManagerWindow : Window
{
    private readonly TaskTypeService _service;

    public TaskTypeManagerWindow(AppDbContext db)
    {
        _service = new TaskTypeService(db);
        InitializeComponent();
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        TypesList.ItemsSource = await _service.GetAllAsync();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var name = NewTypeBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Enter a name.", "Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        await _service.AddAsync(name);
        NewTypeBox.Text = string.Empty;
        await RefreshAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (TypesList.SelectedItem is not TaskType selected) return;

        if (!await _service.CanDeleteAsync(selected.Id))
        {
            MessageBox.Show(
                $"\"{selected.Name}\" has existing entries and cannot be deleted.",
                "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Delete \"{selected.Name}\"?", "Confirm Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        await _service.DeleteAsync(selected.Id);
        await RefreshAsync();
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build TaskNoteTracker
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add TaskNoteTracker/Windows/TaskTypeManagerWindow.xaml TaskNoteTracker/Windows/TaskTypeManagerWindow.xaml.cs
git commit -m "feat: add TaskTypeManagerWindow"
```

---

## Task 14: TrayManager

**Files:**
- Create: `TaskNoteTracker/Tray/TrayManager.cs`

- [ ] **Step 1: Implement TrayManager**

Create `TaskNoteTracker/Tray/TrayManager.cs`:

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
    private readonly TrackingTimer _timer;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _toggleItem;
    private TrackingPopupWindow? _popup;
    private bool _isTracking;

    public event EventHandler? ExitRequested;

    public TrayManager(AppDbContext db)
    {
        _db = db;
        _timer = new TrackingTimer(TimeSpan.FromMinutes(30));
        _timer.IntervalElapsed += OnTimerElapsed;

        _toggleItem = new ToolStripMenuItem("Start Tracking", null, OnToggleTracking);
        var menu = new ContextMenuStrip();
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Open History", null, OnOpenHistory);
        menu.Items.Add("Manage Task Types", null, OnManageTaskTypes);
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
            new HistoryWindow(_db).Show());

    private void OnManageTaskTypes(object? sender, EventArgs e) =>
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
            new TaskTypeManagerWindow(_db).ShowDialog());

    private void OnExit(object? sender, EventArgs e) =>
        ExitRequested?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        _timer.Dispose();
        _notifyIcon.Dispose();
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build TaskNoteTracker
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add TaskNoteTracker/Tray/TrayManager.cs
git commit -m "feat: add TrayManager with NotifyIcon and popup orchestration"
```

---

## Task 15: App.xaml.cs — Wire Everything Together

**Files:**
- Modify: `TaskNoteTracker/App.xaml`
- Modify: `TaskNoteTracker/App.xaml.cs`

- [ ] **Step 1: Replace `App.xaml` — remove StartupUri, keep application resources empty**

```xml
<Application x:Class="TaskNoteTracker.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources/>
</Application>
```

- [ ] **Step 2: Replace `App.xaml.cs`**

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

        _trayManager = new TrayManager(_db);
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

- [ ] **Step 3: Verify full build and tests pass**

```bash
cd "f:/Trojar Test Dev/Task Note Tracker"
dotnet build
dotnet test TaskNoteTracker.Tests
```

Expected: `Build succeeded.` and `Passed! - 35 tests`.

- [ ] **Step 4: Run the app manually and verify**

```bash
dotnet run --project TaskNoteTracker
```

Manual checklist:
- [ ] Tray icon appears in notification area
- [ ] Right-click shows: Start Tracking / Open History / Manage Task Types / Exit
- [ ] Click "Start Tracking" — tooltip changes to "Task Note Tracker — Tracking"
- [ ] "Manage Task Types" opens a dialog — add a type, verify it appears
- [ ] Wait for timer (for quick manual test: temporarily change `TimeSpan.FromMinutes(30)` to `TimeSpan.FromSeconds(10)` in `App.xaml.cs`) — popup appears on top
- [ ] Fill in form and Submit — verify no errors, popup closes
- [ ] "Open History" — verify entry appears with BDT and CDT timestamps
- [ ] Edit an entry — verify changes save
- [ ] Delete an entry — verify confirmation and removal
- [ ] Export CSV and Excel — open files and verify content
- [ ] Registry key written: open regedit → `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` → verify `TaskNoteTracker` entry
- [ ] Exit — tray icon disappears

> **Reminder:** Revert the timer interval back to `TimeSpan.FromMinutes(30)` before the final commit.

- [ ] **Step 5: Commit**

```bash
git add TaskNoteTracker/App.xaml TaskNoteTracker/App.xaml.cs
git commit -m "feat: wire up App.xaml.cs with tray, DB init, and startup registry"
```

---

## All Tasks Complete

Run the full test suite one final time:

```bash
dotnet test TaskNoteTracker.Tests --verbosity normal
```

Expected: `Passed! - 35 tests`, 0 failed.
