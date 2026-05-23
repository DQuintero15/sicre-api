using Hangfire.Dashboard;
using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Infrastructure.Hangfire;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return false;
        return httpContext.User.IsInRole(Role.Administrator.ToString());
    }
}
