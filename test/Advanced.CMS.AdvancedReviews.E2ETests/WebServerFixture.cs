using System.Net;
using System.Net.Sockets;
using Advanced.CMS.ExternalReviews;
using Advanced.CMS.IntegrationTests;
using EPiServer.Applications;
using EPiServer.Data;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Security;
using EPiServer.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using TestSite;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.E2ETests;

public class WebServerFixture : IAsyncLifetime
{
    private const string SolutionName = "Advanced.CMS.AdvancedReviews.sln";

    public const string AdminUserName = "cmsadmin";
    public const string AdminPassword = "sparr0wHawk!";

    private readonly CmsDatabaseFixture _databaseFixture;
    private readonly IHost _host;
    private IPlaywright _playwright;

    public WebServerFixture()
    {
        var port = GetRandomUnusedPort();
        Authority = $"localhost:{port}";
        BaseUrl = $"http://{Authority}";

        var siteRoot = SolutionPathUtility.GetSolutionPath(@"test\sites\TestSite", SolutionName);
        _databaseFixture = new CmsDatabaseFixture(
            SolutionPathUtility.GetSolutionPath(@"test\Advanced.CMS.IntegrationTests\Assets\db_template.mdf", SolutionName),
            Path.Combine(siteRoot, "App_Data", "e2e.mdf"));
        var connectionString =
            $"Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog={_databaseFixture.DatabaseName};Integrated Security=True;Connect Timeout=30;MultipleActiveResultSets=True";
        new DatabaseHelper(connectionString).ExecuteSqlFile(
            SolutionPathUtility.GetSolutionPath(@"test\Advanced.CMS.IntegrationTests\IdentitySchema.sql", SolutionName));

        _host = Host.CreateDefaultBuilder()
            .ConfigureCmsDefaults()
            .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:EPiServerDB"] = connectionString
            }))
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseEnvironment(Environments.Development);
                webBuilder.UseContentRoot(siteRoot);
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls(BaseUrl);
            })
            .ConfigureServices(services =>
            {
                var existingServiceDefinition = services.Single(x => x.ServiceType == typeof(IDatabaseMode));
                services.Remove(existingServiceDefinition);
                services.AddSingleton(existingServiceDefinition.ImplementationType);
                services.AddSingleton<IDatabaseMode>(sp =>
                    new SwitchableDatabaseMode(sp.GetService(existingServiceDefinition.ImplementationType) as IDatabaseMode));
                services.Configure<ExternalReviewOptions>(options =>
                {
                    options.EditableLinksEnabled = true;
                    options.PinCodeSecurity.Enabled = true;
                });
            })
            .Build();
    }

    public string Authority { get; }
    public string BaseUrl { get; }
    public IBrowser Browser { get; private set; }
    public IServiceProvider Services => _host.Services;

    public async Task InitializeAsync()
    {
        Assertions.SetDefaultExpectTimeout(15_000);
        await _host.StartAsync();
        await CreateWebsiteAsync();
        await CreateAdminUserAsync();

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = IsHeadless
        });
    }

    private static bool IsHeadless
    {
        get
        {
#if DEBUG
            return Environment.GetEnvironmentVariable("E2E_HEADLESS") == "1";
#else
            return true;
#endif
        }
    }

    public async Task DisposeAsync()
    {
        if (Browser != null)
        {
            await Browser.DisposeAsync();
        }

        _playwright?.Dispose();
        await _host.StopAsync();
        _host.Dispose();
        _databaseFixture.Dispose();
    }

    private async Task CreateWebsiteAsync()
    {
        var contentRepository = Services.GetInstance<IContentRepository>();
        var startPage = contentRepository.GetDefault<StartPage>(Services.GetInstance<SystemDefinition>().RootPage);
        startPage.Name = "Start";
        var startPageLink = contentRepository.Save(startPage, SaveAction.Publish, AccessLevel.NoAccess).ToReferenceWithoutVersion();

        var website = new InProcessWebsite("Start", startPageLink);
        website.Hosts.Add(new ApplicationHost(Authority)
        {
            PreferredUrlScheme = UrlScheme.Http,
            Type = ApplicationHostType.Default
        });
        await Services.GetInstance<IApplicationRepository>().SaveAsync(website);
    }

    private async Task CreateAdminUserAsync()
    {
        string[] roles = ["WebAdmins", "WebEditors", "CmsAdmins", "CmsEditors"];
        var roleProvider = Services.GetInstance<UIRoleProvider>();
        foreach (var role in roles)
        {
            if (!await roleProvider.RoleExistsAsync(role))
            {
                await roleProvider.CreateRoleAsync(role);
            }
        }

        await Services.GetInstance<UIUserProvider>()
            .CreateUserAsync(AdminUserName, AdminPassword, "cmsadmin@example.com", null, null, true);
        await roleProvider.AddUserToRolesAsync(AdminUserName, roles);
    }

    private static int GetRandomUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
