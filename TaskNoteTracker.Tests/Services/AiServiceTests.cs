using System.IO;
using FluentAssertions;
using TaskNoteTracker.Models;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Tests.Services;

public class AiServiceTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly SettingsService _settingsService;
    private readonly AiService _service;

    public AiServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
        _settingsService = new SettingsService(Path.Combine(_tempDir, "settings.json"));
        _service = new AiService(_settingsService);
    }

    [Fact]
    public async Task GenerateSummaryAsync_ReturnsApiKeyError_WhenKeyIsEmpty()
    {
        var result = await _service.GenerateSummaryAsync([]);

        result.Should().Be("Please configure your OpenAI API key in Settings.");
    }

    [Fact]
    public async Task GenerateSummaryAsync_ReturnsNoEntriesMessage_WhenEntriesEmptyAndKeySet()
    {
        _settingsService.Save(new AppSettings { ApiKey = "sk-test", Model = "gpt-4o-mini" });

        var result = await _service.GenerateSummaryAsync([]);

        result.Should().Be("No entries to analyze. Try adjusting the filters or date range.");
    }

    [Fact]
    public async Task AnalyzePatternsAsync_ReturnsApiKeyError_WhenKeyIsEmpty()
    {
        var result = await _service.AnalyzePatternsAsync([]);

        result.Should().Be("Please configure your OpenAI API key in Settings.");
    }

    [Fact]
    public async Task AnalyzePatternsAsync_ReturnsNoEntriesMessage_WhenEntriesEmptyAndKeySet()
    {
        _settingsService.Save(new AppSettings { ApiKey = "sk-test", Model = "gpt-4o-mini" });

        var result = await _service.AnalyzePatternsAsync([]);

        result.Should().Be("No entries to analyze. Try adjusting the filters or date range.");
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
