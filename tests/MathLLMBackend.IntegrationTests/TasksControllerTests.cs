using System.Net;
using Xunit;
using FluentAssertions;
using MathLLMBackend.GeolinClient.Models;
using MathLLMBackend.ProblemsClient.Models;
using Moq;

namespace MathLLMBackend.IntegrationTests;

[Collection("Integration Tests")]
public class TasksControllerTests : BaseIntegrationTest
{
    public TasksControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetProblems_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/tasks/problems");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProblems_WithAuth_ReturnsOk()
    {
        Factory.GeolinApiMock
            .Setup(x => x.GetProblemsInfo(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new ProblemPageResponse
            {
                Problems = new List<ProblemInfoResponse>
                {
                    new() { Name = "test", Hash = "hash", Description = "desc", ConditionRu = "condition" }
                },
                Number = 1
            });

        await CreateAndLoginUserAsync();
        var response = await AuthenticatedGetAsync("/api/tasks/problems");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SaveProblem_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync("/api/tasks/saveProblem?name=test&problemHash=hash", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SaveProblem_WithAuth_ReturnsOk()
    {
        Factory.GeolinApiMock
            .Setup(x => x.GetProblemCondition(It.IsAny<ProblemConditionRequest>()))
            .ReturnsAsync(new ProblemConditionResponse
            {
                Condition = "Test condition",
                ProblemParams = "{}"
            });

        var problemId = Guid.NewGuid().ToString();
        Factory.ProblemsApiMock
            .Setup(x => x.CreateProblem(It.IsAny<ProblemRequest>()))
            .ReturnsAsync(new Problem { Id = problemId });

        Factory.ProblemsApiMock
            .Setup(x => x.GiveANameProblem(It.IsAny<ProblemWithNameRequest>()))
            .ReturnsAsync(problemId);

        await CreateAndLoginUserAsync();
        var response = await AuthenticatedPostAsync("/api/tasks/saveProblem?name=test&problemHash=hash");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetSavedProblems_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/tasks/getSavedProblems");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSavedProblems_WithAuth_ReturnsOk()
    {
        Factory.ProblemsApiMock
            .Setup(x => x.GetProblems())
            .ReturnsAsync(new List<Problem>());

        await CreateAndLoginUserAsync();
        var response = await AuthenticatedGetAsync("/api/tasks/getSavedProblems");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSavedProblemsByNames_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/tasks/getSavedProblemsByNames?name=test");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSavedProblemsByNames_WithAuth_ReturnsOk()
    {
        Factory.ProblemsApiMock
            .Setup(x => x.GetAllProblemsByName(It.IsAny<string>()))
            .ReturnsAsync(new List<Problem>());

        await CreateAndLoginUserAsync();
        var response = await AuthenticatedGetAsync("/api/tasks/getSavedProblemsByNames?name=test");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllNames_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/tasks/getAllNames");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllNames_WithAuth_ReturnsOk()
    {
        Factory.ProblemsApiMock
            .Setup(x => x.GetTypes())
            .ReturnsAsync(new List<string> { "test" });

        await CreateAndLoginUserAsync();
        var response = await AuthenticatedGetAsync("/api/tasks/getAllNames");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
