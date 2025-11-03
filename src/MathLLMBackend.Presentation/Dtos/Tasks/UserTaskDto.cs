using MathLLMBackend.Domain.Enums;

namespace MathLLMBackend.Presentation.Dtos.Tasks;

public record UserTaskDto(
    Guid Id,
    string ProblemId,
    string DisplayName,
    TaskType TaskType,
    UserTaskStatus Status,
    Guid? AssociatedChatId
);

