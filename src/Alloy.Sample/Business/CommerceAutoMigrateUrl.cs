// using EPiServer.Commerce.Internal.Migration;
// using EPiServer.Framework;
// using EPiServer.Framework.Initialization;
// using EPiServer.ServiceLocation;
// using Microsoft.Extensions.DependencyInjection.Extensions;
//
// namespace Alloy.Sample.Business;
//
// [ModuleDependency(typeof(MigrationInitializationModule))]
// public class CommerceAutoMigrateUrlInitializer : IConfigurableModule
// {
//     public void ConfigureContainer(ServiceConfigurationContext context)
//     {
//         context.Services.RemoveAll(typeof(MigrateActionUrlResolver));
//         context.Services.AddSingleton((MigrateActionUrlResolver) (
//             ( _,  action) => $"EPiServer/Commerce/Migrate/{action}"));
//     }
//
//     public void Initialize(InitializationEngine context)
//     {
//     }
//
//     public void Uninitialize(InitializationEngine context)
//     {
//     }
// }
