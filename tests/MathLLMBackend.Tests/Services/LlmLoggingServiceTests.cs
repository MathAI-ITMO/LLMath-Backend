using MathLLMBackend.Core.Configuration;
using MathLLMBackend.Core.Services.LlmService;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MathLLMBackend.Tests.Services;

public class LlmLoggingServiceTests : IDisposable
{
    private readonly Mock<ILogger<LlmLoggingService>> _loggerMock;
    private readonly string _testLogPath;
    private readonly LlmLoggingConfiguration _config;
    private readonly LlmLoggingService _service;

    public LlmLoggingServiceTests()
    {
        _loggerMock = new Mock<ILogger<LlmLoggingService>>();
        _testLogPath = Path.Combine(Path.GetTempPath(), $"llm_test_{Guid.NewGuid()}.log");
        _config = new LlmLoggingConfiguration
        {
            Enabled = true
        };

        var optionsMock = new Mock<IOptions<LlmLoggingConfiguration>>();
        optionsMock.Setup(x => x.Value).Returns(_config);

        _service = new LlmLoggingService(_loggerMock.Object, optionsMock.Object);
    }

    [Fact]
    public async Task LogInteraction_CallsLogger()
    {
        // Arrange
        var messages = new List<Message>
        {
            new() { MessageType = MessageType.User, Text = "Hello", IsSystemPrompt = false }
        };
        var response = "Hi there!";
        var modelName = "gpt-4o";

        // Act
        await _service.LogInteraction(0, messages, response, modelName);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LLM INTERACTION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        Assert.False(File.Exists(_testLogPath));
    }

    [Fact]
    public async Task LogSolution_CallsLogger()
    {
        // Arrange
        var problem = "2+2";
        var solution = "4";
        var modelName = "gpt-4o";

        // Act
        await _service.LogSolution(problem, solution, modelName);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LLM SOLUTION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        Assert.False(File.Exists(_testLogPath));
    }

    public void Dispose()
    {
        if (File.Exists(_testLogPath))
        {
            File.Delete(_testLogPath);
        }
    }
}
