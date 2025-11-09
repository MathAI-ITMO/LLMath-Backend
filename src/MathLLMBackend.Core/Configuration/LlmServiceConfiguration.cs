namespace MathLLMBackend.Core.Configuration;

public class LlmServiceConfiguration
{
    public required ModelConfiguration ChatModel { get; init; }
    public required ModelConfiguration SolverModel { get; init; }
    public ModelConfiguration? OllamaConfig { get; init; }
}