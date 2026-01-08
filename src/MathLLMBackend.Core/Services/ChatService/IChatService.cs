using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Exceptions;

namespace MathLLMBackend.Core.Services.ChatService;

public interface IChatService
{
    Task<Chat> Create(Chat chat, CancellationToken ct);
    Task<Chat> Create(Chat chat, string problemHash, int explicitTaskType, CancellationToken ct);
    Task<Guid> GetOrCreateProblemChatAsync(string problemHash, string userId, string taskDisplayName, int taskType, CancellationToken ct);
    Task Delete(Chat chat, CancellationToken ct);
    Task DeleteChat(Guid chatId, string userId, CancellationToken ct);
    Task<List<Chat>> GetUserChats(string userId, CancellationToken ct);
    Task<string> CreateMessage(Message message, CancellationToken ct);
    Task<string> CreateMessageForUser(Guid chatId, string userId, string text, CancellationToken ct);
    Task<List<Message>> GetAllMessageFromChat(Chat chat, CancellationToken ct);
    Task<List<Message>> GetAllMessagesFromChatForUser(Guid chatId, string userId, CancellationToken ct);
    Task<List<Message>> GetUserVisibleMessagesFromChat(Guid chatId, string userId, CancellationToken ct);
    Task<Chat?> GetChatById(Guid id, CancellationToken ct);
    Task<Chat> GetChatByIdForUser(Guid chatId, string userId, CancellationToken ct);
    Task<ChatDetails> GetChatDetailsAsync(Guid chatId, string userId, CancellationToken ct);
}

public record ChatDetails(int? TaskType, string? TheoryLink);
