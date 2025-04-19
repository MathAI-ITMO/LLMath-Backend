using System;

namespace MathLLMBackend.Domain.Entities;

public class ProblemVariation
{
    public ProblemVariation(Guid id, Problem problem, long seed)
    {
        Id = id;
        Problem = problem;
        Seed = seed;
    }

    public ProblemVariation() { }

    public Guid Id { get; set; }
    public Problem Problem { get; set; }
    public long Seed { get; set; }
}
