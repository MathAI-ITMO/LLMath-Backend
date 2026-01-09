using MathLLMBackend.Core.Configuration;
using MathLLMBackend.Core.Constants;
using MathLLMBackend.Core.Services.ChatService;
using MathLLMBackend.DataAccess.Contexts;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MathLLMBackend.ProblemsClient.Models;
using MathLLMBackend.Core.Services.ProblemsService;
using MathLLMBackend.Domain.Exceptions;
using Microsoft.Extensions.Configuration;

namespace MathLLMBackend.Core.Services;

public class UserTaskService(
    AppDbContext context,
    IProblemsService problemsService,
    IChatService chatService,
    ILogger<UserTaskService> logger,
    IConfiguration configuration)
    : IUserTaskService
{
    private readonly AppDbContext _context = context;
    private readonly IProblemsService _problemsService = problemsService;
    private readonly IChatService _chatService = chatService;
    private readonly ILogger<UserTaskService> _logger = logger;
    private readonly Dictionary<string, string> _taskModeTitles = configuration.GetSection("TaskModeTitles").Get<Dictionary<string, string>>() 
                                                                  ?? new Dictionary<string, string>();

    public async Task<IEnumerable<UserTask>> GetOrCreateUserTasksAsync(string userId, int taskType, CancellationToken cancellationToken = default)
    {
        if (!_taskModeTitles.TryGetValue(taskType.ToString(), out var typeName))
        {
            _logger.LogWarning("Task type {TaskType} is not configured in TaskModeTitles. Returning empty tasks.", taskType);
            return Enumerable.Empty<UserTask>();
        }

        _logger.LogInformation("Fetching problems of type '{TypeName}' (taskType={TaskType}) from LLMath-Problems for user {UserId}", typeName, taskType, userId);

        var problemsFromDb = new List<Problem>();
        try
        {
            problemsFromDb = await _problemsService.GetSavedProblemsByTypes(typeName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching problems from LLMath-Problems for user {UserId}", userId);
            return Enumerable.Empty<UserTask>();
        }

        if (problemsFromDb.Count == 0)
        {
            _logger.LogInformation("No problems of type '{TypeName}' found in LLMath-Problems database.", typeName);
            return [];
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
                var displayName = !string.IsNullOrWhiteSpace(problemFromDb.Title)
                    ? problemFromDb.Title
                    : GetTruncatedStatement(problemFromDb.Statement);
                
                var newTask = new UserTask
                {
                    ApplicationUserId = userId,
                    ProblemId = problemFromDb.Id,
                    ProblemHash = problemFromDb.Id,
                    DisplayName = displayName,
                    TaskType = taskType,
                    Status = UserTaskStatus.NotStarted,
                    AssociatedChatId = null
                };
                
                _context.UserTasks.Add(newTask);
                newOrExistingUserTasks.Add(newTask);
            }
        }
        
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Returning {Count} UserTasks based on LLMath-Problems DB for user {UserId}", newOrExistingUserTasks.Count, userId);
        return newOrExistingUserTasks.OrderBy(ut => ut.DisplayName);
    }

    public async Task<UserTask> StartUserTaskWithChatAsync(Guid userTaskId, string userId, CancellationToken cancellationToken = default)
    {
        var userTask = await GetUserTaskByIdAsync(userTaskId, userId, cancellationToken);
        if (userTask == null)
        {
            throw new NotFoundException($"Task not found or you don't have permission.");
        }

        if (userTask.AssociatedChatId.HasValue && userTask.AssociatedChatId != Guid.Empty)
        {
            _logger.LogInformation("Task {UserTaskId} already associated with chat {ChatId}. Returning current state.", userTaskId, userTask.AssociatedChatId);
            var confirmedTask = await StartTaskAsync(userTaskId, userTask.AssociatedChatId.Value, userId, cancellationToken);
            return confirmedTask ?? throw new InvalidOperationException("Failed to confirm task state.");
        }

        var chatId = await _chatService.GetOrCreateProblemChatAsync(
            userTask.ProblemHash, 
            userId, 
            userTask.DisplayName, 
            userTask.TaskType, 
            cancellationToken);

        if (chatId == Guid.Empty)
        {
            throw new InvalidOperationException("Failed to obtain a valid chat ID.");
        }

        var updatedTask = await StartTaskAsync(userTaskId, chatId, userId, cancellationToken);
        if (updatedTask == null)
        {
            throw new InvalidOperationException("Failed to update task status.");
        }

        return updatedTask;
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
            _logger.LogInformation("Task {UserTaskId} is already in progress with chat {ChatId}.", userTaskId, chatId);
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

    private static string GetTruncatedStatement(string statement)
    {
        return statement.Length > DisplayConstants.MaxSnippetLength 
            ? statement.Substring(0, DisplayConstants.MaxSnippetLength) + "..." 
            : statement;
    }

} 