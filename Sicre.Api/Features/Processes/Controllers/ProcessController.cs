using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Features.Processes.Dtos;
using Sicre.Api.Features.Processes.Services;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Processes.Controllers;

[ApiController]
[Route("api/process")]
[Authorize]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class ProcessController(IProcessService processService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProcessDto>>>> GetAll(
        [FromQuery] ProcessFilterDto filter
    )
    {
        var result = await processService.GetAllAsync(filter);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProcessDto>>> GetById(Guid id)
    {
        var result = await processService.GetByIdAsync(id);
        return FromResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProcessDto>>> Create([FromBody] CreateProcessDto dto)
    {
        var result = await processService.CreateAsync(dto);
        return FromResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProcessDto>>> Update(
        Guid id,
        [FromBody] UpdateProcessDto dto
    )
    {
        var result = await processService.UpdateAsync(id, dto);
        return FromResult(result);
    }
}
