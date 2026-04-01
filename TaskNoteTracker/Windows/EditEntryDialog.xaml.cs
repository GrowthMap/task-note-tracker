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
