using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Features.Users.Dtos;
using Sicre.Api.Features.Users.Services;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Users.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class UserController(
    IUserService userService,
    IUserProfileService profileService,
    IUserCsvImportService userCsvImportService
) : BaseController
{
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> GetProfile()
    {
        var result = await profileService.GetProfileAsync(GetUserId());
        return FromResult(result);
    }

    [HttpGet("/api/usuario/perfil")]
    public async Task<IActionResult> GetPerfil()
    {
        var data = await profileService.GetUserProfileDataAsync(GetUserId());
        if (data == null)
            return NotFound(new { message = "Usuario no encontrado." });
        return Ok(data);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetAll(
        [FromQuery] UserFilterDto filter
    )
    {
        var result = await userService.GetAllAsync(filter);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id)
    {
        var result = await userService.GetByIdAsync(id);
        return FromResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserDto dto)
    {
        var result = await userService.CreateAsync(dto);
        return FromResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(
        Guid id,
        [FromBody] UpdateUserDto dto
    )
    {
        var result = await userService.UpdateAsync(id, dto);
        return FromResult(result);
    }

    [HttpPost("import-csv")]
    [Authorize(Roles = nameof(Sicre.Api.Domain.Enums.Role.Administrator))]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ImportUsersFromCsvResponseDto>>> ImportCsv(
        IFormFile file
    )
    {
        var result = await userCsvImportService.ImportUsersAsync(file);
        return FromResult(result);
    }
}
