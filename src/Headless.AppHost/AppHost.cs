var builder = DistributedApplication.CreateBuilder(args);

var cms = builder.AddDotnetProject("alloy", "../Alloy.Sample/Alloy.Sample.csproj",
        options => options.LaunchProfileName = "Kestrel (Env: Development)")
    .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
    .WithHttpHealthCheck("/Util/Login");

builder.AddDotnetProject("headless", "../Headless.Sample/Headless.Sample.csproj",
        options => options.LaunchProfileName = "Headless.Sample")
    .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
    .WithEnvironment("Cms__Url", cms.GetEndpoint("http"))
    .WaitFor(cms);

builder.Eventing.Subscribe<ResourceReadyEvent>(cms.Resource, (_, _) =>
{
    Console.WriteLine();
    Console.WriteLine($"CMS is ready: {cms.GetEndpoint("http").Url}/Optimizely/CMS");
    Console.WriteLine();
    return Task.CompletedTask;
});

builder.Build().Run();
