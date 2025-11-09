namespace MathLLMBackend.Presentation.Dtos.Geolin;

public record CheckAnswerErrorResponseDto(
    string Error,
    string Hash,
    string AnswerAttempt,
    int? Seed = null
);

