using System.Security.Principal;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.ExternalReviews.PinCodeSecurity;

/// <summary>
/// Service responsible for handling external links PIN code security
/// </summary>
public interface IExternalLinkPinCodeSecurityHandler
{
    /// <summary>
    /// Checks if current user has access to <paramref name="externalReviewLink"/>
    /// </summary>
    /// <param name="externalReviewLink">External reviews link</param>
    /// <returns></returns>
    bool UserHasAccessToLink(ExternalReviewLink externalReviewLink);

    /// <summary>
    /// Redirect to PIN code login page
    /// </summary>
    /// <param name="externalReviewLink"></param>
    void RedirectToLoginPage(ExternalReviewLink externalReviewLink);

    /// <summary>
    /// Tries to sign in user to allow access to <paramref name="externalReviewLink"/>
    /// </summary>
    /// <param name="externalReviewLink">External review link</param>
    /// <param name="requestedPinCode">PIN code provide by user on ligin page</param>
    /// <returns>True when PIN code is valid and user was signed in</returns>
    bool TryToSignIn(ExternalReviewLink externalReviewLink, string requestedPinCode);
}

internal class DefaultExternalLinkPinCodeSecurityHandler : IExternalLinkPinCodeSecurityHandler
{
    private readonly ExternalReviewOptions _externalReviewOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPrincipalAccessor _principalAccessor;

    public static string ExternalReviewTokens = "ExternalReviewTokens";

    public DefaultExternalLinkPinCodeSecurityHandler(IOptions<ExternalReviewOptions> externalReviewOptions,
        IHttpContextAccessor httpContextAccessor, IPrincipalAccessor principalAccessor)
    {
        _externalReviewOptions = externalReviewOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _principalAccessor = principalAccessor;
    }

    public bool UserHasAccessToLink(ExternalReviewLink externalReviewLink)
    {
        // check if PIN code option is enabled
        if (!_externalReviewOptions.PinCodeSecurity.Enabled)
        {
            return true;
        }

        // check if PIN code is required for the link
        if (string.IsNullOrEmpty(externalReviewLink.PinCode))
        {
            return true;
        }

        if (IsInRoleWithoutPin(_principalAccessor.Principal))
        {
            return true;
        }

        var identityHash = _httpContextAccessor.HttpContext.Request.Cookies[ExternalReviewTokens];

        return externalReviewLink.PinCode == identityHash;
    }

    private bool IsInRoleWithoutPin(IPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var rolesWithoutPin = _externalReviewOptions.PinCodeSecurity.RolesWithoutPin ?? Enumerable.Empty<string>();
        return rolesWithoutPin.Any(principal.IsInRole);
    }

    public void RedirectToLoginPage(ExternalReviewLink externalReviewLink)
    {
        _httpContextAccessor.HttpContext.Response.Redirect("/" + _externalReviewOptions.PinCodeSecurity.ExternalReviewLoginUrl + "?id=" +
                                                           externalReviewLink.Token);
    }

    public bool TryToSignIn(ExternalReviewLink externalReviewLink, string requestedPinCode)
    {
        // check if PIN code provided by user match link PIN code
        var hash = PinCodeHashGenerator.Hash(requestedPinCode, externalReviewLink.Token);
        if (externalReviewLink.PinCode != hash)
        {
            return false;
        }

        _httpContextAccessor.HttpContext.Response.Cookies.Append(ExternalReviewTokens, hash, new CookieOptions
        {
            Expires = DateTime.UtcNow.Add(_externalReviewOptions.PinCodeSecurity.AuthenticationCookieLifeTime),
        });

        return true;
    }
}
