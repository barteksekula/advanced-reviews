using EPiServer.Personalization.VisitorGroups;
using EPiServer.Shell;
using EPiServer.Shell.ViewComposition;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.ExternalReviews.ManageLinks;

/// <summary>
/// Edit Mode component used to manage list of external review links
/// </summary>
internal class ExternalReviewLinksManageComponent : ComponentDefinitionBase
{
    private readonly ExternalReviewOptions _options;
    private readonly IVisitorGroupRepository _visitorGroupRepository;

    public ExternalReviewLinksManageComponent(IOptions<ExternalReviewOptions> options, IVisitorGroupRepository visitorGroupRepository)
        : base("advanced-cms-approval-reviews/dist/external-review-manage-links-widget")
    {
        _options = options.Value;
        _visitorGroupRepository = visitorGroupRepository;
        IsAvailableForUserSelection = _options.IsEnabled;
        LanguagePath = "/externalreviews/component";
        Categories = new[] {"content"};
        SortOrder = 1000;
        PlugInAreas = new[]
        {
            PlugInArea.Navigation
        };
    }

    public override ISettingsDictionary Settings {
        get
        {
            var settings = base.Settings.Copy();

            settings["initialMailSubject"] = _options.EmailSubject;
            settings["initialEditMailMessage"] = _options.EmailEdit;
            settings["initialViewMailMessage"] = _options.EmailView;
            settings["editableLinksEnabled"] = _options.EditableLinksEnabled;
            settings["pinCodeSecurityEnabled"] = _options.PinCodeSecurity.Enabled;
            settings["pinCodeSecurityRequired"] = _options.PinCodeSecurity.Required;
            settings["availableVisitorGroups"] = _visitorGroupRepository.List();
            settings["pinCodeLength"] = _options.PinCodeSecurity.CodeLength;
            settings["isEnabled"] = _options.IsEnabled;
            settings["prolongDays"] = _options.ProlongDays;

            return settings;
        }
    }
}
