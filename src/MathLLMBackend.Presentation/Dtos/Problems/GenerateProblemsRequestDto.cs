namespace MathLLMBackend.Presentation.Dtos.Problems;

public record GenerateProblemsRequestDto(string Name, string ProblemHash, int Count);

