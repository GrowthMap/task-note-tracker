using System.IO;
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
