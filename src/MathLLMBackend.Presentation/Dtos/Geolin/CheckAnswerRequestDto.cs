namespace MathLLMBackend.Presentation.Dtos.Geolin;

public record CheckAnswerRequestDto(
    string Hash,
    string AnswerAttempt,
    int? Seed = null,
    string? ProblemParams = null
);
