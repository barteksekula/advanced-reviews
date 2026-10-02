using System.Net;
using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using EPiServer.ServiceLocation;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Multilanguage;

[Collection(IntegrationTestCollection.Name)]
public class When_Updating_Non_Culture_Specific_Property(When_Updating_Non_Culture_Specific_Property.TestFixture fixture)
    : IClassFixture<CommonFixture>, IClassFixture<When_Updating_Non_Culture_Specific_Property.TestFixture>
{
    public class TestFixture(SiteFixture siteFixture) : IAsyncLifetime
    {
        private readonly List<StandardPage> _pages = [];

        public IContentRepository ContentRepository { get; } = siteFixture.Services.GetInstance<IContentRepository>();
        public ProjectRepository ProjectRepository { get; } = siteFixture.Services.GetInstance<ProjectRepository>();
        public HttpClient Client { get; } = siteFixture.Client;

        public StandardPage CreatePage()
        {
            var page = ContentRepository.CreatePage();
            _pages.Add(page);
            return page;
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            foreach (var page in _pages)
            {
                await ContentRepository.CleanupAsync(page);
            }
        }
    }

    [Fact]
    public async Task Translated_Draft_Shows_Value_From_Unpublished_Master_Page()
    {
        var page = fixture.CreatePage().SetHtml(StaticTexts.UpdatedString);
        var translatedPage = page.Translate();

        var responseText = await GetReviewLinkContent(translatedPage);

        Assert.Contains(StaticTexts.UpdatedString, responseText);
    }

    [Fact]
    public async Task Translated_Draft_Shows_Value_From_Master_Common_Draft()
    {
        var page = fixture.CreatePage().SetHtml(StaticTexts.OriginalNonCultureSpecificContent).PublishPage();
        page.UpdateHtmlInNewVersion(StaticTexts.UpdatedString);
        var translatedPage = page.Translate();

        var responseText = await GetReviewLinkContent(translatedPage);

        Assert.Contains(StaticTexts.UpdatedString, responseText);
        Assert.DoesNotContain(StaticTexts.OriginalNonCultureSpecificContent, responseText);
    }

    [Fact]
    public async Task Translated_Draft_In_Project_Shows_Value_From_Master_Project_Version()
    {
        var project = CreateProject();
        var page = fixture.CreatePage().SetHtml(StaticTexts.OriginalNonCultureSpecificContent).PublishPage();
        page.UpdateHtmlInNewVersion(StaticTexts.ProjectUpdatedString, project);
        var translatedPage = page.Translate(project: project);

        var responseText = await GetReviewLinkContent(translatedPage, project);

        Assert.Contains(StaticTexts.ProjectUpdatedString, responseText);
        Assert.DoesNotContain(StaticTexts.OriginalNonCultureSpecificContent, responseText);
    }

    [Fact]
    public async Task Master_Page_In_Project_Shows_Value_From_Project_Version()
    {
        var project = CreateProject();
        var page = fixture.CreatePage().SetHtml(StaticTexts.OriginalNonCultureSpecificContent).PublishPage();
        var projectVersion = page.UpdateHtmlInNewVersion(StaticTexts.ProjectUpdatedString, project);

        var responseText = await GetReviewLinkContent(projectVersion, project);

        Assert.Contains(StaticTexts.ProjectUpdatedString, responseText);
        Assert.DoesNotContain(StaticTexts.OriginalNonCultureSpecificContent, responseText);
    }

    private async Task<string> GetReviewLinkContent(StandardPage page, Project project = null)
    {
        var reviewLink = page.GenerateExternalReviewLink(project);
        var response = await fixture.Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, reviewLink.LinkUrl));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private Project CreateProject()
    {
        var project = new Project { Name = Guid.NewGuid().ToString() };
        fixture.ProjectRepository.Save(project);
        return project;
    }
}
