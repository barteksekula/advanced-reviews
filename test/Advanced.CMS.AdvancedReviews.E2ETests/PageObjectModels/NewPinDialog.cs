using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Advanced.CMS.AdvancedReviews.E2ETests.PageObjectModels;

public class NewPinDialog(IPage page)
{
    private ILocator Dialog => page.GetByRole(AriaRole.Dialog);
    private ILocator Title => Dialog.Locator(".dialog-title-text");
    private ILocator CommentInput => Dialog.GetByLabel("Add comment...");

    public async Task AddPinAsync(ILocator target, string expectedTitle, string comment)
    {
        await OpenOnAsync(target, expectedTitle);
        await CommentInput.FillAsync(comment);
        await Dialog.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Expect(CommentInput).ToBeHiddenAsync();
    }

    private async Task OpenOnAsync(ILocator target, string expectedTitle)
    {
        for (var attempt = 0; ; attempt++)
        {
            var box = await target.BoundingBoxAsync() ?? throw new InvalidOperationException("The pin target is not visible");
            await page.Mouse.ClickAsync(box.X + Math.Min(40, box.Width / 2), box.Y + box.Height / 2);
            await Expect(CommentInput).ToBeVisibleAsync();
            if (await Title.InnerTextAsync() == expectedTitle || attempt == 10)
            {
                await Expect(Title).ToHaveTextAsync(expectedTitle);
                return;
            }

            await Dialog.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Cancel|Close", RegexOptions.IgnoreCase) }).ClickAsync();
            await Expect(CommentInput).ToBeHiddenAsync();
            await page.WaitForTimeoutAsync(500);
        }
    }
}
