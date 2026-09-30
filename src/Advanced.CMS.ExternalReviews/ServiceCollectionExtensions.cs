using Advanced.CMS.ExternalReviews.EditReview;
using Advanced.CMS.ExternalReviews.PinCodeSecurity;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Core.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Advanced.CMS.ExternalReviews;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExternalReviews(this IServiceCollection services)
    {
        services.AddTransient<ExternalReviewUrlGenerator>();
        services.AddTransient<ExternalReviewLinkBuilder>();
        services.AddTransient<PropertyResolver>();
        services.AddTransient<ProjectContentResolver>();
        services.AddTransient<IExternalReviewLinksRepository, ExternalReviewLinksRepository>();
        services.AddTransient<IExternalLinkPinCodeSecurityHandler, DefaultExternalLinkPinCodeSecurityHandler>();
        services.AddTransient<IPartialRouter, PageEditPartialRouter>();
        services.AddTransient<IPartialRouter, PagePreviewPartialRouter>();
        services.AddSingleton<ExternalReviewState>();
        services.AddTransient<DraftChildrenLoader>();

        var builder = services.AddControllers(options =>
        {
            options.Filters.Add<PinCodeSecurityAuthorizationFilter>(int.MinValue);
        });
        builder.ConfigureApplicationPartManager(manager =>
        {
            manager.FeatureProviders.Add(new InternalApiControllerFeatureProvider());
        });

        return services;
    }
}
