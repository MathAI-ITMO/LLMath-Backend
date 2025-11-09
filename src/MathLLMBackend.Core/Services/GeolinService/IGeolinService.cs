using MathLLMBackend.GeolinClient.Models;

namespace MathLLMBackend.Core.Services.GeolinService;

public interface IGeolinService
{
    Task<ProblemPageResponse> GetProblems(int page, int size, string? prefixName = "", CancellationToken ct = default);
    Task<ProblemConditionResponse> GetProblemCondition(string hash, int seed, string lang = "ru", CancellationToken ct = default);
    Task<ProblemAnswerCheckResponse> CheckProblemAnswer(string hash, string answerAttempt, int? seed = null, string? problemParams = null, CancellationToken ct = default);
    Task<ProblemDataResult> GetProblemDataByPrefix(string prefix, int? seed = null, CancellationToken ct = default);
}