using System.Net;
using System.Net.Sockets;
using EPiServer.Applications;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.E2ETests;

/// <summary>
/// Runs a minimal headless website on another origin than the CMS and registers it as a CMS website application.
/// It renders the preview with the CMS communication injector, the way a front-end with on-page editing does.
/// </summary>
public class RemoteWebsiteFixture(WebServerFixture fixture) : IAsyncLifetime
{
    public const string HeadingProperty = "PageName";
    public const string BodyProperty = "Html";
    public const string HeadingText = "Remote heading";

    private const string ApplicationName = "Remote";

    private WebApplication _app;
    private StartPage _startPage;

    public string Authority { get; private set; }

    public async Task InitializeAsync()
    {
        var port = GetRandomUnusedPort();
        Authority = $"127.0.0.1:{port}";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://{Authority}");
        _app = builder.Build();
        _app.MapGet("/preview", (string key, string ctx) => Results.Content(RenderPreview(key, ctx), "text/html; charset=utf-8"));
        await _app.StartAsync();

        var contentRepository = fixture.Services.GetInstance<IContentRepository>();
        _startPage = contentRepository.GetDefault<StartPage>(ContentReference.RootPage);
        _startPage.Name = "Remote start";
        contentRepository.Save(_startPage, SaveAction.Publish, AccessLevel.NoAccess);

        var website = new Website(ApplicationName, _startPage.ContentLink.ToReferenceWithoutVersion())
        {
            UsePreviewTokens = true
        };
        website.Hosts.Add(new ApplicationHost(Authority)
        {
            PreferredUrlScheme = UrlScheme.Http,
            Type = ApplicationHostType.Primary
        });
        website.PreviewUrlFormats[UrlFormatKey.Any] = new UrlFormat("{host}/preview?key={key}&ver={version}&loc={locale}&ctx={context}");
        await fixture.Services.GetInstance<IApplicationRepository>().SaveAsync(website);
    }

    public async Task DisposeAsync()
    {
        await fixture.Services.GetInstance<IApplicationRepository>().DeleteAsync(ApplicationName);
        fixture.Services.GetInstance<IContentRepository>().Delete(_startPage.ContentLink.ToReferenceWithoutVersion(), true, AccessLevel.NoAccess);
        await _app.DisposeAsync();
    }

    public StandardPage CreateDraftPage()
    {
        var contentRepository = fixture.Services.GetInstance<IContentRepository>();
        var page = contentRepository.GetDefault<StandardPage>(_startPage.ContentLink);
        page.Name = "Remote page " + Guid.NewGuid().ToString("N");
        contentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }

    private string RenderPreview(string key, string ctx)
    {
        var injector = ctx == "edit"
            ? $"<script src=\"{fixture.BaseUrl}/util/javascript/communicationinjector.js\"></script>"
            : "";
        return $$"""
            <!doctype html>
            <html>
            <head>
              <meta charset="utf-8">
              {{injector}}
              <style>body { font-family: sans-serif; margin: 0; padding: 24px; }</style>
            </head>
            <body>
              <h1 data-epi-edit="{{HeadingProperty}}">{{HeadingText}}</h1>
              <div data-epi-edit="{{BodyProperty}}" style="height: 400px; background: #f6f8fa">Remote body of {{WebUtility.HtmlEncode(key)}}</div>
            </body>
            </html>
            """;
    }

    private static int GetRandomUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
