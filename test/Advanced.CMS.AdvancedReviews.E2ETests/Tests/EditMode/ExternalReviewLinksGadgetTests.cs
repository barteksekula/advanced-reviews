using Advanced.CMS.AdvancedReviews.E2ETests.PageObjectModels;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Applications;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using TestSite.Models;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace Advanced.CMS.AdvancedReviews.E2ETests.Tests.EditMode;

[Collection(E2ETestCollection.Name)]
public class ExternalReviewLinksGadgetTests(WebServerFixture fixture)
{
    [Fact]
    public async Task Add_Link_Button_Is_Enabled_For_Opened_Page()
    {
        var page = CreatePublishedPage();

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Add_Link_Button_Is_Enabled_For_Opened_Page));
        var browserPage = await trackableContext.Context.NewPageAsync();

        var editModePage = await new LoginPage(browserPage).LoginAsAdminAsync();
        await editModePage.OpenContentAsync(page.ContentLink, page.Name);
        await editModePage.OpenNavigationPaneAsync();

        await Expect(editModePage.AddReviewLinkButton).ToBeVisibleAsync();
        await Expect(editModePage.AddReviewLinkButton).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Clicking_Anywhere_On_Link_Row_Opens_Review_Link()
    {
        var page = CreatePublishedPage();
        var link = fixture.Services.GetInstance<IExternalReviewLinksRepository>()
            .AddLink(page.ContentLink, false, TimeSpan.FromDays(1), null);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Clicking_Anywhere_On_Link_Row_Opens_Review_Link));
        var browserPage = await trackableContext.Context.NewPageAsync();

        var editModePage = await new LoginPage(browserPage).LoginAsAdminAsync();
        await editModePage.OpenContentAsync(page.ContentLink, page.Name);
        await editModePage.OpenNavigationPaneAsync();
        var row = editModePage.ReviewLinks.First;

        var secondLineOfRow = new Microsoft.Playwright.LocatorClickOptions { Position = new() { X = 60, Y = 28 } };
        var reviewPage = await browserPage.RunAndWaitForPopupAsync(() => row.ClickAsync(secondLineOfRow));
        Assert.EndsWith(link.LinkUrl, reviewPage.Url);

        await editModePage.OpenLinkEditDialogAsync(row);
        Assert.Single(trackableContext.Context.Pages, x => x.Url.EndsWith(link.LinkUrl));
    }

    private StandardPage CreatePublishedPage()
    {
        var contentRepository = fixture.Services.GetInstance<IContentRepository>();
        var website = (InProcessWebsite)fixture.Services.GetInstance<IApplicationRepository>().List().First();
        var page = contentRepository.GetDefault<StandardPage>(website.EntryPoint);
        page.Name = "Reviewed page " + Guid.NewGuid().ToString("N");
        contentRepository.Save(page, SaveAction.Publish, AccessLevel.NoAccess);
        return page;
    }
}
