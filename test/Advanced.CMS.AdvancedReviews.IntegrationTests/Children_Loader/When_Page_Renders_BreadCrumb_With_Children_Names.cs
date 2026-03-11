using System.Net;
using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using HtmlAgilityPack;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Children_Loader;

[Collection(IntegrationTestCollection.Name)]
public class When_Page_Renders_BreadCrumb_With_Children_Names(
    When_Page_Renders_BreadCrumb_With_Children_Names.TestFixture fixture)
    : IClassFixture<CommonFixture>, IClassFixture<When_Page_Renders_BreadCrumb_With_Children_Names.TestFixture>
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
            Page = ContentRepository.CreatePage().ShowBreadcrumbs().PublishPage();
            await Task.CompletedTask;
        }

        public async Task DisposeAsync() => await ContentRepository.CleanupAsync(Page);
    }

    [Fact]
    public async Task Get_Children_Call_Does_Not_Return_Unpublished_Content_Items()
    {
        fixture.Page.UpdatePage();

        var url = fixture.UrlResolver.GetUrl(fixture.Page);
        var originalMessage = new HttpRequestMessage(HttpMethod.Get, url);
        var originalResponse = await fixture.Client.SendAsync(originalMessage);
        Assert.Equal(HttpStatusCode.OK, originalResponse.StatusCode);
        var originalResponseText = await originalResponse.Content.ReadAsStringAsync();
        var originalDocument = new HtmlDocument();
        originalDocument.LoadHtml(originalResponseText);
        var h1Node = originalDocument.DocumentNode.SelectSingleNode("//h1");
        Assert.DoesNotContain(StaticTexts.UpdatedString, h1Node.InnerText);
        var originalHtmlNodeWithChildren =
            originalDocument.DocumentNode.SelectSingleNode(
                $"//*[@id='{StandardPage.PageNameFromGetChildrenCallNodeName}']");
        Assert.NotNull(originalHtmlNodeWithChildren);
        Assert.DoesNotContain(StaticTexts.UpdatedString, originalHtmlNodeWithChildren.InnerText);

        var reviewLink = fixture.Page.GenerateExternalReviewLink();
        var reviewMessage = new HttpRequestMessage(HttpMethod.Get, reviewLink.LinkUrl);
        var reviewResponse = await fixture.Client.SendAsync(reviewMessage);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        var reviewResponseText = await reviewResponse.Content.ReadAsStringAsync();
        var reviewDocument = new HtmlDocument();
        reviewDocument.LoadHtml(reviewResponseText);
        var reviewH1Node = reviewDocument.DocumentNode.SelectSingleNode("//h1");
        Assert.Contains(StaticTexts.UpdatedString, reviewH1Node.InnerText);

        var reviewHtmlNodeWithChildren =
            reviewDocument.DocumentNode.SelectSingleNode(
                $"//*[@id='{StandardPage.PageNameFromGetChildrenCallNodeName}']");
        Assert.NotNull(reviewHtmlNodeWithChildren);

        // this is not updated because `GetChildren` call is not intercepted via DraftContentLoader
        Assert.DoesNotContain(StaticTexts.UpdatedString, reviewHtmlNodeWithChildren.InnerText);
    }
}
