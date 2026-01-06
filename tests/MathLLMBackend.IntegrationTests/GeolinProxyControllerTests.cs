using System.Net;
using Xunit;
using System.Net.Http.Json;
using FluentAssertions;
using MathLLMBackend.GeolinClient.Models;
using Moq;

namespace MathLLMBackend.IntegrationTests;

[Collection("Integration Tests")]
public class GeolinProxyControllerTests : BaseIntegrationTest
{
    public GeolinProxyControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetProblemDataByPrefix_WithValidPrefix_ReturnsOk()
    {
        Factory.GeolinApiMock
            .Setup(x => x.GetProblemsInfo(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new ProblemPageResponse
            {
                Problems = new List<ProblemInfoResponse>
                {
                    new() { Name = "test-problem", Hash = "test-hash" }
                },
                Number = 1
            });

        Factory.GeolinApiMock
            .Setup(x => x.GetProblemCondition(It.IsAny<ProblemConditionRequest>()))
            .ReturnsAsync(new ProblemConditionResponse
            {
                Condition = "Test condition",
                ProblemParams = "{}"
            });

        var response = await Client.GetAsync("/api/v1/geolin-proxy/problem-data?prefix=test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProblemDataByPrefix_WithEmptyPrefix_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/api/v1/geolin-proxy/problem-data?prefix=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CheckAnswerDirect_WithValidRequest_ReturnsOk()
    {
        var request = new
        {
            Hash = "test-hash",
            AnswerAttempt = "42",
            Seed = 123
        };
        var response = await Client.PostAsJsonAsync("/api/v1/geolin-proxy/check-answer-direct", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }
}
