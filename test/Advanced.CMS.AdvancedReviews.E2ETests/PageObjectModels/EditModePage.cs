using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Advanced.CMS.AdvancedReviews.E2ETests.PageObjectModels;

public class EditModePage(IPage page)
{
    private ILocator NavigationPaneToggle => page.GetByTitle("Toggle navigation pane");

    public ILocator AddReviewLinkButton => page.GetByRole(AriaRole.Button, new() { Name = "Add link" });
    public ILocator ReviewLinks => page.Locator(".external-reviews-list .list-item");
    public ILocator LinkDialog => page.GetByRole(AriaRole.Dialog);
    public ILocator PinCodeInput => LinkDialog.GetByLabel("PIN code", new() { Exact = false });

    public async Task AddEditableReviewLinkAsync()
    {
        var linkCount = await ReviewLinks.CountAsync();
        await AddReviewLinkButton.ClickAsync();
        await page.GetByRole(AriaRole.Menuitem, new() { Name = "Edit" }).ClickAsync();
        await Expect(ReviewLinks).ToHaveCountAsync(linkCount + 1);
    }

    public async Task OpenLinkEditDialogAsync(ILocator reviewLink)
    {
        await reviewLink.GetByTitle("Edit", new() { Exact = true }).ClickAsync();
        await Expect(LinkDialog).ToBeVisibleAsync();
    }

    public async Task SaveLinkDialogAsync()
    {
        await LinkDialog.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Expect(LinkDialog).ToBeHiddenAsync();
    }

    public async Task<EditModePage> OpenContentAsync(ContentReference contentLink, string expectedName)
    {
        await page.GotoAsync($"/Optimizely/CMS/#context=epi.cms.contentdata:///{contentLink.ID}");
        await Expect(page.FrameLocator("iframe[title='preview']").GetByText(expectedName)).ToBeVisibleAsync();
        return this;
    }

    public async Task<EditModePage> OpenNavigationPaneAsync()
    {
        await NavigationPaneToggle.ClickAsync();
        await Expect(page.GetByText("External review links")).ToBeVisibleAsync();
        return this;
    }
}
