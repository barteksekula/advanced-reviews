using Microsoft.Playwright;

namespace Advanced.CMS.AdvancedReviews.E2ETests.PageObjectModels;

public class LoginPage(IPage page)
{
    private ILocator NameInput => page.GetByLabel("Name");
    private ILocator PasswordInput => page.GetByLabel("Password");
    private ILocator LoginButton => page.GetByRole(AriaRole.Button, new() { Name = "Log in" });

    public async Task<EditModePage> LoginAsAdminAsync() =>
        await LoginAsync(WebServerFixture.AdminUserName, WebServerFixture.AdminPassword);

    public async Task<EditModePage> LoginAsync(string userName, string password)
    {
        await page.GotoAsync("/Util/Login?ReturnUrl=%2FOptimizely%2FCMS");
        await NameInput.FillAsync(userName);
        await PasswordInput.FillAsync(password);
        await LoginButton.ClickAsync();
        await page.WaitForURLAsync("**/Optimizely/CMS**");
        return new EditModePage(page);
    }
}
