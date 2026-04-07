using EPiServer.Framework.Web.Resources;
using EPiServer.Shell.Modules;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.ExternalReviews;

public class ExternalReviewsShellModule(IOptions<ExternalReviewOptions> options) : ShellModule
{
    /// <inheritdoc />
    public override ModuleViewModel CreateViewModel(ModuleTable moduleTable, IClientResourceService clientResourceService)
    {
        return new AdvancedReviewsModuleViewModel(this, clientResourceService, options.Value);
    }
}
