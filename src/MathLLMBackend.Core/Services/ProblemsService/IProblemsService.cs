using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;

namespace MathLLMBackend.Core.Services.ProblemsService;

public interface IProblemsService
{
    Task<IEnumerable<Problem>> GetProblems(CancellationToken ct);
    Task<IEnumerable<Problem>> GetProblemsByType(TaskType taskType, CancellationToken ct);
    Task<Problem?> GetProblem(Guid problemId, CancellationToken ct);
    Task<Problem> UpdateProblem(Problem problem, CancellationToken ct);
    Task<Problem> CreateProblem(Problem problem, CancellationToken ct);
    Task DeleteProblem(Guid problemId, CancellationToken ct);
    Task ClearTypes(Guid problemId, CancellationToken ct);
    Task<Problem> SetType(Guid problemId, TaskType taskType, CancellationToken ct);
} 
