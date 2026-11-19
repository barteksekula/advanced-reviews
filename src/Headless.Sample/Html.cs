using System.Text;
using System.Text.Json.Nodes;
using static System.Net.WebUtility;

namespace Headless.Sample;

internal static class Html
{
    private const string Empty = "<span class=\"empty\">empty</span>";

    private const string Styles = """
        body { font-family: system-ui, sans-serif; max-width: 960px; margin: 0 auto; padding: 16px; color: #1f2328; }
        a { color: #0969da; }
        table { border-collapse: collapse; margin: 8px 0 16px; }
        th, td { text-align: left; padding: 6px 12px 6px 0; border-bottom: 1px solid #d0d7de; vertical-align: top; }
        table.properties { width: 100%; }
        table.properties th { width: 30%; font-weight: 600; }
        dl { margin: 0; display: grid; grid-template-columns: max-content 1fr; gap: 2px 12px; }
        dt { color: #59636e; }
        dd { margin: 0; }
        ol { margin: 0; padding-left: 20px; }
        .empty, .muted { color: #6e7781; font-style: italic; }
        .richtext { border-left: 3px solid #d0d7de; padding-left: 12px; }
        .richtext > :first-child { margin-top: 0; }
        .richtext > :last-child { margin-bottom: 0; }
        pre { background: #f6f8fa; padding: 12px; overflow-x: auto; font-size: 13px; }
        .banner { padding: 8px 12px; margin-bottom: 16px; background: #fff8c5; border: 1px solid #d4a72c; }
        .error { padding: 8px 12px; margin-bottom: 16px; background: #ffebe9; border: 1px solid #ff8182; }
        details { margin: 16px 0; }
        summary { cursor: pointer; }
        """;

    public static string EditModeScripts(string cmsUrl) => $$"""
        <script src="{{HtmlEncode(cmsUrl.TrimEnd('/'))}}/util/javascript/communicationinjector.js"></script>
        <script>
          window.addEventListener("optimizely:cms:contentSaved", function (event) {
            var previewUrl = event.detail && event.detail.previewUrl;
            if (previewUrl) {
              window.location.replace(previewUrl);
            }
          });
        </script>
        """;

    public static IResult Page(string title, string body, string head = "")
    {
        var html = $"""
            <!doctype html>
            <html>
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="referrer" content="no-referrer">
              <title>{HtmlEncode(title)}</title>
              <style>{Styles}</style>
              {head}
            </head>
            <body>
              <p><a href="/">Headless sample</a></p>
              {body}
            </body>
            </html>
            """;
        return Results.Content(html, "text/html; charset=utf-8");
    }

    public static string PageList(GraphResult result)
    {
        var body = new StringBuilder("<h1>Published pages</h1>");
        body.Append(Error(result));
        body.Append("<table><tr><th>Name</th><th>Locale</th><th>Version</th><th>Types</th></tr>");
        var items = result.Data?["_Page"]?["items"] as JsonArray ?? [];
        foreach (var metadata in items.Select(x => x?["_metadata"]))
        {
            var key = Text(metadata, "key");
            var locale = Text(metadata, "locale");
            body.Append($"""
                <tr>
                  <td><a href="/content/{UrlEncode(key)}?loc={UrlEncode(locale)}">{HtmlEncode(Text(metadata, "displayName"))}</a></td>
                  <td>{HtmlEncode(locale)}</td>
                  <td>{HtmlEncode(Text(metadata, "version"))}</td>
                  <td>{HtmlEncode(Types(metadata))}</td>
                </tr>
                """);
        }

        body.Append("</table>");
        body.Append(Raw(result));
        return body.ToString();
    }

    public static string Content(ContentResult result, string? banner = null, bool isEditMode = false)
    {
        var body = new StringBuilder();
        if (banner != null)
        {
            body.Append($"<div class=\"banner\">{banner}</div>");
        }

        body.Append(Error(result.Response));

        if (result.Metadata == null)
        {
            body.Append("<h1>Content not found</h1>");
        }
        else
        {
            body.Append($"<h1>{HtmlEncode(Text(result.Metadata, "displayName"))}</h1>");
            body.Append(Properties(result.Properties, isEditMode));
            body.Append(Metadata(result.Metadata));
        }

        body.Append(Raw(result.Response));
        return body.ToString();
    }

    public static string TokenDetails(PreviewToken token)
    {
        var expires = token.Expires.HasValue
            ? $"<p>Token expires {token.Expires.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss} ({Remaining(token.Expires.Value)})</p>"
            : "";
        return $"<details><summary>Preview token</summary>{expires}<pre>{HtmlEncode(token.Claims)}</pre></details>";
    }

    private static string Properties(IReadOnlyList<ContentProperty> properties, bool isEditMode)
    {
        if (properties.Count == 0)
        {
            return "<p class=\"muted\">The content type has no properties</p>";
        }

        var rows = properties.Select(x =>
        {
            var editAttribute = isEditMode ? $" data-epi-edit=\"{HtmlEncode(x.Name)}\"" : "";
            return $"<tr><th>{HtmlEncode(x.Name)}</th><td{editAttribute}>{Value(x.Value)}</td></tr>";
        });
        return $"<table class=\"properties\">{string.Concat(rows)}</table>";
    }

    private static string Metadata(JsonNode metadata)
    {
        var rows = new[] { "key", "version", "locale", "status", "lastModified" }
            .Select(x => $"<tr><th>{x}</th><td>{HtmlEncode(Text(metadata, x))}</td></tr>")
            .Append($"<tr><th>types</th><td>{HtmlEncode(Types(metadata))}</td></tr>");
        return $"<details><summary>Metadata</summary><table>{string.Concat(rows)}</table></details>";
    }

    private static string Value(JsonNode? node) => node switch
    {
        null => Empty,
        JsonValue value => HtmlEncode(value.ToString()),
        JsonArray { Count: 0 } => Empty,
        JsonArray array when array.All(x => x is JsonValue) => HtmlEncode(string.Join(", ", array)),
        JsonArray array => $"<ol>{string.Concat(array.Select(x => $"<li>{Value(x)}</li>"))}</ol>",
        JsonObject content when content["html"] is JsonValue html => $"<div class=\"richtext\">{html.GetValue<string>()}</div>",
        JsonObject content when content["_metadata"]?["displayName"] is JsonValue name =>
            $"{HtmlEncode(name.ToString())} <span class=\"muted\">{HtmlEncode(content["__typename"]?.ToString() ?? "")}</span>",
        JsonObject content => Object(content),
        _ => HtmlEncode(node.ToJsonString())
    };

    private static string Object(JsonObject content)
    {
        var entries = content
            .Where(x => !x.Key.StartsWith('_'))
            .Select(x => $"<dt>{HtmlEncode(x.Key)}</dt><dd>{Value(x.Value)}</dd>")
            .ToList();
        return entries.Count == 0 ? Empty : $"<dl>{string.Concat(entries)}</dl>";
    }

    private static string Error(GraphResult result) => result.Error == null
        ? ""
        : $"<div class=\"error\">{HtmlEncode(result.Error)}</div>";

    private static string Raw(GraphResult result) =>
        $"<details><summary>Graph response (HTTP {result.StatusCode})</summary><pre>{HtmlEncode(result.RawResponse)}</pre></details>";

    private static string Text(JsonNode? metadata, string field) => metadata?[field]?.ToString() ?? "";

    private static string Types(JsonNode? metadata) =>
        metadata?["types"] is JsonArray types ? string.Join(", ", types.Select(x => x?.ToString())) : "";

    private static string Remaining(DateTimeOffset expires)
    {
        var remaining = expires - DateTimeOffset.UtcNow;
        return remaining > TimeSpan.Zero ? $"in {(int)remaining.TotalMinutes} min {remaining.Seconds} s" : "expired";
    }
}
