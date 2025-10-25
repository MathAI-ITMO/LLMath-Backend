namespace MathLLMBackend.Presentation.Dtos.Stats;

public class TaskItemDto
{
    public Guid UserTaskId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid? ChatId { get; set; }
    public int TaskType { get; set; }
}
