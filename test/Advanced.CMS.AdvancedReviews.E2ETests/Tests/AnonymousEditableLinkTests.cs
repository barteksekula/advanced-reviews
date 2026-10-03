using Advanced.CMS.AdvancedReviews.E2ETests.PageObjectModels;
using Advanced.CMS.ApprovalReviews;
using Advanced.CMS.ExternalReviews;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Applications;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.Options;
using TestSite.Models;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace Advanced.CMS.AdvancedReviews.E2ETests.Tests;

[Collection(E2ETestCollection.Name)]
public class AnonymousEditableLinkTests(WebServerFixture fixture)
{
    private IExternalReviewLinksRepository LinksRepository => fixture.Services.GetInstance<IExternalReviewLinksRepository>();

    [Fact]
    public async Task Anonymous_Reviewer_Can_Add_Comment_That_Is_Saved()
    {
        var page = CreateDraftPage();
        var link = LinksRepository.AddLink(page.ContentLink, true, TimeSpan.FromDays(1), null);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Anonymous_Reviewer_Can_Add_Comment_That_Is_Saved));
        var browserPage = await trackableContext.Context.NewPageAsync();
        await browserPage.GotoAsync(link.LinkUrl);

        var reviewPage = await new EditableReviewPage(browserPage).ConfirmNameAsync("External Reviewer");
        await reviewPage.ExpectReviewedContentAsync(page.Name);
        await reviewPage.AddCommentAsync("Please fix the heading");
        await Expect(reviewPage.Pins).ToHaveCountAsync(1);

        var savedPin = Assert.Single(fixture.Services.GetInstance<IApprovalReviewsRepository>().Load(link.ContentLink));
        Assert.Equal(link.Token, savedPin.Token);
        Assert.Contains("Please fix the heading", savedPin.Data);
        Assert.Contains("External Reviewer", savedPin.Data);

        await browserPage.ReloadAsync();
        await new EditableReviewPage(browserPage).ConfirmNameAsync("External Reviewer");
        await Expect(reviewPage.Pins).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Reviewer_Cannot_Remove_Own_Pin_Saved_Without_Token()
    {
        const string reviewerName = "External Reviewer";
        var page = CreateDraftPage();
        var link = LinksRepository.AddLink(page.ContentLink, true, TimeSpan.FromDays(1), null);
        fixture.Services.GetInstance<IApprovalReviewsRepository>().Update(link.ContentLink, new ReviewLocation
        {
            Data = $$"""
                {"documentRelativePosition":{"x":10,"y":10},"documentSize":{"x":1000,"y":500},"isDone":false,"priority":"Normal",
                "firstComment":{"author":"{{reviewerName}}","text":"Pin without token","date":"2026-10-02T10:00:00.000Z"},"comments":[]}
                """
        });

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Reviewer_Cannot_Remove_Own_Pin_Saved_Without_Token));
        var browserPage = await trackableContext.Context.NewPageAsync();
        await browserPage.GotoAsync(link.LinkUrl);

        var reviewPage = await new EditableReviewPage(browserPage).ConfirmNameAsync(reviewerName);
        await reviewPage.AddCommentAsync("Pin created through the link");
        await browserPage.ReloadAsync();
        await new EditableReviewPage(browserPage).ConfirmNameAsync(reviewerName);
        await reviewPage.OpenPinListAsync();

        await Expect(reviewPage.PinListItem("Pin without token").Locator(".delete")).ToHaveCountAsync(0);
        await Expect(reviewPage.PinListItem("Pin created through the link").Locator(".delete")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Pin_Protected_Link_Asks_For_Pin_Code_First()
    {
        var page = CreateDraftPage();
        var link = LinksRepository.AddLink(page.ContentLink, true, TimeSpan.FromDays(1), null);
        LinksRepository.UpdateLink(link.Token, null, "1234", null, null);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Pin_Protected_Link_Asks_For_Pin_Code_First));
        var browserPage = await trackableContext.Context.NewPageAsync();
        await browserPage.GotoAsync(link.LinkUrl);

        var loginPage = new PinCodeLoginPage(browserPage);
        await Expect(loginPage.CodeInput).ToBeVisibleAsync();

        var reviewPage = await (await loginPage.SubmitAsync("1234")).ConfirmNameAsync("External Reviewer");
        await reviewPage.ExpectReviewedContentAsync(page.Name);
    }

    [Fact]
    public async Task Reviewer_Is_Sent_To_Login_When_Anonymous_Links_Are_Disabled()
    {
        var options = fixture.Services.GetInstance<IOptions<ExternalReviewOptions>>().Value;
        options.AllowAnonymousEditableLinks = false;
        try
        {
            var link = LinksRepository.AddLink(CreateDraftPage().ContentLink, true, TimeSpan.FromDays(1), null);

            await using var trackableContext = await TrackableContext.Get(fixture, nameof(Reviewer_Is_Sent_To_Login_When_Anonymous_Links_Are_Disabled));
            var browserPage = await trackableContext.Context.NewPageAsync();
            await browserPage.GotoAsync(link.LinkUrl);

            Assert.Contains(WebServerFixture.LoginPath, browserPage.Url);
            await Expect(new LoginPage(browserPage).LoginButton).ToBeVisibleAsync();
        }
        finally
        {
            options.AllowAnonymousEditableLinks = true;
        }
    }

    private StandardPage CreateDraftPage()
    {
        var contentRepository = fixture.Services.GetInstance<IContentRepository>();
        var website = (InProcessWebsite)fixture.Services.GetInstance<IApplicationRepository>().List().First();
        var page = contentRepository.GetDefault<StandardPage>(website.EntryPoint);
        page.Name = "Reviewed page " + Guid.NewGuid().ToString("N");
        contentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }
}
