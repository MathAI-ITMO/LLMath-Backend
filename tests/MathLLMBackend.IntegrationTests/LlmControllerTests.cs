using System.Net;
using Xunit;
using System.Net.Http.Json;
using FluentAssertions;
using MathLLMBackend.GeolinClient.Models;
using Moq;

namespace MathLLMBackend.IntegrationTests;

[Collection("Integration Tests")]
public class LlmControllerTests : BaseIntegrationTest
{
    public LlmControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SolveProblem_WithValidRequest_ReturnsOk()
    {
        Factory.LlmServiceMock
            .Setup(x => x.SolveProblem(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test solution");

        var request = new { ProblemDescription = "Solve 2+2" };
        var response = await AuthenticatedPostAsync("/api/v1/llm/solve-problem", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("solution");
    }

    [Fact]
    public async Task SolveProblem_WithEmptyDescription_ReturnsBadRequest()
    {
        var request = new { ProblemDescription = "" };
        var response = await AuthenticatedPostAsync("/api/v1/llm/solve-problem", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExtractAnswer_WithValidRequest_ReturnsOk()
    {
        Factory.LlmServiceMock
            .Setup(x => x.ExtractAnswer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("42");

        var request = new
        {
            ProblemStatement = "What is 2+2?",
            Solution = "2+2 = 4"
        };
        var response = await AuthenticatedPostAsync("/api/v1/llm/extract-answer", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExtractAnswer_WithEmptyProblemStatement_ReturnsBadRequest()
    {
        var request = new
        {
            ProblemStatement = "",
            Solution = "2+2 = 4"
        };
        var response = await AuthenticatedPostAsync("/api/v1/llm/extract-answer", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExtractAnswer_WithEmptySolution_ReturnsBadRequest()
    {
        var request = new
        {
            ProblemStatement = "What is 2+2?",
            Solution = ""
        };
        var response = await AuthenticatedPostAsync("/api/v1/llm/extract-answer", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
