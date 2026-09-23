using System.Net;
using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Basic;

[Collection(IntegrationTestCollection.Name)]
public class When_Page_Is_Updated(When_Page_Is_Updated.TestFixture fixture)
    : IClassFixture<CommonFixture>, IClassFixture<When_Page_Is_Updated.TestFixture>
{
    public class TestFixture(SiteFixture siteFixture) : IAsyncLifetime
    {
        public IContentRepository ContentRepository { get; } = siteFixture.Services.GetInstance<IContentRepository>();
        public IUrlResolver UrlResolver { get; } = siteFixture.Services.GetInstance<IUrlResolver>();
        public IServiceProvider Services => siteFixture.Services;
        public HttpClient Client { get; } = siteFixture.Client;

        public StandardPage Page { get; private set; }

        public async Task InitializeAsync()
        {
            Page = ContentRepository.CreatePage().PublishPage();
            await Task.CompletedTask;
        }

        public async Task DisposeAsync() => await ContentRepository.CleanupAsync(Page);
    }

    [Fact]
    public async Task After_Updating_Page_Public_URL_Still_Shows_Published_Content_And_Token_Shows_Draft()
    {
        var url = fixture.UrlResolver.GetUrl(fixture.Page);
        var originalMessage = new HttpRequestMessage(HttpMethod.Get, url);
        var originalResponse = await fixture.Client.SendAsync(originalMessage);
        Assert.Equal(HttpStatusCode.OK, originalResponse.StatusCode);
        var originalResponseText = await originalResponse.Content.ReadAsStringAsync();
        Assert.Contains(fixture.Page.PageName, originalResponseText);

        fixture.Page.UpdatePage();

        var message = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await fixture.Client.SendAsync(message);
        var responseText = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(StaticTexts.UpdatedString, responseText);

        var reviewLink = fixture.Page.GenerateExternalReviewLink();

        var messageReviewLink = new HttpRequestMessage(HttpMethod.Get, reviewLink.LinkUrl);
        var responseReviewLink = await fixture.Client.SendAsync(messageReviewLink);
        var responseTextReviewLink = await responseReviewLink.Content.ReadAsStringAsync();
        Assert.Contains(StaticTexts.UpdatedString, responseTextReviewLink);
    }
}

