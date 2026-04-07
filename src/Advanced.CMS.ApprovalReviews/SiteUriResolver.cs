using EPiServer.Applications;

namespace Advanced.CMS.ApprovalReviews;

public interface ISiteUriResolver
{
    InProcessWebsite GetWebsite(ContentReference contentReference = null);
    ContentReference GetStartPage(ContentReference contentReference = null);
    Uri GetUri(ContentReference contentReference);
}

internal class SiteUriResolver(IApplicationResolver applicationResolver) : ISiteUriResolver
{
    public InProcessWebsite GetWebsite(ContentReference contentReference = null)
    {
        var application = contentReference == null
            ? applicationResolver.GetByContext()
            : applicationResolver.GetByContent(contentReference, false);
        return application as InProcessWebsite;
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
