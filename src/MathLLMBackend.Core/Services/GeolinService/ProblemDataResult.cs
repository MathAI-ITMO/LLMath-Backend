namespace MathLLMBackend.Core.Services.GeolinService;

public record ProblemDataResult(
    string Name,
    string Hash,
    string Condition,
    int Seed,
    string ProblemParams
);
