using Advanced.CMS.ExternalReviews.EditReview;
using EPiServer.Shell;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.ExternalReviews;

internal class ExternalReviewUrlGenerator(ExternalReviewState externalReviewState, IOptions<ExternalReviewOptions> options)
{
    internal const string AnonymousRoutePrefix = "advanced-reviews";

    private bool IsAnonymous => options.Value.AllowAnonymousEditableLinks;

    public string ReviewsUrl => IsAnonymous
        ? $"/{AnonymousRoutePrefix}/edit"
        : Paths.ToResource("advanced-cms-external-reviews", $"PageEdit/{nameof(PageEditController.Index)}");

    public string AddPinUrl => IsAnonymous
        ? $"/{AnonymousRoutePrefix}/edit/{nameof(PageEditController.AddPin)}"
        : Paths.ToResource("advanced-cms-external-reviews", $"PageEdit/{nameof(PageEditController.AddPin)}");

    public string RemovePinUrl => IsAnonymous
        ? $"/{AnonymousRoutePrefix}/edit/{nameof(PageEditController.RemovePin)}"
        : Paths.ToResource("advanced-cms-external-reviews", $"PageEdit/{nameof(PageEditController.RemovePin)}");

    public string AnonymousAvatarUrl => $"/{AnonymousRoutePrefix}/avatar";

    public string GetAnonymousResourceUrl(string fileName) => $"/{AnonymousRoutePrefix}/resources/{fileName}";

    public string GetProxiedImageUrl(ContentReference contentLink)
    {
        return $"/ImageProxy/{externalReviewState.Token}/{contentLink}";
    }
}
