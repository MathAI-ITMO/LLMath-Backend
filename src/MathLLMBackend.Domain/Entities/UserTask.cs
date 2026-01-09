using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MathLLMBackend.Domain.Enums;

namespace MathLLMBackend.Domain.Entities;

public class UserTask
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = null!;
    [ForeignKey(nameof(ApplicationUserId))]
    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Required]
    [MaxLength(512)]
    public string ProblemId { get; set; } = null!;

    [Required]
    [MaxLength(1024)]
    public string DisplayName { get; set; } = null!;

    [Required]
    public int TaskType { get; set; }

    [Required]
    public UserTaskStatus Status { get; set; }

    public Guid? AssociatedChatId { get; set; }

    public string ProblemHash { get; set; } = null!;

    public UserTask()
    {
        Id = Guid.NewGuid();
        Status = UserTaskStatus.NotStarted;
    }
} 