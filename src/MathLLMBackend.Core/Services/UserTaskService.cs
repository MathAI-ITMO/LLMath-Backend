using MathLLMBackend.DataAccess.Contexts;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MathLLMBackend.Core.Services.ProblemsService;
using Microsoft.Extensions.Configuration;

namespace MathLLMBackend.Core.Services;

public class UserTaskService : IUserTaskService
{
    private readonly AppDbContext _context;
    private readonly IProblemsService _problemsService;
    private readonly ILogger<UserTaskService> _logger;
    private readonly Dictionary<string, string> _taskModeTitles;

    public UserTaskService(
        AppDbContext context,
        IProblemsService problemsService,
        ILogger<UserTaskService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _problemsService = problemsService;
        _logger = logger;
        // Загружаем маппинг taskType -> typeName из конфигурации (раздел TaskModeTitles)
        _taskModeTitles = configuration.GetSection("TaskModeTitles").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
    }

    public async Task<IEnumerable<UserTask>> GetOrCreateUserTasksAsync(string userId, int taskType, CancellationToken cancellationToken = default)
    {
        // Определяем имя типа задачи по конфигурации
        string? typeName = _taskModeTitles.TryGetValue(taskType.ToString(), out var tn) ? tn : null;
        if (typeName == null)
        {
            _logger.LogWarning("Task type {TaskType} отсутствует в конфигурации TaskModeTitles. Будут возвращены пустые задачи.", taskType);
            return Enumerable.Empty<UserTask>();
        }

        _logger.LogInformation("Fetching problems of type '{TypeName}' (taskType={TaskType}) from LLMath-Problems for user {UserId}", typeName, taskType, userId);

        List<ProblemsClient.Models.Problem> problemsFromDb;
        try
        {
            problemsFromDb = await _problemsService.GetSavedProblemsByTypes(typeName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching problems of type '{TypeName}' from LLMath-Problems service.", typeName);
            return Enumerable.Empty<UserTask>();
        }

        if (problemsFromDb == null || !problemsFromDb.Any())
        {
            _logger.LogInformation("No problems of type '{TypeName}' found in LLMath-Problems database.", typeName);
            return Enumerable.Empty<UserTask>();
        }

        var newOrExistingUserTasks = new List<UserTask>();

        foreach (var problemFromDb in problemsFromDb)
        {
            if (string.IsNullOrEmpty(problemFromDb.Id))
            {
                _logger.LogWarning("Problem from DB has null or empty ID. Skipping. Problem ID: {ProblemID}", problemFromDb.Id);
                continue;
            }

            // Проверяем, существует ли уже UserTask для этой задачи из LLMath-Problems
            var existingUserTask = await _context.UserTasks
                .FirstOrDefaultAsync(ut => ut.ApplicationUserId == userId
                    && ut.ProblemHash == problemFromDb.Id
                    && ut.TaskType == taskType, cancellationToken);

            if (existingUserTask != null)
            {
                // Если UserTask уже есть, просто используем его
                newOrExistingUserTasks.Add(existingUserTask);
            }
            else
            {
                // Если UserTask нет, создаем новый
                var newTask = new UserTask
                {
                    ApplicationUserId = userId,
                    ProblemId = problemFromDb.Id,       // Используем ID из LLMath-Problems как ProblemId
                    ProblemHash = problemFromDb.Id,     // И как ProblemHash для связи с ChatService
                    DisplayName = !string.IsNullOrWhiteSpace(problemFromDb.Title)
                        ? problemFromDb.Title
                        : problemFromDb.Statement.Substring(0, Math.Min(50, problemFromDb.Statement.Length)) + "...",
                    TaskType = taskType, // Пока используем переданный taskType, но можно будет брать из problemFromDb, если добавим туда поле "тип"
                    Status = UserTaskStatus.NotStarted,
                    AssociatedChatId = null
                };
                _context.UserTasks.Add(newTask);
                newOrExistingUserTasks.Add(newTask);
            }
        }

        // Сохраняем все новые UserTask, созданные в этом цикле
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Returning {Count} UserTasks based on LLMath-Problems DB for user {UserId}", newOrExistingUserTasks.Count, userId);
        return newOrExistingUserTasks.OrderBy(ut => ut.DisplayName);
    }

    public async Task<UserTask?> StartTaskAsync(Guid userTaskId, Guid chatId, string userId, CancellationToken cancellationToken = default)
    {
        var userTask = await _context.UserTasks
            .FirstOrDefaultAsync(ut => ut.Id == userTaskId && ut.ApplicationUserId == userId, cancellationToken);

        if (userTask == null)
        {
            _logger.LogWarning("UserTask with ID {UserTaskId} not found for user {UserId}", userTaskId, userId);
            return null; // Задача не найдена или не принадлежит пользователю
        }

        if (userTask.Status == UserTaskStatus.InProgress && userTask.AssociatedChatId == chatId)
        {
            _logger.LogInformation("Task {UserTaskId} is already in progress with chat {ChatId}.", userTaskId, chatId);
            return userTask; // Задача уже в нужном состоянии
        }

        if (userTask.AssociatedChatId != null && userTask.AssociatedChatId != chatId)
        {
            _logger.LogWarning("Task {UserTaskId} is already associated with a different chat {ExistingChatId}. Cannot associate with new chat {NewChatId}.",
               userTaskId, userTask.AssociatedChatId, chatId);
            // Возможно, здесь стоит вернуть ошибку или текущее состояние?
            // Пока возвращаем null, сигнализируя о проблеме.
            return null;
        }

        userTask.Status = UserTaskStatus.InProgress;
        userTask.AssociatedChatId = chatId;
        // userTask.UpdatedAt = DateTime.UtcNow;

        _context.UserTasks.Update(userTask);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Task {UserTaskId} status updated to InProgress and associated with chat {ChatId}", userTaskId, chatId);

        return userTask;
    }

    public async Task<UserTask?> GetUserTaskByIdAsync(Guid userTaskId, string userId, CancellationToken cancellationToken = default)
    {
        var userTask = await _context.UserTasks
            .FirstOrDefaultAsync(ut => ut.Id == userTaskId && ut.ApplicationUserId == userId, cancellationToken);

        if (userTask == null)
        {
            _logger.LogWarning("UserTask with ID {UserTaskId} not found for user {UserId} in GetUserTaskByIdAsync.", userTaskId, userId);
            return null;
        }
        return userTask;
    }

    public async Task<UserTask?> CompleteTaskAsync(Guid userTaskId, string userId, CancellationToken cancellationToken = default)
    {
        var userTask = await _context.UserTasks
            .FirstOrDefaultAsync(ut => ut.Id == userTaskId && ut.ApplicationUserId == userId, cancellationToken);

        if (userTask == null)
        {
            _logger.LogWarning("CompleteTask: UserTask with ID {UserTaskId} not found for user {UserId}", userTaskId, userId);
            return null;
        }

        if (userTask.Status == UserTaskStatus.Solved)
        {
            _logger.LogInformation("CompleteTask: Task {UserTaskId} is already marked as solved.", userTaskId);
            return userTask;
        }

        userTask.Status = UserTaskStatus.Solved;
        _context.UserTasks.Update(userTask);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("CompleteTask: Task {UserTaskId} marked as solved for user {UserId}", userTaskId, userId);

        return userTask;
    }
}