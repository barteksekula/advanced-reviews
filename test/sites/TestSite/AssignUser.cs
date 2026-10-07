using System.Security.Claims;
using EPiServer.Cms.UI.AspNetIdentity;

namespace TestSite;

public class AssignUser : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> nextAction)
    {
        return app =>
        {
            app.Use(async (context, next) =>
            {
                await AssignUserToContext(context.Request.HttpContext);
                await next();
            });
            nextAction(app);
        };
    }

    private async Task AssignUserToContext(HttpContext context)
    {
        var userName = context.Request.Query["user"];
        if (string.IsNullOrEmpty(userName)) return;

        var pwd = context.Request.Query["pwd"];
        var aum = context.RequestServices.GetService<ApplicationUserProvider<ApplicationUser>>();
        var u = await aum.GetUserAsync(userName);
        var sm = context.RequestServices.GetService<ApplicationSignInManager<ApplicationUser>>();
        var pwdRes = await sm.CheckPasswordSignInAsync(u as ApplicationUser, pwd, false);
        if (pwdRes.Succeeded)
        {
            var res = await sm.GenerateUserIdentityAsync(u as ApplicationUser);
            var cp = new ClaimsPrincipal(res);
            context.User = cp;
        }
    }
}
