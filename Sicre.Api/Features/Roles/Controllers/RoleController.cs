using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Features.Roles.Dtos;
using Sicre.Api.Features.Roles.Services;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Roles.Controllers;

[ApiController]
[Route("api/role")]
[Authorize]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class RoleController(IRoleService roleService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetAll()
    {
        var result = await roleService.GetAllRolesAsync();
        return FromResult(result);
    }
}
