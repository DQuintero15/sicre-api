using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sicre.Api.Config;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Shared;
using Sicre.Api.Shared.Email;

namespace Sicre.Api.Infrastructure.Persistence.Seeders;

public class AdminSeeder(
    ILogger<AdminSeeder> logger,
    UserManager<User> userManager,
    IPasswordService passwordService,
    IEmailService emailService,
    IEmailTemplateService emailTemplateService,
    IOptions<AppSettings> options,
    ApplicationDbContext db
)
{
    private readonly AppSettings _settings = options.Value;
    private const string AdminProcess = "Gerencia";
    private const string AdminPosition = "Líder de Gerencia";
    private const string AdminBranch = "Llanogas";

    public async Task SeedAsync()
    {
        if (await userManager.Users.AnyAsync())
        {
            logger.LogInformation("Ya existen usuarios. Se omite el seeder de admin.");
            return;
        }

        var email = _settings.Seed.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogError("Seed.Email no está configurado. Se omite el seeder de admin.");
            return;
        }

        var process = await db.Processes.FirstOrDefaultAsync(p => p.Name == AdminProcess);
        var position = await db.Positions.FirstOrDefaultAsync(p => p.Name == AdminPosition);
        var branch = await db.Branches.FirstOrDefaultAsync(b =>
            b.Name.ToLower() == AdminBranch.ToLower()
        );

        var admin = new User
        {
            Email = email,
            UserName = email,
            FirstName = "Admin",
            LastName = "SICRE",
            ProcessId = process?.Id,
            PositionId = position?.Id,
            BranchId = branch?.Id,
            IsActive = true,
            HasChangedDefaultPassword = false,
        };

        var password = passwordService.GenerateSecurePassword(15);
        var result = await userManager.CreateAsync(admin, password);

        if (!result.Succeeded)
        {
            logger.LogError(
                "Error al crear usuario admin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(admin, Role.Administrator.ToString());
        if (!roleResult.Succeeded)
        {
            logger.LogError(
                "Error al asignar rol Administrator: {Errors}",
                string.Join(", ", roleResult.Errors.Select(e => e.Description))
            );
        }

        logger.LogInformation("Usuario admin creado: {Email}", email);

        var body = emailTemplateService.GetInvitationEmailTemplate(
            $"{admin.FirstName} {admin.LastName}",
            password
        );
        await emailService.SendEmailAsync(
            email,
            "Tu cuenta de administrador ha sido creada — SICRE",
            body
        );

        logger.LogInformation("Credenciales de admin enviadas a {Email}", email);
    }
}
