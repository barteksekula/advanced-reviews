using Advanced.CMS.ApprovalReviews;
using Advanced.CMS.ExternalReviews;
using Advanced.CMS.ExternalReviews.EditReview;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.AdvancedReviews;

/// <summary>
/// Controller used to render editable external review page for reviewers without CMS account
/// </summary>
internal class ExternalReviewEditController(
    EditableReviewService editableReviewService,
    IOptions<ExternalReviewOptions> externalReviewOptions)
    : Controller
{
    private bool IsDisabled => !externalReviewOptions.Value.AllowAnonymousEditableLinks;

    [HttpGet]
    public async Task<ActionResult> Index(string id)
    {
        if (IsDisabled)
        {
            return new NotFoundResult();
        }

        var externalReviewLink = editableReviewService.GetEditableLink(id);
        if (externalReviewLink == null)
        {
            return new NotFoundObjectResult("Content not found");
        }

        if (!editableReviewService.UserHasAccessToLink(externalReviewLink))
        {
            editableReviewService.RedirectToLoginPage(externalReviewLink);
            return new EmptyResult();
        }

        return View("Index", await editableReviewService.CreatePreviewModelAsync(externalReviewLink, true));
    }

    [HttpPost]
    public ActionResult AddPin([FromBody] ReviewLocation reviewLocation) =>
        IsDisabled ? new NotFoundResult() : editableReviewService.AddPin(reviewLocation);

    [HttpPost]
    public ActionResult RemovePin([FromBody] DeleteReviewLocation location) =>
        IsDisabled ? new NotFoundResult() : editableReviewService.RemovePin(location);

    [HttpGet]
    public ActionResult Resource(string id) =>
        IsDisabled ? new NotFoundResult() : editableReviewService.GetResource(id);

    [HttpGet]
    public ActionResult Avatar(string id) =>
        IsDisabled ? new NotFoundResult() : editableReviewService.GetIdenticon(id);
}
