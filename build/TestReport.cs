#:property PublishAot=false
#:property Nullable=disable

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

if (args.Length != 4)
{
    Console.Error.WriteLine("Usage: dotnet run build/TestReport.cs -- <trx directory> <videos directory> <traces directory> <output directory>");
    return 1;
}

var (resultsDirectory, videosDirectory, tracesDirectory, outputDirectory) = (args[0], args[1], args[2], args[3]);
var publicUrl = Environment.GetEnvironmentVariable("REPORT_PUBLIC_URL")?.TrimEnd('/');
var repositoryUrl = $"{Environment.GetEnvironmentVariable("GITHUB_SERVER_URL")}/{Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")}";
var commit = Environment.GetEnvironmentVariable("GITHUB_SHA");
var runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");

Directory.CreateDirectory(Path.Combine(outputDirectory, "videos"));
Directory.CreateDirectory(Path.Combine(outputDirectory, "traces"));

var suites = Directory.Exists(resultsDirectory)
    ? Directory.GetFiles(resultsDirectory, "*.trx").Order().Select(TrxSuite.Load).ToList()
    : [];

var tests = suites.SelectMany(suite => suite.Tests).ToList();

foreach (var (test, video) in AssignArtifacts(videosDirectory, "*.webm", "videos"))
{
    test.Videos.Add(video);
}

foreach (var (test, trace) in AssignArtifacts(tracesDirectory, "*.zip", "traces"))
{
    test.Traces.Add(trace);
}
var passed = tests.Count(test => test.Outcome == "Passed");
var failed = tests.Count(test => test.Outcome == "Failed");
var skipped = tests.Count - passed - failed;

File.WriteAllText(Path.Combine(outputDirectory, "index.html"), RenderPage());
File.WriteAllText(Path.Combine(outputDirectory, "summary.json"), JsonSerializer.Serialize(new
{
    schemaVersion = 1,
    label = "tests",
    message = failed > 0 ? $"{failed} failed, {passed} passed" : $"{passed} passed",
    color = failed > 0 || tests.Count == 0 ? "red" : "brightgreen"
}));

Console.WriteLine($"Test report: {passed} passed, {failed} failed, {skipped} skipped -> {Path.GetFullPath(outputDirectory)}");
return 0;

IEnumerable<(TestResult Test, string RelativePath)> AssignArtifacts(string sourceDirectory, string pattern, string targetFolder)
{
    if (!Directory.Exists(sourceDirectory))
    {
        yield break;
    }

    foreach (var file in Directory.GetFiles(sourceDirectory, pattern).Order())
    {
        var name = Path.GetFileNameWithoutExtension(file);
        var owner = tests
            .Where(test => name == test.MethodName || name.StartsWith(test.MethodName + "-") || name.StartsWith(test.MethodName + "_"))
            .MaxBy(test => test.MethodName.Length);
        if (owner == null)
        {
            continue;
        }

        var relativePath = $"{targetFolder}/{Path.GetFileName(file)}";
        File.Copy(file, Path.Combine(outputDirectory, relativePath), true);
        yield return (owner, relativePath);
    }
}

string RenderPage()
{
    var html = new StringBuilder();
    var status = failed > 0 ? "failed" : "passed";
    var generated = suites.Select(suite => suite.Finished).DefaultIfEmpty(DateTimeOffset.UtcNow).Max();

    html.Append($$"""
        <!doctype html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>Advanced Reviews tests</title>
        <style>
        :root { --bg: #f6f7f9; --surface: #fff; --text: #1d2330; --muted: #5f6b7a; --border: #e1e5ea; --passed: #1f883d; --failed: #cf222e; --skipped: #9a6700; --accent: #0969da; }
        @media (prefers-color-scheme: dark) { :root { --bg: #0d1117; --surface: #161b22; --text: #e6edf3; --muted: #8d96a0; --border: #30363d; --passed: #3fb950; --failed: #f85149; --skipped: #d29922; --accent: #4493f8; } }
        * { box-sizing: border-box; }
        body { margin: 0; background: var(--bg); color: var(--text); font: 15px/1.5 system-ui, -apple-system, "Segoe UI", sans-serif; }
        main { max-width: 1100px; margin: 0 auto; padding: 32px 16px 64px; }
        h1 { margin: 0 0 4px; font-size: 26px; }
        h2 { margin: 40px 0 12px; font-size: 19px; }
        h3 { margin: 24px 0 8px; font-size: 15px; color: var(--muted); font-weight: 600; }
        a { color: var(--accent); }
        .meta { color: var(--muted); margin: 0 0 24px; }
        .cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 12px; }
        .card { background: var(--surface); border: 1px solid var(--border); border-radius: 10px; padding: 14px 16px; }
        .card strong { display: block; font-size: 28px; line-height: 1.2; }
        .card span { color: var(--muted); font-size: 13px; }
        .card.passed strong { color: var(--passed); }
        .card.failed strong { color: var(--failed); }
        .card.skipped strong { color: var(--skipped); }
        .banner { margin: 0 0 20px; padding: 10px 14px; border-radius: 8px; color: #fff; font-weight: 600; }
        .banner.passed { background: var(--passed); }
        .banner.failed { background: var(--failed); }
        .tests { background: var(--surface); border: 1px solid var(--border); border-radius: 10px; overflow: hidden; }
        details { border-top: 1px solid var(--border); }
        details:first-child { border-top: 0; }
        summary { display: flex; align-items: center; gap: 10px; padding: 9px 14px; cursor: pointer; list-style: none; }
        summary::-webkit-details-marker { display: none; }
        summary:hover { background: var(--bg); }
        .name { flex: 1; min-width: 0; overflow-wrap: anywhere; }
        .duration, .tag { color: var(--muted); font-size: 13px; white-space: nowrap; }
        .dot { width: 10px; height: 10px; border-radius: 50%; flex: none; }
        .dot.Passed { background: var(--passed); }
        .dot.Failed { background: var(--failed); }
        .dot.NotExecuted { background: var(--skipped); }
        .body { padding: 4px 14px 16px 34px; }
        .body video { width: 100%; max-width: 860px; border-radius: 8px; border: 1px solid var(--border); background: #000; display: block; margin: 8px 0; }
        pre { background: var(--bg); border: 1px solid var(--border); border-radius: 8px; padding: 10px 12px; overflow-x: auto; font-size: 12.5px; white-space: pre-wrap; }
        </style>
        </head>
        <body>
        <main>
        <h1>Advanced Reviews tests</h1>
        <p class="meta">{{Encode(generated.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture))}}{{RenderCommitLinks()}}</p>
        <p class="banner {{status}}">{{(failed > 0 ? $"{failed} of {tests.Count} tests failed" : $"All {passed} executed tests passed")}}</p>
        <div class="cards">
        <div class="card"><strong>{{tests.Count}}</strong><span>tests</span></div>
        <div class="card passed"><strong>{{passed}}</strong><span>passed</span></div>
        <div class="card failed"><strong>{{failed}}</strong><span>failed</span></div>
        <div class="card skipped"><strong>{{skipped}}</strong><span>skipped</span></div>
        <div class="card"><strong>{{FormatDuration(suites.Aggregate(TimeSpan.Zero, (sum, suite) => sum + suite.Duration))}}</strong><span>duration</span></div>
        </div>
        """);

    foreach (var suite in suites)
    {
        html.Append($"<h2>{Encode(suite.Title)} <span class=\"tag\">{suite.Tests.Count(test => test.Outcome == "Passed")}/{suite.Tests.Count} passed</span></h2>");
        foreach (var group in suite.Tests.GroupBy(test => test.ClassName).OrderBy(group => group.Key))
        {
            html.Append($"<h3>{Encode(Humanize(group.Key))}</h3><div class=\"tests\">");
            foreach (var test in group.OrderBy(test => test.DisplayName))
            {
                html.Append(RenderTest(test));
            }
            html.Append("</div>");
        }
    }

    html.Append("</main></body></html>");
    return html.ToString();
}

string RenderTest(TestResult test)
{
    var tags = new List<string>();
    if (test.Videos.Count > 0)
    {
        tags.Add("video");
    }
    if (test.Outcome == "NotExecuted")
    {
        tags.Add("skipped");
    }

    var body = new StringBuilder();
    if (!string.IsNullOrEmpty(test.Message))
    {
        body.Append($"<pre>{Encode(test.Message)}{(string.IsNullOrEmpty(test.StackTrace) ? "" : "\n\n" + Encode(test.StackTrace))}</pre>");
    }
    foreach (var video in test.Videos)
    {
        body.Append($"<video controls preload=\"none\" src=\"{Encode(video)}\"></video>");
    }
    foreach (var trace in test.Traces)
    {
        var traceUrl = publicUrl == null ? trace : $"https://trace.playwright.dev/?trace={Uri.EscapeDataString($"{publicUrl}/{trace}")}";
        var context = Path.GetFileNameWithoutExtension(trace)[test.MethodName.Length..].TrimStart('_', '-');
        var label = test.Traces.Count == 1 ? "Open Playwright trace" : $"Open Playwright trace ({(context.Length == 0 ? "main" : context)})";
        body.Append($"<p><a href=\"{Encode(traceUrl)}\">{Encode(label)}</a></p>");
    }

    var summary = $"<summary><span class=\"dot {test.Outcome}\"></span><span class=\"name\">{Encode(test.DisplayName)}</span>"
        + string.Concat(tags.Select(tag => $"<span class=\"tag\">{tag}</span>"))
        + $"<span class=\"duration\">{FormatDuration(test.Duration)}</span></summary>";

    return body.Length == 0
        ? $"<details>{summary}</details>"
        : $"<details{(test.Outcome == "Failed" ? " open" : "")}>{summary}<div class=\"body\">{body}</div></details>";
}

string RenderCommitLinks()
{
    if (string.IsNullOrEmpty(commit))
    {
        return string.Empty;
    }

    var links = $" · commit <a href=\"{Encode($"{repositoryUrl}/commit/{commit}")}\">{Encode(commit[..7])}</a>";
    return string.IsNullOrEmpty(runId) ? links : links + $" · <a href=\"{Encode($"{repositoryUrl}/actions/runs/{runId}")}\">workflow run</a>";
}

static string Encode(string value) => WebUtility.HtmlEncode(value);

static string Humanize(string value) => value.Replace('_', ' ');

static string FormatDuration(TimeSpan duration) => duration.TotalSeconds switch
{
    < 1 => $"{duration.TotalMilliseconds:0} ms",
    < 60 => $"{duration.TotalSeconds:0.0} s",
    _ => $"{(int)duration.TotalMinutes} min {duration.Seconds} s"
};

class TrxSuite
{
    private static readonly XNamespace Ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    public string Title { get; private init; }
    public DateTimeOffset Finished { get; private init; }
    public TimeSpan Duration { get; private init; }
    public List<TestResult> Tests { get; private init; }

    public static TrxSuite Load(string path)
    {
        var document = XDocument.Load(path);
        var times = document.Root!.Element(Ns + "Times");
        var started = DateTimeOffset.Parse(times!.Attribute("start")!.Value, CultureInfo.InvariantCulture);
        var finished = DateTimeOffset.Parse(times.Attribute("finish")!.Value, CultureInfo.InvariantCulture);
        var classNames = document.Descendants(Ns + "UnitTest").ToDictionary(
            unitTest => unitTest.Attribute("id")!.Value,
            unitTest => unitTest.Element(Ns + "TestMethod")!.Attribute("className")!.Value);

        return new TrxSuite
        {
            Title = Path.GetFileNameWithoutExtension(path) switch
            {
                "E2ETests" => "End-to-end tests",
                "IntegrationTests" => "Integration tests",
                var name => name
            },
            Finished = finished.ToUniversalTime(),
            Duration = finished - started,
            Tests = document.Descendants(Ns + "UnitTestResult")
                .Select(result => TestResult.From(result, classNames[result.Attribute("testId")!.Value], Ns))
                .ToList()
        };
    }
}

class TestResult
{
    public string ClassName { get; private init; }
    public string MethodName { get; private init; }
    public string DisplayName { get; private init; }
    public string Outcome { get; private init; }
    public TimeSpan Duration { get; private init; }
    public string Message { get; private init; }
    public string StackTrace { get; private init; }
    public List<string> Videos { get; } = [];
    public List<string> Traces { get; } = [];

    public static TestResult From(XElement result, string fullClassName, XNamespace ns)
    {
        var testName = result.Attribute("testName")!.Value;
        var name = testName.StartsWith(fullClassName + ".") ? testName[(fullClassName.Length + 1)..] : testName;
        var methodName = name.Split('(')[0];
        var output = result.Element(ns + "Output");
        var errorInfo = output?.Element(ns + "ErrorInfo");
        var outcome = result.Attribute("outcome")?.Value ?? "NotExecuted";

        return new TestResult
        {
            ClassName = fullClassName[(fullClassName.LastIndexOf('.') + 1)..],
            MethodName = methodName,
            DisplayName = methodName.Replace('_', ' ') + name[methodName.Length..],
            Outcome = outcome,
            Duration = TimeSpan.TryParse(result.Attribute("duration")?.Value, CultureInfo.InvariantCulture, out var duration) ? duration : TimeSpan.Zero,
            Message = errorInfo?.Element(ns + "Message")?.Value ?? (outcome == "NotExecuted" ? output?.Element(ns + "StdOut")?.Value : null),
            StackTrace = errorInfo?.Element(ns + "StackTrace")?.Value
        };
    }
}
