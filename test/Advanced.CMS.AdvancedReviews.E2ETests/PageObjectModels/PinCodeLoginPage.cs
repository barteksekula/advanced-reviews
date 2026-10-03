using Microsoft.Playwright;

namespace Advanced.CMS.AdvancedReviews.E2ETests.PageObjectModels;

public class PinCodeLoginPage(IPage page)
{
    public ILocator CodeInput => page.GetByPlaceholder("Enter security code");

    public async Task<EditableReviewPage> SubmitAsync(string pinCode)
    {
        await CodeInput.FillAsync(pinCode);
        await page.GetByRole(AriaRole.Button, new() { Name = "Go" }).ClickAsync();
        return new EditableReviewPage(page);
    }
}
