using EPiServer.ServiceLocation;

namespace Alloy.Sample.Business
{
    /// <summary>
    /// Provides extension methods for types intended to be used when working with page types
    /// </summary>
    public static class PageTypeExtensions
    {
        /// <summary>
        /// Returns the definition for a specific page type
        /// </summary>
        /// <param name="pageType"></param>
        /// <returns></returns>
        public static ContentType GetPageType(this Type pageType)
        {
            var pageTypeRepository = ServiceLocator.Current.GetInstance<IContentTypeRepository>();

            return pageTypeRepository.Load(pageType);
        }
    }
}
