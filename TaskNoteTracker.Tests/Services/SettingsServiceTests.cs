using System.IO;
using System.Text.Json;
using FluentAssertions;
using TaskNoteTracker.Models;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _filePath;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "settings.json");
        _service = new SettingsService(_filePath);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var settings = _service.Load();

        settings.ApiKey.Should().BeEmpty();
        settings.Model.Should().Be("gpt-4o-mini");
    }

    [Fact]
    public void Load_ReturnsSettings_WhenFileExists()
    {
        File.WriteAllText(_filePath, "{\"apiKey\":\"sk-abc\",\"model\":\"gpt-4o\"}");

        var settings = _service.Load();

        settings.ApiKey.Should().Be("sk-abc");
        settings.Model.Should().Be("gpt-4o");
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var original = new AppSettings { ApiKey = "sk-xyz", Model = "gpt-4o" };
        _service.Save(original);

        var loaded = _service.Load();

        loaded.ApiKey.Should().Be("sk-xyz");
        loaded.Model.Should().Be("gpt-4o");
    }

    [Fact]
    public void Save_WritesCamelCaseJson()
    {
        _service.Save(new AppSettings { ApiKey = "sk-test", Model = "gpt-4o-mini" });

        var json = File.ReadAllText(_filePath);

        json.Should().Contain("\"apiKey\"");
        json.Should().Contain("\"model\"");
        json.Should().Contain("sk-test");
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
