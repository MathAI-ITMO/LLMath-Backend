using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MathLLMBackend.Core.Services.InviteCodeService;
using MathLLMBackend.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using MathLLMBackend.Domain.Entities;

namespace MathLLMBackend.Presentation.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "RequireAdminRole")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;
    private readonly IInviteCodeService _inviteCodeService;
    private readonly AppDbContext _dbContext;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger,
        IInviteCodeService inviteCodeService,
        AppDbContext dbContext)
    {
        _userManager = userManager;
        _logger = logger;
        _inviteCodeService = inviteCodeService;
        _dbContext = dbContext;
    }

    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers()
    {
        // Get all users with their invite code usage
        var usersWithInviteCodes = await _dbContext.Users
            .Include(u => u.UsedInviteCode)
            .ThenInclude(ic => ic.CreatedBy)
            .ToListAsync();

        var userDtos = new List<object>();

        foreach (var user in usersWithInviteCodes)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var usedInviteCode = user.UsedInviteCode == null ? null : new
            {
                user.UsedInviteCode.Id,
                user.UsedInviteCode.Code,
                user.UsedInviteCode.CreatedAt,
                CreatedBy = new { user.UsedInviteCode.CreatedBy.Id, user.UsedInviteCode.CreatedBy.Email }
            };

            userDtos.Add(new
            {
                user.Id,
                user.Email,
                user.EmailConfirmed,
                user.LockoutEnabled,
                user.LockoutEnd,
                Roles = roles,
                UsedInviteCode = usedInviteCode
            });
        }

        return Ok(userDtos);
    }

    [HttpPost("users/promote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PromoteToAdmin([FromQuery] string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return BadRequest("User not found");
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return BadRequest("User is already an admin");
        }

        var result = await _userManager.AddToRoleAsync(user, "Admin");
        if (result.Succeeded)
        {
            return Ok();
        }

        return BadRequest(result.Errors);
    }

    public record CreateInviteCodeRequest(string Code, int MaxUsages);

    [HttpPost("invite-codes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateInviteCode([FromBody] CreateInviteCodeRequest request)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            var inviteCode = await _inviteCodeService.CreateInviteCodeAsync(
                request.Code,
                request.MaxUsages,
                currentUser.Id);

            return Ok(new
            {
                inviteCode.Id,
                inviteCode.Code,
                inviteCode.MaxUsages,
                inviteCode.CurrentUsages,
                inviteCode.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("invite-codes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllInviteCodes()
    {
        var inviteCodes = await _inviteCodeService.GetAllInviteCodesAsync();
        return Ok(inviteCodes.Select(ic => new
        {
            ic.Id,
            ic.Code,
            ic.MaxUsages,
            ic.CurrentUsages,
            ic.CreatedAt,
            CreatedBy = new { ic.CreatedBy.Id, ic.CreatedBy.Email },
            UsedBy = ic.UsedBy.Select(u => new { u.Id, u.Email })
        }));
    }

    [HttpDelete("invite-codes/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteInviteCode(Guid id)
    {
        await _inviteCodeService.DeleteInviteCodeAsync(id);
        return Ok();
    }
} 