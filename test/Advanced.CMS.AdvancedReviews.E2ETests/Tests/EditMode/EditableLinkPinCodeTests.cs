using Advanced.CMS.AdvancedReviews.E2ETests.PageObjectModels;
using Advanced.CMS.ExternalReviews;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Applications;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.Options;
using TestSite.Models;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace Advanced.CMS.AdvancedReviews.E2ETests.Tests.EditMode;

[Collection(E2ETestCollection.Name)]
public class EditableLinkPinCodeTests(WebServerFixture fixture)
{
    [Fact]
    public async Task Editor_Can_Secure_Editable_Link_With_Pin_Code_When_Anonymous_Links_Are_Allowed()
    {
        var page = CreatePublishedPage();

        await using var trackableContext = await TrackableContext.Get(fixture, nameof(Editor_Can_Secure_Editable_Link_With_Pin_Code_When_Anonymous_Links_Are_Allowed));
        var browserPage = await trackableContext.Context.NewPageAsync();
        var editModePage = await OpenExternalReviewLinksAsync(browserPage, page);

        await editModePage.AddEditableReviewLinkAsync();
        await editModePage.OpenLinkEditDialogAsync(editModePage.ReviewLinks.First);
        await Expect(editModePage.PinCodeInput).ToHaveAttributeAsync("type", "text");
        await Expect(editModePage.PinCodeInput).ToHaveAttributeAsync("autocomplete", "off");
        await Expect(editModePage.PinCodeInput).ToHaveCSSAsync("-webkit-text-security", "disc");
        await editModePage.PinCodeInput.FillAsync("1234");
        await editModePage.SaveLinkDialogAsync();

        await Expect(editModePage.ReviewLinks.First.GetByTitle("Link secured with PIN code")).ToBeVisibleAsync();
        var link = Assert.Single(fixture.Services.GetInstance<IExternalReviewLinksRepository>().GetLinksForContent(page.ContentLink, null));
        Assert.True(link.IsEditable);
        Assert.False(string.IsNullOrEmpty(link.PinCode));

        await using var reviewerContext = await TrackableContext.Get(fixture, nameof(Editor_Can_Secure_Editable_Link_With_Pin_Code_When_Anonymous_Links_Are_Allowed) + "_reviewer");
        var reviewerPage = await reviewerContext.Context.NewPageAsync();
        await reviewerPage.GotoAsync(link.LinkUrl);
        var reviewPage = await new PinCodeLoginPage(reviewerPage).SubmitAsync("1234");
        await reviewPage.ConfirmNameAsync("External Reviewer");
        await reviewPage.ExpectReviewedContentAsync(page.Name);
    }

    [Fact]
    public async Task Editable_Link_Has_No_Pin_Code_Field_When_Anonymous_Links_Are_Not_Allowed()
    {
        var options = fixture.Services.GetInstance<IOptions<ExternalReviewOptions>>().Value;
        options.AllowAnonymousEditableLinks = false;
        try
        {
            var page = CreatePublishedPage();

            await using var trackableContext = await TrackableContext.Get(fixture, nameof(Editable_Link_Has_No_Pin_Code_Field_When_Anonymous_Links_Are_Not_Allowed));
            var browserPage = await trackableContext.Context.NewPageAsync();
            var editModePage = await OpenExternalReviewLinksAsync(browserPage, page);

            await editModePage.AddEditableReviewLinkAsync();
            await editModePage.OpenLinkEditDialogAsync(editModePage.ReviewLinks.First);

            await Expect(editModePage.PinCodeInput).ToHaveCountAsync(0);
        }
        finally
        {
            options.AllowAnonymousEditableLinks = true;
        }
    }

    private static async Task<EditModePage> OpenExternalReviewLinksAsync(Microsoft.Playwright.IPage browserPage, StandardPage page)
    {
        var editModePage = await new LoginPage(browserPage).LoginAsAdminAsync();
        await editModePage.OpenContentAsync(page.ContentLink, page.Name);
        return await editModePage.OpenNavigationPaneAsync();
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
