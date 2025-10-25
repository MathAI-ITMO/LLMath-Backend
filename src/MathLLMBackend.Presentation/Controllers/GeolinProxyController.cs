using MathLLMBackend.Core.Services.GeolinService;
using MathLLMBackend.Presentation.Dtos.Geolin;
using Microsoft.AspNetCore.Mvc;

namespace MathLLMBackend.Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class GeolinProxyController : ControllerBase
    {
        private readonly IGeolinService _geolinService;
        private readonly ILogger<GeolinProxyController> _logger;

        public GeolinProxyController(IGeolinService geolinService, ILogger<GeolinProxyController> logger)
        {
            _geolinService = geolinService ?? throw new ArgumentNullException(nameof(geolinService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("problem-data")]
        public async Task<IActionResult> GetProblemData(
            [FromQuery] string prefix,
            [FromQuery] int? seed = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return BadRequest(new GetProblemDataErrorResponseDto("Prefix cannot be empty."));
            }

            try
            {
                var result = await _geolinService.GetProblemDataByPrefix(prefix, seed, ct);

                var response = new GetProblemDataResponseDto(
                    Name: result.Name,
                    Hash: result.Hash,
                    Condition: result.Condition,
                    Seed: result.Seed,
                    ProblemParams: result.ProblemParams
                );

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business logic error for prefix '{Prefix}'", prefix);
                return NotFound(new GetProblemDataErrorResponseDto(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting problem data for prefix '{Prefix}'", prefix);
                return StatusCode(500, new GetProblemDataErrorResponseDto($"An error occurred: {ex.Message}"));
            }
        }

        [HttpPost("check-answer")]
        public async Task<IActionResult> CheckAnswer(
            [FromBody] CheckAnswerRequestDto request,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Hash))
            {
                return BadRequest(new CheckAnswerErrorResponseDto("Hash is required and cannot be empty.", request.Hash, request.AnswerAttempt, request.Seed));
            }

            if (string.IsNullOrWhiteSpace(request.AnswerAttempt))
            {
                return BadRequest(new CheckAnswerErrorResponseDto("Answer attempt is required and cannot be empty.", request.Hash, request.AnswerAttempt, request.Seed));
            }

            try
            {
                _logger.LogInformation("Checking answer for hash: {Hash}", request.Hash);

                var checkResult = await _geolinService.CheckProblemAnswer(
                    request.Hash,
                    request.AnswerAttempt,
                    request.Seed,
                    request.ProblemParams,
                    ct);

                var isCorrect = checkResult.Verdict >= 1.0;
                var message = isCorrect
                    ? $"Answer is correct (verdict: {checkResult.Verdict})"
                    : $"Answer is incorrect (verdict: {checkResult.Verdict})";

                var response = new CheckAnswerResponseDto(
                    IsCorrect: isCorrect,
                    Message: message,
                    Hash: request.Hash,
                    AnswerAttempt: request.AnswerAttempt,
                    Seed: request.Seed
                );

                _logger.LogInformation("Answer check completed for hash '{Hash}'. Result: {IsCorrect} (verdict: {Verdict})",
                    request.Hash, isCorrect, checkResult.Verdict);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking answer for hash '{Hash}'", request.Hash);
                return StatusCode(500, new CheckAnswerErrorResponseDto($"An error occurred: {ex.Message}", request.Hash, request.AnswerAttempt, request.Seed));
            }
        }
    }
}