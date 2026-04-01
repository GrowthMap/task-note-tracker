using ClosedXML.Excel;
using FluentAssertions;
using System.IO;
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
