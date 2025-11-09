using MathLLMBackend.Core.Services.GeolinService;
using MathLLMBackend.Presentation.Dtos.Geolin;
using Microsoft.AspNetCore.Mvc;

namespace MathLLMBackend.Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/geolin-proxy")]
    public class GeolinProxyController : ControllerBase
    {
        private readonly IGeolinService _geolinService;

        public GeolinProxyController(IGeolinService geolinService)
        {
            _geolinService = geolinService ?? throw new ArgumentNullException(nameof(geolinService));
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

            return Ok(response);
        }
    }
}