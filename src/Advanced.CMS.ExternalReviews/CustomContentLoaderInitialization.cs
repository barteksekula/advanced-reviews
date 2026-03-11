using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.Web;

namespace Advanced.CMS.ExternalReviews;

[ModuleDependency(typeof(InitializationModule))]
internal class CustomContentLoaderInitialization : IInitializableModule
{
    public void Initialize(InitializationEngine context)
    {
        var events = ServiceLocator.Current.GetInstance<IContentEvents>();
        events.LoadingContent += Events_LoadingContent;
    }

    public void Uninitialize(InitializationEngine context)
    {
        var events = ServiceLocator.Current.GetInstance<IContentEvents>();
        events.LoadingContent -= Events_LoadingContent;
    }

    private void Events_LoadingContent(object sender, ContentEventArgs e)
    {
        var externalReviewState = ServiceLocator.Current.GetInstance<ExternalReviewState>();

        if (!externalReviewState.IsInExternalReviewContext)
        {
            return;
        }

        if (externalReviewState.CustomLoaded.Contains(e.ContentLink.ToString()))
        {
            var cachedContent = externalReviewState.GetCachedContent(e.ContentLink);
            if (cachedContent != null)
            {
                e.ContentLink = cachedContent.ContentLink;
                e.Content = cachedContent;
                e.CancelAction = true;
            }

            return;
        }

        externalReviewState.CustomLoaded.Add(e.ContentLink.ToString());

        var unpublished = e.ContentLink.LoadUnpublishedVersion();
        if (unpublished == null)
        {
            return;
        }

        var externalReviewLinksRepository = ServiceLocator.Current.GetInstance<IExternalReviewLinksRepository>();
        var externalReviewLink = externalReviewLinksRepository.GetContentByToken(externalReviewState.Token);
        if (externalReviewLink!= null && externalReviewLink.IsExpired())
        {
            return;
        }

        var content = ServiceLocator.Current.GetInstance<IContentLoader>().Get<IContent>(unpublished);

        if (content is not IVersionable versionable || versionable.HasExpired())
        {
            return;
        }

        externalReviewState.SetCachedLink(content);

        e.ContentLink = unpublished;
        e.Content = content;
        e.CancelAction = true;
    }
}
