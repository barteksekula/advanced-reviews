using EPiServer.Applications;
using EPiServer.Web.Routing;

namespace Advanced.CMS.ApprovalReviews;

public interface IStartPageUrlResolver
{
    string GetUrl(ContentReference contentReference, string languageBranch);
}

internal class StartPageUrlResolver(IUrlResolver urlResolver, IApplicationResolver applicationResolver)
    : IStartPageUrlResolver
{
    public string GetUrl(ContentReference contentReference, string languageBranch)
    {
        var application = applicationResolver.GetByContent(contentReference, true);
        return application is not InProcessWebsite inProcessWebsite
            ? null
            : urlResolver.GetUrl(inProcessWebsite.EntryPoint, languageBranch);
    }
}
