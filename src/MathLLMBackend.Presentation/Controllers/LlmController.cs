using MathLLMBackend.Core.Services.LlmService;
using MathLLMBackend.Presentation.Dtos.Llm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathLLMBackend.Presentation.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class LlmController : ControllerBase
{
    private readonly ILlmService _llmService;
    private readonly ILogger<LlmController> _logger;

    public LlmController(ILlmService llmService, ILogger<LlmController> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    /// <summary>
    /// Решает математическую задачу с помощью LLM
    /// </summary>
    [HttpPost("solve-problem")]
    public async Task<IActionResult> SolveProblem([FromBody] SolveProblemRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ProblemDescription))
        {
            return BadRequest("Problem description cannot be empty");
        }

        try
        {
            _logger.LogInformation("Solving problem using LLM: {Problem}", request.ProblemDescription);
            var solution = await _llmService.SolveProblem(request.ProblemDescription, ct);
            return Ok(new SolveProblemResponseDto(solution));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error solving problem with LLM");
            return StatusCode(500, "Error solving problem: " + ex.Message);
        }
    }

    /// <summary>
    /// Извлекает финальный ответ из готового решения задачи
    /// </summary>
    [HttpPost("extract-answer")]
    public async Task<IActionResult> ExtractAnswer([FromBody] ExtractAnswerRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("ExtractAnswer called with ProblemStatement length: {ProblemStatementLength}, Solution length: {SolutionLength}",
            request.ProblemStatement?.Length ?? 0, request.Solution?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(request.ProblemStatement))
        {
            _logger.LogWarning("ExtractAnswer: Problem statement is empty");
            return BadRequest("Problem statement cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(request.Solution))
        {
            _logger.LogWarning("ExtractAnswer: Solution is empty");
            return BadRequest("Solution cannot be empty");
        }

        try
        {
            var extractedAnswer = await _llmService.ExtractAnswer(request.ProblemStatement, request.Solution, ct);
            return Ok(new ExtractAnswerResponseDto(extractedAnswer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting answer from solution");
            return StatusCode(500, "Error extracting answer: " + ex.Message);
        }
    }
}