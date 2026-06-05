using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.Users.Dtos;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;
using Sicre.Api.Shared.Email;
using Sicre.Api.Shared.Extensions;

namespace Sicre.Api.Features.Users.Services;

public interface IUserService
{
    Task<ApiResponse<PagedResult<UserDto>>> GetAllAsync(UserFilterDto filter);
    Task<ApiResponse<UserDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<UserDto>> CreateAsync(CreateUserDto dto);
    Task<ApiResponse<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto);
    Task<ApiResponse<bool>> ResendTemporaryPasswordAsync(Guid id);
    Task<ApiResponse<ResendTemporaryPasswordBulkResponseDto>> ResendTemporaryPasswordBulkAsync();
}

public class UserService(
    ILogger<UserService> logger,
    UserManager<User> userManager,
    IPasswordService passwordService,
    IEmailService emailService,
    IEmailTemplateService emailTemplateService,
    ApplicationDbContext db
) : IUserService
{
    public async Task<ApiResponse<UserDto>> CreateAsync(CreateUserDto dto)
    {
        try
        {
            var emailTaken = await userManager.FindByEmailAsync(dto.Email);
            if (emailTaken != null)
                return ApiResponse<UserDto>.Fail(
                    HttpStatusCode.Conflict,
                    "Ya existe un usuario con este correo electrónico."
                );

            var user = new User
            {
                Email = dto.Email,
                UserName = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                PositionId = dto.PositionId,
                ProcessId = dto.ProcessId,
                BranchId = dto.BranchId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                HasChangedDefaultPassword = false,
            };

            var password = passwordService.GenerateSecurePassword(15);
            var createResult = await userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                logger.LogError(
                    "Error al crear usuario: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description))
                );
                return ApiResponse<UserDto>.Fail(
                    HttpStatusCode.BadRequest,
                    "Error al crear el usuario."
                );
            }

            var roleResult = await userManager.AddToRoleAsync(user, dto.Role);
            if (!roleResult.Succeeded)
            {
                logger.LogError(
                    "Error al asignar rol: {Errors}",
                    string.Join(", ", roleResult.Errors.Select(e => e.Description))
                );
                return ApiResponse<UserDto>.Fail(
                    HttpStatusCode.BadRequest,
                    "Error al asignar el rol al usuario."
                );
            }

            _ = SendCredentialsEmailAsync(user, password);

            var created = await db
                .Users.Include(u => u.Position)
                .Include(u => u.Process)
                .Include(u => u.Branch)
                .FirstAsync(u => u.Id == user.Id);

            return ApiResponse<UserDto>.Ok(
                ToDto(created, dto.Role),
                "Usuario creado exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear usuario {Email}", dto.Email);
            return ApiResponse<UserDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al crear el usuario."
            );
        }
    }

    public async Task<ApiResponse<UserDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var user = await db
                .Users.Include(u => u.Position)
                .Include(u => u.Process)
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return ApiResponse<UserDto>.Fail(HttpStatusCode.NotFound, "Usuario no encontrado.");

            var roles = await userManager.GetRolesAsync(user);
            return ApiResponse<UserDto>.Ok(ToDto(user, roles.FirstOrDefault()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener usuario {Id}", id);
            return ApiResponse<UserDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener el usuario."
            );
        }
    }

    public async Task<ApiResponse<PagedResult<UserDto>>> GetAllAsync(UserFilterDto filter)
    {
        try
        {
            var query = db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Email))
            {
                var email = filter.Email.ToLower();
                query = query.Where(u => u.Email!.ToLower().Contains(email));
            }

            if (!string.IsNullOrWhiteSpace(filter.FirstName))
            {
                var fn = filter.FirstName.ToLower();
                query = query.Where(u => u.FirstName.ToLower().Contains(fn));
            }

            if (!string.IsNullOrWhiteSpace(filter.LastName))
            {
                var ln = filter.LastName.ToLower();
                query = query.Where(u => u.LastName.ToLower().Contains(ln));
            }

            if (filter.IsActive.HasValue)
                query = query.Where(u => u.IsActive == filter.IsActive.Value);

            if (filter.Role.HasValue)
            {
                var roleName = filter.Role.Value.ToString();
                var usersInRole = await db
                    .UserRoles.Where(ur =>
                        db.Roles.Any(r => r.Id == ur.RoleId && r.Name == roleName)
                    )
                    .Select(ur => ur.UserId)
                    .ToListAsync();
                query = query.Where(u => usersInRole.Contains(u.Id));
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(search)
                    || u.LastName.ToLower().Contains(search)
                    || (u.Email != null && u.Email.ToLower().Contains(search))
                );
            }

            var total = await query.CountAsync();
            var users = await query
                .Include(u => u.Position)
                .Include(u => u.Process)
                .Include(u => u.Branch)
                .OrderBy(u => u.Email)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var items = new List<UserDto>();
            foreach (var u in users)
            {
                var roles = await userManager.GetRolesAsync(u);
                items.Add(ToDto(u, roles.FirstOrDefault()));
            }

            return ApiResponse<PagedResult<UserDto>>.Ok(
                new PagedResult<UserDto>
                {
                    Items = items,
                    TotalItems = total,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener usuarios");
            return ApiResponse<PagedResult<UserDto>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener los usuarios."
            );
        }
    }

    public async Task<ApiResponse<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        try
        {
            var user = await db
                .Users.Include(u => u.Position)
                .Include(u => u.Process)
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return ApiResponse<UserDto>.Fail(HttpStatusCode.NotFound, "Usuario no encontrado.");

            bool emailChanged = !string.Equals(
                user.Email,
                dto.Email,
                StringComparison.OrdinalIgnoreCase
            );
            if (emailChanged)
            {
                var conflict = await userManager.FindByEmailAsync(dto.Email);
                if (conflict != null && conflict.Id != user.Id)
                    return ApiResponse<UserDto>.Fail(
                        HttpStatusCode.Conflict,
                        "Ya existe otro usuario con este correo electrónico."
                    );
            }

            user.Email = dto.Email;
            user.UserName = dto.Email;
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;
            user.PositionId = dto.PositionId;
            user.ProcessId = dto.ProcessId;
            user.BranchId = dto.BranchId;
            user.IsActive = dto.IsActive;

            var currentRoles = await userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(dto.Role))
            {
                await userManager.RemoveFromRolesAsync(user, currentRoles);
                var addRole = await userManager.AddToRoleAsync(user, dto.Role);
                if (!addRole.Succeeded)
                {
                    logger.LogError(
                        "Error al asignar rol: {Errors}",
                        string.Join(", ", addRole.Errors.Select(e => e.Description))
                    );
                    return ApiResponse<UserDto>.Fail(
                        HttpStatusCode.BadRequest,
                        "Error al actualizar el rol del usuario."
                    );
                }
            }

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                logger.LogError(
                    "Error al actualizar usuario: {Errors}",
                    string.Join(", ", updateResult.Errors.Select(e => e.Description))
                );
                return ApiResponse<UserDto>.Fail(
                    HttpStatusCode.BadRequest,
                    "Error al actualizar el usuario."
                );
            }

            if (emailChanged)
            {
                if (!user.HasChangedDefaultPassword)
                {
                    var newPassword = passwordService.GenerateSecurePassword(15);
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var reset = await userManager.ResetPasswordAsync(user, token, newPassword);
                    if (reset.Succeeded)
                        _ = SendCredentialsEmailAsync(user, newPassword);
                }
                else
                {
                    _ = SendEmailChangedNotificationAsync(user);
                }
            }

            var updated = await db
                .Users.Include(u => u.Position)
                .Include(u => u.Process)
                .Include(u => u.Branch)
                .FirstAsync(u => u.Id == id);

            return ApiResponse<UserDto>.Ok(
                ToDto(updated, dto.Role),
                "Usuario actualizado exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al actualizar usuario {Id}", id);
            return ApiResponse<UserDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al actualizar el usuario."
            );
        }
    }

    public async Task<ApiResponse<bool>> ResendTemporaryPasswordAsync(Guid id)
    {
        try
        {
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return ApiResponse<bool>.Fail(HttpStatusCode.NotFound, "Usuario no encontrado.");

            if (user.HasChangedDefaultPassword)
                return ApiResponse<bool>.Fail(
                    HttpStatusCode.Conflict,
                    "El usuario ya realizó su primer cambio de contraseña."
                );

            var newPassword = passwordService.GenerateSecurePassword(15);
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await userManager.ResetPasswordAsync(user, token, newPassword);

            if (!reset.Succeeded)
            {
                logger.LogError(
                    "Error al restablecer contraseña para reenvío: {Errors}",
                    string.Join(", ", reset.Errors.Select(e => e.Description))
                );
                return ApiResponse<bool>.Fail(
                    HttpStatusCode.BadRequest,
                    "Error al generar la nueva contraseña temporal."
                );
            }

            _ = SendCredentialsEmailAsync(user, newPassword);

            return ApiResponse<bool>.Ok(
                true,
                "Correo de contraseña temporal enviado exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al reenviar contraseña temporal al usuario {Id}", id);
            return ApiResponse<bool>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al reenviar la contraseña temporal."
            );
        }
    }

    public async Task<
        ApiResponse<ResendTemporaryPasswordBulkResponseDto>
    > ResendTemporaryPasswordBulkAsync()
    {
        try
        {
            var pendingUsers = await db
                .Users.Where(u => !u.HasChangedDefaultPassword && u.IsActive)
                .ToListAsync();

            int sent = 0;
            int failed = 0;

            foreach (var user in pendingUsers)
            {
                try
                {
                    var newPassword = passwordService.GenerateSecurePassword(15);
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var reset = await userManager.ResetPasswordAsync(user, token, newPassword);

                    if (reset.Succeeded)
                    {
                        _ = SendCredentialsEmailAsync(user, newPassword);
                        sent++;
                    }
                    else
                    {
                        logger.LogWarning(
                            "No se pudo restablecer contraseña para usuario {Id}: {Errors}",
                            user.Id,
                            string.Join(", ", reset.Errors.Select(e => e.Description))
                        );
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error procesando usuario {Id} en envío masivo", user.Id);
                    failed++;
                }
            }

            return ApiResponse<ResendTemporaryPasswordBulkResponseDto>.Ok(
                new ResendTemporaryPasswordBulkResponseDto
                {
                    TotalPending = pendingUsers.Count,
                    Sent = sent,
                    Failed = failed,
                },
                $"Envío masivo completado: {sent} enviados, {failed} fallidos."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en envío masivo de contraseñas temporales");
            return ApiResponse<ResendTemporaryPasswordBulkResponseDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error en el envío masivo de contraseñas temporales."
            );
        }
    }

    private async Task SendCredentialsEmailAsync(User user, string password)
    {
        try
        {
            var body = emailTemplateService.GetInvitationEmailTemplate(
                $"{user.FirstName} {user.LastName}",
                password
            );
            await emailService.SendEmailAsync(
                user.Email!,
                "Tu cuenta de acceso ha sido creada — SICRE",
                body
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al enviar credenciales a {Email}", user.Email);
        }
    }

    private async Task SendEmailChangedNotificationAsync(User user)
    {
        try
        {
            var body = emailTemplateService.GetEmailChangedNotificationTemplate(
                $"{user.FirstName} {user.LastName}",
                user.Email!
            );
            await emailService.SendEmailAsync(
                user.Email!,
                "Tu correo electrónico ha sido actualizado — SICRE",
                body
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error al enviar notificación de cambio de email a {Email}",
                user.Email
            );
        }
    }

    private static UserDto ToDto(User u, string? roleName) =>
        new()
        {
            Id = u.Id,
            Email = u.Email!,
            FirstName = u.FirstName,
            LastName = u.LastName,
            PhoneNumber = u.PhoneNumber,
            PositionId = u.PositionId,
            PositionName = u.Position?.Name,
            ProcessId = u.ProcessId,
            ProcessName = u.Process?.Name,
            BranchId = u.BranchId,
            BranchName = u.Branch?.Name,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.LockoutEnd?.UtcDateTime,
            HasChangedDefaultPassword = u.HasChangedDefaultPassword,
            RoleName = GetRoleDisplayName(roleName),
        };

    private static string? GetRoleDisplayName(string? roleName)
    {
        if (string.IsNullOrEmpty(roleName))
            return null;
        return Enum.TryParse<Role>(roleName, true, out var role) ? role.GetDisplayName() : roleName;
    }
}
