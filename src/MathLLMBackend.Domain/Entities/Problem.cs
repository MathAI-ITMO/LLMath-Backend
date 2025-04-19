using System;

namespace MathLLMBackend.Domain.Entities;

public class Problem 
{
    public Problem(Guid id, string name, string hash, ProblemVariation[] variations)
    {
        Id = id;
        Name = name;
        Variations = variations;
        Hash = hash;
    }

    public Problem() { }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Hash { get; set; }
    public ProblemVariation[] Variations { get; set; }
}
