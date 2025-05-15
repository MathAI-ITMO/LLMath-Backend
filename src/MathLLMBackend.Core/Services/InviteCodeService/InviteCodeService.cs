using MathLLMBackend.DataAccess.Contexts;
using MathLLMBackend.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MathLLMBackend.Core.Services.InviteCodeService;

public interface IInviteCodeService
{
    Task<InviteCode> CreateInviteCodeAsync(string code, int maxUsages, string createdById, CancellationToken ct = default);
    Task<InviteCode?> GetInviteCodeByCodeAsync(string code, CancellationToken ct = default);
    Task<List<InviteCode>> GetAllInviteCodesAsync(CancellationToken ct = default);
    Task DeleteInviteCodeAsync(Guid id, CancellationToken ct = default);
    Task<bool> UseInviteCodeAsync(string code, ApplicationUser user, CancellationToken ct = default);
}

public class InviteCodeService : IInviteCodeService
{
    private readonly AppDbContext _context;

    public InviteCodeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<InviteCode> CreateInviteCodeAsync(string code, int maxUsages, string createdById, CancellationToken ct = default)
    {
        var inviteCode = new InviteCode(code, maxUsages, createdById);
        _context.InviteCodes.Add(inviteCode);
        await _context.SaveChangesAsync(ct);
        return inviteCode;
    }

    public async Task<InviteCode?> GetInviteCodeByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _context.InviteCodes
            .Include(ic => ic.CreatedBy)
            .Include(ic => ic.UsedBy)
            .FirstOrDefaultAsync(ic => ic.Code == code, ct);
    }

    public async Task<List<InviteCode>> GetAllInviteCodesAsync(CancellationToken ct = default)
    {
        return await _context.InviteCodes
            .Include(ic => ic.CreatedBy)
            .Include(ic => ic.UsedBy)
            .ToListAsync(ct);
    }

    public async Task DeleteInviteCodeAsync(Guid id, CancellationToken ct = default)
    {
        var inviteCode = await _context.InviteCodes
            .Include(ic => ic.UsedBy)
            .FirstOrDefaultAsync(ic => ic.Id == id, ct);

        if (inviteCode != null)
        {
            if (inviteCode.UsedBy.Any())
            {
                throw new InvalidOperationException("Cannot delete invite code that has been used by users");
            }

            _context.InviteCodes.Remove(inviteCode);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> UseInviteCodeAsync(string code, ApplicationUser user, CancellationToken ct = default)
    {
        var inviteCode = await GetInviteCodeByCodeAsync(code, ct);
        if (inviteCode == null || !inviteCode.CanBeUsed)
        {
            return false;
        }

        try
        {
            inviteCode.Use(user);
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}