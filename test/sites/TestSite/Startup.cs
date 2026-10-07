using Advanced.CMS.AdvancedReviews;
using Advanced.CMS.Development;
using Advanced.CMS.IntegrationTests;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Cms.UI.VisitorGroups;
using EPiServer.Data;
using EPiServer.DependencyInjection;
using EPiServer.Framework.Web.Resources;
using EPiServer.Scheduler;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceDescriptor = Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

namespace TestSite;

public class Startup(IWebHostEnvironment webHostingEnvironment, IConfiguration configuration)
{
    public static string DatabaseName { get; set; }

    public IConfiguration Configuration { get; set; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        var dbPath = Path.Combine(webHostingEnvironment.ContentRootPath, "App_Data\\cms.mdf");
        var connectionString = Configuration.GetConnectionString("EPiServerDB") ?? $"Data Source=(LocalDb)\\MSSQLLocalDB;AttachDbFilename={dbPath};Initial Catalog={DatabaseName};Integrated Security=True;Connect Timeout=30;MultipleActiveResultSets=True";
        services.Configure<DataAccessOptions>(o =>
        {
            o.SetConnectionString(connectionString);
        });
        services.Configure<ClientResourceOptions>(o => o.Debug = true);

        //NETCORE: Skip Antiforgery checks in tests since we are constructing test requests programatically out of browser
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IFilterProvider, SkipAntiforgeryFilterProvider>());
        services.Configure<SchedulerOptions>(options =>
        {
            options.Enabled = false;
            options.PingTime = new TimeSpan(10, 10, 10);
        });

        services.AddMvc();

        if (webHostingEnvironment.IsDevelopment())
        {
            services.AddUIMappedFileProviders(webHostingEnvironment.ContentRootPath, @"..\..\..\");
        }

        services.Configure<RazorViewEngineOptions>(options =>
        {
            options.ViewLocationExpanders.Add(new SiteViewEngineLocationExpander());
        });

        services.AddStartupFilter<AssignUser>();
        services.AddCmsHost()
            .AddCmsHtmlHelpers()
            .AddCmsUI()
            .AddAdmin()
            .AddVisitorGroupsUI()
            .AddCmsAspNetIdentity<ApplicationUser>();

        services.Configure<DataAccessOptions>(options => options.UpdateDatabaseCompatibilityLevel = true);
        services.AddCmsImageSharpImageLibrary();
        services.AddAdvancedReviews();

        services.Configure<StaticFileOptions>("foo", o => o.OnPrepareResponse = c => c.Context.Response.Headers.Append("X-From-Custom-Option", "Something"));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.UseMiddleware<FakeUserMiddleware>();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            //Do some registration before MapContent
            endpoints.MapDefaultControllerRoute();
            endpoints.MapContent();
        });
    }
}
