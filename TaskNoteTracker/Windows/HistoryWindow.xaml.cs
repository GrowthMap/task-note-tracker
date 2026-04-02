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
    private readonly AiService _aiService;
    private readonly EntryService _entryService;
    private readonly TaskTypeService _taskTypeService;
    private readonly ExportService _exportService = new();

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
