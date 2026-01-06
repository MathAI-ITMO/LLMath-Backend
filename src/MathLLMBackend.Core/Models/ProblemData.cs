namespace MathLLMBackend.Core.Models;

public class ProblemData
{
    public string Name { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public int? Seed { get; set; }
    public string? ProblemParams { get; set; }
}
