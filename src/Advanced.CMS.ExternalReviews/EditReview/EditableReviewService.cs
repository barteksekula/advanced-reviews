using Advanced.CMS.ApprovalReviews;
using Advanced.CMS.ApprovalReviews.Notifications;
using Advanced.CMS.ExternalReviews.PinCodeSecurity;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Cms.Shell;
using EPiServer.Framework.Hosting;
using EPiServer.Framework.Modules;
using EPiServer.Framework.Modules.Internal;
using EPiServer.Framework.Serialization;
using EPiServer.Shell.Services.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.ExternalReviews.EditReview;

internal class EditableReviewService(
    IContentLoader contentLoader,
    IExternalReviewLinksRepository externalReviewLinksRepository,
    IApprovalReviewsRepository approvalReviewsRepository,
    IOptions<ExternalReviewOptions> externalReviewOptions,
    IObjectSerializerFactory serializerFactory,
    IStartPageUrlResolver startPageUrlResolver,
    PropertyResolver propertyResolver,
    ReviewsNotifier reviewsNotifier,
    ExternalReviewUrlGenerator externalReviewUrlGenerator,
    ReviewUrlGenerator reviewUrlGenerator,
    IModuleResourceResolver moduleResourceResolver,
    IExternalLinkPinCodeSecurityHandler externalLinkPinCodeSecurityHandler,
    ICompositeFileProvider compositeFileProvider)
{
    private const string ScriptFileName = "editable-external-review-component.js";
    private const string StyleFileName = "editable-external-review-component.css";

    private static readonly Dictionary<string, string> ResourceContentTypes = new()
    {
        [ScriptFileName] = "text/javascript",
        [StyleFileName] = "text/css"
    };

    private ExternalReviewOptions Options => externalReviewOptions.Value;

    public ExternalReviewLink GetEditableLink(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var externalReviewLink = externalReviewLinksRepository.GetContentByToken(token);
        return externalReviewLink.IsEditableLink() ? externalReviewLink : null;
    }

    public bool UserHasAccessToLink(ExternalReviewLink externalReviewLink) =>
        externalLinkPinCodeSecurityHandler.UserHasAccessToLink(externalReviewLink);

    public void RedirectToLoginPage(ExternalReviewLink externalReviewLink) =>
        externalLinkPinCodeSecurityHandler.RedirectToLoginPage(externalReviewLink);

    public async Task<ContentPreviewModel> CreatePreviewModelAsync(ExternalReviewLink externalReviewLink, bool isAnonymous)
    {
        var content = contentLoader.Get<IContent>(externalReviewLink.ContentLink);
        var startPageUrl = startPageUrlResolver.GetUrl(externalReviewLink.ContentLink, content.LanguageBranch());
        var serializer = serializerFactory.GetSerializer(KnownContentTypes.Json);

        return new ContentPreviewModel
        {
            Token = externalReviewLink.Token,
            Name = content.Name,
            EditableContentUrlSegment =
                UrlPath.Combine(startPageUrl, Options.ContentIframeEditUrlSegment, externalReviewLink.Token),
            AddPinUrl = $"{UrlPath.EnsureStartsWithSlash(externalReviewUrlGenerator.AddPinUrl)}",
            RemovePinUrl = $"{UrlPath.EnsureStartsWithSlash(externalReviewUrlGenerator.RemovePinUrl)}",
            AvatarUrl = isAnonymous
                ? externalReviewUrlGenerator.AnonymousAvatarUrl
                : $"{UrlPath.EnsureStartsWithSlash(reviewUrlGenerator.AvatarUrl)}",
            ReviewJsScriptPath = isAnonymous
                ? externalReviewUrlGenerator.GetAnonymousResourceUrl(ScriptFileName)
                : GetModulePath(ScriptFileName),
            ReviewCssPath = isAnonymous
                ? externalReviewUrlGenerator.GetAnonymousResourceUrl(StyleFileName)
                : GetModulePath(StyleFileName),
            ReviewPins = serializer.Serialize(approvalReviewsRepository.Load(externalReviewLink.ContentLink)
                .Select(x => new { x.Id, x.Data, IsRemovable = x.Token == externalReviewLink.Token })),
            Metadata = serializer.Serialize(await propertyResolver.ResolveAsync(content as ContentData)),
            Options = serializer.Serialize(new
            {
                Options.AllowScreenshotAttachments
            })
        };
    }

    public ActionResult GetResource(string fileName)
    {
        if (fileName == null || !ResourceContentTypes.TryGetValue(fileName, out var contentType))
        {
            return new NotFoundResult();
        }

        var fileInfo = compositeFileProvider.GetFileInfo(GetModulePath(fileName));
        if (!fileInfo.Exists)
        {
            return new NotFoundResult();
        }

        return new FileStreamResult(fileInfo.CreateReadStream(), contentType);
    }

    public ActionResult GetIdenticon(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new NotFoundResult();
        }

        return new FileContentResult(new IdenticonGenerator().CreateIdenticon(userName), "image/png");
    }

    public ActionResult AddPin(ReviewLocation reviewLocation)
    {
        var token = reviewLocation?.Token;
        var reviewLink = GetEditableLink(token);
        if (reviewLink == null)
        {
            return new BadRequestResult();
        }

        if (!UserHasAccessToLink(reviewLink))
        {
            return new StatusCodeResult(StatusCodes.Status403Forbidden);
        }

        var existingLocations = approvalReviewsRepository.Load(reviewLink.ContentLink).ToList();
        if (!ValidateReviewLocation(reviewLocation) || !PreservesExistingComments(existingLocations, reviewLocation))
        {
            return new BadRequestResult();
        }

        if (string.IsNullOrWhiteSpace(reviewLocation.Id) &&
            existingLocations.Count > Options.Restrictions.MaxReviewLocationsForContent)
        {
            return new BadRequestResult();
        }

        _ = reviewsNotifier.NotifyCmsEditor(reviewLink.ContentLink, token, reviewLocation.Data, false);

        var location = approvalReviewsRepository.Update(reviewLink.ContentLink, reviewLocation);
        if (location == null)
        {
            return new BadRequestResult();
        }

        return new RestResult
        {
            Data = location
        };
    }

    public ActionResult RemovePin(DeleteReviewLocation location)
    {
        var token = location?.Token;
        var reviewLink = GetEditableLink(token);
        if (reviewLink == null)
        {
            return new BadRequestResult();
        }

        if (!UserHasAccessToLink(reviewLink))
        {
            return new StatusCodeResult(StatusCodes.Status403Forbidden);
        }

        var existingLocation = approvalReviewsRepository.Load(reviewLink.ContentLink).FirstOrDefault(x => x.Id == location.Id);
        if (existingLocation == null)
        {
            return new BadRequestResult();
        }

        if (existingLocation.Token != token)
        {
            return new StatusCodeResult(StatusCodes.Status403Forbidden);
        }

        approvalReviewsRepository.RemoveReviewLocation(location.Id, reviewLink.ContentLink);
        return new EmptyResult();
    }

    private bool ValidateReviewLocation(ReviewLocation reviewLocation)
    {
        bool ValidateComment(CommentDto comment)
        {
            return comment.Text.Length <= Options.Restrictions.MaxCommentLength;
        }

        var reviewLocationDto = Deserialize(reviewLocation.Data);
        if (reviewLocationDto == null)
        {
            return false;
        }

        if (!ValidateComment(reviewLocationDto.FirstComment))
        {
            return false;
        }

        if (reviewLocationDto.Comments.Count() > Options.Restrictions.MaxCommentsForReviewLocation)
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

    private bool PreservesExistingComments(IEnumerable<ReviewLocation> existingLocations, ReviewLocation reviewLocation)
    {
        if (string.IsNullOrWhiteSpace(reviewLocation.Id))
        {
            return true;
        }

        var existingLocation = existingLocations.FirstOrDefault(x => x.Id == reviewLocation.Id);
        if (existingLocation == null)
        {
            return false;
        }

        var existing = Deserialize(existingLocation.Data);
        var updated = Deserialize(reviewLocation.Data);
        var existingComments = existing?.Comments?.ToList() ?? [];
        var updatedComments = updated.Comments?.ToList() ?? [];

        return Equals(existing?.FirstComment, updated.FirstComment) &&
               updatedComments.Count >= existingComments.Count &&
               existingComments.SequenceEqual(updatedComments.Take(existingComments.Count));
    }

    private ReviewLocationDto Deserialize(string data)
    {
        return serializerFactory.GetSerializer(KnownContentTypes.Json).Deserialize<ReviewLocationDto>(data);
    }

    private string GetModulePath(string fileName)
    {
        return moduleResourceResolver.TryResolveClientPath(typeof(EditableReviewService).Assembly,
            $"ClientResources/dist/{fileName}", out var path)
            ? path
            : "";
    }

    private class ReviewLocationDto
    {
        public CommentDto FirstComment { get; set; }
        public IEnumerable<CommentDto> Comments { get; set; }
    }

    private record CommentDto
    {
        public string Author { get; set; }
        public string Text { get; set; }
        public string Date { get; set; }
        public string Screenshot { get; set; }
    }
}
