using System.Net;
using System.Text.RegularExpressions;
using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.ServiceLocation;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Projects;

[Collection(IntegrationTestCollection.Name)]
public class When_Page_In_Project_Links_To_Other_Page(When_Page_In_Project_Links_To_Other_Page.TestFixture fixture)
    : IClassFixture<CommonFixture>, IClassFixture<When_Page_In_Project_Links_To_Other_Page.TestFixture>
{
    public class TestFixture(SiteFixture siteFixture) : IAsyncLifetime
    {
        public IContentRepository ContentRepository { get; } = siteFixture.Services.GetInstance<IContentRepository>();
        public ProjectRepository ProjectRepository { get; } = siteFixture.Services.GetInstance<ProjectRepository>();
        public HttpClient Client { get; } = siteFixture.Client;

        public StandardPage TargetPage { get; private set; }

        public async Task InitializeAsync()
        {
            TargetPage = ContentRepository.CreatePage().PublishPage();
            await Task.CompletedTask;
        }

        public async Task DisposeAsync() => await ContentRepository.CleanupAsync(TargetPage);
    }

    [Fact]
    public async Task Link_In_Page_Xhtml_Should_Contain_Project_Preview_Postfix()
    {
        var page = fixture.ContentRepository.CreatePage().AddLinkInXhtml(fixture.TargetPage);
        var reviewLink = page.GenerateExternalReviewLink(CreateProject());

        var href = await GetLinkHref(reviewLink);

        Assert.Contains($"/externalContentView/{reviewLink.Token}", href);
        Assert.Contains("preview_generated=True", href);
    }

    [Fact]
    public async Task Link_In_Block_Xhtml_Should_Contain_Project_Preview_Postfix()
    {
        var page = fixture.ContentRepository.CreatePage().AddBlockWithLink(fixture.TargetPage);
        var reviewLink = page.GenerateExternalReviewLink(CreateProject());

        var href = await GetLinkHref(reviewLink);

        Assert.Contains($"/externalContentView/{reviewLink.Token}", href);
        Assert.Contains("preview_generated=True", href);
    }

    [Fact]
    public async Task Link_Outside_Of_Project_Should_Not_Contain_Preview_Postfix()
    {
        var page = fixture.ContentRepository.CreatePage().AddBlockWithLink(fixture.TargetPage);
        var reviewLink = page.GenerateExternalReviewLink();

        var href = await GetLinkHref(reviewLink);

        Assert.DoesNotContain("externalContentView", href);
    }

    private async Task<string> GetLinkHref(ExternalReviewLink reviewLink)
    {
        var response = await fixture.Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, reviewLink.LinkUrl));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseText = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(responseText, $"<a id=\"{StaticTexts.LinkToOtherPageId}\" href=\"([^\"]*)\"");
        Assert.True(match.Success, $"Link not rendered in response:{Environment.NewLine}{responseText}");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private Project CreateProject()
    {
        var project = new Project
        {
            Name = Guid.NewGuid().ToString()
        };

        fixture.ProjectRepository.Save(project);
        return project;
    }
}
