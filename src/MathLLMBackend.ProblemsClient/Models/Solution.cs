using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MathLLMBackend.ProblemsClient.Models;

public class Solution
{
    [JsonPropertyName("steps")]
    public List<ProblemStep> Steps { get; set; } = new();
}

