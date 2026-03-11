using System.Net;
using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using Advanced.CMS.ExternalReviews;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Multilanguage;

[Collection(IntegrationTestCollection.Name)]
public class When_Translating_Page(When_Translating_Page.TestFixture fixture)
    : IClassFixture<CommonFixture>, IClassFixture<When_Translating_Page.TestFixture>
{
    public class TestFixture(SiteFixture siteFixture) : IAsyncLifetime
    {
        public IContentRepository ContentRepository { get; } = siteFixture.Services.GetInstance<IContentRepository>();
        public IUrlResolver UrlResolver { get; } = siteFixture.Services.GetInstance<IUrlResolver>();
        public IServiceProvider Services => siteFixture.Services;
        public HttpClient Client { get; } = siteFixture.Client;

        public StandardPage Page { get; private set; }
        public StandardPage TranslatedPage { get; private set; }

        public async Task InitializeAsync()
        {
            Page = ContentRepository.CreatePage().PublishPage();
            TranslatedPage = Page.Translate();
            await Task.CompletedTask;
        }

        public async Task DisposeAsync() => await ContentRepository.CleanupAsync(Page);
    }

    [Fact]
    public async Task Page_Is_Not_Visible_But_Token_Shows_Unpublished_Content()
    {
        var url = fixture.UrlResolver.GetUrl(fixture.Page);
        var originalMessage = new HttpRequestMessage(HttpMethod.Get, url);
        var originalResponse = await fixture.Client.SendAsync(originalMessage);
        Assert.Equal(HttpStatusCode.OK, originalResponse.StatusCode);
        var originalResponseText = await originalResponse.Content.ReadAsStringAsync();
        Assert.Contains(fixture.Page.PageName, originalResponseText);

        var urlSv = fixture.UrlResolver.GetUrl(fixture.TranslatedPage.ContentLink);
        var originalMessageSv = new HttpRequestMessage(HttpMethod.Get, urlSv);
        var originalResponseSv = await fixture.Client.SendAsync(originalMessageSv);
        Assert.Equal(HttpStatusCode.NotFound, originalResponseSv.StatusCode);

        var reviewLink = fixture.TranslatedPage.GenerateExternalReviewLink();

        var messageReviewLink = new HttpRequestMessage(HttpMethod.Get, reviewLink.LinkUrl);
        var responseReviewLink = await fixture.Client.SendAsync(messageReviewLink);
        var responseTextReviewLink = await responseReviewLink.Content.ReadAsStringAsync();
        Assert.Contains(fixture.Page.PageName + "_sv", responseTextReviewLink);

        var maxExpected = fixture.Services.GetInstance<ExternalReviewOptions>().MaxExpectedContentLoadsPerRequest;
        Assert.True(
            CustomContentLoaderInitialization.LastRequestContentLoadCount < maxExpected,
            $"Content load count ({CustomContentLoaderInitialization.LastRequestContentLoadCount}) exceeded threshold ({maxExpected}). Possible recursive content loading.");
    }
}

