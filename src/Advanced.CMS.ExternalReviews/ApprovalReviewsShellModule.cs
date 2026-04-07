using Advanced.CMS.ApprovalReviews;
using EPiServer.Framework.Web.Resources;
using EPiServer.Security;
using EPiServer.Shell.Modules;
using EPiServer.Shell.Profile.Internal;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.ExternalReviews;

public class ApprovalReviewsShellModule(
    IOptions<ExternalReviewOptions> options,
    ReviewUrlGenerator reviewUrlGenerator,
    IPrincipalAccessor principalAccessor,
    ICurrentUiCulture currentUiCulture) : ShellModule
{
    /// <inheritdoc />
    public override ModuleViewModel CreateViewModel(ModuleTable moduleTable,
        IClientResourceService clientResourceService)
    {
        var model = new AdvancedReviewsModuleViewModel(this, clientResourceService, options.Value)
        {
            Language = currentUiCulture.Get(principalAccessor.Principal.Identity.Name).Name,
            AvatarUrl = reviewUrlGenerator.AvatarUrl
        };
        return model;
    }
}
