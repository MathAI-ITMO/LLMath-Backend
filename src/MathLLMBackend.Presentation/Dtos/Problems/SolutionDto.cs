using System.Collections.Generic;

namespace MathLLMBackend.Presentation.Dtos.Problems;

public class SolutionDto
{
    public List<ProblemStepDto> Steps { get; set; } = new();
}

