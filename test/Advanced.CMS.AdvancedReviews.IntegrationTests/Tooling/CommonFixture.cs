using EPiServer.Applications;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using TestSite.Models;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;

public class CommonFixture : IDisposable
{
    private readonly InProcessWebsite site;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IContentRepository contentRepository;
    private readonly StartPage start;

    public CommonFixture()
    {
        contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
        _applicationRepository = ServiceLocator.Current.GetInstance<IApplicationRepository>();

        var sysDef = ServiceLocator.Current.GetInstance<SystemDefinition>();
        start = contentRepository.GetDefault<StartPage>(sysDef.RootPage);
        start.Name = "Start";
        var startPage = contentRepository.Save(start, SaveAction.Publish, AccessLevel.NoAccess)
            .ToReferenceWithoutVersion();

        site = new InProcessWebsite("Start", startPage);
        var applicationHost = new ApplicationHost("localhost")
        {
            PreferredUrlScheme = UrlScheme.Http,
            Type = ApplicationHostType.Default
        };
        // applicationHost.Url = "";
        site.Hosts.Add(applicationHost);
        _applicationRepository.SaveAsync(site).GetAwaiter().GetResult();
    }

    public InProcessWebsite Site => site;

    public void Dispose()
    {
        _applicationRepository.DeleteAsync(site.Name).GetAwaiter().GetResult();
        contentRepository.Delete(start.ContentLink, true, AccessLevel.NoAccess);
    }
}
