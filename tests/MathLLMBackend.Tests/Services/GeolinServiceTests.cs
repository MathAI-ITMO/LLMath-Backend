using FluentAssertions;
using MathLLMBackend.Core.Services.GeolinService;
using MathLLMBackend.GeolinClient;
using MathLLMBackend.GeolinClient.Models;
using MathLLMBackend.GeolinClient.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MathLLMBackend.Tests.Services;

public class GeolinServiceTests
{
    private readonly Mock<IGeolinApi> _geolinApiMock;
    private readonly Mock<ILogger<GeolinService>> _loggerMock;
    private readonly Mock<IOptions<GeolinClientOptions>> _optionsMock;
    private readonly GeolinService _service;

    public GeolinServiceTests()
    {
        _geolinApiMock = new Mock<IGeolinApi>();
        _loggerMock = new Mock<ILogger<GeolinService>>();
        _optionsMock = new Mock<IOptions<GeolinClientOptions>>();
        
        _optionsMock.Setup(x => x.Value).Returns(new GeolinClientOptions
        {
            BaseAddress = "https://test.com",
            AuthorizationHeader = "test-auth"
        });

        _service = new GeolinService(
            _geolinApiMock.Object,
            _loggerMock.Object,
            _optionsMock.Object);
    }

    [Fact]
    public async Task GetProblems_ReturnsResponseFromApi()
    {
        const int page = 1;
        const int size = 10;
        const string prefixName = "test";

        var expectedResponse = new ProblemPageResponse
        {
            Problems = new List<ProblemInfoResponse>
            {
                new() { Hash = "hash1", Name = "Problem 1" },
                new() { Hash = "hash2", Name = "Problem 2" }
            },
            Number = 2
        };

        _geolinApiMock
            .Setup(x => x.GetProblemsInfo(page, size, prefixName))
            .ReturnsAsync(expectedResponse);

        var result = await _service.GetProblems(page, size, prefixName);

        result.Should().NotBeNull();
        result.Problems.Should().HaveCount(2);
        result.Number.Should().Be(2);
    }

    [Fact]
    public async Task GetProblems_WhenPrefixNameIsNull_UsesEmptyString()
    {
        const int page = 1;
        const int size = 10;

        var expectedResponse = new ProblemPageResponse
        {
            Problems = new List<ProblemInfoResponse>(),
            Number = 0
        };

        _geolinApiMock
            .Setup(x => x.GetProblemsInfo(page, size, It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        var result = await _service.GetProblems(page, size, null);

        result.Should().NotBeNull();
        _geolinApiMock.Verify(x => x.GetProblemsInfo(page, size, It.Is<string>(s => string.IsNullOrEmpty(s))), Times.Once);
    }

    [Fact]
    public async Task GetProblems_WhenApiThrows_PropagatesException()
    {
        const int page = 1;
        const int size = 10;

        _geolinApiMock
            .Setup(x => x.GetProblemsInfo(page, size, It.IsAny<string>()))
            .ThrowsAsync(new Exception("API Error"));

        var act = async () => await _service.GetProblems(page, size);

        await act.Should().ThrowAsync<Exception>().WithMessage("API Error");
    }
}
