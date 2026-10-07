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

        var reviewPage = await new EditableReviewPage(browserPage).EnterNameAsync("External Reviewer");
        await reviewPage.ExpectReviewedContentAsync(page.Name);
        await reviewPage.AddCommentAsync("Please fix the heading");
        await Expect(reviewPage.Pins).ToHaveCountAsync(1);

        var savedPin = Assert.Single(fixture.Services.GetInstance<IApprovalReviewsRepository>().Load(link.ContentLink));
        Assert.Equal(link.Token, savedPin.Token);
        Assert.Contains("Please fix the heading", savedPin.Data);
        Assert.Contains("External Reviewer", savedPin.Data);

        await browserPage.ReloadAsync();
        await new EditableReviewPage(browserPage).ContinueWithRememberedNameAsync("External Reviewer");
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

        var reviewPage = await new EditableReviewPage(browserPage).EnterNameAsync(reviewerName);
        await reviewPage.AddCommentAsync("Pin created through the link");
        await browserPage.ReloadAsync();
        await new EditableReviewPage(browserPage).ContinueWithRememberedNameAsync(reviewerName);
        await reviewPage.OpenPinListAsync();

        await Expect(reviewPage.PinListItem("Pin without token").Locator(".delete")).ToHaveCountAsync(0);
        await Expect(reviewPage.PinListItem("Pin created through the link").Locator(".delete")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Reviewer_Can_Reply_To_Pin_And_Resolve_It()
    {
        const string reviewerName = "External Reviewer";
        var page = CreateDraftPage();
        var link = LinksRepository.AddLink(page.ContentLink, true, TimeSpan.FromDays(1), null);
        AddPin(link, reviewerName, "Heading is too long");

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Reviewer_Can_Reply_To_Pin_And_Resolve_It));
        var browserPage = await trackableContext.Context.NewPageAsync();
        await browserPage.GotoAsync(link.LinkUrl);

        var reviewPage = await new EditableReviewPage(browserPage).EnterNameAsync(reviewerName);
        await reviewPage.OpenPinListAsync();
        await reviewPage.OpenPinDetailsAsync("Heading is too long");
        await reviewPage.ReplyAsync("Shortened it a bit");
        await Expect(reviewPage.PinDetails).ToContainTextAsync("Shortened it a bit");
        await reviewPage.ResolveAsync();

        var savedPin = Assert.Single(fixture.Services.GetInstance<IApprovalReviewsRepository>().Load(link.ContentLink));
        Assert.Contains("Shortened it a bit", savedPin.Data);
        Assert.Contains("\"isDone\":true", savedPin.Data);
    }

    [Fact]
    public async Task Reviewer_Can_Remove_Own_Pin()
    {
        const string reviewerName = "External Reviewer";
        var page = CreateDraftPage();
        var link = LinksRepository.AddLink(page.ContentLink, true, TimeSpan.FromDays(1), null);
        AddPin(link, reviewerName, "Pin to remove");

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Reviewer_Can_Remove_Own_Pin));
        var browserPage = await trackableContext.Context.NewPageAsync();
        await browserPage.GotoAsync(link.LinkUrl);

        var reviewPage = await new EditableReviewPage(browserPage).EnterNameAsync(reviewerName);
        await reviewPage.OpenPinListAsync();
        await reviewPage.RemovePinAsync("Pin to remove");

        await Expect(reviewPage.PinListItem("Pin to remove")).ToHaveCountAsync(0);
        Assert.Empty(fixture.Services.GetInstance<IApprovalReviewsRepository>().Load(link.ContentLink));
    }

    [Fact]
    public async Task Expired_Editable_Link_Is_Not_Found()
    {
        var link = LinksRepository.AddLink(CreateDraftPage().ContentLink, true, TimeSpan.FromDays(1), null);
        LinksRepository.UpdateLink(link.Token, DateTime.Now.AddMinutes(-1), null, null, null);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Expired_Editable_Link_Is_Not_Found));
        var browserPage = await trackableContext.Context.NewPageAsync();
        var response = await browserPage.GotoAsync(link.LinkUrl);

        Assert.Equal(404, response.Status);
        await Expect(new EditableReviewPage(browserPage).NameInput).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Wrong_Pin_Code_Does_Not_Grant_Access()
    {
        var link = LinksRepository.AddLink(CreateDraftPage().ContentLink, true, TimeSpan.FromDays(1), null);
        LinksRepository.UpdateLink(link.Token, null, "1234", null, null);

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Wrong_Pin_Code_Does_Not_Grant_Access));
        var browserPage = await trackableContext.Context.NewPageAsync();
        await browserPage.GotoAsync(link.LinkUrl);

        var response = await browserPage.RunAndWaitForResponseAsync(
            () => new PinCodeLoginPage(browserPage).SubmitAsync("4321"),
            response => response.Request.Method == "POST");
        Assert.Equal(404, response.Status);

        await browserPage.GotoAsync(link.LinkUrl);
        await Expect(new PinCodeLoginPage(browserPage).CodeInput).ToBeVisibleAsync();
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
        await Expect(loginPage.CodeInput).ToHaveAttributeAsync("type", "text");
        await Expect(loginPage.CodeInput).ToHaveAttributeAsync("autocomplete", "off");
        await Expect(loginPage.CodeInput).ToHaveCSSAsync("-webkit-text-security", "disc");

        var reviewPage = await (await loginPage.SubmitAsync("1234")).EnterNameAsync("External Reviewer");
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

    private void AddPin(ExternalReviewLink link, string author, string comment) =>
        fixture.Services.GetInstance<IApprovalReviewsRepository>().Update(link.ContentLink, new ReviewLocation
        {
            Token = link.Token,
            Data = $$"""
                {"documentRelativePosition":{"x":10,"y":10},"documentSize":{"x":1000,"y":500},"isDone":false,"priority":"Normal",
                "firstComment":{"author":"{{author}}","text":"{{comment}}","date":"2026-10-02T10:00:00.000Z"},"comments":[]}
                """
        });

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
