using Advanced.CMS.ApprovalReviews;
using Advanced.CMS.ApprovalReviews.Notifications;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Cms.Shell;
using EPiServer.Framework.Modules;
using EPiServer.Framework.Modules.Internal;
using EPiServer.Framework.Serialization;
using EPiServer.Shell.Services.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.ExternalReviews.EditReview;

/// <summary>
/// Controller used to render editable external review page
/// </summary>
internal class PageEditController : Controller
{
    private readonly IContentLoader _contentLoader;
    private readonly IExternalReviewLinksRepository _externalReviewLinksRepository;
    private readonly IApprovalReviewsRepository _approvalReviewsRepository;
    private readonly ExternalReviewOptions _externalReviewOptions;
    private readonly IObjectSerializerFactory _serializerFactory;
    private readonly IStartPageUrlResolver _startPageUrlResolver;
    private readonly PropertyResolver _propertyResolver;
    private readonly ReviewsNotifier _reviewsNotifier;
    private readonly ReviewUrlGenerator _reviewUrlGenerator;
    private readonly ExternalReviewUrlGenerator _externalReviewUrlGenerator;
    private readonly IModuleResourceResolver _moduleResourceResolver;

    public PageEditController(IContentLoader contentLoader,
        IExternalReviewLinksRepository externalReviewLinksRepository,
        IApprovalReviewsRepository approvalReviewsRepository,
        IOptions<ExternalReviewOptions> externalReviewOptions, IObjectSerializerFactory serializerFactory,
        IStartPageUrlResolver startPageUrlResolver,
        PropertyResolver propertyResolver,
        ReviewsNotifier reviewsNotifier, ExternalReviewUrlGenerator externalReviewUrlGenerator,
        ReviewUrlGenerator reviewUrlGenerator, IModuleResourceResolver moduleResourceResolver)
    {
        _contentLoader = contentLoader;
        _externalReviewLinksRepository = externalReviewLinksRepository;
        _approvalReviewsRepository = approvalReviewsRepository;
        _externalReviewOptions = externalReviewOptions.Value;
        _serializerFactory = serializerFactory;
        _startPageUrlResolver = startPageUrlResolver;
        _propertyResolver = propertyResolver;
        _reviewsNotifier = reviewsNotifier;
        _externalReviewUrlGenerator = externalReviewUrlGenerator;
        _reviewUrlGenerator = reviewUrlGenerator;
        _moduleResourceResolver = moduleResourceResolver;

        approvalReviewsRepository.OnBeforeUpdate += ApprovalReviewsRepository_OnBeforeUpdate;
    }

    // [ConvertEditLinksFilter]
    public async Task<ActionResult> Index(string id)
    {
        var externalReviewLink = _externalReviewLinksRepository.GetContentByToken(id);
        if (!externalReviewLink.IsEditableLink())
        {
            return new NotFoundObjectResult("Content not found");
        }

        var content = _contentLoader.Get<IContent>(externalReviewLink.ContentLink);
        var startPageUrl = _startPageUrlResolver.GetUrl(externalReviewLink.ContentLink, content.LanguageBranch());

        var serializer = _serializerFactory.GetSerializer(KnownContentTypes.Json);
        var pagePreviewModel = new ContentPreviewModel
        {
            Token = id,
            Name = content.Name,
            EditableContentUrlSegment =
                UrlPath.Combine(startPageUrl, _externalReviewOptions.ContentIframeEditUrlSegment, id),
            AddPinUrl = $"{UrlPath.EnsureStartsWithSlash(_externalReviewUrlGenerator.AddPinUrl)}",
            RemovePinUrl = $"{UrlPath.EnsureStartsWithSlash(_externalReviewUrlGenerator.RemovePinUrl)}",
            AvatarUrl = $"{UrlPath.EnsureStartsWithSlash(_reviewUrlGenerator.AvatarUrl)}",
            ReviewJsScriptPath = GetPath("ClientResources/dist/editable-external-review-component.js"),
            ReviewCssPath = GetPath("ClientResources/dist/editable-external-review-component.css"),
            ReviewPins = serializer.Serialize(_approvalReviewsRepository.Load(externalReviewLink.ContentLink)),
            Metadata = serializer.Serialize(await _propertyResolver.ResolveAsync(content as ContentData)),
            Options = serializer.Serialize(_externalReviewOptions)
        };
        return View("Index", pagePreviewModel);
    }

    [HttpPost]
    public ActionResult AddPin([FromBody] ReviewLocation reviewLocation)
    {
        var token = reviewLocation.Token;
        if (string.IsNullOrWhiteSpace(token))
        {
            return new BadRequestResult();
        }

        var reviewLink = _externalReviewLinksRepository.GetContentByToken(token);
        if (reviewLink == null)
        {
            return new BadRequestResult();
        }

        if (!ValidateReviewLocation(reviewLocation))
        {
            return new BadRequestResult();
        }

        //TODO: security issue - we post whole item and external reviewer can modify this

        _ = _reviewsNotifier.NotifyCmsEditor(reviewLink.ContentLink, token, reviewLocation.Data, false);

        var location = _approvalReviewsRepository.Update(reviewLink.ContentLink, reviewLocation);
        if (location == null)
        {
            return new BadRequestResult();
        }

        return new RestResult
        {
            Data = location
        };
    }

    [HttpPost]
    public ActionResult RemovePin([FromBody] DeleteReviewLocation location)
    {
        var token = location.Token;
        if (string.IsNullOrWhiteSpace(token))
        {
            return new BadRequestResult();
        }

        var reviewLink = _externalReviewLinksRepository.GetContentByToken(token);
        if (reviewLink == null)
        {
            return new BadRequestResult();
        }

        _approvalReviewsRepository.RemoveReviewLocation(location.Id, reviewLink.ContentLink);
        return new EmptyResult();
    }

    private bool ValidateReviewLocation(ReviewLocation reviewLocation)
    {
        bool ValidateComment(CommentDto comment)
        {
            return comment.Text.Length <= _externalReviewOptions.Restrictions.MaxCommentLength;
        }

        var serializer = _serializerFactory.GetSerializer(KnownContentTypes.Json);
        var reviewLocationDto = serializer.Deserialize<ReviewLocationDto>(reviewLocation.Data);
        if (reviewLocationDto == null)
        {
            return false;
        }

        if (!ValidateComment(reviewLocationDto.FirstComment))
        {
            return false;
        }

        if (reviewLocationDto.Comments.Count() > _externalReviewOptions.Restrictions.MaxCommentsForReviewLocation)
        {
            return false;
        }

        foreach (var comment in reviewLocationDto.Comments)
        {
            if (!ValidateComment(comment))
            {
                return false;
            }
        }

        return true;
    }

    private string GetPath(string url)
    {
        return _moduleResourceResolver.TryResolveClientPath(typeof(PageEditController).Assembly, url,
            out var path)
            ? path
            : "";
    }

    private void ApprovalReviewsRepository_OnBeforeUpdate(object sender, BeforeUpdateEventArgs e)
    {
        if (e.IsNew == false)
        {
            return;
        }

        if (e.ReviewLocations.Count() > _externalReviewOptions.Restrictions.MaxReviewLocationsForContent)
        {
            e.Cancel = true;
        }
    }

    private class ReviewLocationDto
    {
        public CommentDto FirstComment { get; set; }
        public IEnumerable<CommentDto> Comments { get; set; }
    }

    private class CommentDto
    {
        public string Text { get; set; }
    }
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
