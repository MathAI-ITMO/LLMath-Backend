using MathLLMBackend.Domain.Entities;

namespace MathLLMBackend.Core.Services;

public interface IUserTaskService
{
    Task<IEnumerable<UserTask>> GetOrCreateUserTasksAsync(string userId, int taskType, CancellationToken cancellationToken = default);
    Task<UserTask> StartUserTaskWithChatAsync(Guid userTaskId, string userId, CancellationToken cancellationToken = default);
    Task<UserTask?> CompleteTaskAsync(Guid userTaskId, string userId, CancellationToken cancellationToken = default);
} 