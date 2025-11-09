using MathLLMBackend.Core.Configuration;
using MathLLMBackend.Core.Services.LlmService;
using MathLLMBackend.DataAccess.Contexts;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using MathLLMBackend.Core.Services.ProblemsService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace MathLLMBackend.Core.Services.ChatService;

public class ChatService : IChatService
{
    private readonly AppDbContext _dbContext;
    private readonly ILlmService _llmService;
    private readonly IProblemsService _problemsService;
    private readonly IOptions<PromptConfiguration> _prompts;
    private readonly IOptions<LlmServiceConfiguration> _llmConfig;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        AppDbContext dbContext,
        ILlmService llmService,
        IProblemsService problemsService,
        IOptions<PromptConfiguration> prompts,
        IOptions<LlmServiceConfiguration> llmConfig,
        ILogger<ChatService> logger)
    {
        _dbContext = dbContext;
        _llmService = llmService;
        _problemsService = problemsService;
        _prompts = prompts;
        _llmConfig = llmConfig;
        _logger = logger;
    }

    public async Task<Chat> Create(Chat chat, CancellationToken ct)
    {
        chat.Type = ChatType.Chat;
        var res = await _dbContext.Chats.AddAsync(chat, ct);
        await _dbContext.Messages.AddAsync(
            new Message(
                res.Entity,
                _prompts.Value.DefaultSystemPrompt,
                MessageType.System)
            , ct);

        await _dbContext.SaveChangesAsync(ct);
        return res.Entity;
    }

    public async Task<Chat> Create(Chat chat, string problemDbId, TaskType explicitTaskType, CancellationToken ct)
    {
        chat.Type = ChatType.ProblemSolver;
        var problemFromDb = await _problemsService.GetProblemFromDbAsync(problemDbId, ct);

        if (problemFromDb == null)
        {
            _logger.LogError("Problem with ID {ProblemDbId} not found in LLMath-Problems database.", problemDbId);
            throw new KeyNotFoundException($"Problem with ID {problemDbId} not found in LLMath-Problems database.");
        }

        string problemCondition = problemFromDb.Statement;
        string? llmSolution = problemFromDb.LlmSolution is string sol && !string.IsNullOrWhiteSpace(sol)
            ? sol
            : problemFromDb.LlmSolution?.ToString();

        if (string.IsNullOrWhiteSpace(llmSolution))
        {
            _logger.LogWarning("Problem {ProblemDbId} has no LLM solution. Tutor solution will not be included.", problemDbId);
        }

        var addedChatEntityEntry = await _dbContext.Chats.AddAsync(chat, ct);
        await _dbContext.SaveChangesAsync(ct);
        var newChatEntity = addedChatEntityEntry.Entity;

        string systemPromptText = GetSystemPromptByTaskType(explicitTaskType);

        var userTask = await _dbContext.UserTasks
            .FirstOrDefaultAsync(ut => ut.ProblemHash == problemDbId &&
                                   ut.ApplicationUserId == chat.UserId &&
                                   ut.Status == UserTaskStatus.InProgress, ct);
        if (userTask != null)
        {
            userTask.AssociatedChatId = newChatEntity.Id;
            _dbContext.UserTasks.Update(userTask);
        }

        var systemMessage = new Message(newChatEntity, systemPromptText, MessageType.System);
        Message? solutionMessageForLlm = null;
        if (explicitTaskType != TaskType.Exam && !string.IsNullOrWhiteSpace(llmSolution))
        {
            var tutorSolutionText = _prompts.Value.TutorSolutionPrompt.Replace("{solution}", llmSolution);
            solutionMessageForLlm = new Message(newChatEntity, tutorSolutionText, MessageType.User, isSystemPrompt: true);
        }

        var fixedCondition = problemCondition.Replace("\r\n", "\\\\").Replace("\n", "\\\\");
        var conditionTextForDisplay = $"**Условие задачи:** ({problemFromDb.Id})<br/><br/>\n\n{fixedCondition}\n\n";
        var conditionMessageForDisplay = new Message(newChatEntity, conditionTextForDisplay, MessageType.Assistant);

        var messagesToSaveInDb = new List<Message> { systemMessage, conditionMessageForDisplay };
        if (solutionMessageForLlm != null)
        {
            messagesToSaveInDb.Add(solutionMessageForLlm);
        }
        await _dbContext.Messages.AddRangeAsync(messagesToSaveInDb, ct);

        await _dbContext.SaveChangesAsync(ct);

        var initialPromptForLlm = GetInitialPromptByTaskType(explicitTaskType);
        var messagesForInitialBotGeneration = new List<Message> { systemMessage };
        if (solutionMessageForLlm != null) messagesForInitialBotGeneration.Add(solutionMessageForLlm);
        messagesForInitialBotGeneration.Add(new Message(newChatEntity, problemCondition, MessageType.User, isSystemPrompt: true));
        messagesForInitialBotGeneration.Add(new Message(newChatEntity, initialPromptForLlm, MessageType.User, isSystemPrompt: true));

        var initialBotMessageText = await _llmService.GenerateNextMessageAsync(messagesForInitialBotGeneration, explicitTaskType, ct);

        var botInitialDisplayMessage = new Message(newChatEntity, initialBotMessageText, MessageType.Assistant);
        await _dbContext.Messages.AddAsync(botInitialDisplayMessage, ct);
        await _dbContext.SaveChangesAsync(ct);

        return newChatEntity;
    }

    public async Task<List<Chat>> GetUserChats(string userId, CancellationToken ct)
    {
        var chats = await _dbContext.Chats.Where(c => c.User.Id == userId).ToListAsync(cancellationToken: ct);
        return chats;
    }

    public async Task Delete(Chat chat, CancellationToken ct)
    {
        _dbContext.Chats.Remove(chat);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<string> CreateMessage(Message message, CancellationToken ct)
    {
        await _dbContext.Messages.AddAsync(message, ct);
        await _dbContext.SaveChangesAsync(ct);

        var currentChat = await _dbContext.Chats
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == message.ChatId, ct);

        if (currentChat == null)
        {
            _logger.LogError("Chat with ID {ChatId} not found in CreateMessage.", message.ChatId);
            return string.Empty;
        }

        TaskType taskType = await DetermineTaskTypeAsync(currentChat, ct);
        var messagesForLlm = currentChat.Messages.ToList();

        if (taskType == TaskType.Exam)
        {
            messagesForLlm.RemoveAll(m => m.IsSystemPrompt && m.Text.Contains("Вот правильное решение задачи"));
        }

        string llmResponseText = await _llmService.GenerateNextMessageAsync(messagesForLlm, taskType, ct);

        if (!string.IsNullOrEmpty(llmResponseText))
        {
            var botMessage = new Message(currentChat, llmResponseText, MessageType.Assistant);
            await _dbContext.Messages.AddAsync(botMessage, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
        else
        {
            _logger.LogWarning("LLM returned empty or null response for chat {ChatId}", currentChat.Id);
        }

        return llmResponseText;
    }

    private async Task<TaskType> DetermineTaskTypeAsync(Chat currentChat, CancellationToken ct)
    {
        TaskType taskType = TaskType.Tutor;

        if (currentChat.Type == ChatType.ProblemSolver)
        {
            var userTask = await _dbContext.UserTasks
               .FirstOrDefaultAsync(ut => ut.AssociatedChatId == currentChat.Id, ct);
            if (userTask != null)
            {
                taskType = userTask.TaskType;
            }
            else
            {
                var systemMessageInHistory = currentChat.Messages.FirstOrDefault(m => m.MessageType == MessageType.System);
                if (systemMessageInHistory != null)
                {
                    if (systemMessageInHistory.Text == _prompts.Value.LearningSystemPrompt) taskType = TaskType.Learning;
                    else if (systemMessageInHistory.Text == _prompts.Value.GuidedSystemPrompt) taskType = TaskType.Guided;
                    else if (systemMessageInHistory.Text == _prompts.Value.ExamSystemPrompt) taskType = TaskType.Exam;
                    else _logger.LogWarning("Could not determine taskType from system prompt for chat {ChatId}", currentChat.Id);
                }
                else
                {
                    _logger.LogWarning("No UserTask and no system prompt found to determine taskType for chat {ChatId}", currentChat.Id);
                }
            }
        }
        return taskType;
    }

    public async Task<List<Message>> GetAllMessageFromChat(Chat chat, CancellationToken ct)
    {
        return await _dbContext.Messages.Where(m => m.ChatId == chat.Id).OrderBy(m => m.CreatedAt).ToListAsync(ct);
    }

    public async Task<Chat?> GetChatById(Guid id, CancellationToken ct)
    {
        return await _dbContext.Chats
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken: ct);
    }

    public async Task<Guid> GetOrCreateProblemChatAsync(string problemHash, string userId, string taskDisplayName, TaskType taskType, CancellationToken ct)
    {
        var chatName = $"{taskDisplayName} {DateTime.Now:dd.MM.yyyy HH:mm}";
        var newChatEntity = new Chat
        {
            Name = chatName,
            UserId = userId,
            Type = ChatType.ProblemSolver
        };

        var createdChat = await Create(newChatEntity, problemHash, taskType, ct);
        return createdChat.Id;
    }

    public async Task<Message?> GetMessageId(Guid id, CancellationToken ct)
    {
        return await _dbContext.Messages.FirstOrDefaultAsync(c => c.Id == id, cancellationToken: ct);
    }

    private string GetSystemPromptByTaskType(TaskType taskType)
    {
        return taskType switch
        {
            TaskType.Learning => _prompts.Value.LearningSystemPrompt,
            TaskType.Guided => _prompts.Value.GuidedSystemPrompt,
            TaskType.Exam => _prompts.Value.ExamSystemPrompt,
            _ => _prompts.Value.TutorSystemPrompt
        };
    }

    private string GetInitialPromptByTaskType(TaskType taskType)
    {
        return taskType switch
        {
            TaskType.Learning => _prompts.Value.LearningInitialPrompt,
            TaskType.Guided => _prompts.Value.GuidedInitialPrompt,
            TaskType.Exam => _prompts.Value.ExamInitialPrompt,
            _ => _prompts.Value.TutorInitialPrompt
        };
    }
}
