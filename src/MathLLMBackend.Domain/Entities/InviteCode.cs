using Microsoft.AspNetCore.Identity;

namespace MathLLMBackend.Domain.Entities;

public class InviteCode
{
    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public int MaxUsages { get; private set; }
    public int CurrentUsages { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string CreatedById { get; private set; }
    public ApplicationUser CreatedBy { get; private set; } = null!;
    public ICollection<ApplicationUser> UsedBy { get; private set; } = new List<ApplicationUser>();

    private InviteCode() { } // For EF Core

    public InviteCode(string code, int maxUsages, string createdById)
    {
        Id = Guid.NewGuid();
        Code = code;
        MaxUsages = maxUsages;
        CurrentUsages = 0;
        CreatedAt = DateTime.UtcNow;
        CreatedById = createdById;
    }

    public bool CanBeUsed => CurrentUsages < MaxUsages;

    public void Use(ApplicationUser user)
    {
        if (!CanBeUsed)
        {
            throw new InvalidOperationException("Invite code has reached its maximum usage limit");
        }

        if (user.UsedInviteCode != null)
        {
            throw new InvalidOperationException("User has already used an invite code");
        }

        CurrentUsages++;
        user.UsedInviteCode = this;
        user.UsedInviteCodeId = Id;
        UsedBy.Add(user);
    }
}