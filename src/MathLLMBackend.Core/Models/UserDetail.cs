namespace MathLLMBackend.Core.Models;

public class UserDetail
{
    public List<TaskItem> SolvedTasks { get; set; } = new();
    public List<TaskItem> InProgressTasks { get; set; } = new();
    public List<ChatItem> Chats { get; set; } = new();
}

public class TaskItem
{
    public Guid UserTaskId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid? ChatId { get; set; }
    public int TaskType { get; set; }
}

public class ChatItem
{
    public Guid ChatId { get; set; }
    public string Name { get; set; } = string.Empty;
}
