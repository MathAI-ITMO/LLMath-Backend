namespace MathLLMBackend.Presentation.Dtos.Geolin;

public class CheckAnswerRequest
{
    public string Hash { get; set; } = "";
    public string AnswerAttempt { get; set; } = "";
    public int? Seed { get; set; }
    public string? ProblemParams { get; set; }
}
