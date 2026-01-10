using MathLLMBackend.Core.Services.ProblemsService;
using MathLLMBackend.Domain.Constants;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using MathLLMBackend.Presentation.Dtos.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathLLMBackend.Presentation.Controllers;

[Authorize(Roles = Role.Admin)]
[Route("api/[controller]")]
[ApiController]
public class ProblemsController(IProblemsService problemsService) : ControllerBase
{
    private readonly IProblemsService _problemsService = problemsService;

    [HttpGet]
    public async Task<IActionResult> GetProblems(CancellationToken ct)
    {
        var problems = await _problemsService.GetProblems(ct);
        return Ok(problems.Select(MapToDto));
    }

    [HttpGet("type/{type}")]
    public async Task<IActionResult> GetProblemsByType(TaskType type, CancellationToken ct)
    {
        var problems = await _problemsService.GetProblemsByType(type, ct);
        return Ok(problems.Select(MapToDto));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProblem([FromBody] CreateProblemRequestDto dto, CancellationToken ct)
    {
        var problem = new Problem
        {
            Title = dto.Title,
            Statement = dto.Statement,
            LlmSolution = dto.LlmSolution,
            TheoryLink = dto.TheoryLink
        };

        if (!string.IsNullOrEmpty(dto.GeolinHash))
        {
            problem.GeolinProblemData = new GeolinProblemData(problem.Id, dto.GeolinHash, dto.GeolinSeed ?? 0);
        }

        var createdProblem = await _problemsService.CreateProblem(problem, ct);

        foreach (var type in dto.Types)
        {
            await _problemsService.SetType(createdProblem.Id, type, ct);
        }

        var result = await _problemsService.GetProblem(createdProblem.Id, ct);
        return Ok(MapToDto(result!));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProblem(Guid id, [FromBody] UpdateProblemRequestDto dto, CancellationToken ct)
    {
        var problem = await _problemsService.GetProblem(id, ct);
        if (problem == null)
        {
            return NotFound();
        }

        problem.Title = dto.Title;
        problem.Statement = dto.Statement;
        problem.LlmSolution = dto.LlmSolution;
        problem.TheoryLink = dto.TheoryLink;

        if (!string.IsNullOrEmpty(dto.GeolinHash))
        {
            if (problem.GeolinProblemData == null)
            {
                problem.GeolinProblemData = new GeolinProblemData(problem.Id, dto.GeolinHash, dto.GeolinSeed ?? 0);
            }
            else
            {
                problem.GeolinProblemData.Hash = dto.GeolinHash;
                problem.GeolinProblemData.Seed = dto.GeolinSeed ?? 0;
            }
        }
        else
        {
            problem.GeolinProblemData = null!;
        }

        // Manage types.
        await _problemsService.ClearTypes(problem.Id, ct);
        foreach (var type in dto.Types)
        {
            await _problemsService.SetType(problem.Id, type, ct);
        }

        await _problemsService.UpdateProblem(problem, ct);

        var result = await _problemsService.GetProblem(id, ct);
        return Ok(MapToDto(result!));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProblem(Guid id, CancellationToken ct)
    {
        await _problemsService.DeleteProblem(id, ct);
        return NoContent();
    }

    private static AdminProblemDto MapToDto(Problem problem)
    {
        return new AdminProblemDto(
            problem.Id,
            problem.Title,
            problem.Statement,
            problem.LlmSolution,
            problem.TheoryLink,
            problem.GeolinProblemData?.Hash,
            problem.GeolinProblemData?.Seed,
            problem.Types.Select(t => t.TaskType)
        );
    }
}
