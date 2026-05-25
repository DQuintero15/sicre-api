using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sicre.Api.Config;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Features.Users.Dtos;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Users.Services;

public interface IUserCsvImportService
{
    Task<ApiResponse<ImportUsersFromCsvResponseDto>> ImportUsersAsync(IFormFile file);
}

public class UserCsvImportService(
    ApplicationDbContext db,
    UserManager<User> userManager,
    IUserCsvParserService userCsvParserService,
    IPasswordService passwordService,
    IWebHostEnvironment env,
    IOptions<AppSettings> options,
    ILogger<UserCsvImportService> logger
) : IUserCsvImportService
{
    private const string DefaultRole = "ReportResponsible";
    private const string FallbackDevPassword = "Sicre123*";

    private static readonly EmailAddressAttribute EmailValidator = new();

    public async Task<ApiResponse<ImportUsersFromCsvResponseDto>> ImportUsersAsync(IFormFile file)
    {
        try
        {
            if (file.Length == 0)
                return ApiResponse<ImportUsersFromCsvResponseDto>.Fail(
                    HttpStatusCode.BadRequest,
                    "El archivo CSV está vacío."
                );

            var parsedCsv = await userCsvParserService.ParseAsync(file);
            if (!parsedCsv.IsValid)
                return ApiResponse<ImportUsersFromCsvResponseDto>.Fail(
                    parsedCsv.StatusCode,
                    parsedCsv.ErrorMessage
                );

            var processMap = await LoadProcessMapAsync();
            var positionMap = await LoadPositionMapAsync();
            var branchMap = await LoadBranchMapAsync();

            var response = new ImportUsersFromCsvResponseDto
            {
                TotalRows = parsedCsv.Rows.Count,
            };

            foreach (var row in parsedCsv.Rows)
            {
                var rowResult = await ProcessRowAsync(row, processMap, positionMap, branchMap);
                response.Rows.Add(rowResult);

                if (!rowResult.Success)
                {
                    response.FailedRows++;
                    continue;
                }

                response.ProcessedRows++;
                if (rowResult.Created)
                    response.CreatedUsers++;
                else
                    response.UpdatedUsers++;
            }

            return ApiResponse<ImportUsersFromCsvResponseDto>.Ok(
                response,
                $"Importación completada. {response.CreatedUsers} creados, {response.UpdatedUsers} actualizados, {response.FailedRows} fallidos."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inesperado al importar usuarios por CSV");
            return ApiResponse<ImportUsersFromCsvResponseDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error inesperado durante la importación CSV."
            );
        }
    }

    private async Task<ImportUsersFromCsvRowResultDto> ProcessRowAsync(
        ParsedUserCsvRow row,
        Dictionary<string, Process> processMap,
        Dictionary<string, Position> positionMap,
        Dictionary<string, Guid> branchMap
    )
    {
        var email = UserCsvParserService.CleanValue(row.Email);

        var rowResult = new ImportUsersFromCsvRowResultDto
        {
            RowNumber = row.RowNumber,
            Email = email,
        };

        try
        {
            if (string.IsNullOrWhiteSpace(email) || !EmailValidator.IsValid(email))
            {
                rowResult.Message = "Email inválido.";
                return rowResult;
            }

            var normalizedEmail = email.ToLowerInvariant();
            rowResult.Email = normalizedEmail;

            var fullName = UserCsvParserService.CleanValue(row.Name);
            if (string.IsNullOrWhiteSpace(fullName))
            {
                rowResult.Message = "Nombre vacío.";
                return rowResult;
            }

            var (firstName, lastName) = SplitName(fullName);
            var process = await ResolveProcessAsync(
                UserCsvParserService.CleanValue(row.ProcessName),
                processMap
            );
            var position = await ResolvePositionAsync(
                UserCsvParserService.CleanValue(row.PositionName),
                positionMap
            );
            var branchId = ResolveBranchId(row.BranchName, branchMap);

            // Single role: use parsed role or fall back to default
            var roleName = string.IsNullOrWhiteSpace(row.DesiredRole) ? DefaultRole : row.DesiredRole;

            var existingUser = await userManager.FindByEmailAsync(normalizedEmail);

            if (existingUser == null)
            {
                var newUser = new User
                {
                    Email = normalizedEmail,
                    UserName = normalizedEmail,
                    FirstName = firstName,
                    LastName = lastName,
                    ProcessId = process?.Id,
                    PositionId = position?.Id,
                    BranchId = branchId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    HasChangedDefaultPassword = false,
                };

                var password = GetImportPassword();
                var createResult = await userManager.CreateAsync(newUser, password);
                if (!createResult.Succeeded)
                {
                    rowResult.Message = GetIdentityErrors(createResult.Errors);
                    return rowResult;
                }

                var addRoleResult = await userManager.AddToRoleAsync(newUser, roleName);
                if (!addRoleResult.Succeeded)
                {
                    rowResult.Message = GetIdentityErrors(addRoleResult.Errors);
                    return rowResult;
                }

                rowResult.Success = true;
                rowResult.Created = true;
                rowResult.RoleAdded = roleName;
                rowResult.Message = "Usuario creado correctamente.";
                return rowResult;
            }

            // Update existing user
            existingUser.FirstName = firstName;
            existingUser.LastName = lastName;
            existingUser.Email = normalizedEmail;
            existingUser.UserName = normalizedEmail;
            existingUser.ProcessId = process?.Id;
            existingUser.PositionId = position?.Id;
            existingUser.BranchId = branchId;

            var updateResult = await userManager.UpdateAsync(existingUser);
            if (!updateResult.Succeeded)
            {
                rowResult.Message = GetIdentityErrors(updateResult.Errors);
                return rowResult;
            }

            // Sync role: replace if different
            var currentRoles = await userManager.GetRolesAsync(existingUser);
            if (!currentRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
            {
                if (currentRoles.Count > 0)
                    await userManager.RemoveFromRolesAsync(existingUser, currentRoles);

                var addRoleResult = await userManager.AddToRoleAsync(existingUser, roleName);
                if (!addRoleResult.Succeeded)
                {
                    rowResult.Message = GetIdentityErrors(addRoleResult.Errors);
                    return rowResult;
                }

                rowResult.RoleAdded = roleName;
            }

            rowResult.Success = true;
            rowResult.Created = false;
            rowResult.Message = "Usuario actualizado correctamente.";
            return rowResult;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error procesando fila {RowNumber} del CSV", row.RowNumber);
            rowResult.Message = "Error inesperado al procesar la fila.";
            return rowResult;
        }
    }

    private async Task<Dictionary<string, Process>> LoadProcessMapAsync()
    {
        var processes = await db.Processes.AsNoTracking().ToListAsync();

        return processes
            .Select(p => new { Process = p, Key = UserCsvParserService.NormalizeKey(p.Name) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.First().Process);
    }

    private async Task<Dictionary<string, Position>> LoadPositionMapAsync()
    {
        var positions = await db.Positions.AsNoTracking().ToListAsync();

        return positions
            .Select(p => new { Position = p, Key = UserCsvParserService.NormalizeKey(p.Name) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.First().Position);
    }

    private async Task<Dictionary<string, Guid>> LoadBranchMapAsync()
    {
        var branches = await db.Branches.AsNoTracking().ToListAsync();

        return branches
            .Select(b => new { Branch = b, Key = UserCsvParserService.NormalizeKey(b.Name) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.First().Branch.Id);
    }

    private static Guid? ResolveBranchId(string? branchName, Dictionary<string, Guid> branchMap)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            return null;

        var key = UserCsvParserService.NormalizeKey(branchName);
        return branchMap.TryGetValue(key, out var id) ? id : null;
    }

    private async Task<Process?> ResolveProcessAsync(
        string? processName,
        Dictionary<string, Process> processMap
    )
    {
        if (string.IsNullOrWhiteSpace(processName))
            return null;

        var sanitized = UserCsvParserService.CleanValue(processName);
        var normalized = UserCsvParserService.NormalizeKey(sanitized);

        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        if (processMap.TryGetValue(normalized, out var existing))
            return existing;

        var process = new Process { Name = sanitized };
        db.Processes.Add(process);
        await db.SaveChangesAsync();

        processMap[normalized] = process;
        return process;
    }

    private async Task<Position?> ResolvePositionAsync(
        string? positionName,
        Dictionary<string, Position> positionMap
    )
    {
        if (string.IsNullOrWhiteSpace(positionName))
            return null;

        var sanitized = UserCsvParserService.CleanValue(positionName);
        var normalized = UserCsvParserService.NormalizeKey(sanitized);

        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        if (positionMap.TryGetValue(normalized, out var existing))
            return existing;

        var position = new Position { Name = sanitized };
        db.Positions.Add(position);
        await db.SaveChangesAsync();

        positionMap[normalized] = position;
        return position;
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var tokens = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(UserCsvParserService.CleanValue)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        if (tokens.Length == 0)
            return ("Usuario", "SinApellido");

        if (tokens.Length == 1)
            return (LimitLength(tokens[0]), LimitLength(tokens[0]));

        if (tokens.Length == 2)
            return (LimitLength(tokens[0]), LimitLength(tokens[1]));

        if (tokens.Length == 3)
            return (LimitLength($"{tokens[0]} {tokens[1]}"), LimitLength(tokens[2]));

        if (tokens.Length == 4)
            return (
                LimitLength($"{tokens[0]} {tokens[1]}"),
                LimitLength($"{tokens[2]} {tokens[3]}")
            );

        // For names with more than 4 tokens: last two tokens are surnames, rest are first names.
        var firstName = string.Join(' ', tokens.Take(tokens.Length - 2));
        var lastName = string.Join(' ', tokens.Skip(tokens.Length - 2));
        return (LimitLength(firstName), LimitLength(lastName));
    }

    private static string GetIdentityErrors(IEnumerable<IdentityError> errors)
    {
        var list = errors.Select(x => x.Description).ToList();
        return list.Count == 0 ? "Operación rechazada por Identity." : string.Join(", ", list);
    }

    private static string LimitLength(string value)
    {
        const int maxLength = 50;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private string GetImportPassword()
    {
        if (!env.IsDevelopment())
            return passwordService.GenerateSecurePassword(15);

        var configured = options.Value.Seed.DefaultUserPassword;
        return !string.IsNullOrWhiteSpace(configured) ? configured : FallbackDevPassword;
    }
}
