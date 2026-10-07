using Advanced.CMS.AdvancedReviews.E2ETests.PageObjectModels;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Applications;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using TestSite.Models;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace Advanced.CMS.AdvancedReviews.E2ETests.Tests;

[Collection(E2ETestCollection.Name)]
public class ViewLinkTests(WebServerFixture fixture)
{
    private const string PublishedText = "Published text";
    private const string DraftText = "Draft text";

    private IExternalReviewLinksRepository LinksRepository => fixture.Services.GetInstance<IExternalReviewLinksRepository>();

    [Fact]
    public async Task View_Link_Shows_Draft_Content_To_Anonymous_Reviewer()
    {
        var link = LinksRepository.AddLink(CreatePublishedPageWithDraft(), false, TimeSpan.FromDays(1), null);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(View_Link_Shows_Draft_Content_To_Anonymous_Reviewer));
        var browserPage = await trackableContext.Context.NewPageAsync();
        var response = await browserPage.GotoAsync(link.LinkUrl);

        Assert.Equal(200, response.Status);
        await Expect(browserPage.Locator("#html")).ToContainTextAsync(DraftText);
        await Expect(browserPage.Locator("#html")).Not.ToContainTextAsync(PublishedText);
    }

    [Fact]
    public async Task Expired_View_Link_Is_Not_Found()
    {
        var link = LinksRepository.AddLink(CreatePublishedPageWithDraft(), false, TimeSpan.FromDays(1), null);
        LinksRepository.UpdateLink(link.Token, DateTime.Now.AddMinutes(-1), null, null, null);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Expired_View_Link_Is_Not_Found));
        var response = await trackableContext.Context.APIRequest.GetAsync(link.LinkUrl);

        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task Deleted_View_Link_Is_Not_Found()
    {
        var link = LinksRepository.AddLink(CreatePublishedPageWithDraft(), false, TimeSpan.FromDays(1), null);
        LinksRepository.DeleteLink(link.Token);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Deleted_View_Link_Is_Not_Found));
        var response = await trackableContext.Context.APIRequest.GetAsync(link.LinkUrl);

        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task Pin_Protected_View_Link_Shows_Content_After_Pin_Code_Is_Entered()
    {
        var link = LinksRepository.AddLink(CreatePublishedPageWithDraft(), false, TimeSpan.FromDays(1), null);
        LinksRepository.UpdateLink(link.Token, null, "1234", null, null);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Pin_Protected_View_Link_Shows_Content_After_Pin_Code_Is_Entered));
        var browserPage = await trackableContext.Context.NewPageAsync();
        await browserPage.GotoAsync(link.LinkUrl);

        var loginPage = new PinCodeLoginPage(browserPage);
        await Expect(loginPage.CodeInput).ToBeVisibleAsync();
        await Expect(browserPage.Locator("#html")).ToHaveCountAsync(0);

        await loginPage.SubmitAsync("1234");
        await Expect(browserPage.Locator("#html")).ToContainTextAsync(DraftText);
    }

    private ContentReference CreatePublishedPageWithDraft()
    {
        var contentRepository = fixture.Services.GetInstance<IContentRepository>();
        var website = (InProcessWebsite)fixture.Services.GetInstance<IApplicationRepository>().List().First();
        var page = contentRepository.GetDefault<StandardPage>(website.EntryPoint);
        page.Name = "Reviewed page " + Guid.NewGuid().ToString("N");
        page.Html = new XhtmlString($"<p>{PublishedText}</p>");
        var publishedLink = contentRepository.Save(page, SaveAction.Publish, AccessLevel.NoAccess);

        var draft = (StandardPage)contentRepository.Get<StandardPage>(publishedLink).CreateWritableClone();
        draft.Html = new XhtmlString($"<p>{DraftText}</p>");
        return contentRepository.Save(draft, SaveAction.Save | SaveAction.ForceNewVersion, AccessLevel.NoAccess);
    }
}
