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
