using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;

namespace Alloy.Sample.Business.EditorDescriptors
{
    /// <summary>
    /// Provides a list of options corresponding to ContactPage pages on the site
    /// </summary>
    /// <seealso cref="ContactPageSelector"/>
    [ServiceConfiguration]
    public class ContactPageSelectionFactory : ISelectionFactory
    {
        private readonly ContentLocator _contentLocator;

        public ContactPageSelectionFactory(ContentLocator contentLocator)
        {
            _contentLocator = contentLocator;
        }

        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            var contactPages = _contentLocator.GetContactPages();

            return new List<SelectItem>(contactPages.Select(c => new SelectItem { Value = c.ContentLink, Text = c.Name }));
        }
    }
}
