using Advanced.CMS.ApprovalReviews;
using Advanced.CMS.ExternalReviews.HeadlessPreview;
using EPiServer.Cms.Shell;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.ExternalReviews.ReviewLinksRepository;

internal class ExternalReviewLinkBuilder(
    IOptions<ExternalReviewOptions> options,
    IStartPageUrlResolver startPageUrlResolver,
    ProjectRepository projectRepository,
    ExternalReviewUrlGenerator externalReviewUrlGenerator,
    HeadlessPreviewService headlessPreviewService,
    IContentLoader contentLoader)
{
    public ExternalReviewLink FromExternalReview(ExternalReviewLinkDds externalReviewLinkDds)
    {
        var projectName = "";
        var projectId = externalReviewLinkDds.ProjectId;
        if (projectId.HasValue)
        {
            try
            {
                var project = projectRepository.Get(externalReviewLinkDds.ProjectId.Value);
                projectName = project.Name;
            }
            catch
            {
                projectName = string.Empty;
                projectId = null;
            }
        }

        string externalUrlPrefix;
        if (externalReviewLinkDds.IsEditable)
        {
            externalUrlPrefix = UrlPath.EnsureStartsWithSlash(externalReviewUrlGenerator.ReviewsUrl);
        }
        else if (headlessPreviewService.IsHeadless(externalReviewLinkDds.ContentLink))
        {
            externalUrlPrefix = externalReviewUrlGenerator.HeadlessPreviewUrl;
        }
        else
        {
            var content = contentLoader.Get<IContent>(externalReviewLinkDds.ContentLink);
            var url = startPageUrlResolver.GetUrl(externalReviewLinkDds.ContentLink, content.LanguageBranch());
            // the preview url has to be language specific as it's handled entirely by the EPiServer partial router
            // the edit url is just a pure aspnet.mvc controller, handled outside EPiServer
            externalUrlPrefix = UrlPath.Combine(url, options.Value.ContentPreviewUrl);
        }

        return new ExternalReviewLink
        {
            ContentLink = externalReviewLinkDds.ContentLink,
            IsEditable = externalReviewLinkDds.IsEditable,
            ProjectId = projectId,
            Token = externalReviewLinkDds.Token,
            ValidTo = externalReviewLinkDds.ValidTo,
            LinkUrl = UrlPath.Combine(externalUrlPrefix, externalReviewLinkDds.Token),
            PinCode = externalReviewLinkDds.PinCode,
            DisplayName = externalReviewLinkDds.DisplayName,
            ProjectName = projectName,
            VisitorGroups = externalReviewLinkDds.VisitorGroups
        };
    }
}
