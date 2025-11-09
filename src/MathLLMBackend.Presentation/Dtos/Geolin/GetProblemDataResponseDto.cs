namespace MathLLMBackend.Presentation.Dtos.Geolin;

public record GetProblemDataResponseDto(
    string Name,
    string Hash,
    string Condition,
    int Seed,
    string ProblemParams
);
