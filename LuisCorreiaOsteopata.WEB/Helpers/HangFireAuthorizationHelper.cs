using Hangfire.Dashboard;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public class HangFireAuthorizationHelper : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity.IsAuthenticated && httpContext.User.IsInRole("Administrador");
    }
}
