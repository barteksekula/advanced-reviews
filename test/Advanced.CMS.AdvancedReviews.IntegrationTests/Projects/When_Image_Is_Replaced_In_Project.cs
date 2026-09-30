using System.Net;
using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.ServiceLocation;
using HtmlAgilityPack;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Projects;

[Collection(IntegrationTestCollection.Name)]
public class When_Image_Is_Replaced_In_Project(When_Image_Is_Replaced_In_Project.TestFixture fixture)
    : IClassFixture<CommonFixture>, IClassFixture<When_Image_Is_Replaced_In_Project.TestFixture>
{
    public class TestFixture(SiteFixture siteFixture) : IAsyncLifetime
    {
        public IContentRepository ContentRepository { get; } = siteFixture.Services.GetInstance<IContentRepository>();
        public ProjectRepository ProjectRepository { get; } = siteFixture.Services.GetInstance<ProjectRepository>();
        public HttpClient Client { get; } = siteFixture.Client;

        public StandardPage Page { get; private set; }
        public ImageFile ProjectImageVersion { get; private set; }
        public Media ReplacementImage { get; private set; }
        public ExternalReviewLink ProjectReviewLink { get; private set; }

        public async Task InitializeAsync()
        {
            var mediaItems = new FakeImageFactory().GetMediaItems();
            ReplacementImage = mediaItems[1];

            var project = new Project { Name = Guid.NewGuid().ToString() };
            ProjectRepository.Save(project);

            var image = ContentRepository.CreateDraftImage(mediaItems[0]).PublishImage();
            ProjectImageVersion = image.ReplaceImageInProject(ReplacementImage, project);

            Page = ContentRepository.CreatePage()
                .ReferenceImageInXhtml(image)
                .ReferenceUnpublishedImageInContentReference(image.ContentLink.ToReferenceWithoutVersion())
                .PublishPage();
            ProjectReviewLink = Page.GenerateExternalReviewLink(project);

            await Task.CompletedTask;
        }

        public async Task DisposeAsync() => await ContentRepository.CleanupAsync(Page);
    }

    [Fact]
    public async Task Image_In_Xhtml_Should_Show_Project_Version()
    {
        var src = await GetImageSrc($"//img[@id='{StaticTexts.XhtmlImageId}']");

        await AssertProjectVersionIsProxied(src);
    }

    [Fact]
    public async Task Image_In_ContentReference_Should_Show_Project_Version()
    {
        var src = await GetImageSrc("//div[@id='image']//img");

        await AssertProjectVersionIsProxied(src);
    }

    private async Task<string> GetImageSrc(string xpath)
    {
        var response = await fixture.Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, fixture.ProjectReviewLink.LinkUrl));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(await response.Content.ReadAsStringAsync());
        var imgNode = htmlDocument.DocumentNode.SelectSingleNode(xpath);
        Assert.NotNull(imgNode);
        return WebUtility.HtmlDecode(imgNode.GetAttributeValue("src", string.Empty));
    }

    private async Task AssertProjectVersionIsProxied(string src)
    {
        Assert.Equal($"/ImageProxy/{fixture.ProjectReviewLink.Token}/{fixture.ProjectImageVersion.ContentLink}", src);

        var imageResponse = await fixture.Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, src));
        Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);
        Assert.Equal(fixture.ReplacementImage.Bytes, await imageResponse.Content.ReadAsByteArrayAsync());
    }
}
