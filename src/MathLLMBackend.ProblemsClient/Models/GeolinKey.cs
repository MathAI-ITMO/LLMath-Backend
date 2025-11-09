using System.Text.Json.Serialization;

namespace MathLLMBackend.ProblemsClient.Models;

public class GeolinKey
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    [JsonPropertyName("seed")]
    public int Seed { get; set; }
}

