using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Features.Branches.Dtos;
using Sicre.Api.Features.Branches.Services;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Branches.Controllers;

[ApiController]
[Route("api/branch")]
[Authorize]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class BranchController(IBranchService branchService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<BranchDto>>>> GetAll(
        [FromQuery] BranchFilterDto filter
    )
    {
        var result = await branchService.GetAllAsync(filter);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> GetById(Guid id)
    {
        var result = await branchService.GetByIdAsync(id);
        return FromResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BranchDto>>> Create([FromBody] CreateBranchDto dto)
    {
        var result = await branchService.CreateAsync(dto);
        return FromResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> Update(
        Guid id,
        [FromBody] UpdateBranchDto dto
    )
    {
        var result = await branchService.UpdateAsync(id, dto);
        return FromResult(result);
    }
}
