using System.Net;
using System.Web;
using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Applications;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.HeadlessPreview;

[Collection(IntegrationTestCollection.Name)]
public class When_Content_Is_Rendered_By_Remote_Website(When_Content_Is_Rendered_By_Remote_Website.TestFixture fixture)
    : IClassFixture<CommonFixture>,
      IClassFixture<When_Content_Is_Rendered_By_Remote_Website.TestFixture>
{
    private const string RemoteHost = "headless.example.com";

    public class TestFixture(SiteFixture siteFixture) : IAsyncLifetime
    {
        private readonly IApplicationRepository _applicationRepository = siteFixture.Services.GetInstance<IApplicationRepository>();
        private Website _website;
        private StartPage _start;

        public IContentRepository ContentRepository { get; } = siteFixture.Services.GetInstance<IContentRepository>();
        public IExternalReviewLinksRepository LinksRepository { get; } = siteFixture.Services.GetInstance<IExternalReviewLinksRepository>();
        public IPreviewTokenService PreviewTokenService { get; } = siteFixture.Services.GetInstance<IPreviewTokenService>();
        public HttpClient Client { get; } = siteFixture.CreateClientWithoutRedirects();

        public StandardPage Page { get; private set; }

        public async Task InitializeAsync()
        {
            _start = ContentRepository.GetDefault<StartPage>(ContentReference.RootPage);
            _start.Name = "Headless start";
            ContentRepository.Save(_start, SaveAction.Publish, AccessLevel.NoAccess);

            Page = ContentRepository.GetDefault<StandardPage>(_start.ContentLink);
            Page.PageName = "headless page";
            ContentRepository.Save(Page, AccessLevel.NoAccess);

            _website = new Website("Headless", _start.ContentLink.ToReferenceWithoutVersion())
            {
                UsePreviewTokens = true
            };
            _website.Hosts.Add(new ApplicationHost(RemoteHost)
            {
                PreferredUrlScheme = UrlScheme.Https,
                Type = ApplicationHostType.Primary
            });
            _website.PreviewUrlFormats[UrlFormatKey.Any] = new UrlFormat("{host}/preview?key={key}&ver={version}&loc={locale}");
            await _applicationRepository.SaveAsync(_website);
        }

        public async Task DisposeAsync()
        {
            await _applicationRepository.DeleteAsync(_website.Name);
            ContentRepository.Delete(_start.ContentLink.ToReferenceWithoutVersion(), true, AccessLevel.NoAccess);
        }
    }

    [Fact]
    public void View_Link_Points_To_Review_Redirect()
    {
        var link = CreateViewLink();

        Assert.Equal($"/advanced-reviews/preview/{link.Token}", link.LinkUrl);
    }

    [Fact]
    public async Task Opening_Link_Redirects_To_Remote_Preview_With_New_Preview_Token()
    {
        var link = CreateViewLink();

        var first = await fixture.Client.GetAsync(link.LinkUrl);
        var second = await fixture.Client.GetAsync(link.LinkUrl);

        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        var location = first.Headers.Location;
        Assert.Equal(RemoteHost, location.Host);
        var query = HttpUtility.ParseQueryString(location.Query);
        Assert.Equal(fixture.Page.ContentGuid.ToString("N"), query["key"]);
        Assert.Equal(fixture.Page.ContentLink.WorkID.ToString(), query["ver"]);

        var previewToken = query["preview_token"];
        Assert.False(string.IsNullOrEmpty(previewToken));
        Assert.True(fixture.PreviewTokenService.TryValidatePreviewToken(previewToken, out _, out var principal));
        Assert.True(principal.IsInRole("Everyone"));
        Assert.True(principal.IsInRole("Anonymous"));
        Assert.False(principal.IsInRole("WebEditors"));

        var secondToken = HttpUtility.ParseQueryString(second.Headers.Location.Query)["preview_token"];
        Assert.NotEqual(previewToken, secondToken);
    }

    [Fact]
    public async Task Expired_Link_Returns_Not_Found()
    {
        var link = CreateViewLink();
        fixture.LinksRepository.UpdateLink(link.Token, DateTime.Now.AddDays(-1), null, null, null);

        var response = await fixture.Client.GetAsync(link.LinkUrl);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Editable_Link_Token_Returns_Not_Found()
    {
        var link = fixture.LinksRepository.AddLink(fixture.Page.ContentLink, true, TimeSpan.FromDays(1), null);

        var response = await fixture.Client.GetAsync($"/advanced-reviews/preview/{link.Token}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private ExternalReviewLink CreateViewLink() =>
        fixture.LinksRepository.AddLink(fixture.Page.ContentLink, false, TimeSpan.FromDays(1), null);
}
