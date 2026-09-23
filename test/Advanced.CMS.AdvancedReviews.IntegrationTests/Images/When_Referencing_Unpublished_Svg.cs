using System.Net;
using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Images;

[Collection(IntegrationTestCollection.Name)]
public class When_Referencing_Unpublished_Svg(When_Referencing_Unpublished_Svg.TestFixture fixture)
    : IClassFixture<CommonFixture>, IClassFixture<When_Referencing_Unpublished_Svg.TestFixture>
{
    public class TestFixture(SiteFixture siteFixture) : IAsyncLifetime
    {
        public IContentRepository ContentRepository { get; } = siteFixture.Services.GetInstance<IContentRepository>();
        public IUrlResolver UrlResolver { get; } = siteFixture.Services.GetInstance<IUrlResolver>();
        public HttpClient Client { get; } = siteFixture.Client;
        public FakeImageFactory FakeImageFactory = new();

        public StandardPage DraftPage { get; private set; }
        public VectorImageFile DraftImage { get; private set; }
        public ImageFile DraftRasterImage { get; private set; }
        public ExternalReviewLink GeneratedReviewLink { get; private set; }
        public Media FakeImage { get; private set; }

        public async Task InitializeAsync()
        {
            FakeImage = FakeImageFactory.GetVectorMedia();
            DraftImage = ContentRepository.CreateDraftVectorImage(FakeImage);
            DraftRasterImage = ContentRepository.CreateDraftImage(FakeImageFactory.GetMediaItems().First());
            DraftPage = ContentRepository.CreatePage().PublishPage();
            GeneratedReviewLink = DraftPage.GenerateExternalReviewLink();

            await Task.CompletedTask;
        }

        public async Task DisposeAsync() => await ContentRepository.CleanupAsync(DraftPage);
    }

    [Fact]
    public async Task Svg_Is_Proxied_Unchanged_When_Thumbnail_Size_Is_Requested()
    {
        var url = $"/ImageProxy/{fixture.GeneratedReviewLink.Token}/{fixture.DraftImage.ContentLink}?width=100&height=100";
        var response = await fixture.Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(fixture.FakeImage.Bytes, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Svg_Is_Proxied_Unchanged_When_No_Size_Is_Requested()
    {
        var url = $"/ImageProxy/{fixture.GeneratedReviewLink.Token}/{fixture.DraftImage.ContentLink}";
        var response = await fixture.Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(fixture.FakeImage.Bytes, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Raster_Image_Is_Still_Resized_When_Thumbnail_Size_Is_Requested()
    {
        var url = $"/ImageProxy/{fixture.GeneratedReviewLink.Token}/{fixture.DraftRasterImage.ContentLink}?width=10&height=10";
        var response = await fixture.Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
    }
}
