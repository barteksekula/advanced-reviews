using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Advanced.CMS.AdvancedReviews.E2ETests.PageObjectModels;

public class EditModePage(IPage page)
{
    private ILocator NavigationPaneToggle => page.GetByTitle("Toggle navigation pane");

    public ILocator AddReviewLinkButton => page.GetByRole(AriaRole.Button, new() { Name = "Add link" });

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
