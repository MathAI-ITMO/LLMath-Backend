using MathLLMBackend.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace MathLLMBackend.Domain.Entities;

public class Chat
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string UserId { get; private set; }
    public ApplicationUser User { get; private set; } = null!;
    public ChatType Type { get; set; }
    public ICollection<Message> Messages { get; private set; } = new List<Message>();

    private Chat() { } // For EF Core

    public Chat(string name, string userId)
    {
        Id = Guid.NewGuid();
        Name = name;
        UserId = userId;
        Type = ChatType.Chat;
    }
}
