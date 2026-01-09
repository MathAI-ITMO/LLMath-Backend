using System.Security.Claims;
using MathLLMBackend.Core.Services.LlmService;
using MathLLMBackend.DataAccess.Contexts;
using MathLLMBackend.DataAccess.Services;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.GeolinClient;
using MathLLMBackend.GeolinClient.Options;
using MathLLMBackend.ProblemsClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MathLLMBackend.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "TestDb_" + Guid.NewGuid();
    
    public Mock<IGeolinApi> GeolinApiMock { get; } = new();
    public Mock<IProblemsAPI> ProblemsApiMock { get; } = new();
    public Mock<ILlmService> LlmServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptorsToRemove = services.Where(d => 
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))).ToList();
            
            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            var appDbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
            if (appDbContextDescriptor != null)
            {
                services.Remove(appDbContextDescriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
            });

            var geolinApiDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IGeolinApi));
            if (geolinApiDescriptor != null)
            {
                services.Remove(geolinApiDescriptor);
            }
            services.AddSingleton(GeolinApiMock.Object);

            var problemsApiDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IProblemsAPI));
            if (problemsApiDescriptor != null)
            {
                services.Remove(problemsApiDescriptor);
            }
            services.AddSingleton(ProblemsApiMock.Object);

            var llmServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ILlmService));
            if (llmServiceDescriptor != null)
            {
                services.Remove(llmServiceDescriptor);
            }
            services.AddSingleton(LlmServiceMock.Object);

            services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

            services.PostConfigure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 3;
            });

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });

            services.Configure<GeolinClientOptions>(options =>
            {
                options.BaseAddress = "http://test-geolin.com";
                options.AuthorizationHeader = "TestAuth";
            });

            var warmupServiceDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(WarmupService));
            if (warmupServiceDesc != null)
            {
                services.Remove(warmupServiceDesc);
            }
            services.AddScoped<WarmupService>(sp =>
            {
                var dbContext = sp.GetRequiredService<AppDbContext>();
                var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
                var logger = sp.GetRequiredService<ILogger<WarmupService>>();
                return new TestWarmupService(dbContext, roleManager, logger);
            });
        });

        builder.UseEnvironment("Testing");
    }

    public async Task<ApplicationUser> CreateTestUserAsync(string email = "test@example.com", string password = "Test123!@#")
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            StudentGroup = "TestGroup"
        };

        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, MathLLMBackend.Domain.Constants.RoleConstants.User);
        return user;
    }

    public HttpClient CreateAuthenticatedClient(ApplicationUser user)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", user.Id.ToString());
        return client;
    }
}
