using Advanced.CMS.ExternalReviews.DraftContentAreaPreview;
using EPiServer.Applications;
using EPiServer.DependencyInjection;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.ExternalReviews;

/// <summary>
/// Register ContentArea draft preview
/// </summary>
[ModuleDependency(typeof(InitializationModule))]
internal class ExternalReviewsInitializationModule : IConfigurableModule
{
    public void ConfigureContainer(ServiceConfigurationContext context)
    {
        context.ConfigurationComplete += (_, _) =>
        {
            // Intercepted to rewrite urls of content items which belong to the same project
            context.Services.Intercept<IUrlResolver>(
                (locator, defaultUrlResolver) =>
                    new PreviewUrlResolver(defaultUrlResolver, locator.GetInstance<IContentLoader>(),
                        locator.GetInstance<IPermanentLinkMapper>(), locator.GetInstance<IContentProviderManager>(),
                        locator.GetInstance<ExternalReviewState>(),
                        locator.GetInstance<ExternalReviewUrlGenerator>(),
                        locator.GetInstance<IOptions<ExternalReviewOptions>>(),
                        locator.GetInstance<IApplicationResolver>()));

            // Intercepted in order to return unpublished content items
            context.Services.Intercept<IPublishedStateAssessor>(
                (locator, defaultPublishedStateAssessor) =>
                    new PublishedStateAssessorDecorator(defaultPublishedStateAssessor, locator.GetInstance<ExternalReviewState>()));

            // Intercepted in order to not filter out content without Everyone access, used in project mode
            context.Services.Intercept<IContentAccessEvaluator>(
                (locator, defaultContentAccessEvaluator) =>
                    new ContentAccessEvaluatorDecorator(defaultContentAccessEvaluator,
                        locator.GetInstance<ExternalReviewState>()));
        };
    }

    public void Initialize(InitializationEngine context)
    {
    }

    public void Uninitialize(InitializationEngine context)
    {
    }
}
