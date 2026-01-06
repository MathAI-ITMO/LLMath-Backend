using MathLLMBackend.Core.Constants;
using MathLLMBackend.Core.Services.LlmService;
using MathLLMBackend.Core.Services.PromptService;
using MathLLMBackend.DataAccess.Contexts;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using MathLLMBackend.Core.Services.ProblemsService;
using MathLLMBackend.ProblemsClient.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MathLLMBackend.Core.Services.ChatService;

public class ChatService : IChatService
{
    private readonly AppDbContext _dbContext;
    private readonly ILlmService _llmService;
    private readonly IProblemsService _problemsService;
    private readonly IPromptService _promptService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        AppDbContext dbContext, 
        ILlmService llmService, 
        IProblemsService problemsService,
        IPromptService promptService,
        ILogger<ChatService> logger)
    {
        _dbContext = dbContext;
        _llmService = llmService;
        _problemsService = problemsService;
        _promptService = promptService;
        _logger = logger;
    }
    
    public async Task<Chat> Create(Chat chat, CancellationToken ct)
    {
        chat.Type = ChatType.Chat;
        var chatEntry = await _dbContext.Chats.AddAsync(chat, ct);
        
        var systemMessage = new Message(
            chatEntry.Entity,
            _promptService.GetDefaultSystemPrompt(),
            MessageType.System);
        
        await _dbContext.Messages.AddAsync(systemMessage, ct);
        await _dbContext.SaveChangesAsync(ct);
        
        return chatEntry.Entity;
    }

    public async Task<Chat> Create(Chat chat, string problemDbId, int explicitTaskType, CancellationToken ct)
    {
        chat.Type = ChatType.ProblemSolver;
        _logger.LogInformation("Creating chat for ProblemSolver. ProblemDB_ID: {ProblemDbId}, ExplicitTaskType: {ExplicitTaskType}", 
            problemDbId, explicitTaskType);

        var problem = await GetProblemOrThrowAsync(problemDbId, ct);
        var llmSolution = ExtractLlmSolution(problem);
        
        var newChat = await CreateChatEntityAsync(chat, ct);
        await AssociateUserTaskIfExistsAsync(newChat, problemDbId, chat.UserId, ct);
        
        var messages = BuildInitialMessages(newChat, problem, llmSolution, explicitTaskType);
        await _dbContext.Messages.AddRangeAsync(messages, ct);
        await _dbContext.SaveChangesAsync(ct);
        
        var initialBotMessage = await GenerateInitialBotMessageAsync(newChat, problem, llmSolution, explicitTaskType, ct);
        await _dbContext.Messages.AddAsync(initialBotMessage, ct);
        await _dbContext.SaveChangesAsync(ct);
        
        return newChat;
    }

    private async Task<Problem> GetProblemOrThrowAsync(string problemDbId, CancellationToken ct)
    {
        var problem = await _problemsService.GetProblemFromDbAsync(problemDbId, ct);
        
        if (problem == null)
        {
            _logger.LogError("Problem with ID {ProblemDbId} not found in LLMath-Problems database.", problemDbId);
            throw new KeyNotFoundException($"Problem with ID {problemDbId} not found in LLMath-Problems database.");
        }

        var conditionSnippet = problem.Statement.Length > 50 
            ? problem.Statement.Substring(0, 50) + "..." 
            : problem.Statement;
        
        _logger.LogInformation("Using problem: {ProblemId}, Condition snippet: {ConditionSnippet}", 
            problem.Id, conditionSnippet);
        
        return problem;
    }

    private static string? ExtractLlmSolution(Problem problem)
    {
        return problem.LlmSolution is string sol && !string.IsNullOrWhiteSpace(sol)
            ? sol
            : problem.LlmSolution?.ToString();
    }

    private async Task<Chat> CreateChatEntityAsync(Chat chat, CancellationToken ct)
    {
        var chatEntry = await _dbContext.Chats.AddAsync(chat, ct);
        await _dbContext.SaveChangesAsync(ct);
        return chatEntry.Entity;
    }

    private async Task AssociateUserTaskIfExistsAsync(Chat chat, string problemDbId, string userId, CancellationToken ct)
    {
        var userTask = await _dbContext.UserTasks
            .FirstOrDefaultAsync(ut => ut.ProblemHash == problemDbId 
                && ut.ApplicationUserId == userId 
                && ut.Status == UserTaskStatus.InProgress, ct);
        
        if (userTask != null)
        {
            userTask.AssociatedChatId = chat.Id;
            _dbContext.UserTasks.Update(userTask);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    private List<Message> BuildInitialMessages(Chat chat, Problem problem, string? llmSolution, int taskType)
    {
        var systemPrompt = _promptService.GetSystemPromptByTaskType(taskType);
        var systemMessage = new Message(chat, systemPrompt, MessageType.System);
        
        var messages = new List<Message> { systemMessage };
        
        var formattedCondition = FormatProblemConditionForDisplay(problem);
        var conditionMessage = new Message(chat, formattedCondition, MessageType.Assistant);
        messages.Add(conditionMessage);
        
        if (taskType != TaskTypes.Exam && !string.IsNullOrWhiteSpace(llmSolution))
        {
            var tutorSolutionPrompt = _promptService.GetTutorSolutionPrompt(llmSolution);
            var solutionMessage = new Message(chat, tutorSolutionPrompt, MessageType.User, isSystemPrompt: true);
            messages.Add(solutionMessage);
        }
        
        return messages;
    }

    private static string FormatProblemConditionForDisplay(Problem problem)
    {
        var fixedCondition = problem.Statement.Replace("\r\n", "\\\\").Replace("\n", "\\\\");
        return $"**Условие задачи:** ({problem.Id})<br/><br/>\n\n{fixedCondition}\n\n";
    }

    private async Task<Message> GenerateInitialBotMessageAsync(
        Chat chat, 
        Problem problem, 
        string? llmSolution, 
        int taskType, 
        CancellationToken ct)
    {
        var systemPrompt = _promptService.GetSystemPromptByTaskType(taskType);
        var systemMessage = new Message(chat, systemPrompt, MessageType.System);
        
        var messagesForLlm = new List<Message> { systemMessage };
        
        if (taskType != TaskTypes.Exam && !string.IsNullOrWhiteSpace(llmSolution))
        {
            var tutorSolutionPrompt = _promptService.GetTutorSolutionPrompt(llmSolution);
            messagesForLlm.Add(new Message(chat, tutorSolutionPrompt, MessageType.User, isSystemPrompt: true));
        }
        
        messagesForLlm.Add(new Message(chat, problem.Statement, MessageType.User, isSystemPrompt: true));
        
        var initialPrompt = _promptService.GetInitialPromptByTaskType(taskType, problem.Statement, string.Empty);
        messagesForLlm.Add(new Message(chat, initialPrompt, MessageType.User, isSystemPrompt: true));

        _logger.LogInformation("Starting initial LLM generation for chat {ChatId} | taskType = {TaskType}", 
            chat.Id, taskType);
        
        var botMessageText = await _llmService.GenerateNextMessageAsync(messagesForLlm, taskType, ct);
        
        _logger.LogInformation("Initial bot message generated for chat {ChatId} | taskType = {TaskType}", 
            chat.Id, taskType);
        
        return new Message(chat, botMessageText ?? string.Empty, MessageType.Assistant);
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
            throw new KeyNotFoundException($"Chat with ID {message.ChatId} not found.");
        }

        int taskType = await DetermineTaskTypeAsync(currentChat, ct);

        _logger.LogInformation("Generating (full) response in chat {ChatId} | taskType = {TaskType}", currentChat.Id, taskType);
        
        var messagesForLlm = currentChat.Messages.ToList();
        
        if (taskType == TaskTypes.Exam)
        {
            messagesForLlm.RemoveAll(m => m.IsSystemPrompt && m.Text.Contains(MessageConstants.TutorSolutionMarker));
        }

        string llmResponseText = await _llmService.GenerateNextMessageAsync(messagesForLlm, taskType, ct);

        if (!string.IsNullOrEmpty(llmResponseText))
        {
            var botMessage = new Message(currentChat, llmResponseText, MessageType.Assistant);
            await _dbContext.Messages.AddAsync(botMessage, ct);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("LLM full response saved for chat {ChatId}. Length: {Length}", currentChat.Id, llmResponseText.Length);
        }
        else
        {
            _logger.LogWarning("LLM returned empty or null full response for chat {ChatId}", currentChat.Id);
        }
        
        return llmResponseText;
    }

    private async Task<int> DetermineTaskTypeAsync(Chat currentChat, CancellationToken ct)
    {
        int taskType = 0; 
        
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
                taskType = DetermineTaskTypeFromSystemPrompt(currentChat);
            }
        }
        
        return taskType;
    }

    private int DetermineTaskTypeFromSystemPrompt(Chat chat)
    {
        var systemMessage = chat.Messages.FirstOrDefault(m => m.MessageType == MessageType.System);
        
        if (systemMessage == null)
        {
            _logger.LogWarning("No system prompt found to determine taskType for chat {ChatId}", chat.Id);
            return TaskTypes.Default;
        }
        
        var systemPromptText = systemMessage.Text;
        
        if (systemPromptText == _promptService.GetLearningSystemPrompt())
            return TaskTypes.Learning;
        
        if (systemPromptText == _promptService.GetGuidedSystemPrompt())
            return TaskTypes.Guided;
        
        if (systemPromptText == _promptService.GetExamSystemPrompt())
            return TaskTypes.Exam;
        
        _logger.LogWarning("Could not determine taskType from system prompt for chat {ChatId}", chat.Id);
        return TaskTypes.Default;
    }
    
    public async Task<List<Message>> GetAllMessageFromChat(Chat chat, CancellationToken ct)
    {
        return await _dbContext.Messages
            .Where(m => m.ChatId == chat.Id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Chat?> GetChatById(Guid id, CancellationToken ct)
    {
        var chat = await _dbContext.Chats
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken: ct);
        
        if (chat != null)
        {
            await _dbContext.Entry(chat)
                .Reference(c => c.User)
                .LoadAsync(ct);
        }
        
        return chat;
    }

    public async Task<Guid> GetOrCreateProblemChatAsync(string problemHash, string userId, string taskDisplayName, int taskType, CancellationToken ct)
    {
        var chatName = $"{taskDisplayName} {DateTime.Now:dd.MM.yyyy HH:mm}";
        var newChat = new Chat
        {
            Name = chatName,
            UserId = userId,
            Type = ChatType.ProblemSolver
        };

        var createdChat = await Create(newChat, problemHash, taskType, ct);
        return createdChat.Id;
    }

    public async Task<ChatDetails> GetChatDetailsAsync(Guid chatId, CancellationToken ct)
    {
        var chat = await GetChatById(chatId, ct);
        
        if (chat == null || chat.Type != ChatType.ProblemSolver)
        {
            return new ChatDetails(null, null);
        }

        var userTask = await _dbContext.UserTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(ut => ut.AssociatedChatId == chatId, ct);
        
        if (userTask == null)
        {
            return new ChatDetails(null, null);
        }

        int? taskType = userTask.TaskType;
        string? theoryLink = null;

        try
        {
            var problem = await _problemsService.GetProblemFromDbAsync(userTask.ProblemId, ct);
            theoryLink = problem?.TheoryLink;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get theory link for problem {ProblemId}", userTask.ProblemId);
        }

        return new ChatDetails(taskType, theoryLink);
    }
}
