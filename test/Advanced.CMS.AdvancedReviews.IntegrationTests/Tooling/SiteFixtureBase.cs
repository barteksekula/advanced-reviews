using Advanced.CMS.ExternalReviews;
using Advanced.CMS.IntegrationTests;
using EPiServer.Cms.Shell.UI.Rest.Projects;
using EPiServer.ServiceLocation;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestSite;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;

public class SiteFixtureBase : IDisposable
{
    private readonly UIServiceFixture<Program> _serviceFixture;
    private CmsDatabaseFixture _databaseFixture;

    public SiteFixtureBase(Action<ExternalReviewOptions> optionsCallback = null)
    {
        string connectionString = EnsureDatabase();
        _serviceFixture = new UIServiceFixture<Program>(connectionString, collection =>
        {
            collection.Configure<ProjectUIOptions>(options =>
            {
                options.ProjectModeEnabled = true;
            });

            if (optionsCallback != null)
            {
                collection.Configure(optionsCallback);
            }
        });
        try
        {
            Client = _serviceFixture.CreateClient();
        }
        catch
        {
            Dispose();
        }
    }

    private string EnsureDatabase()
    {
        var dbFile = Path.GetFullPath(@"..\..\..\sites\TestSite\App_Data\cms.mdf", Environment.CurrentDirectory);
        var databaseMdfTemplateFile = Path.GetFullPath(@"..\..\..\Advanced.CMS.IntegrationTests\Assets\db_template.mdf", Environment.CurrentDirectory);
        _databaseFixture = new CmsDatabaseFixture(databaseMdfTemplateFile, dbFile);
        var connectionString =
            $"Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog={_databaseFixture.DatabaseName};Integrated Security=True;Connect Timeout=30;MultipleActiveResultSets=True";
        var dbHelper = new DatabaseHelper(connectionString);
        var aspNetIdentitySchema = SolutionPathUtility.GetSolutionPath(
            @"test\Advanced.CMS.IntegrationTests\IdentitySchema.sql", "Advanced.CMS.AdvancedReviews.sln");
        dbHelper.ExecuteSqlFile(aspNetIdentitySchema);

        return connectionString;
    }

    public IServiceProvider Services => _serviceFixture.Services;
    public HttpClient Client { get; set; }

    public HttpClient CreateClient() => _serviceFixture.CreateClient();

    public HttpClient CreateClientWithoutRedirects() =>
        _serviceFixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public void Dispose()
    {
        foreach (var hostedService in Services.GetAllInstances<IHostedService>().ToList())
        {
            hostedService.StopAsync(new CancellationToken()).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        Client.Dispose();
        _serviceFixture.Dispose();
        _databaseFixture.Dispose();
    }
}
