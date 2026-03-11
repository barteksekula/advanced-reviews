using System.Net;
using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Multilanguage;

[Collection(IntegrationTestCollection.Name)]
public class When_Page_Created_In_Other_Language_Than_StartPage(When_Page_Created_In_Other_Language_Than_StartPage.TestFixture fixture)
    : IClassFixture<CommonFixture>, IClassFixture<When_Page_Created_In_Other_Language_Than_StartPage.TestFixture>
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
            Page = ContentRepository.CreatePageInDifferentLanguage().WithoutEveryoneAccess();
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
        Assert.Equal(HttpStatusCode.NotFound, originalResponse.StatusCode);

        fixture.Page.UpdatePage();

        var reviewLink = fixture.Page.GenerateExternalReviewLink();

        var messageReviewLink = new HttpRequestMessage(HttpMethod.Get, reviewLink.LinkUrl);
        var responseReviewLink = await fixture.Client.SendAsync(messageReviewLink);
        var responseTextReviewLink = await responseReviewLink.Content.ReadAsStringAsync();
        Assert.Contains(StaticTexts.UpdatedString, responseTextReviewLink);
    }
}

