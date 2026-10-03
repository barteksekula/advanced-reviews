using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using Advanced.CMS.ExternalReviews;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.AnonymousEditableLinks;

public class AnonymousEditableLinksEnabledFixture(SiteFixture siteFixture) : OptionsOverrideFixture(siteFixture)
{
    protected override void ApplyOverride(ExternalReviewOptions options)
    {
        options.EditableLinksEnabled = true;
        options.AllowAnonymousEditableLinks = true;
        options.PinCodeSecurity.Enabled = true;
    }
}

public class AnonymousEditableLinksDisabledFixture(SiteFixture siteFixture) : OptionsOverrideFixture(siteFixture)
{
    protected override void ApplyOverride(ExternalReviewOptions options)
    {
        options.EditableLinksEnabled = true;
        options.AllowAnonymousEditableLinks = false;
    }
}
