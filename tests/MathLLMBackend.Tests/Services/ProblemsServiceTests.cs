using FluentAssertions;
using MathLLMBackend.Core.Services.ProblemsService;
using MathLLMBackend.GeolinClient;
using MathLLMBackend.ProblemsClient;
using MathLLMBackend.ProblemsClient.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Refit;
using Xunit;

namespace MathLLMBackend.Tests.Services;

public class ProblemsServiceTests
{
    private readonly Mock<IProblemsAPI> _problemsApiMock;
    private readonly Mock<IGeolinApi> _geolinApiMock;
    private readonly Mock<ILogger<ProblemsService>> _loggerMock;
    private readonly ProblemsService _service;

    public ProblemsServiceTests()
    {
        _problemsApiMock = new Mock<IProblemsAPI>();
        _geolinApiMock = new Mock<IGeolinApi>();
        _loggerMock = new Mock<ILogger<ProblemsService>>();

        _service = new ProblemsService(
            _problemsApiMock.Object,
            _geolinApiMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetSavedProblems_ReturnsProblemsFromApi()
    {
        var expectedProblems = new List<Problem>
        {
            new() { Id = "1", Statement = "Problem 1", Title = "Title 1" },
            new() { Id = "2", Statement = "Problem 2", Title = "Title 2" }
        };

        _problemsApiMock
            .Setup(x => x.GetProblems())
            .ReturnsAsync(expectedProblems);

        var result = await _service.GetSavedProblems();

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedProblems);
    }

    [Fact]
    public async Task GetSavedProblems_WhenApiThrows_PropagatesException()
    {
        _problemsApiMock
            .Setup(x => x.GetProblems())
            .ThrowsAsync(new Exception("API Error"));

        var act = async () => await _service.GetSavedProblems();

        await act.Should().ThrowAsync<Exception>().WithMessage("API Error");
    }

    [Fact]
    public async Task GetSavedProblemsByTypes_ReturnsProblemsFromApi()
    {
        const string typeName = "Learning";
        var expectedProblems = new List<Problem>
        {
            new() { Id = "1", Statement = "Problem 1", Title = "Title 1" }
        };

        _problemsApiMock
            .Setup(x => x.GetProblemsByType(typeName))
            .ReturnsAsync(expectedProblems);

        var result = await _service.GetSavedProblemsByTypes(typeName);

        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(expectedProblems);
    }

    [Fact]
    public async Task GetSavedProblemsByTypes_WhenApiReturns404_ReturnsEmptyList()
    {
        const string typeName = "NonExistent";

        var apiException = await ApiException.Create(
            new HttpRequestMessage(),
            HttpMethod.Get,
            new HttpResponseMessage(System.Net.HttpStatusCode.NotFound),
            new RefitSettings());

        _problemsApiMock
            .Setup(x => x.GetProblemsByType(typeName))
            .ThrowsAsync(apiException);

        var result = await _service.GetSavedProblemsByTypes(typeName);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSavedProblemsByTypes_WhenApiThrowsNon404_PropagatesException()
    {
        const string typeName = "Learning";

        _problemsApiMock
            .Setup(x => x.GetProblemsByType(typeName))
            .ThrowsAsync(new Exception("Server Error"));

        var act = async () => await _service.GetSavedProblemsByTypes(typeName);

        await act.Should().ThrowAsync<Exception>().WithMessage("Server Error");
    }

    [Fact]
    public async Task GetProblemFromDbAsync_ReturnsProblemFromApi()
    {
        const string problemId = "problem1";
        var expectedProblem = new Problem
        {
            Id = problemId,
            Statement = "Test Statement",
            Title = "Test Title"
        };

        _problemsApiMock
            .Setup(x => x.GetProblemById(problemId))
            .ReturnsAsync(expectedProblem);

        var result = await _service.GetProblemFromDbAsync(problemId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(problemId);
        result.Statement.Should().Be("Test Statement");
    }

    [Fact]
    public async Task GetProblemFromDbAsync_WhenProblemNotFound_ReturnsNull()
    {
        const string problemId = "nonExistent";

        var apiException = await ApiException.Create(
            new HttpRequestMessage(),
            HttpMethod.Get,
            new HttpResponseMessage(System.Net.HttpStatusCode.NotFound),
            new RefitSettings());

        _problemsApiMock
            .Setup(x => x.GetProblemById(problemId))
            .ThrowsAsync(apiException);

        var result = await _service.GetProblemFromDbAsync(problemId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProblemFromDbAsync_WhenApiThrowsNon404_PropagatesException()
    {
        const string problemId = "problem1";

        _problemsApiMock
            .Setup(x => x.GetProblemById(problemId))
            .ThrowsAsync(new Exception("Server Error"));

        var act = async () => await _service.GetProblemFromDbAsync(problemId);

        await act.Should().ThrowAsync<Exception>().WithMessage("Server Error");
    }

    [Fact]
    public async Task GetSavedProblemsByNames_ReturnsProblemsFromApi()
    {
        const string name = "TestName";
        var expectedProblems = new List<Problem>
        {
            new() { Id = "1", Statement = "Problem 1", Title = "Title 1" }
        };

        _problemsApiMock
            .Setup(x => x.GetAllProblemsByName(name))
            .ReturnsAsync(expectedProblems);

        var result = await _service.GetSavedProblemsByNames(name);

        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(expectedProblems);
    }

    [Fact]
    public async Task GetSavedProblemsByNames_WhenApiReturns404_ReturnsEmptyList()
    {
        const string name = "NonExistent";

        var apiException = await ApiException.Create(
            new HttpRequestMessage(),
            HttpMethod.Get,
            new HttpResponseMessage(System.Net.HttpStatusCode.NotFound),
            new RefitSettings());

        _problemsApiMock
            .Setup(x => x.GetAllProblemsByName(name))
            .ThrowsAsync(apiException);

        var result = await _service.GetSavedProblemsByNames(name);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSavedProblemsByNames_WhenApiThrowsNon404_PropagatesException()
    {
        const string name = "TestName";

        _problemsApiMock
            .Setup(x => x.GetAllProblemsByName(name))
            .ThrowsAsync(new Exception("Server Error"));

        var act = async () => await _service.GetSavedProblemsByNames(name);

        await act.Should().ThrowAsync<Exception>().WithMessage("Server Error");
    }

    [Fact]
    public async Task GetAllTypes_ReturnsTypesFromApi()
    {
        var expectedTypes = new List<string> { "Type1", "Type2", "Type3" };

        _problemsApiMock
            .Setup(x => x.GetTypes())
            .ReturnsAsync(expectedTypes);

        var result = await _service.GetAllTypes();

        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedTypes);
    }

    [Fact]
    public async Task GetAllTypes_WhenApiThrows_PropagatesException()
    {
        _problemsApiMock
            .Setup(x => x.GetTypes())
            .ThrowsAsync(new Exception("API Error"));

        var act = async () => await _service.GetAllTypes();

        await act.Should().ThrowAsync<Exception>().WithMessage("API Error");
    }
}
