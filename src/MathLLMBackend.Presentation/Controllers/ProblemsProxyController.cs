using System.Collections.Generic;
using System.Linq;
using MathLLMBackend.Core.Services.ProblemsService;
using MathLLMBackend.ProblemsClient.Models;
using MathLLMBackend.Presentation.Dtos.Problems;
using Microsoft.AspNetCore.Mvc;

namespace MathLLMBackend.Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/problems-proxy")]
    public class ProblemsProxyController : ControllerBase
    {
        private readonly IProblemsService _problemsService;

        public ProblemsProxyController(IProblemsService problemsService)
        {
            _problemsService = problemsService ?? throw new ArgumentNullException(nameof(problemsService));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct = default)
        {
            var problems = await _problemsService.GetSavedProblems(ct);
            return Ok(problems);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProblemRequestDto request, CancellationToken ct = default)
        {
            if (request is null)
            {
                return BadRequest(new { error = "Request body is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Statement))
            {
                return BadRequest(new { error = "Statement cannot be empty." });
            }

            if (request.GeolinAnsKey is null || string.IsNullOrWhiteSpace(request.GeolinAnsKey.Hash))
            {
                return BadRequest(new { error = "GeolinAnsKey.Hash cannot be empty." });
            }

            var problemRequest = new ProblemRequest
            {
                Statement = request.Statement,
                GeolinAnsKey = new GeolinKey
                {
                    Hash = request.GeolinAnsKey.Hash,
                    Seed = request.GeolinAnsKey.Seed
                },
                Result = request.Result,
                Solution = new Solution
                {
                    Steps = request.Solution?.Steps?.Select(step => new ProblemStep
                    {
                        Order = step.Order,
                        Prerequisites = new AdditionalProperty(),
                        Transition = new AdditionalProperty(),
                        Outcomes = new AdditionalProperty()
                    }).ToList() ?? new List<ProblemStep>()
                }
            };

            var createdProblem = await _problemsService.CreateProblemAsync(problemRequest, ct);
            return CreatedAtAction(nameof(GetById), new { id = createdProblem.Id }, createdProblem);
        }

        [HttpGet("types")]
        public async Task<IActionResult> GetTypes(CancellationToken ct = default)
        {
            var types = await _problemsService.GetAllTypes(ct);
            return Ok(types);
        }

        [HttpGet("by-name")]
        public async Task<IActionResult> GetByName([FromQuery] string name, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { error = "Name cannot be empty." });
            }

            var problems = await _problemsService.GetSavedProblemsByNames(name, ct);
            return Ok(problems);
        }

        [HttpGet("by-type")]
        public async Task<IActionResult> GetByType([FromQuery(Name = "type")] string typeName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return BadRequest(new { error = "Type cannot be empty." });
            }

            var problems = await _problemsService.GetSavedProblemsByTypes(typeName, ct);
            return Ok(problems);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { error = "Id cannot be empty." });
            }

            var problem = await _problemsService.GetProblemFromDbAsync(id, ct);
            if (problem is null)
            {
                return NotFound();
            }
            return Ok(problem);
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateProblemsRequestDto request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Name cannot be empty." });
            }
            if (string.IsNullOrWhiteSpace(request.ProblemHash))
            {
                return BadRequest(new { error = "ProblemHash cannot be empty." });
            }
            if (request.Count <= 0)
            {
                return BadRequest(new { error = "Count must be positive." });
            }

            var created = await _problemsService.SaveProblems(request.Name, request.ProblemHash, request.Count, ct);
            return Ok(created);
        }
    }
}


