using Advanced.CMS.AdvancedReviews.E2ETests.PageObjectModels;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.ServiceLocation;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace Advanced.CMS.AdvancedReviews.E2ETests.Tests;

[Collection(E2ETestCollection.Name)]
public class RemoteWebsiteTests(WebServerFixture fixture, RemoteWebsiteFixture remoteWebsite) : IClassFixture<RemoteWebsiteFixture>
{
    [Fact]
    public async Task Reviewer_Can_Pin_Property_Of_Remote_Website_Through_Editable_Link()
    {
        var page = remoteWebsite.CreateDraftPage();
        var link = fixture.Services.GetInstance<IExternalReviewLinksRepository>().AddLink(page.ContentLink, true, TimeSpan.FromDays(1), null);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Reviewer_Can_Pin_Property_Of_Remote_Website_Through_Editable_Link));
        var browserPage = await trackableContext.Context.NewPageAsync();
        await browserPage.GotoAsync(link.LinkUrl);

        var reviewPage = await new EditableReviewPage(browserPage).EnterNameAsync("External Reviewer");
        await reviewPage.ExpectReviewedContentAsync(RemoteWebsiteFixture.HeadingText);
        var body = browserPage.FrameLocator("#editableIframe").Locator($"[data-epi-edit='{RemoteWebsiteFixture.BodyProperty}']");
        await Expect(body).ToContainTextAsync(page.ContentGuid.ToString("N"));

        await new NewPinDialog(browserPage).AddPinAsync(body, RemoteWebsiteFixture.BodyProperty, "Remote body needs work");

        var pin = await PinStore.WaitForPinAsync(fixture, link.ContentLink, "Remote body needs work");
        Assert.Equal(link.Token, pin.Token);
        Assert.Contains($"\"propertyName\":\"{RemoteWebsiteFixture.BodyProperty}\"", pin.Data);
        await Expect(reviewPage.Pins).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Editor_Can_Pin_Property_Of_Remote_Website_In_Edit_Mode()
    {
        var page = remoteWebsite.CreateDraftPage();

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Editor_Can_Pin_Property_Of_Remote_Website_In_Edit_Mode));
        var browserPage = await trackableContext.Context.NewPageAsync();
        var editModePage = await new LoginPage(browserPage).LoginAsAdminAsync();
        await editModePage.OpenContentAsync(page.ContentLink, RemoteWebsiteFixture.HeadingText);
        await editModePage.TurnOnAdvancedReviewAsync();

        var body = editModePage.Preview.Locator($"[data-epi-edit='{RemoteWebsiteFixture.BodyProperty}']");
        await new NewPinDialog(browserPage).AddPinAsync(body, RemoteWebsiteFixture.BodyProperty, "Remote body in edit mode");

        var pin = await PinStore.WaitForPinAsync(fixture, page.ContentLink, "Remote body in edit mode");
        Assert.Contains($"\"propertyName\":\"{RemoteWebsiteFixture.BodyProperty}\"", pin.Data);
        await Expect(editModePage.Pins).ToHaveCountAsync(1);
    }
}
