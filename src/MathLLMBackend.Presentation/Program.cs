using MathLLMBackend.Core;
using MathLLMBackend.DataAccess;
using MathLLMBackend.DataAccess.Services;
using MathLLMBackend.GeolinClient;
using MathLLMBackend.GeolinClient.Options;
using MathLLMBackend.ProblemsClient;
using MathLLMBackend.ProblemsClient.Options;
using Microsoft.OpenApi.Models;
using MathLLMBackend.Presentation.Middlewares;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using NLog;
using NLog.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MathLLMBackend.DataAccess.Contexts;
using MathLLMBackend.Presentation.Configuration;
using MathLLMBackend.Domain.Entities;
using System.Threading;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    // Устанавливаем минимальное количество рабочих потоков и потоков завершения IOCP
    // Значения подбираются экспериментально. Например:
    int minWorkerThreads = 100; 
    int minCompletionPortThreads = 100; 
    ThreadPool.SetMinThreads(minWorkerThreads, minCompletionPortThreads);

    var builder = WebApplication.CreateBuilder(args);
    // Загружаем секретные настройки (не фиксированы в репозитории)
    builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);
    builder.Services.AddHttpLogging(o => { });
    var configuration = builder.Configuration;
    var corsConfiguration = configuration.GetSection(nameof(CorsConfiguration)).Get<CorsConfiguration>() ?? new CorsConfiguration();

    if (corsConfiguration.Enabled)
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(
                policy =>
                {
                    policy.WithOrigins(corsConfiguration.Origin.Split(';'))
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
        });
    }

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    CoreServicesRegistrar.Configure(builder.Services, configuration);
    GeolinClientRegistrar.Configure(builder.Services, configuration.GetSection(nameof(GeolinClientOptions)).Bind);
    ProblemsClientRegistrar.Configure(builder.Services, configuration.GetSection(nameof(ProblemsClientOptions)).Bind);
    DataAccessRegistrar.Configure(builder.Services, configuration);
    
    builder.Services.AddIdentityApiEndpoints<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
    })
        .AddEntityFrameworkStores<AppDbContext>();
    
    builder.Services.AddAuthorization();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
    
    
    
    builder.Services.AddSwaggerGen(c =>
        {
            var openApiSecurityScheme = new OpenApiSecurityScheme()
            {
                Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer [space] {your token}'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            };

            c.AddSecurityDefinition("Bearer", openApiSecurityScheme);

            var openApiSecurityRequirement = new OpenApiSecurityRequirement()
            {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header
                },
                new List<string>()
            }
            };

            c.AddSecurityRequirement(openApiSecurityRequirement);
        });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var warmupService = scope.ServiceProvider.GetRequiredService<WarmupService>();
        await warmupService.WarmupAsync();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (corsConfiguration.Enabled)
    {
        app.UseCors();
    }

    app.MapIdentityApi<ApplicationUser>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.UseHttpLogging();

    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}
