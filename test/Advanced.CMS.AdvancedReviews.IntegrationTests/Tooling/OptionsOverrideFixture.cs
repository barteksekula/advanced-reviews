using Advanced.CMS.ExternalReviews;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;

public abstract class OptionsOverrideFixture(SiteFixture siteFixture) : IAsyncLifetime
{
    private readonly IOptions<ExternalReviewOptions> _optionsAccessor = siteFixture.Services.GetRequiredService<IOptions<ExternalReviewOptions>>();
    private ExternalReviewOptions _originalSnapshot;

    public Task InitializeAsync()
    {
        _originalSnapshot = DeepClone(_optionsAccessor.Value);
        ApplyOverride(_optionsAccessor.Value);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (_originalSnapshot != null)
        {
            CopyExternalReviewOptions(_originalSnapshot, _optionsAccessor.Value);
        }
        return Task.CompletedTask;
    }

    protected abstract void ApplyOverride(ExternalReviewOptions options);

    private static ExternalReviewOptions DeepClone(ExternalReviewOptions source)
    {
        if (source == null) return new ExternalReviewOptions();

        var clone = new ExternalReviewOptions
        {
            IsEnabled = source.IsEnabled,
            IsReviewCommentsCommandEnabled = source.IsReviewCommentsCommandEnabled,
            ContentPreviewUrl = source.ContentPreviewUrl,
            ContentIframeEditUrlSegment = source.ContentIframeEditUrlSegment,
            EmailSubject = source.EmailSubject,
            EmailEdit = source.EmailEdit,
            EmailView = source.EmailView,
            EditableLinksEnabled = source.EditableLinksEnabled,
            IsAdminModePinReviewerPluginEnabled = source.IsAdminModePinReviewerPluginEnabled,
            AllowScreenshotAttachments = source.AllowScreenshotAttachments,
            ViewLinkValidTo = source.ViewLinkValidTo,
            EditLinkValidTo = source.EditLinkValidTo,
            ProlongDays = source.ProlongDays,
            InterceptGetChildren = source.InterceptGetChildren,
            MaxExpectedContentLoadsPerRequest = source.MaxExpectedContentLoadsPerRequest,
            Restrictions =
            {
                MaxReviewLocationsForContent = source.Restrictions.MaxReviewLocationsForContent,
                MaxCommentsForReviewLocation = source.Restrictions.MaxCommentsForReviewLocation,
                MaxCommentLength = source.Restrictions.MaxCommentLength
            },
            PinCodeSecurity =
            {
                Enabled = source.PinCodeSecurity.Enabled,
                Required = source.PinCodeSecurity.Required,
                ExternalReviewLoginUrl = source.PinCodeSecurity.ExternalReviewLoginUrl,
                RolesWithoutPin = source.PinCodeSecurity.RolesWithoutPin,
                AuthenticationCookieLifeTime = source.PinCodeSecurity.AuthenticationCookieLifeTime,
                CodeLength = source.PinCodeSecurity.CodeLength
            }
        };

        return clone;
    }

    private static void CopyExternalReviewOptions(ExternalReviewOptions from, ExternalReviewOptions to)
    {
        if (from == null || to == null)
        {
            return;
        }

        to.IsEnabled = from.IsEnabled;
        to.IsReviewCommentsCommandEnabled = from.IsReviewCommentsCommandEnabled;
        to.ContentPreviewUrl = from.ContentPreviewUrl;
        to.ContentIframeEditUrlSegment = from.ContentIframeEditUrlSegment;
        to.EmailSubject = from.EmailSubject;
        to.EmailEdit = from.EmailEdit;
        to.EmailView = from.EmailView;
        to.EditableLinksEnabled = from.EditableLinksEnabled;
        to.IsAdminModePinReviewerPluginEnabled = from.IsAdminModePinReviewerPluginEnabled;
        to.AllowScreenshotAttachments = from.AllowScreenshotAttachments;
        to.ViewLinkValidTo = from.ViewLinkValidTo;
        to.EditLinkValidTo = from.EditLinkValidTo;
        to.ProlongDays = from.ProlongDays;
        to.InterceptGetChildren = from.InterceptGetChildren;
        to.MaxExpectedContentLoadsPerRequest = from.MaxExpectedContentLoadsPerRequest;

        to.Restrictions.MaxReviewLocationsForContent = from.Restrictions.MaxReviewLocationsForContent;
        to.Restrictions.MaxCommentsForReviewLocation = from.Restrictions.MaxCommentsForReviewLocation;
        to.Restrictions.MaxCommentLength = from.Restrictions.MaxCommentLength;

        to.PinCodeSecurity.Enabled = from.PinCodeSecurity.Enabled;
        to.PinCodeSecurity.Required = from.PinCodeSecurity.Required;
        to.PinCodeSecurity.ExternalReviewLoginUrl = from.PinCodeSecurity.ExternalReviewLoginUrl;
        to.PinCodeSecurity.RolesWithoutPin = from.PinCodeSecurity.RolesWithoutPin;
        to.PinCodeSecurity.AuthenticationCookieLifeTime = from.PinCodeSecurity.AuthenticationCookieLifeTime;
        to.PinCodeSecurity.CodeLength = from.PinCodeSecurity.CodeLength;
    }
}
