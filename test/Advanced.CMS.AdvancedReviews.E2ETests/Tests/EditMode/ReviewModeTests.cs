using Advanced.CMS.AdvancedReviews.E2ETests.PageObjectModels;
using Advanced.CMS.ApprovalReviews;
using EPiServer.Applications;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using TestSite.Models;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace Advanced.CMS.AdvancedReviews.E2ETests.Tests.EditMode;

[Collection(E2ETestCollection.Name)]
public class ReviewModeTests(WebServerFixture fixture)
{
    [Fact]
    public async Task Review_Mode_Shows_Pins_After_Changing_Content()
    {
        var firstPage = CreateDraftPage();
        var secondPage = CreateDraftPage();
        AddPin(secondPage.ContentLink, "Pin on the second page");

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Review_Mode_Shows_Pins_After_Changing_Content));
        var browserPage = await trackableContext.Context.NewPageAsync();
        var editModePage = await new LoginPage(browserPage).LoginAsAdminAsync();

        await editModePage.OpenContentAsync(firstPage.ContentLink, firstPage.Name);
        await editModePage.TurnOnAdvancedReviewAsync();
        await editModePage.TurnOffAdvancedReviewAsync();

        await editModePage.OpenContentAsync(secondPage.ContentLink, secondPage.Name);
        await editModePage.TurnOnAdvancedReviewAsync();

        await Expect(editModePage.Pins).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Review_Mode_Scrolls_Long_Preview_And_Places_Pin_On_Property()
    {
        var page = CreateDraftPage("""<div style="height: 2500px">Top of the page</div><p id="bottom">Bottom of the page</p>""");

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Review_Mode_Scrolls_Long_Preview_And_Places_Pin_On_Property));
        var browserPage = await trackableContext.Context.NewPageAsync();
        var editModePage = await new LoginPage(browserPage).LoginAsAdminAsync();
        await editModePage.OpenContentAsync(page.ContentLink, page.Name);
        await editModePage.TurnOnAdvancedReviewAsync();

        var bottom = editModePage.Preview.Locator("#bottom");
        var overlayBox = await editModePage.ReviewOverlay.BoundingBoxAsync();
        await browserPage.Mouse.MoveAsync(overlayBox.X + overlayBox.Width / 2, overlayBox.Y + overlayBox.Height / 2);
        await browserPage.Mouse.WheelAsync(0, 3000);
        await WaitUntilAsync(async () => (await bottom.BoundingBoxAsync()).Y < overlayBox.Y + overlayBox.Height);

        await new NewPinDialog(browserPage).AddPinAsync(bottom, "Html", "Bottom paragraph needs work");

        var pin = await PinStore.WaitForPinAsync(fixture, page.ContentLink, "Bottom paragraph needs work");
        Assert.Contains("\"propertyName\":\"Html\"", pin.Data);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (!await condition())
        {
            if (DateTime.UtcNow > timeout)
            {
                throw new TimeoutException("The preview did not scroll with the review overlay");
            }

            await Task.Delay(100);
        }
    }

    private void AddPin(ContentReference contentLink, string comment) =>
        fixture.Services.GetInstance<IApprovalReviewsRepository>().Update(contentLink, new ReviewLocation
        {
            Data = $$"""
                {"documentRelativePosition":{"x":40,"y":40},"documentSize":{"x":1000,"y":500},"isDone":false,"priority":"Normal",
                "firstComment":{"author":"cmsadmin","text":"{{comment}}","date":"2026-10-02T10:00:00.000Z"},"comments":[]}
                """
        });

    private StandardPage CreateDraftPage(string html = null)
    {
        var contentRepository = fixture.Services.GetInstance<IContentRepository>();
        var website = fixture.Services.GetInstance<IApplicationRepository>().List().OfType<InProcessWebsite>().First();
        var page = contentRepository.GetDefault<StandardPage>(website.EntryPoint);
        page.Name = "Reviewed page " + Guid.NewGuid().ToString("N");
        if (html != null)
        {
            page.Html = new XhtmlString(html);
        }

        contentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }
}
