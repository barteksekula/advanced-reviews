using Microsoft.Playwright;

namespace Advanced.CMS.AdvancedReviews.E2ETests;

public class TrackableContext : IAsyncDisposable
{
    private const string VideosDirectory = "videos";

    private readonly List<IPage> _pages = [];
    private string _testName;

    public IBrowserContext Context { get; private set; }

    private static bool IsVideoRecordingEnabled => Environment.GetEnvironmentVariable("E2E_RECORD") == "1";

    public static async Task<TrackableContext> Get(WebServerFixture fixture, string testName)
    {
        var context = await fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = fixture.BaseUrl,
            ViewportSize = new ViewportSize
            {
                Width = 1920,
                Height = 1080
            },
            RecordVideoDir = IsVideoRecordingEnabled ? Path.Combine(VideosDirectory, "raw") : null,
            RecordVideoSize = IsVideoRecordingEnabled ? new RecordVideoSize { Width = 1280, Height = 720 } : null
        });

        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });

        var trackableContext = new TrackableContext
        {
            Context = context,
            _testName = testName
        };
        context.Page += (_, page) => trackableContext._pages.Add(page);
        return trackableContext;
    }

    public async ValueTask DisposeAsync()
    {
        await Context.Tracing.StopAsync(new TracingStopOptions
        {
            Path = $"traces/{_testName}.zip"
        });
        await Context.DisposeAsync();
        await SaveVideosAsync();
        GC.SuppressFinalize(this);
    }

    private async Task SaveVideosAsync()
    {
        var videos = _pages.Select(page => page.Video).Where(video => video != null).ToList();
        for (var i = 0; i < videos.Count; i++)
        {
            var suffix = i == 0 ? string.Empty : $"-{i + 1}";
            await videos[i].SaveAsAsync(Path.Combine(VideosDirectory, $"{_testName}{suffix}.webm"));
            await videos[i].DeleteAsync();
        }
    }
}
