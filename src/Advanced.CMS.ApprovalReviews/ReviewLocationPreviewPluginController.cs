using EPiServer.Shell.Services.Rest;
using Microsoft.AspNetCore.Mvc;
using EditUrlResolver = EPiServer.Web.Routing.EditUrlResolver;

namespace Advanced.CMS.ApprovalReviews;

internal class ReviewLocationPreviewPluginController(
    IApprovalReviewsRepository repository,
    IContentLoader contentLoader,
    EditUrlResolver editUrlResolver,
    ReviewUrlGenerator reviewUrlGenerator)
    : Controller
{
    public IActionResult Index()
    {
        var viewModel = new ViewModel
        {
            ControllerUrl = reviewUrlGenerator.ReviewLocationPluginUrl
        };

        return View("Index", viewModel);
    }

    public ActionResult GetAll()
    {
        var result = repository.LoadAll().GroupBy(x => x.ContentLink.ToReferenceWithoutVersion()).Select(x =>
        {
            var content = contentLoader.Get<IContent>(x.Key);

            return new
            {
                Id = x.Key,
                content.Name,
                ContentLinks = x.Select(c =>
                {
                    var editModeLink = editUrlResolver.GetEditViewUrl(c.ContentLink);

                    return new { c.ContentLink, EditModeUrl = editModeLink, c.SerializedReview };
                })
            };
        });

        return new RestResult { Data = result };
    }

    [HttpPost]
    public void DeleteReviewLocation([FromBody] Dto dto)
    {
        repository.Delete(ContentReference.Parse(dto.ContentLink));
    }
}

internal class Dto
{
    public string ContentLink { get; set; }
}

public class ViewModel
{
    public string ControllerUrl { get; set; }
}
