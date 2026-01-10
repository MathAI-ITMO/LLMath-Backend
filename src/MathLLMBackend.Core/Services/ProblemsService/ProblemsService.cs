using MathLLMBackend.DataAccess.Contexts;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MathLLMBackend.Core.Services.ProblemsService;

public class ProblemsService(AppDbContext dbContext) : IProblemsService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IEnumerable<Problem>> GetProblems(CancellationToken ct)
    {
        return await _dbContext.Problems
            .Include(p => p.Types)
            .Include(p => p.GeolinProblemData)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Problem>> GetProblemsByType(TaskType taskType, CancellationToken ct)
    {
        return await _dbContext.Problems
            .Include(p => p.Types)
            .Include(p => p.GeolinProblemData)
            .Where(p => p.Types.Any(t => t.TaskType == taskType))
            .ToListAsync(ct);
    }

    public async Task<Problem?> GetProblem(Guid problemId, CancellationToken ct)
    {
        return await _dbContext.Problems
            .Include(p => p.Types)
            .Include(p => p.GeolinProblemData)
            .FirstOrDefaultAsync(p => p.Id == problemId, ct);
    }

    public async Task<Problem> UpdateProblem(Problem problem, CancellationToken ct)
    {
        _dbContext.Problems.Update(problem);
        await _dbContext.SaveChangesAsync(ct);
        return problem;
    }

    public async Task<Problem> CreateProblem(Problem problem, CancellationToken ct)
    {
        _dbContext.Problems.Add(problem);
        await _dbContext.SaveChangesAsync(ct);
        return problem;
    }

    public async Task DeleteProblem(Guid problemId, CancellationToken ct)
    {
        var problem = await _dbContext.Problems.FindAsync([problemId], ct);
        if (problem != null)
        {
            _dbContext.Problems.Remove(problem);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task ClearTypes(Guid problemId, CancellationToken ct)
    {
        var types = await _dbContext.Set<ProblemTaskType>()
            .Where(t => t.ProblemId == problemId)
            .ToListAsync(ct);
        
        _dbContext.Set<ProblemTaskType>().RemoveRange(types);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<Problem> SetType(Guid problemId, TaskType taskType, CancellationToken ct)
    {
        var problem = await _dbContext.Problems
            .Include(p => p.Types)
            .FirstOrDefaultAsync(p => p.Id == problemId, ct);
        
        if (problem == null)
        {
            throw new KeyNotFoundException($"Problem with id {problemId} not found");
        }

        var existingType = problem.Types.FirstOrDefault(t => t.TaskType == taskType);
        if (existingType == null)
        {
            var problemTaskType = new ProblemTaskType(problem, taskType);
            _dbContext.Set<ProblemTaskType>().Add(problemTaskType);
            await _dbContext.SaveChangesAsync(ct);
        }

        return problem;
    }
}