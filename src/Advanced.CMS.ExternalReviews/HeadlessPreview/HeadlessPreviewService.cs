using System.Globalization;
using System.Security.Claims;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Applications;
using EPiServer.Cms.Shell;
using EPiServer.Security;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.ExternalReviews.HeadlessPreview;

internal class HeadlessPreviewService(
    IApplicationResolver applicationResolver,
    IApplicationPreviewResolver applicationPreviewResolver,
    IPreviewTokenService previewTokenService,
    IContentLoader contentLoader,
    IOptions<ExternalReviewOptions> externalReviewOptions)
{
    private const string PreviewTokenParameter = "preview_token";
    private const string ReviewerName = "ExternalReviewer";
    private static readonly string[] AnonymousVisitorRoles = ["Everyone", "Anonymous"];

    private HeadlessPreviewOptions Options => externalReviewOptions.Value.HeadlessPreview;

    public bool IsHeadless(ContentReference contentLink) =>
        Options.Enabled && applicationResolver.GetByContent(contentLink, true) is IRemotelyPreviewableApplication;

    public Task<string> GetPreviewUrlAsync(ExternalReviewLink externalReviewLink, CancellationToken cancellationToken) =>
        GetPreviewUrlAsync(externalReviewLink, ApplicationPreviewContextMode.Preview, cancellationToken);

    public async Task<string> GetPreviewUrlAsync(ExternalReviewLink externalReviewLink, ApplicationPreviewContextMode mode,
        CancellationToken cancellationToken)
    {
        var application = await applicationResolver.GetByContentAsync(externalReviewLink.ContentLink, true, cancellationToken);
        if (!Options.Enabled || application is not IRemotelyPreviewableApplication remoteApplication)
        {
            return null;
        }

        var content = contentLoader.Get<IContent>(externalReviewLink.ContentLink);
        var context = new ApplicationPreviewContext
        {
            Application = application,
            Locale = CultureInfo.GetCultureInfo(content.LanguageBranch()),
            Mode = mode,
            UsePreviewTokens = false,
            ForceAbsoluteUrls = true
        };

        var previews = await applicationPreviewResolver.GetRelevantPreviewsAsync(externalReviewLink.ContentLink, context, cancellationToken);
        var previewUrl = previews
            .Where(x => x.ApplicationName == application.Name)
            .Select(x => x.Url)
            .FirstOrDefault(x => !string.IsNullOrEmpty(x));
        if (previewUrl == null)
        {
            return null;
        }

        if (!remoteApplication.UsePreviewTokens)
        {
            return previewUrl;
        }

        if (!previewTokenService.TryGetPreviewToken(CreateReviewerPrincipal(), out var previewToken))
        {
            return null;
        }

        var urlBuilder = new UrlBuilder(previewUrl);
        urlBuilder.QueryCollection[PreviewTokenParameter] = previewToken;
        return urlBuilder.ToString();
    }

    private ClaimsPrincipal CreateReviewerPrincipal()
    {
        var roles = AnonymousVisitorRoles.Concat(Options.ReviewerRoles ?? []).Distinct(StringComparer.OrdinalIgnoreCase);
        var claims = roles
            .Select(role => new Claim(ClaimTypes.Role, role))
            .Prepend(new Claim(ClaimTypes.Name, ReviewerName));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, nameof(HeadlessPreviewService)));
    }
}
