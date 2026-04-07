using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Core.Internal;
using EPiServer.Framework.Blobs;
using EPiServer.ImageLibrary;
using Microsoft.AspNetCore.Mvc;

namespace Advanced.CMS.AdvancedReviews;

internal class ImageProxyController(
    IExternalReviewLinksRepository externalReviewLinksRepository,
    IContentLoader contentLoader,
    ThumbnailManager thumbnailManager)
    : Controller
{
    private string ThumbnailMimeType => "image/png";

    public IActionResult Index([FromRoute] string token, [FromRoute] string contentLink, [FromQuery] int? width, [FromQuery] int? height)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(contentLink))
        {
            return new NotFoundResult();
        }

        var externalReviewLink = externalReviewLinksRepository.GetContentByToken(token);
        if (externalReviewLink.IsExpired())
        {
            return new NotFoundResult();
        }

        if(!ContentReference.TryParse(contentLink.Trim('/'), out var contentReference))
        {
            return new NotFoundResult();
        }

        var content = contentLoader.Get<IContent>(contentReference);
        if (content is not ImageData imageData)
        {
            return new NotFoundResult();
        }

        var returnThumbnail = width.HasValue && height.HasValue;

        var originalBlobBytes = imageData.BinaryData.ReadAllBytes();
        var blobToReturn = returnThumbnail ? Generate(originalBlobBytes, width.Value, height.Value) : originalBlobBytes;

        return File(blobToReturn, imageData.MimeType);
    }

    private byte[] Generate(byte[] blobBytes, int width, int height)
    {
        var imgOperation = new ImageOperation(ImageEditorCommand.ResizeKeepScale, width, height)
        {
            // use transparency color
            BackgroundColor = "#00000000"
        };

        try
        {
            return thumbnailManager.ImageService.RenderImage(blobBytes,
                new List<ImageOperation> { imgOperation }, ThumbnailMimeType, 1, 50);
        }
        catch
        {
            return null;
        }
    }
}
