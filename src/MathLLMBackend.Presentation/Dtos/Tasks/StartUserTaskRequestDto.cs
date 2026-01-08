using System.ComponentModel.DataAnnotations;

namespace MathLLMBackend.Presentation.Dtos.Tasks; // Изменено пространство имен

public record StartUserTaskRequestDto(
    [Required] Guid ChatId
); 