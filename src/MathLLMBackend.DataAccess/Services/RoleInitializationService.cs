using MathLLMBackend.Domain.Configuration;
using MathLLMBackend.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MathLLMBackend.DataAccess.Services;

public class RoleInitializationService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AdminConfiguration _adminConfig;

    public RoleInitializationService(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<AdminConfiguration> adminConfig)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _adminConfig = adminConfig.Value;
    }

    public async Task InitializeRolesAsync()
    {
        var roles = new[] { "User", "Admin" };
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminUser = await _userManager.FindByEmailAsync(_adminConfig.Email);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = _adminConfig.Email,
                Email = _adminConfig.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(adminUser, _adminConfig.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await _userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
} 