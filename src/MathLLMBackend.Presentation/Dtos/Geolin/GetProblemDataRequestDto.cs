namespace MathLLMBackend.Presentation.Dtos.Geolin;

public record GetProblemDataRequestDto(
    string Prefix,
    int? Seed = null
);
