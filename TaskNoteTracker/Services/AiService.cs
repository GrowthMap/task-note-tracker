using System.Text;
using OpenAI.Chat;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Models;

namespace TaskNoteTracker.Services;

public class AiService(SettingsService settingsService)
{
    private const string SystemPrompt =
        "You are a productivity analyst reviewing time-tracking data. " +
        "Be concise, specific, and actionable. " +
        "Focus on patterns and insights that help the user understand how they spend their time.";

    private const string ApiKeyError =
        "Please configure your OpenAI API key in Settings.";

    private const string NoEntriesError =
        "No entries to analyze. Try adjusting the filters or date range.";

    public async Task<string> GenerateSummaryAsync(IEnumerable<TaskEntry> entries)
    {
        var settings = settingsService.Load();
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return ApiKeyError;

        var list = entries.ToList();
        if (list.Count == 0)
            return NoEntriesError;

        var client = new ChatClient(model: settings.Model, apiKey: settings.ApiKey);
        var response = await client.CompleteChatAsync(
        [
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(BuildSummaryPrompt(list))
        ]);
        return response.Value.Content[0].Text;
    }

    public async Task<string> AnalyzePatternsAsync(IEnumerable<TaskEntry> entries)
    {
        var settings = settingsService.Load();
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return ApiKeyError;

        var list = entries.ToList();
        if (list.Count == 0)
            return NoEntriesError;

        var client = new ChatClient(model: settings.Model, apiKey: settings.ApiKey);
        var response = await client.CompleteChatAsync(
        [
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(BuildPatternsPrompt(list))
        ]);
        return response.Value.Content[0].Text;
    }

    private static string BuildSummaryPrompt(List<TaskEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "Summarize the following work entries, grouped by task type. " +
            "Highlight key activities and time distribution:");
        sb.AppendLine();
        foreach (var e in entries)
        {
            sb.AppendLine($"[{e.TimestampUtc:yyyy-MM-dd HH:mm} UTC] [{e.TaskType.Name}] {e.Activity}");
            if (!string.IsNullOrWhiteSpace(e.Notes))
                sb.AppendLine($"  Notes: {e.Notes}");
        }
        return sb.ToString();
    }

    private static string BuildPatternsPrompt(List<TaskEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze the following work entries and identify patterns. Include:");
        sb.AppendLine("- Which task types dominate (by entry count)");
        sb.AppendLine("- Time-of-day trends");
        sb.AppendLine("- Recurring activities");
        sb.AppendLine("- Actionable suggestions (e.g. scheduling recommendations)");
        sb.AppendLine();
        foreach (var e in entries)
        {
            sb.AppendLine($"[{e.TimestampUtc:yyyy-MM-dd HH:mm} UTC] [{e.TaskType.Name}] {e.Activity}");
            if (!string.IsNullOrWhiteSpace(e.Notes))
                sb.AppendLine($"  Notes: {e.Notes}");
        }
        return sb.ToString();
    }
}
