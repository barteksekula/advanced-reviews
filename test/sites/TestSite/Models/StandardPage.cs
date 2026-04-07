using System.ComponentModel.DataAnnotations;
using EPiServer.Applications;
using EPiServer.ServiceLocation;
using EPiServer.Web;

namespace TestSite.Models;

[ContentType(GUID = "85FC1C77-0040-4D05-AF42-14C165D882C7")]
public class StandardPage : PageData
{
    public virtual bool ShowBreadcrumbs { get; set; }
    public virtual ContentArea ContentArea { get; set; }
    public virtual XhtmlString Html { get; set; }

    [UIHint(UIHint.Image)]
    public virtual ContentReference Image { get; set; }

    public const string PageNameFromGetChildrenCallNodeName = "page-name-from-get-children-call";

    public string PageNameFromGetChildrenCall
    {
        get
        {
            var iContentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            var iApplicationRepository = ServiceLocator.Current.GetInstance<IApplicationRepository>();
            return iContentRepository.GetChildren<PageData>((iApplicationRepository.List().First() as InProcessWebsite)
                    .EntryPoint).FirstOrDefault()?.Name;
        }
    }
}
