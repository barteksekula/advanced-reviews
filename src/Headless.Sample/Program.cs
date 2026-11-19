using Headless.Sample;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GraphOptions>(builder.Configuration.GetSection("Graph"));
builder.Services.Configure<CmsOptions>(builder.Configuration.GetSection("Cms"));
builder.Services.AddHttpClient<GraphClient>();

var app = builder.Build();

app.MapGet("/", async (GraphClient graph, CancellationToken cancellationToken) =>
    Html.Page("Published pages", Html.PageList(await graph.ListPagesAsync(cancellationToken))));

app.MapGet("/content/{key}", async (string key, string? loc, GraphClient graph, CancellationToken cancellationToken) =>
    Html.Page("Published content", Html.Content(await graph.GetPublishedAsync(key, loc, cancellationToken))));

app.MapGet("/preview", async (
    string key,
    string? ver,
    string? loc,
    string? ctx,
    [FromQuery(Name = "preview_token")] string? previewToken,
    GraphClient graph,
    IOptions<CmsOptions> cmsOptions,
    HttpResponse response,
    CancellationToken cancellationToken) =>
{
    response.Headers.CacheControl = "no-store";

    if (string.IsNullOrEmpty(previewToken))
    {
        return Html.Page("Preview", "<div class=\"error\">The preview_token query parameter is missing</div>");
    }

    var isEditMode = string.Equals(ctx, "edit", StringComparison.OrdinalIgnoreCase);
    var result = await graph.GetPreviewAsync(key, ver, loc, previewToken, cancellationToken);
    var banner = result.Response.StatusCode == StatusCodes.Status401Unauthorized
        ? "Preview, the token was rejected. If it has expired, open the review link again"
        : isEditMode ? "Edit mode preview, updated when a property is saved" : "Preview of unpublished content";

    var body = Html.Content(result, banner, isEditMode) + Html.TokenDetails(PreviewToken.Read(previewToken));
    return isEditMode
        ? Html.Page("Preview", body, Html.EditModeScripts(cmsOptions.Value.Url))
        : Html.Page("Preview", body);
});

app.Run();
