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

    [Fact]
    public async Task Editor_Can_Add_View_Link()
    {
        var page = CreatePublishedPage();

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Editor_Can_Add_View_Link));
        var browserPage = await trackableContext.Context.NewPageAsync();
        var editModePage = await OpenExternalReviewLinksAsync(browserPage, page);

        await Expect(editModePage.EmptyListMessage).ToBeVisibleAsync();
        await editModePage.AddViewReviewLinkAsync();

        await Expect(editModePage.ReviewLinks.First).ToContainTextAsync("View link");
        var link = Assert.Single(LinksRepository.GetLinksForContent(page.ContentLink, null));
        Assert.False(link.IsEditable);
    }

    [Fact]
    public async Task Editor_Can_Delete_Link()
    {
        var page = CreatePublishedPage();
        LinksRepository.AddLink(page.ContentLink, false, TimeSpan.FromDays(1), null);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Editor_Can_Delete_Link));
        var browserPage = await trackableContext.Context.NewPageAsync();
        var editModePage = await OpenExternalReviewLinksAsync(browserPage, page);

        await editModePage.DeleteLinkAsync(editModePage.ReviewLinks.First);

        await Expect(editModePage.EmptyListMessage).ToBeVisibleAsync();
        await AssertEventuallyAsync(() => !LinksRepository.GetLinksForContent(page.ContentLink, null).Any());
    }

    [Fact]
    public async Task Editor_Can_Give_Link_A_Display_Name()
    {
        var page = CreatePublishedPage();
        var link = LinksRepository.AddLink(page.ContentLink, false, TimeSpan.FromDays(1), null);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Editor_Can_Give_Link_A_Display_Name));
        var browserPage = await trackableContext.Context.NewPageAsync();
        var editModePage = await OpenExternalReviewLinksAsync(browserPage, page);

        await editModePage.OpenLinkEditDialogAsync(editModePage.ReviewLinks.First);
        await editModePage.DisplayNameInput.FillAsync("Marketing review");
        await editModePage.SaveLinkDialogAsync();

        await Expect(editModePage.ReviewLinks.First).ToContainTextAsync("Marketing review");
        await AssertEventuallyAsync(() => LinksRepository.GetContentByToken(link.Token).DisplayName == "Marketing review");
        await Expect(editModePage.ReviewLinks.First.GetByTitle("Link secured with PIN code")).ToHaveCountAsync(0);
        Assert.True(string.IsNullOrEmpty(LinksRepository.GetContentByToken(link.Token).PinCode));
    }

    [Fact]
    public async Task Expired_Link_Is_Shown_As_Inactive()
    {
        var page = CreatePublishedPage();
        var link = LinksRepository.AddLink(page.ContentLink, false, TimeSpan.FromDays(1), null);
        LinksRepository.UpdateLink(link.Token, DateTime.Now.AddMinutes(-1), null, null, null);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Expired_Link_Is_Shown_As_Inactive));
        var browserPage = await trackableContext.Context.NewPageAsync();
        var editModePage = await OpenExternalReviewLinksAsync(browserPage, page);
        var row = editModePage.ReviewLinks.First;

        await Expect(row).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("inactive"));
        await Expect(row.Locator("a")).ToHaveCountAsync(0);
        await Expect(editModePage.ShareButton(row)).ToBeDisabledAsync();
    }

    [Fact]
    public async Task Share_Requires_Valid_Email_Address()
    {
        var page = CreatePublishedPage();
        LinksRepository.AddLink(page.ContentLink, false, TimeSpan.FromDays(1), null);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Share_Requires_Valid_Email_Address));
        var browserPage = await trackableContext.Context.NewPageAsync();
        var editModePage = await OpenExternalReviewLinksAsync(browserPage, page);

        await editModePage.OpenShareDialogAsync(editModePage.ReviewLinks.First);
        await Expect(editModePage.ShareSendButton).ToBeDisabledAsync();

        await editModePage.ShareEmailInput.FillAsync("not-an-email");
        await Expect(editModePage.ShareSendButton).ToBeDisabledAsync();

        await editModePage.ShareEmailInput.FillAsync("reviewer@example.com");
        await Expect(editModePage.ShareSendButton).ToBeEnabledAsync();
    }

    private IExternalReviewLinksRepository LinksRepository => fixture.Services.GetInstance<IExternalReviewLinksRepository>();

    private static async Task<EditModePage> OpenExternalReviewLinksAsync(Microsoft.Playwright.IPage browserPage, StandardPage page)
    {
        var editModePage = await new LoginPage(browserPage).LoginAsAdminAsync();
        await editModePage.OpenContentAsync(page.ContentLink, page.Name);
        return await editModePage.OpenNavigationPaneAsync();
    }

    private static async Task AssertEventuallyAsync(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < timeout, "Condition was not met in time");
            await Task.Delay(100);
        }
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
