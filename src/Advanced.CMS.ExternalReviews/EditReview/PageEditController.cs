using Advanced.CMS.ApprovalReviews;
using Microsoft.AspNetCore.Mvc;

namespace Advanced.CMS.ExternalReviews.EditReview;

/// <summary>
/// Controller used to render editable external review page
/// </summary>
internal class PageEditController(EditableReviewService editableReviewService) : Controller
{
    // [ConvertEditLinksFilter]
    public async Task<ActionResult> Index(string id)
    {
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

        return View("Index", await editableReviewService.CreatePreviewModelAsync(externalReviewLink, false));
    }

    [HttpPost]
    public ActionResult AddPin([FromBody] ReviewLocation reviewLocation) => editableReviewService.AddPin(reviewLocation);

    [HttpPost]
    public ActionResult RemovePin([FromBody] DeleteReviewLocation location) => editableReviewService.RemovePin(location);
}

internal class DeleteReviewLocation
{
    public string Token { get; set; }
    public string Id { get; set; }
}

public class ContentPreviewModel
{
    public string Token { get; set; }
    public string Name { get; set; }

    /// <summary>
    /// Url used by the iframe, it contains specific language branch in which the content was created
    /// </summary>
    public string EditableContentUrlSegment { get; set; }

    /// <summary>
    /// Url where new pins will be posted to
    /// </summary>
    public string AddPinUrl { get; set; }
    public string RemovePinUrl { get; set; }
    public string AvatarUrl { get; set; }

    public string ReviewJsScriptPath { get; set; }
    public string ReviewCssPath { get; set; }
    public string ReviewPins { get; set; }
    public string Metadata { get; set; }
    public string Options { get; set; }
}

//TODO: pass restrictions to client?
