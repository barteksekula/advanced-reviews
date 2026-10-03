using System.Net;
using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.ServiceLocation;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.AnonymousEditableLinks;

[Collection(IntegrationTestCollection.Name)]
public class When_Anonymous_Editable_Links_Disabled(When_Anonymous_Editable_Links_Disabled.TestFixture fixture)
    : IClassFixture<CommonFixture>,
      IClassFixture<AnonymousEditableLinksDisabledFixture>,
      IClassFixture<When_Anonymous_Editable_Links_Disabled.TestFixture>
{
    public class TestFixture(SiteFixture siteFixture) : IAsyncLifetime
    {
        public IContentRepository ContentRepository { get; } = siteFixture.Services.GetInstance<IContentRepository>();
        public IExternalReviewLinksRepository LinksRepository { get; } = siteFixture.Services.GetInstance<IExternalReviewLinksRepository>();
        public HttpClient Client { get; } = siteFixture.CreateClientWithoutRedirects();

        public StandardPage Page { get; private set; }

        public Task InitializeAsync()
        {
            Page = ContentRepository.CreatePage().WithoutEveryoneAccess();
            return Task.CompletedTask;
        }

        public async Task DisposeAsync() => await ContentRepository.CleanupAsync(Page);
    }

    [Fact]
    public async Task Anonymous_User_Is_Redirected_To_Login()
    {
        var link = fixture.LinksRepository.AddLink(fixture.Page.ContentLink, true, TimeSpan.FromDays(1), null);

        var response = await fixture.Client.GetAsync(link.LinkUrl);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Login", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Logged_In_User_Sees_Editable_Review_Page()
    {
        var link = fixture.LinksRepository.AddLink(fixture.Page.ContentLink, true, TimeSpan.FromDays(1), null);

        var response = await fixture.Client.GetAsync(link.LinkUrl + "?username=reviewer&roles=WebEditors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("reviews-editor", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Anonymous_Routes_Return_404()
    {
        var link = fixture.LinksRepository.AddLink(fixture.Page.ContentLink, true, TimeSpan.FromDays(1), null);

        var response = await fixture.Client.GetAsync($"/advanced-reviews/edit/{link.Token}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
