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
        _taskModeTitles = configuration.GetSection("TaskModeTitles").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
    }

    public async Task<IEnumerable<UserTask>> GetOrCreateUserTasksAsync(string userId, TaskType taskType, CancellationToken cancellationToken = default)
    {
        string? typeName = _taskModeTitles.TryGetValue(taskType.ToString(), out var tn) ? tn : null;
        if (typeName == null)
        {
            _logger.LogWarning("Task type {TaskType} not found in TaskModeTitles configuration. Returning empty tasks.", taskType);
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
                _logger.LogWarning("Problem from DB has null or empty ID. Skipping.");
                continue;
            }

            var existingUserTask = await _context.UserTasks
                .FirstOrDefaultAsync(ut => ut.ApplicationUserId == userId
                    && ut.ProblemHash == problemFromDb.Id
                    && ut.TaskType == taskType, cancellationToken);

            if (existingUserTask != null)
            {
                newOrExistingUserTasks.Add(existingUserTask);
            }
            else
            {
                var newTask = new UserTask
                {
                    ApplicationUserId = userId,
                    ProblemId = problemFromDb.Id,
                    ProblemHash = problemFromDb.Id,
                    DisplayName = !string.IsNullOrWhiteSpace(problemFromDb.Title)
                        ? problemFromDb.Title
                        : problemFromDb.Statement.Substring(0, Math.Min(50, problemFromDb.Statement.Length)) + "...",
                    TaskType = taskType,
                    Status = UserTaskStatus.NotStarted,
                    AssociatedChatId = null
                };
                _context.UserTasks.Add(newTask);
                newOrExistingUserTasks.Add(newTask);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return newOrExistingUserTasks.OrderBy(ut => ut.DisplayName);
    }

    public async Task<UserTask?> StartTaskAsync(Guid userTaskId, Guid chatId, string userId, CancellationToken cancellationToken = default)
    {
        var userTask = await _context.UserTasks
            .FirstOrDefaultAsync(ut => ut.Id == userTaskId && ut.ApplicationUserId == userId, cancellationToken);

        if (userTask == null)
        {
            _logger.LogWarning("UserTask with ID {UserTaskId} not found for user {UserId}", userTaskId, userId);
            return null;
        }

        if (userTask.Status == UserTaskStatus.InProgress && userTask.AssociatedChatId == chatId)
        {
            return userTask;
        }

        if (userTask.AssociatedChatId != null && userTask.AssociatedChatId != chatId)
        {
            _logger.LogWarning("Task {UserTaskId} is already associated with a different chat {ExistingChatId}. Cannot associate with new chat {NewChatId}.",
               userTaskId, userTask.AssociatedChatId, chatId);
            return null;
        }

        userTask.Status = UserTaskStatus.InProgress;
        userTask.AssociatedChatId = chatId;

        _context.UserTasks.Update(userTask);
        await _context.SaveChangesAsync(cancellationToken);

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