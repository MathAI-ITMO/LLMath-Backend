namespace MathLLMBackend.Presentation.Dtos.Llm;

public record ExtractAnswerRequestDto(
    string ProblemStatement,
    string Solution
);
