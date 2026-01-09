using MathLLMBackend.Core.Constants;
using MathLLMBackend.Domain.Models;
using MathLLMBackend.GeolinClient;
using MathLLMBackend.GeolinClient.Models;
using System.Text.Json;

namespace MathLLMBackend.Core.Services.GeolinService;

public class GeolinService : IGeolinService
{
    private readonly IGeolinApi _geolinApi;
    private readonly Random _random = new();

    public GeolinService(
        IGeolinApi geolinApi)
    {
        _geolinApi = geolinApi;
    }

    public async Task<ProblemPageResponse> GetProblems(int page, int size, string? prefixName = "", CancellationToken ct = default)
    {
        return await _geolinApi.GetProblemsInfo(page, size, prefixName);
    }

    public async Task<ProblemData> GetProblemDataByPrefixAsync(string prefix, int? seed = null, CancellationToken ct = default)
    {
        var problem = await FindProblemByPrefixAsync(prefix, ct);
        if (problem == null)
        {
            throw new InvalidOperationException($"No problem found for prefix '{prefix}'.");
        }

        var seedToUse = seed ?? _random.Next(1, 1000000000);
        var condition = await GetProblemConditionAsync(problem.Hash, seedToUse, ct);
        if (condition == null)
        {
            throw new InvalidOperationException("Failed to get problem condition from GeoLin.");
        }

        var finalSeed = TryExtractSeedFromParams(condition.ProblemParams) ?? seedToUse;

        return new ProblemData
        {
            Name = problem.Name,
            Hash = problem.Hash,
            Condition = condition.Condition,
            Seed = finalSeed,
            ProblemParams = condition.ProblemParams
        };
    }

    public async Task<AnswerCheckResult> CheckAnswerAsync(string hash, string answerAttempt, int? seed = null, string? problemParams = null, CancellationToken ct = default)
    {
        var verdict = await GetAnswerVerdictAsync(hash, answerAttempt, seed, problemParams, ct);
        var isCorrect = verdict >= GeolinConstants.CorrectAnswerVerdictThreshold;

        return new AnswerCheckResult
        {
            IsCorrect = isCorrect,
            Verdict = verdict
        };
    }

    private async Task<ProblemInfoResponse?> FindProblemByPrefixAsync(string prefix, CancellationToken ct)
    {
        var problemPage = await _geolinApi.GetProblemsInfo(page: 1, size: 10, prefixName: prefix);
        
        if (problemPage.Problems.Count == 0)
        {
            return null;
        }

        var problem = problemPage.Problems.FirstOrDefault(p => p.Name == prefix) 
                     ?? problemPage.Problems.First();

        return string.IsNullOrWhiteSpace(problem.Hash) ? null : problem;
    }

    private async Task<ProblemConditionResponse?> GetProblemConditionAsync(string hash, int seed, CancellationToken ct)
    {
               return await _geolinApi.GetProblemCondition(new ProblemConditionRequest
               {
                   Hash = hash,
                   Seed = seed,
                   Lang = LocalizationConstants.RussianLanguageCode
               });
    }

    private async Task<double> GetAnswerVerdictAsync(string hash, string answerAttempt, int? seed, string? problemParams, CancellationToken ct)
    {
        var checkRequest = new ProblemAnswerCheckRequest
        {
            Hash = hash,
            AnswerAttempt = answerAttempt,
            Seed = seed,
            ProblemParams = problemParams
        };

        var response = await _geolinApi.CheckProblemAnswer(checkRequest);
        return response.Verdict;
    }

    private static int? TryExtractSeedFromParams(string? problemParams)
    {
        if (string.IsNullOrWhiteSpace(problemParams))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(problemParams);
            if (doc.RootElement.TryGetProperty("seed", out var seedElement) && 
                seedElement.ValueKind == JsonValueKind.Number)
            {
                return seedElement.GetInt32();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

} 