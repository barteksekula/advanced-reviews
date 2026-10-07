using System.Net;
using System.Net.Sockets;
using Advanced.CMS.ExternalReviews;
using Advanced.CMS.IntegrationTests;
using EPiServer.Applications;
using EPiServer.Authorization;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using TestSite.Models;
using Xunit;
using Program = TestSite.Program;

namespace Advanced.CMS.AdvancedReviews.E2ETests;

public class WebServerFixture : IAsyncLifetime
{
    private const string SolutionName = "Advanced.CMS.AdvancedReviews.sln";

    public const string AdminUserName = "cmsadmin";
    public const string AdminPassword = "sparr0wHawk!";
    public const string LoginPath = "/Util/Login";

    private readonly CmsDatabaseFixture _databaseFixture;
    private readonly string _connectionString;
    private readonly UIServiceFixture<Program> _factory;
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
        _connectionString =
            $"Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog={_databaseFixture.DatabaseName};Integrated Security=True;Connect Timeout=30;MultipleActiveResultSets=True";
        new DatabaseHelper(_connectionString).ExecuteSqlFile(
            SolutionPathUtility.GetSolutionPath(@"test\Advanced.CMS.IntegrationTests\IdentitySchema.sql", SolutionName));

        _factory = new UIServiceFixture<Program>(_connectionString, services =>
        {
            services.ConfigureApplicationCookie(options => options.LoginPath = LoginPath);
            services.Configure<ExternalReviewOptions>(options =>
            {
                options.EditableLinksEnabled = true;
                options.AllowAnonymousEditableLinks = true;
                options.PinCodeSecurity.Enabled = true;
            });
        }, siteRoot);
        _factory.UseKestrel(port);
    }

    public string Authority { get; }
    public string BaseUrl { get; }
    public IBrowser Browser { get; private set; }
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        Assertions.SetDefaultExpectTimeout(15_000);
        _factory.StartServer();
        await CreateWebsiteAsync();
        await WaitForDatabaseProvisioningAsync();

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
        await _factory.DisposeAsync();
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

    private async Task WaitForDatabaseProvisioningAsync()
    {
        var timeout = DateTime.UtcNow.AddSeconds(30);
        while (!await IsLastUserProvisionedAsync())
        {
            if (DateTime.UtcNow > timeout)
            {
                throw new TimeoutException("Users were not provisioned");
            }

            await Task.Delay(100);
        }
    }

    private async Task<bool> IsLastUserProvisionedAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*) FROM AspNetUserRoles userRoles
            JOIN AspNetUsers users ON users.Id = userRoles.UserId
            JOIN AspNetRoles roles ON roles.Id = userRoles.RoleId
            WHERE users.UserName = @userName AND roles.Name = @roleName
            """;
        command.Parameters.AddWithValue("@userName", "reid");
        command.Parameters.AddWithValue("@roleName", Roles.WebEditors);
        return (int)await command.ExecuteScalarAsync() > 0;
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
