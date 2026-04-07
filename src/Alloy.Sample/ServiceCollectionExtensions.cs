using System.IO;
using EPiServer.Framework.Hosting;
using EPiServer.Web.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Alloy.Sample
{
    internal static class InternalServiceCollectionExtensions
    {
        /// <internal-api/>
        public static IServiceCollection AddUIMappedFileProviders(this IServiceCollection services, string applicationRootPath, string uiSolutionRelativePath)
        {
            var uiSolutionFolder = Path.Combine(applicationRootPath, uiSolutionRelativePath);
            services.Configure<CompositeFileProviderOptions>(c =>
            {
                c.BasePathFileProviders.Add(new MappingPhysicalFileProvider("/Optimizely/advanced-cms-external-reviews", string.Empty, Path.Combine(uiSolutionFolder, @"src\Advanced.CMS.ExternalReviews")));
                c.BasePathFileProviders.Add(new MappingPhysicalFileProvider("/Optimizely/advanced-cms-approval-reviews", string.Empty, Path.Combine(uiSolutionFolder, @"src\Advanced.CMS.ApprovalReviews")));
            });
            return services;
        }
    }
}
