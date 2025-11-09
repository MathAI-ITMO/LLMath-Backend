using System.Text.Json.Serialization;

namespace MathLLMBackend.ProblemsClient.Models;

public class Problem
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("statement")]
    public string Statement { get; set; } = string.Empty;

    [JsonPropertyName("llm_solution")]
    public object? LlmSolution { get; set; }

    [JsonPropertyName("geolin_ans_key")]
    public GeolinKey GeolinAnsKey { get; set; } = new();

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("solution")]
    public Solution Solution { get; set; } = new();
}

