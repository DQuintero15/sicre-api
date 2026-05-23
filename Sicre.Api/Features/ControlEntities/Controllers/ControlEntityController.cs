using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Features.ControlEntities.Dtos;
using Sicre.Api.Features.ControlEntities.Services;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.ControlEntities.Controllers;

[ApiController]
[Route("api/controlentity")]
[Authorize]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class ControlEntityController(IControlEntityService controlEntityService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ControlEntityDto>>>> GetAll(
        [FromQuery] ControlEntityFilterDto filter
    )
    {
        var result = await controlEntityService.GetAllAsync(filter);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ControlEntityDto>>> GetById(Guid id)
    {
        var result = await controlEntityService.GetByIdAsync(id);
        return FromResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ControlEntityDto>>> Create(
        [FromBody] CreateControlEntityDto dto
    )
    {
        var result = await controlEntityService.CreateAsync(dto);
        if (result.Success)
            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
        return FromResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ControlEntityDto>>> Update(
        Guid id,
        [FromBody] UpdateControlEntityDto dto
    )
    {
        var result = await controlEntityService.UpdateAsync(id, dto);
        return FromResult(result);
    }
}
