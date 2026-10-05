using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Advanced.CMS.AdvancedReviews.E2ETests.PageObjectModels;

public class EditableReviewPage(IPage page)
{
    private ILocator ReviewedContent => page.FrameLocator("#editableIframe").Locator("body");
    private ILocator OverlayDocument => page.Locator("#review-overlay > div");
    private ILocator CommentInput => page.GetByRole(AriaRole.Dialog).GetByLabel("Add comment...");
    private ILocator SaveButton => page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = "Save" });

    public ILocator NameInput => page.GetByRole(AriaRole.Dialog).GetByLabel("Name");
    public ILocator Pins => page.Locator(".review-location");

    public ILocator PinListItem(string firstComment) => page.Locator(".locations li").Filter(new() { HasText = firstComment });

    public async Task OpenPinListAsync()
    {
        await page.GetByTitle("Expand review panel").ClickAsync();
        await Expect(page.Locator(".locations")).ToBeVisibleAsync();
    }

    public async Task<EditableReviewPage> EnterNameAsync(string name)
    {
        await NameInput.FillAsync(name);
        await page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();
        await Expect(NameInput).ToBeHiddenAsync();
        return this;
    }

    public async Task<EditableReviewPage> ContinueWithRememberedNameAsync(string expectedName)
    {
        await Expect(NameInput).ToHaveValueAsync(expectedName);
        await NameInput.PressAsync("Enter");
        await Expect(NameInput).ToBeHiddenAsync();
        return this;
    }

    public async Task ExpectReviewedContentAsync(string text) => await Expect(ReviewedContent).ToContainTextAsync(text);

    public async Task AddCommentAsync(string comment)
    {
        await WaitForOverlayAsync();
        await OverlayDocument.ClickAsync(new() { Position = new() { X = 40, Y = 20 } });
        await CommentInput.FillAsync(comment);
        await SaveButton.ClickAsync();
        await Expect(CommentInput).ToBeHiddenAsync();
    }

    private async Task WaitForOverlayAsync()
    {
        await Expect(OverlayDocument).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('#review-overlay > div').offsetHeight > 0");
    }
}
