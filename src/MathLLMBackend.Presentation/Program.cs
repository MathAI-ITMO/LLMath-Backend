using System.Text;
using MathLLMBackend.Core;
using MathLLMBackend.DataAccess;
using MathLLMBackend.DataAccess.Services;
using MathLLMBackend.GeolinClient;
using MathLLMBackend.GeolinClient.Options;
using MathLLMBackend.ProblemsClient;
using MathLLMBackend.ProblemsClient.Options;
using Microsoft.OpenApi.Models;
using MathLLMBackend.Presentation.Middlewares;
using NLog;
using NLog.Web;
using Microsoft.AspNetCore.Identity;
using MathLLMBackend.DataAccess.Contexts;
using MathLLMBackend.Domain.Configuration;
using MathLLMBackend.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var logger = LogManager.Setup()
    .LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddHttpLogging();

    var configuration = builder.Configuration;
    var corsConfiguration = configuration
        .GetSection(nameof(CorsConfiguration))
        .Get<CorsConfiguration>()
        ?? new CorsConfiguration();

    if (corsConfiguration.Enabled)
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
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
    GeolinClientRegistrar.Configure(
        builder.Services,
        configuration.GetSection(nameof(GeolinClientOptions)).Bind);
    ProblemsClientRegistrar.Configure(
        builder.Services,
        configuration.GetSection(nameof(ProblemsClientOptions)).Bind);
    DataAccessRegistrar.Configure(builder.Services, configuration);

    builder.Services.Configure<AdminConfiguration>(
        configuration.GetSection("AdminConfiguration"));

    // Identity setup (registers the cookie handler internally)
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // Configure cookie options (SameSite=None to allow cross-site fetches)
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.Name         = ".AspNetCore.Identity.Application";
        options.Cookie.HttpOnly     = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite     = SameSiteMode.None;
    });

    // Add Authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole",
            policy => policy.RequireRole("Admin"));
        options.AddPolicy("RequireUserRole",
            policy => policy.RequireRole("User", "Admin"));
    });

    // Authentication: cookie first, fallback to JWT
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme       = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = configuration["Jwt:Issuer"],
            ValidAudience            = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("JWT key not found")))
        };
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Swagger with cookie & bearer definitions
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "MathLLM API", Version = "v1" });

        c.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
        {
            Type        = SecuritySchemeType.ApiKey,
            In          = ParameterLocation.Cookie,
            Name        = ".AspNetCore.Identity.Application",
            Description = "Cookie authentication. Use /api/auth/login to get it.",
            Scheme      = "cookie"
        });

        c.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
        {
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            Description  = "JWT Bearer. Use /api/auth/login?useToken=true."
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "cookieAuth"
                    }
                },
                Array.Empty<string>()
            },
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "bearerAuth"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // Warmup & role initialization
    using (var scope = app.Services.CreateScope())
    {
        var warmupService = scope.ServiceProvider.GetRequiredService<WarmupService>();
        await warmupService.WarmupAsync();

        var roleInit = scope.ServiceProvider.GetRequiredService<RoleInitializationService>();
        await roleInit.InitializeRolesAsync();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MathLLM API v1");
            c.EnablePersistAuthorization();
        });
    }

    if (corsConfiguration.Enabled)
        app.UseCors();

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.UseHttpLogging();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}