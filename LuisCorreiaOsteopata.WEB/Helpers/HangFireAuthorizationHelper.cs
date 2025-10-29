using Hangfire.Dashboard;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public class HangFireAuthorizationHelper : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext.User.Identity?.IsAuthenticated == true &&
            httpContext.User.IsInRole("Administrador"))
        {
            return true;
        }

        httpContext.Response.Redirect("/Account/NotAuthorized");
        return false;
    }
}
