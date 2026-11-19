using Advanced.CMS.ExternalReviews.HeadlessPreview;
using Advanced.CMS.ExternalReviews.PinCodeSecurity;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using Microsoft.AspNetCore.Mvc;

namespace Advanced.CMS.AdvancedReviews;

/// <summary>
/// Controller used to redirect external reviewers to the preview of content rendered by a remote (headless) website
/// </summary>
internal class ExternalReviewPreviewController(
    IExternalReviewLinksRepository externalReviewLinksRepository,
    IExternalLinkPinCodeSecurityHandler externalLinkPinCodeSecurityHandler,
    HeadlessPreviewService headlessPreviewService)
    : Controller
{
    [HttpGet]
    public async Task<ActionResult> Index(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return new NotFoundObjectResult("Content not found");
        }

        var externalReviewLink = externalReviewLinksRepository.GetContentByToken(id);
        if (!externalReviewLink.IsPreviewableLink())
        {
            return new NotFoundObjectResult("Content not found");
        }

        if (!externalLinkPinCodeSecurityHandler.UserHasAccessToLink(externalReviewLink))
        {
            externalLinkPinCodeSecurityHandler.RedirectToLoginPage(externalReviewLink);
            return new EmptyResult();
        }

        var previewUrl = await headlessPreviewService.GetPreviewUrlAsync(externalReviewLink, cancellationToken);
        if (previewUrl == null)
        {
            return new NotFoundObjectResult("Content not found");
        }

        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        return new RedirectResult(previewUrl);
    }
}
