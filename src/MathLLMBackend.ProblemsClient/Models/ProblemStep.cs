using System.Text.Json.Serialization;

namespace MathLLMBackend.ProblemsClient.Models;

public class ProblemStep
{
    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("prerequisites")]
    public AdditionalProperty Prerequisites { get; set; } = new();

    [JsonPropertyName("transition")]
    public AdditionalProperty Transition { get; set; } = new();

    [JsonPropertyName("outcomes")]
    public AdditionalProperty Outcomes { get; set; } = new();
}

