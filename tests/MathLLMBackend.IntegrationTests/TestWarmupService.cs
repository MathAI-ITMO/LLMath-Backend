using MathLLMBackend.DataAccess.Contexts;
using MathLLMBackend.DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MathLLMBackend.IntegrationTests;

internal class TestWarmupService : WarmupService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<WarmupService> _logger;

    public TestWarmupService(AppDbContext dbContext, ILogger<WarmupService> logger) 
        : base(dbContext, logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public override async Task WarmupAsync()
    {
        _logger.LogInformation("Starting database warmup...");
        
        var isInMemory = _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        if (isInMemory)
        {
            await _dbContext.Database.EnsureCreatedAsync();
            _logger.LogInformation("Database warmup completed successfully (InMemory)");
        }
        else
        {
            await base.WarmupAsync();
        }
    }
}
