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
