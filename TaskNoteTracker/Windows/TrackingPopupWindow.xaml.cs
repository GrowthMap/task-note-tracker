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
