namespace MathLLMBackend.Presentation.Dtos.Geolin;

public record CheckAnswerResponseDto(
    bool IsCorrect,
    string Message,
    string Hash,
    string AnswerAttempt,
    int? Seed = null
);

public record CheckAnswerErrorResponseDto(
    string Error,
    string Hash,
    string AnswerAttempt,
    int? Seed = null
);
