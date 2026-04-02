using System.Text.Json.Serialization;

namespace TaskNoteTracker.Models;

public record AppSettings
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; init; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; init; } = "gpt-4o-mini";
}
