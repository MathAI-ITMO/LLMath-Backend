using Microsoft.AspNetCore.Identity;

namespace MathLLMBackend.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public InviteCode? UsedInviteCode { get; set; }
    public Guid? UsedInviteCodeId { get; set; }
}