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
