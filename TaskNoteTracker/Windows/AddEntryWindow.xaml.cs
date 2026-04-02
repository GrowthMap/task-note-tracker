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
            EntryDatePicker.SelectedDate = now.Date;
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
            if (EntryDatePicker.SelectedDate is not { } selectedDate)
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
            var bdt = DateTime.SpecifyKind(
                new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, hour, minute, 0),
                DateTimeKind.Unspecified);
            timestampUtc = TimezoneService.ToUtc(bdt);
        }
        else
        {
            timestampUtc = DateTime.UtcNow;
        }

        await _entryService.CreateAsync(
            timestampUtc,
            ActivityBox.Text,
            string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text,
            taskType.Id);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
