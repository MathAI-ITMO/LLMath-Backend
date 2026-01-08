using MathLLMBackend.Domain.Entities;

namespace MathLLMBackend.Core.Services.LlmService;

public interface ILlmLoggingService
{
    Task LogInteraction(int taskType, IEnumerable<Message> messages, string response, string modelName);
    Task LogSolution(string problem, string solution, string modelName);
} 