using MathLLMBackend.Core.Constants;
using MathLLMBackend.Core.Services.GeolinService;
using MathLLMBackend.Domain.Constants;
using MathLLMBackend.Presentation.Dtos.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MathLLMBackend.Presentation.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TasksController : ControllerBase
{
    private readonly IGeolinService _geolinService;

    public TasksController(IGeolinService geolinService)
    {
        _geolinService = geolinService;
    }

    [HttpGet("problems")]
    [Authorize(Roles = Role.Admin)]
    public async Task<IActionResult> GetProblems(
        [FromQuery] int page = GeolinConstants.Pagination.DefaultPage, 
        [FromQuery] int size = GeolinConstants.Pagination.DefaultPageSize, 
        [FromQuery] string? prefixName = "", 
        CancellationToken ct = default)
    {
        var response = await _geolinService.GetProblems(page, size, prefixName, ct);
        
        var problems = response.Problems.Select(p => new ProblemDto(
            Hash: p.Hash,
            Name: p.Name,
            Description: p.Description,
            Condition: p.ConditionRu
        )).ToList();

        var result = new ProblemsPageDto(
            Problems: problems,
            Number: response.Number
        );

        return Ok(result);
    }    
} 