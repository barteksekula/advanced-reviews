using Advanced.CMS.ExternalReviews.Properties;
using EPiServer.ServiceLocation;

namespace Advanced.CMS.ExternalReviews;

[Options]
public class ExternalReviewOptions
{
    /// <summary>
    /// Gets or sets if the plugin should be initialized in Edit Mode
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets if Editor can add comments from Edit Mode by highlighting specific areas on the page
    /// </summary>
    public bool IsReviewCommentsCommandEnabled { get; set; } = true;

    /// <summary>
    /// URL used for displaying readonly version of page
    /// </summary>
    public string ContentPreviewUrl { get; set; } = "externalContentView";

    /// <summary>
    /// URL to editable content iframe
    /// </summary>
    public string ContentIframeEditUrlSegment { get; set; } = "externalPageReview";

    /// <summary>
    /// email subject template
    /// </summary>
    public string EmailSubject { get; set; } = Resources.EmailSubject;

    /// <summary>
    /// email template used for editable links
    /// </summary>
    public string EmailEdit { get; set; } = Resources.mail_edit;

    /// <summary>
    /// email template used for readonly content links
    /// </summary>
    public string EmailView { get; set; } = Resources.mail_preview;

    /// <summary>
    /// When true then Editor can create editable links
    /// </summary>
    public bool EditableLinksEnabled { get; set; } = false;

    /// <summary>
    /// Enable Admin Mode pin reviewer plugin which allows admins to view and delete saved comments
    /// </summary>
    public bool IsAdminModePinReviewerPluginEnabled { get; set; } = true;

    /// <summary>
    /// Allow to take screenshots of the page to highlight spotted issues
    /// </summary>
    public bool AllowScreenshotAttachments { get; set; } = true;

    /// <summary>
    /// For how long view link is valid
    /// </summary>
    public TimeSpan ViewLinkValidTo { get; set; } = TimeSpan.FromDays(5);

    /// <summary>
    /// For how long editable link is valid
    /// </summary>
    public TimeSpan EditLinkValidTo { get; set; } = TimeSpan.FromDays(5);

    /// <summary>
    /// Number of days added to link valid date
    /// </summary>
    public int ProlongDays { get; set; } = 5;

    /// <summary>
    /// Restriction options
    /// </summary>
    public ExternalReviewRestrictionOptions Restrictions { get; } = new ExternalReviewRestrictionOptions();

    /// <summary>
    /// PIN Code security options
    /// </summary>
    public PinCodeSecurityOptions PinCodeSecurity { get; } = new PinCodeSecurityOptions();

    /// <summary>
    /// Intercept all calls to ContentLoader.GetChildren.
    /// This settings may greatly affect the performance, and is very intrusive into the regular episerver page lifecycle
    /// </summary>
    public bool InterceptGetChildren { get; set; } = false;

    /// <summary>
    /// Maximum number of content loads expected per request in external review context.
    /// A warning is logged when this threshold is exceeded, which may indicate recursive content loading.
    /// </summary>
    public int MaxExpectedContentLoadsPerRequest { get; set; } = 100;
}

public class ExternalReviewRestrictionOptions
{
    /// <summary>
    /// Maximum number of review locations that can be added to the page
    /// </summary>
    public int MaxReviewLocationsForContent { get; set; } = int.MaxValue;

    /// <summary>
    /// Maximum number of comments that can be added to one review location
    /// </summary>
    public int MaxCommentsForReviewLocation { get; set; } = int.MaxValue;

    /// <summary>
    /// maximum length of comment
    /// </summary>
    public int MaxCommentLength { get; set; } = int.MaxValue;
}

public class PinCodeSecurityOptions
{
    /// <summary>
    /// When true, then PIN code check is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// When true, it would not be possible to create a new a new link without a PIN
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>
    /// URL for login page
    /// </summary>
    public string ExternalReviewLoginUrl { get; set; } = "ExternalReviewLogin";

    /// <summary>
    /// Roles that can access links without PIN
    /// </summary>
    public IEnumerable<string> RolesWithoutPin { get; set; } = new[] {"WebEditors", "WebAdmins"};

    /// <summary>
    /// For how long authentication cookie should be valid
    /// </summary>
    public TimeSpan AuthenticationCookieLifeTime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// PIN code length
    /// </summary>
    public int CodeLength { get; set; } = 4;
}
