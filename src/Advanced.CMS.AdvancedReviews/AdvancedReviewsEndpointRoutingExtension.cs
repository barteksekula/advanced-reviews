using Advanced.CMS.ExternalReviews;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.AdvancedReviews;

internal class AdvancedReviewsEndpointRoutingExtension : IEndpointRoutingExtension
{
    public void MapEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        var options = endpointRouteBuilder.ServiceProvider.GetInstance<IOptions<ExternalReviewOptions>>();

        endpointRouteBuilder.MapControllerRoute("ImageProxy", "/ImageProxy/{token}/{contentLink}",
            new { controller = "ImageProxy", action = "Index" }).AllowAnonymous();

        endpointRouteBuilder.MapControllerRoute("ExternalReviewLogin",
            $"/{options.Value.PinCodeSecurity.ExternalReviewLoginUrl}",
            new { controller = "ExternalReviewLogin", action = "Index" });

        endpointRouteBuilder.MapControllerRoute("ExternalReviewLoginSubmit",
            $"/{options.Value.PinCodeSecurity.ExternalReviewLoginUrl}",
            new { controller = "ExternalReviewLogin", action = "Submit" },
            new { httpMethod = new HttpMethodRouteConstraint(HttpMethods.Post) });

        MapAnonymousEditableLinkRoutes(endpointRouteBuilder);
    }

    private static void MapAnonymousEditableLinkRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        const string prefix = ExternalReviewUrlGenerator.AnonymousRoutePrefix;
        var get = new { httpMethod = new HttpMethodRouteConstraint(HttpMethods.Get) };
        var post = new { httpMethod = new HttpMethodRouteConstraint(HttpMethods.Post) };

        MapExternalReviewEditRoute(endpointRouteBuilder, $"/{prefix}/edit/AddPin", "AddPin", post);
        MapExternalReviewEditRoute(endpointRouteBuilder, $"/{prefix}/edit/RemovePin", "RemovePin", post);
        MapExternalReviewEditRoute(endpointRouteBuilder, $"/{prefix}/edit/{{id}}", "Index", get);
        MapExternalReviewEditRoute(endpointRouteBuilder, $"/{prefix}/resources/{{id}}", "Resource", get);
        MapExternalReviewEditRoute(endpointRouteBuilder, $"/{prefix}/avatar/{{id}}", "Avatar", get);
    }

    private static void MapExternalReviewEditRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern,
        string action, object constraints)
    {
        endpointRouteBuilder.MapControllerRoute($"ExternalReviewEdit{action}", pattern,
            new { controller = "ExternalReviewEdit", action }, constraints).AllowAnonymous();
    }
}
