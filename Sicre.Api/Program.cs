using System.Runtime.InteropServices;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Sicre.Api.Config;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Features.Auth.Services;
using Sicre.Api.Features.Branches.Services;
using Sicre.Api.Features.ControlEntities.Services;
using Sicre.Api.Features.Positions.Services;
using Sicre.Api.Features.Processes.Services;
using Sicre.Api.Features.Roles.Services;
using Sicre.Api.Features.TwoFactor.Services;
using Sicre.Api.Features.Users.Services;
using Sicre.Api.Infrastructure.Hangfire;
using Sicre.Api.Infrastructure.Jobs;
using Sicre.Api.Infrastructure.Middleware;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Infrastructure.Persistence.Seeders;
using Sicre.Api.Shared;
using Sicre.Api.Shared.Email;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration);
var appSettings = builder.Configuration.Get<AppSettings>()!;

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Hangfire
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(
        options =>
            options.UseNpgsqlConnection(
                builder.Configuration.GetConnectionString("DefaultConnection")
            ),
        new PostgreSqlStorageOptions { QueuePollInterval = TimeSpan.FromSeconds(15) }
    )
);
builder.Services.AddHangfireServer(options => options.WorkerCount = 5);

// Identity
builder
    .Services.AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(appSettings.Jwt.SecretKey)
            ),
            ValidateIssuer = true,
            ValidIssuer = appSettings.Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = appSettings.Jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins([.. appSettings.Cors.AllowedOrigins])
            .WithMethods([.. appSettings.Cors.AllowedMethods])
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Data Protection (para 2FA secrets)
builder.Services.AddDataProtection();

// Shared utilities
builder.Services.AddSingleton<IDateHelper, DateHelper>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddHttpContextAccessor();

// Email
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();

// Auth feature services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICookieService, CookieService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// TwoFactor feature services
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();

// Users feature services
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IUserService, UserService>();

// Roles feature services
builder.Services.AddScoped<IRoleService, RoleService>();

// Catalog feature services
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<IProcessService, ProcessService>();
builder.Services.AddScoped<IControlEntityService, ControlEntityService>();

// Job services
builder.Services.AddScoped<IMaintenanceJobService, MaintenanceJobService>();

// Seeders con dependencias
builder.Services.AddScoped<AdminSeeder>();

// Controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Migrate and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await RoleSeeder.SeedAsync(roleManager);
    await BranchSeeder.SeedAsync(db);
    await ControlEntitySeeder.SeedAsync(db);
    await ProcessSeeder.SeedAsync(db);
    await PositionSeeder.SeedAsync(db);
    await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync();
    await SICRESettingsSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthMiddleware();
app.UseAuthorization();
app.MapControllers();

// Hangfire dashboard (solo Administrador)
app.UseHangfireDashboard(
    "/hangfire",
    new DashboardOptions { Authorization = [new HangfireAuthorizationFilter()] }
);

// Recurring jobs
var colombiaZone = TimeZoneInfo.FindSystemTimeZoneById(
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "SA Pacific Standard Time"
        : "America/Bogota"
);

RecurringJob.AddOrUpdate<IMaintenanceJobService>(
    "refresh-token-cleanup",
    service => service.CleanupRefreshTokensAsync(),
    "0 3 * * *",
    new RecurringJobOptions { TimeZone = colombiaZone }
);

app.Run();
