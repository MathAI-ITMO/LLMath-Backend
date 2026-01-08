using MathLLMBackend.Domain.Models;
using MathLLMBackend.GeolinClient.Models;

namespace MathLLMBackend.Core.Services.GeolinService;

public interface IGeolinService
{
    Task<ProblemPageResponse> GetProblems(int page, int size, string? prefixName = "", CancellationToken ct = default);
    Task<ProblemData> GetProblemDataByPrefixAsync(string prefix, int? seed = null, CancellationToken ct = default);
    Task<AnswerCheckResult> CheckAnswerAsync(string hash, string answerAttempt, int? seed = null, string? problemParams = null, CancellationToken ct = default);
} 