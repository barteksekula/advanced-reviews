using System.Globalization;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Pipeline;
using EPiServer.Web.Routing;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.ExternalReviews;

/// <summary>
/// Partial router used to display readonly version of the page
/// </summary>
internal class PagePreviewPartialRouter(
    IExternalReviewLinksRepository externalReviewLinksRepository,
    IOptions<ExternalReviewOptions> externalReviewOptions,
    ProjectContentResolver projectContentResolver,
    IContentLanguageAccessor contentLanguageAccessor,
    ExternalReviewState externalReviewState,
    IContentVersionRepository contentVersionRepository)
    : IPartialRouter<IContent, IContent>
{
    public PartialRouteData GetPartialVirtualPath(IContent content, UrlGeneratorContext segmentContext)
    {
        return new PartialRouteData();
    }

    public object RoutePartial(IContent content, UrlResolverContext segmentContext)
    {
        if (!externalReviewOptions.Value.IsEnabled)
        {
            return null;
        }

        var nextSegment = segmentContext.GetNextSegment(segmentContext.RemainingSegments);
        if (nextSegment.Next.IsEmpty)
        {
            return null;
        }

        if (!string.Equals(nextSegment.Next.ToString(), externalReviewOptions.Value.ContentPreviewUrl,
                StringComparison.CurrentCultureIgnoreCase))
        {
            return null;
        }

        nextSegment = segmentContext.GetNextSegment(nextSegment.Remaining);
        var token = nextSegment.Next.ToString();

        var externalReviewLink = externalReviewLinksRepository.GetContentByToken(token);
        if (!externalReviewLink.IsPreviewableLink())
        {
            return null;
        }

        var version = contentVersionRepository.Load(externalReviewLink.ContentLink);
        contentLanguageAccessor.Language = new CultureInfo(version.LanguageBranch);
        externalReviewState.Token = token;
        externalReviewState.ProjectId = externalReviewLink.ProjectId;
        externalReviewState.PreferredLanguage = version.LanguageBranch;
        externalReviewState.ImpersonatedVisitorGroupsById = externalReviewLink.VisitorGroups;

        try
        {
            var versionedContent = projectContentResolver.TryGetProjectPageVersion(externalReviewLink, content,
                segmentContext.Url.QueryCollection);

            segmentContext.RemainingSegments = nextSegment.Remaining;

            // set ContentLink in DataTokens to make IPageRouteHelper working
            segmentContext.RouteValues[RoutingConstants.ContentLinkKey] = versionedContent.ContentLink;

            return versionedContent;
        }
        catch
        {
            return null;
        }
    }
}
