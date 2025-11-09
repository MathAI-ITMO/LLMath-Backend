namespace MathLLMBackend.Presentation.Dtos.Problems;

public class CreateProblemRequestDto
{
    public string Statement { get; set; } = string.Empty;
    public GeolinAnswerKeyDto GeolinAnsKey { get; set; } = new();
    public string Result { get; set; } = string.Empty;
    public SolutionDto Solution { get; set; } = new();
}

