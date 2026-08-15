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
    }
}
