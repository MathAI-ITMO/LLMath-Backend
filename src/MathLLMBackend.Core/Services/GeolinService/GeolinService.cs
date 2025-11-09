using MathLLMBackend.GeolinClient;
using MathLLMBackend.GeolinClient.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MathLLMBackend.Core.Services.GeolinService;

public class GeolinService : IGeolinService
{
    private readonly IGeolinApi _geolinApi;
    private readonly ILogger<GeolinService> _logger;

    public GeolinService(IGeolinApi geolinApi, ILogger<GeolinService> logger)
    {
        _geolinApi = geolinApi;
        _logger = logger;
    }

    public async Task<ProblemPageResponse> GetProblems(int page, int size, string? prefixName = "", CancellationToken ct = default)
    {
        return await _geolinApi.GetProblemsInfo(page, size, prefixName);
    }

    public async Task<ProblemConditionResponse> GetProblemCondition(string hash, int seed, string lang = "ru", CancellationToken ct = default)
    {

        var request = new ProblemConditionRequest
        {
            Hash = hash,
            Seed = seed,
            Lang = lang
        };

        return await _geolinApi.GetProblemCondition(request);
    }

    public async Task<ProblemAnswerCheckResponse> CheckProblemAnswer(string hash, string answerAttempt, int? seed = null, string? problemParams = null, CancellationToken ct = default)
    {

        var request = new ProblemAnswerCheckRequest
        {
            Hash = hash,
            AnswerAttempt = answerAttempt
        };

        if (seed.HasValue)
        {
            request.Seed = seed.Value;
        }

        if (!string.IsNullOrWhiteSpace(problemParams))
        {
            request.ProblemParams = problemParams;
        }

        return await _geolinApi.CheckProblemAnswer(request);
    }

    public async Task<ProblemDataResult> GetProblemDataByPrefix(string prefix, int? seed = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting problem data for prefix: {Prefix}", prefix);

        // Get problem info by prefix
        var problemPage = await _geolinApi.GetProblemsInfo(page: 1, size: 10, prefixName: prefix);

        if (problemPage?.Problems == null || !problemPage.Problems.Any())
        {
            _logger.LogWarning("No problems found for prefix: {Prefix}", prefix);
            throw new InvalidOperationException($"No problem found for prefix '{prefix}'.");
        }

        // Find exact match or use first problem
        var problemInfo = problemPage.Problems.Count == 1
            ? problemPage.Problems.First()
            : problemPage.Problems.FirstOrDefault(p => p.Name == prefix) ?? problemPage.Problems.First();

        _logger.LogInformation("Found problem: {Name} with hash: {Hash}", problemInfo.Name, problemInfo.Hash);

        if (string.IsNullOrWhiteSpace(problemInfo.Hash))
        {
            _logger.LogWarning("Problem hash is empty for prefix: {Prefix}", prefix);
            throw new InvalidOperationException("Problem hash received from GeoLin is empty.");
        }

        // Determine seed to use
        var seedToUse = seed ?? Random.Shared.Next(1, 1000000000);
        _logger.LogInformation("Using seed: {Seed}", seedToUse);

        // Get problem condition
        var conditionResponse = await GetProblemCondition(problemInfo.Hash, seedToUse, "ru", ct);

        if (conditionResponse == null)
        {
            _logger.LogWarning("No condition response for hash: {Hash}", problemInfo.Hash);
            throw new InvalidOperationException("Failed to get problem condition from GeoLin.");
        }

        // Extract seed from problem params if available
        var finalSeed = ExtractSeedFromProblemParams(conditionResponse.ProblemParams, seedToUse);

        _logger.LogInformation("Successfully prepared problem data for prefix '{Prefix}' with seed {Seed}", prefix, finalSeed);

        return new ProblemDataResult(
            Name: problemInfo.Name,
            Hash: problemInfo.Hash,
            Condition: conditionResponse.Condition,
            Seed: finalSeed,
            ProblemParams: conditionResponse.ProblemParams
        );
    }

    private int ExtractSeedFromProblemParams(string? problemParams, int defaultSeed)
    {
        if (string.IsNullOrWhiteSpace(problemParams))
        {
            return defaultSeed;
        }

        try
        {
            using var document = JsonDocument.Parse(problemParams);
            if (document.RootElement.TryGetProperty("seed", out var seedElement) &&
                seedElement.ValueKind == JsonValueKind.Number)
            {
                var seedFromParams = seedElement.GetInt32();
                if (seedFromParams != defaultSeed)
                {
                    _logger.LogWarning("Seed in problem_params {SeedFromParams} differs from requested seed {RequestedSeed}. Using the one from problem_params.",
                        seedFromParams, defaultSeed);
                }
                return seedFromParams;
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx, "Failed to parse ProblemParams as JSON: {ProblemParams}, using default seed {Seed}",
                problemParams, defaultSeed);
        }

        return defaultSeed;
    }
}