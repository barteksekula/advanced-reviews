using EPiServer.Applications;

namespace Advanced.CMS.ApprovalReviews;

public interface ISiteUriResolver
{
    InProcessWebsite GetWebsite(ContentReference contentReference = null);
    ContentReference GetStartPage(ContentReference contentReference = null);
    Uri GetUri(ContentReference contentReference);
}

internal class SiteUriResolver(IApplicationRepository applicationRepository) : ISiteUriResolver
{
    public InProcessWebsite GetWebsite(ContentReference contentReference = null)
    {
        //IApplicationResolver applicationResolver
        // var application = contentReference == null
        //     ? applicationResolver.GetByContext()
        //     : applicationResolver.GetByContent(contentReference, false);
        // return application as InProcessWebsite;

        var applications = applicationRepository.List().Cast<InProcessWebsite>().ToList();
        return applications.First();
    }

    public ContentReference GetStartPage(ContentReference contentReference = null)
    {
        var inProcessWebsite = GetWebsite(contentReference);
        return inProcessWebsite?.EntryPoint;
    }

    public Uri GetUri(ContentReference contentReference = null)
    {
        var inProcessWebsite = GetWebsite(contentReference);
        return inProcessWebsite?.Hosts.FirstOrDefault()?.Url;
    }
}
