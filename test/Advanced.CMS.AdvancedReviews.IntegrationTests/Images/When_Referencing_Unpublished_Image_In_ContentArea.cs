using System.Net;
using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using HtmlAgilityPack;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Images;

[Collection(IntegrationTestCollection.Name)]
public class When_Referencing_Unpublished_Image_In_ContentArea(When_Referencing_Unpublished_Image_In_ContentArea.TestFixture fixture)
    : IClassFixture<CommonFixture>, IClassFixture<When_Referencing_Unpublished_Image_In_ContentArea.TestFixture>
{
    public class TestFixture(SiteFixture siteFixture) : IAsyncLifetime
    {
        public IContentRepository ContentRepository { get; } = siteFixture.Services.GetInstance<IContentRepository>();
        public IUrlResolver UrlResolver { get; } = siteFixture.Services.GetInstance<IUrlResolver>();
        public IServiceProvider Services => siteFixture.Services;
        public HttpClient Client { get; } = siteFixture.Client;
        public FakeImageFactory FakeImageFactory = new();

        public StandardPage DraftPage { get; private set; }
        public ImageFile DraftImage { get; private set; }
        public ExternalReviewLink GeneratedReviewLink { get; private set; }
        public Media FakeImage { get; private set; }

        public async Task InitializeAsync()
        {
            FakeImage = FakeImageFactory.GetMediaItems().First();
            DraftImage = ContentRepository.CreateDraftImage(FakeImage);
            DraftPage = ContentRepository.CreatePage().ReferenceUnpublishedImageInContentArea(DraftImage.ContentLink).PublishPage();
            GeneratedReviewLink = DraftPage.GenerateExternalReviewLink();

            await Task.CompletedTask;
        }

        public async Task DisposeAsync() => await ContentRepository.CleanupAsync(DraftPage);
    }

    [Fact]
    public async Task Standard_Url_Image_Is_Not_Returned()
    {
        var url = fixture.UrlResolver.GetUrl(fixture.DraftPage);
        var message = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await fixture.Client.SendAsync(message);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(content);
        var imgNode = htmlDocument.DocumentNode.SelectNodes("//img").First();
        Assert.NotNull(imgNode);

        var src = imgNode.GetAttributeValue("src", string.Empty);
        Assert.Empty(src);
    }

    [Fact(Skip = "Fix image proxy after CMS13 upgrade")]
    public async Task Review_Link_Returns_200_And_Image_Is_Proxied()
    {
        var message = new HttpRequestMessage(HttpMethod.Get, fixture.GeneratedReviewLink.LinkUrl);
        var response = await fixture.Client.SendAsync(message);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(content);
        var imgNode = htmlDocument.DocumentNode.SelectNodes("//img").First();
        Assert.NotNull(imgNode);
        var expectedUrl = $"/ImageProxy/{fixture.GeneratedReviewLink.Token}/{fixture.DraftImage.ContentLink}";

        var src = imgNode.GetAttributeValue("src", string.Empty);
        Assert.Equal(expectedUrl, src);

        var imageRequest = new HttpRequestMessage(HttpMethod.Get, src);
        var imageResponse = await fixture.Client.SendAsync(imageRequest);
        Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);

        var contentType = imageResponse.Content.Headers.ContentType?.MediaType;
        Assert.True(contentType?.StartsWith("image/") == true, $"Expected image content type, but got: {contentType}");

        var returnedImageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(fixture.FakeImage.Bytes, returnedImageBytes);
    }
}
