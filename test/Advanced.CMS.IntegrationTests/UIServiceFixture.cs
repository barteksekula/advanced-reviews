using EPiServer.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Advanced.CMS.IntegrationTests;

public class UIServiceFixture<TStartup>(
    string connectionString,
    Action<IServiceCollection> customRegistrations = null,
    string contentRoot = null)
    : WebApplicationFactory<TStartup>
    where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (contentRoot != null)
        {
            builder.UseContentRoot(contentRoot);
        }

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(
                new Dictionary<string, string>
                {
                    ["ConnectionStrings:EPiServerDB"] = connectionString
                });
        });
        builder.ConfigureServices(context =>
        {
            //Get existing service registration for DatabaseModeService
            var existingServiceDefinition = context.Single(x => x.ServiceType == typeof(IDatabaseMode));
            //Remove it
            context.Remove(existingServiceDefinition);
            //Register DatabaseModeService with implementation as service type.
            context.AddSingleton(existingServiceDefinition.ImplementationType);
            //Registera a IDatabaseMode service with SwitchableDatabaseMode resolving DatabaseModeService as inner
            context.AddSingleton<IDatabaseMode>((sp) => new SwitchableDatabaseMode(sp.GetService(existingServiceDefinition.ImplementationType) as IDatabaseMode));
        });

        if (customRegistrations != null)
        {
            builder.ConfigureServices(customRegistrations);
        }
    }
}
