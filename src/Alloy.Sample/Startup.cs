using Advanced.CMS.AdvancedReviews;
using Advanced.CMS.ApprovalReviews;
using Alloy.Sample.Extensions;
using Alloy.Sample.Infrastructure;
using EPiServer.Cms.Shell.UI.Configurations;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Cms.UI.VisitorGroups;
using EPiServer.Data;
using EPiServer.DependencyInjection;
using EPiServer.Framework.Web.Resources;
using EPiServer.Scheduler;
using EPiServer.Web.Routing;

namespace Alloy.Sample
{
    public class Startup
    {
        private readonly IWebHostEnvironment _webHostingEnvironment;

        public Startup(IWebHostEnvironment webHostingEnvironment)
        {
            _webHostingEnvironment = webHostingEnvironment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (_webHostingEnvironment.IsDevelopment())
            {
                AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(_webHostingEnvironment.ContentRootPath, "App_Data"));

                services.Configure<SchedulerOptions>(options => options.Enabled = false);

                services.AddUIMappedFileProviders(_webHostingEnvironment.ContentRootPath, @"..\..\");
                services.Configure<ClientResourceOptions>(uiOptions =>
                {
                    uiOptions.Debug = true;
                });
            }

            services.AddCmsAspNetIdentity<ApplicationUser>();
            services.AddMvc();
            services.AddAlloy();
            services.AddCmsHost()
                .AddCmsHtmlHelpers()
                .AddCmsUI()
                .AddAdmin()
                // .AddCommerce()
                .AddVisitorGroupsUI()
                .AddTinyMce();

            services.AddCmsImageSharpImageLibrary();

            services.Configure<DataAccessOptions>(options => options.UpdateDatabaseCompatibilityLevel = true);

            services.AddEmbeddedLocalization<Startup>();

            services.AddAdvancedReviews(e =>
            {
                e.EditableLinksEnabled = true;
                e.PinCodeSecurity.Enabled = true;
                e.PinCodeSecurity.Required = true;
                e.PinCodeSecurity.CodeLength = 5;
                e.ProlongDays = 10;
            });

            services.Configure<ApprovalOptions>(options =>
            {
                options.Notifications.NotificationsEnabled = true;
            });

            services.Configure<CmsFeatureOptions>(options =>
            {
                options.SectionsVisibility.OnPageEditing = true;
            });
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMiddleware<AdministratorRegistrationPageMiddleware>();
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapContent();
                endpoints.MapControllerRoute("Register", "/Register", new { controller = "Register", action = "Index" });
                endpoints.MapRazorPages();
            });
        }
    }
}
