using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Features.Positions.Dtos;
using Sicre.Api.Features.Positions.Services;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Positions.Controllers;

[ApiController]
[Route("api/jobposition")]
[Authorize]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class PositionController(IPositionService positionService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PositionDto>>>> GetAll(
        [FromQuery] PositionFilterDto filter
    )
    {
        var result = await positionService.GetAllAsync(filter);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> GetById(Guid id)
    {
        var result = await positionService.GetByIdAsync(id);
        return FromResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PositionDto>>> Create(
        [FromBody] CreatePositionDto dto
    )
    {
        var result = await positionService.CreateAsync(dto);
        return FromResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> Update(
        Guid id,
        [FromBody] UpdatePositionDto dto
    )
    {
        var result = await positionService.UpdateAsync(id, dto);
        return FromResult(result);
    }
}
