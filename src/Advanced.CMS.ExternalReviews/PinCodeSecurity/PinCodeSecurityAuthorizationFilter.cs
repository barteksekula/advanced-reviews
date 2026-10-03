using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Advanced.CMS.ExternalReviews.PinCodeSecurity;

internal class PinCodeSecurityAuthorizationFilter(
    ExternalReviewState externalReviewState,
    IExternalReviewLinksRepository externalReviewLinksRepository,
    IExternalLinkPinCodeSecurityHandler externalLinkPinCodeSecurityHandler)
    : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!externalReviewState.IsInExternalReviewContext)
        {
            return;
        }

        var externalReviewLink = externalReviewLinksRepository.GetContentByToken(externalReviewState.Token);
        if (externalReviewLink == null || externalLinkPinCodeSecurityHandler.UserHasAccessToLink(externalReviewLink))
        {
            return;
        }

        externalLinkPinCodeSecurityHandler.RedirectToLoginPage(externalReviewLink);
        context.Result = new EmptyResult();
    }
}
