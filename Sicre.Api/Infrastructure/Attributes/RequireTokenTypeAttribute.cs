using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sicre.Api.Shared;

namespace Sicre.Api.Infrastructure.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTokenTypeAttribute(params string[] requiredTokenTypes)
    : Attribute,
        IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var allowAnonymous = context
            .ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>()
            .Any();

        if (allowAnonymous)
            return;

        var tokenTypeClaim = context.HttpContext.User.FindFirst(Constants.ClaimNames.TokenType);

        if (tokenTypeClaim == null || !requiredTokenTypes.Contains(tokenTypeClaim.Value))
            context.Result = new ForbidResult();
    }
}
