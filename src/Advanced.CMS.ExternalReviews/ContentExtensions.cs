using EPiServer.Logging;
using EPiServer.ServiceLocation;

namespace Advanced.CMS.ExternalReviews;

public static class ContentExtensions
{
    private static readonly ILogger _log = LogManager.GetLogger(typeof(ContentExtensions));

    public static ContentReference LoadUnpublishedVersion(this ContentReference baseReference)
    {
        var externalReviewState = ServiceLocator.Current.GetInstance<ExternalReviewState>();
        var projectContentResolver = ServiceLocator.Current.GetInstance<ProjectContentResolver>();
        var contentVersionRepository = ServiceLocator.Current.GetInstance<IContentVersionRepository>();

        if (externalReviewState.ProjectId.HasValue)
        {
            // load version from project
            return projectContentResolver.GetProjectReference(baseReference, externalReviewState.ProjectId.Value, externalReviewState.PreferredLanguage);
        }

        // load common draft instead of published version
        ContentVersion loadCommonDraft;
        try
        {
            loadCommonDraft = contentVersionRepository.LoadCommonDraft(baseReference, externalReviewState.PreferredLanguage);
        }
        catch (ContentNotFoundException)
        {
            _log.Debug($"Advanced Reviews: Content {baseReference} not found for LoadUnpublishedVersion");
            loadCommonDraft = null;
        }

        if (loadCommonDraft == null)
        {
            // fallback to default implementation if there is no common draft in a given language
            return null;
        }

        return loadCommonDraft.ContentLink;
    }

    public static bool HasExpired(this IVersionable content)
    {
        return content.Status == VersionStatus.Published && content.StopPublish < DateTime.Now;
    }
}
