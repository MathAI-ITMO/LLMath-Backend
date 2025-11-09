using MathLLMBackend.GeolinClient;
using MathLLMBackend.GeolinClient.Models;
using MathLLMBackend.ProblemsClient;
using MathLLMBackend.ProblemsClient.Models;
using Microsoft.Extensions.Logging;
using Refit;

namespace MathLLMBackend.Core.Services.ProblemsService;

public class ProblemsService : IProblemsService
{
    private readonly IProblemsApi _problemsApi;
    private readonly IGeolinApi _geolinApi;
    private readonly ILogger<ProblemsService> _logger;
    public ProblemsService(IProblemsApi problemsApi, IGeolinApi geolinApi, ILogger<ProblemsService> logger)
    {
        _problemsApi = problemsApi;
        _geolinApi = geolinApi;
        _logger = logger;
    }

    public async Task<Problem> CreateProblemAsync(ProblemRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _problemsApi.CreateProblem(request);
    }
    public async Task<List<Problem>> GetSavedProblems(CancellationToken ct = default)
    {
        return await _problemsApi.GetProblems();
    }
    public async Task<List<Problem>> SaveProblems(string name, string problemHash, int variationCount, CancellationToken ct = default)
    {
        List<Problem> result = new();
        for (int i = 0; i < variationCount; ++i)
        {
            int seed = new Random().Next();
            var problem = await _geolinApi.GetProblemCondition(
                new ProblemConditionRequest()
                {
                    Hash = problemHash,
                    Seed = seed,
                    Lang = "ru"
                });
            var problemMongo = new ProblemRequest()
            {
                Statement = problem.Condition,
                GeolinAnsKey = new GeolinKey()
                {
                    Hash = problemHash,
                    Seed = seed
                }
            };
            var createdProblem = await _problemsApi.CreateProblem(problemMongo);
            result.Add(createdProblem);
            await _problemsApi.GiveANameProblem(new ProblemWithNameRequest()
            {
                Name = name,
                ProblemId = createdProblem.Id
            });
        }
        return result;
    }
    public async Task<List<Problem>> GetSavedProblemsByNames(string name, CancellationToken ct = default)
    {
        try
        {
            var problems = await _problemsApi.GetAllProblemsByName(name);
            return problems;
        }
        catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<Problem>();
        }
    }
    public async Task<List<Problem>> GetSavedProblemsByTypes(string typeName, CancellationToken ct = default)
    {
        try
        {
            var problems = await _problemsApi.GetProblemsByType(typeName);
            return problems;
        }
        catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<Problem>();
        }
    }
    public async Task<List<string>> GetAllTypes(CancellationToken ct = default)
    {
        return await _problemsApi.GetTypes();
    }
    public async Task<Problem?> GetProblemFromDbAsync(string problemDbId, CancellationToken ct = default)
    {
        try
        {
            return await _problemsApi.GetProblemById(problemDbId);
        }
        catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
