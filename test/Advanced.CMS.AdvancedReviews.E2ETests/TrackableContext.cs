using Microsoft.Playwright;

namespace Advanced.CMS.AdvancedReviews.E2ETests;

public class TrackableContext : IAsyncDisposable
{
    private string _testName;

    public IBrowserContext Context { get; private set; }

    public static async Task<TrackableContext> Get(WebServerFixture fixture, string testName)
    {
        var context = await fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = fixture.BaseUrl,
            ViewportSize = new ViewportSize
            {
                Width = 1920,
                Height = 1080
            }
        });

        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });

        return new TrackableContext
        {
            Context = context,
            _testName = testName
        };
    }

    public async ValueTask DisposeAsync()
    {
        await Context.Tracing.StopAsync(new TracingStopOptions
        {
            Path = $"traces/{_testName}.zip"
        });
        await Context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
