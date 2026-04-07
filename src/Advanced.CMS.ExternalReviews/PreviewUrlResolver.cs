using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Applications;
using EPiServer.Cms.Shell;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.ExternalReviews;

internal class PreviewUrlResolver(
    IUrlResolver defaultUrlResolver,
    IContentLoader contentLoader,
    IPermanentLinkMapper permanentLinkMapper,
    IContentProviderManager providerManager,
    ExternalReviewState externalReviewState,
    ExternalReviewUrlGenerator externalReviewUrlGenerator,
    IOptions<ExternalReviewOptions> externalReviewOptions,
    IApplicationResolver siteDefinitionResolver)
    : IUrlResolver
{
    private const string PreviewGenerated = "preview_generated";

    public static bool IsGeneratedForProjectPreview(NameValueCollection queryString)
    {
        return queryString[PreviewGenerated] != null;
    }

    public string GetVirtualPath(ContentReference contentLink, string language,
        UrlResolverArguments virtualPathArguments)
    {
        var virtualPathData = defaultUrlResolver.GetUrl(contentLink, language, virtualPathArguments);
        if (!externalReviewState.IsInExternalReviewContext || virtualPathData == null)
        {
            return virtualPathData;
        }

        var content = contentLoader.Get<IContent>(contentLink);
        if (ShouldGenerateProjectModeUrlPostfix(content))
        {
            var virtualPath = GetAccessibleVirtualPath(virtualPathData, content as PageData, language);
            virtualPathData = AppendGeneratedPostfix(virtualPath);
        }

        if (content is ImageData imageData)
        {
            virtualPathData = externalReviewUrlGenerator.GetProxiedImageUrl(imageData.ContentLink);

        }

        return virtualPathData;
    }

    private bool ShouldGenerateProjectModeUrlPostfix(IContent content)
    {
        if (!externalReviewState.IsInProjectReviewContext)
        {
            return false;
        }

        if (content is not PageData)
        {
            return false;
        }

        var externalReviewLinksRepository = ServiceLocator.Current.GetInstance<IExternalReviewLinksRepository>();
        var hasPinCode = externalReviewLinksRepository.HasPinCode(externalReviewState.Token);
        var pageParentSite = siteDefinitionResolver.GetByContent(content.ContentLink, false);
        return !hasPinCode || pageParentSite == siteDefinitionResolver.GetByContext();
    }

    /// <summary>
    /// Get a virtual path that can be accessed. If URL segment has been changed we need
    /// to return the original (published) version's URL segment for the routing to work.
    /// </summary>
    private string GetAccessibleVirtualPath(string url, PageData data, string language)
    {
        var virtualPath = url;
        var provider = providerManager.ProviderMap.GetProvider(data.ContentLink.ProviderName);
        var masterContent = (PageData)provider.GetScatteredContents(
            new[] { data.ContentLink.ToReferenceWithoutVersion() },
            new LanguageSelector(language ?? data.LanguageBranch())).FirstOrDefault();
        if (masterContent != null)
        {
            var urlSegment = data.URLSegment;
            var masterContentUrlSegment = masterContent.URLSegment;
            if (masterContentUrlSegment != urlSegment)
            {
                var count = Regex.Matches(virtualPath, Regex.Escape(urlSegment)).Count;
                // If there are multiple occurrences we should just skip the replace to avoid problems
                if (count == 1)
                {
                    virtualPath = virtualPath.Replace(urlSegment, masterContentUrlSegment);
                }
            }
        }

        return virtualPath;
    }

    public string GetUrl(ContentReference contentLink, string language, UrlResolverArguments urlResolverArguments)
    {
        var url = defaultUrlResolver.GetUrl(contentLink, language, urlResolverArguments);
        if (!externalReviewState.IsInExternalReviewContext || url == null)
        {
            return url;
        }

        return GetInternalUrl(contentLink, url);
    }

    public string GetUrl(UrlBuilder urlBuilderWithInternalUrl, UrlResolverArguments arguments)
    {
        var url = defaultUrlResolver.GetUrl(urlBuilderWithInternalUrl, arguments);
        if (!externalReviewState.IsInExternalReviewContext || url == null)
        {
            return url;
        }

        var linkMap = permanentLinkMapper.Find(urlBuilderWithInternalUrl);
        return linkMap != null ? GetInternalUrl(linkMap.ContentReference, url) : url;
    }

    /// <summary>
    /// Appends project info or proxies the images through custom image handler to bypass security checks
    /// </summary>
    /// <param name="contentLink"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    private string GetInternalUrl(ContentReference contentLink, string url)
    {
        var content = contentLoader.Get<IContent>(contentLink);
        if (ShouldGenerateProjectModeUrlPostfix(content))
        {
            return AppendGeneratedPostfix(url);
        }

        if (content is ImageData imageData)
        {
            return externalReviewUrlGenerator.GetProxiedImageUrl(imageData.ContentLink);
        }

        return url;
    }

    public bool TryToPermanent(string url, out string permanentUrl)
    {
        return defaultUrlResolver.TryToPermanent(url, out permanentUrl);
    }

    public ContentRouteData Route(UrlBuilder urlBuilder, RouteArguments routeArguments)
    {
        var contentRouteData = defaultUrlResolver.Route(urlBuilder, routeArguments);

        if (contentRouteData?.RemainingPath == null)
        {
            return contentRouteData;
        }

        if (contentRouteData.RemainingPath.StartsWith($"{externalReviewOptions.Value.ContentPreviewUrl}/", StringComparison.CurrentCultureIgnoreCase))
        {
            // If we failed to route then it means that a start page in the same language does not exist and our partial routers will not be able to step in
            // We need to fallback to master language also if the translated start page is not published
            if (contentRouteData.Content == null || !contentRouteData.Content.IsPublished())
            {
                var masterLanguage = LanguageSelector.AutoDetect().LanguageBranch;
                urlBuilder.Path = urlBuilder.Path.Replace($"/{contentRouteData.RouteLanguage}", $"/{masterLanguage}");
                var masterRoutedData = defaultUrlResolver.Route(urlBuilder, routeArguments);
                return masterRoutedData;
            }
        }

        return contentRouteData;
    }

    private string AppendGeneratedPostfix(string url)
    {
        var urlBuilder = new UrlBuilder(UrlPath.Combine(url, externalReviewOptions.Value.ContentPreviewUrl,
            externalReviewState.Token));
        urlBuilder.QueryCollection.Add(PreviewGenerated, bool.TrueString);
        return urlBuilder.ToString();
    }
}
