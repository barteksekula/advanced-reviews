using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace Headless.Sample;

internal class GraphOptions
{
    public string Endpoint { get; set; } = "https://cg.optimizely.com/content/v2";
    public string SingleKey { get; set; } = "";
}

internal class CmsOptions
{
    public string Url { get; set; } = "http://localhost:8000";
}

internal record GraphResult(int StatusCode, JsonNode? Data, string? Error, string RawResponse);

internal record ContentProperty(string Name, JsonNode? Value);

internal record ContentResult(GraphResult Response, JsonNode? Metadata, IReadOnlyList<ContentProperty> Properties);

internal class GraphClient(HttpClient httpClient, IOptions<GraphOptions> options)
{
    private const string MetadataFields = "_metadata { key version locale displayName status lastModified types }";
    private const string TypeFieldsQuery = "query Type($name: String!) { __type(name: $name) { fields { name } } }";

    public Task<GraphResult> ListPagesAsync(CancellationToken cancellationToken) =>
        SendAsync($"query Pages {{ _Page(limit: 50) {{ items {{ {MetadataFields} }} }} }}", new Dictionary<string, string?>(), null, cancellationToken);

    public Task<ContentResult> GetPublishedAsync(string key, string? locale, CancellationToken cancellationToken) =>
        GetContentAsync(new ContentFilter(key, null, locale), null, cancellationToken);

    public Task<ContentResult> GetPreviewAsync(string key, string? version, string? locale, string previewToken,
        CancellationToken cancellationToken) =>
        GetContentAsync(new ContentFilter(key, version, locale), previewToken, cancellationToken);

    private async Task<ContentResult> GetContentAsync(ContentFilter filter, string? previewToken, CancellationToken cancellationToken)
    {
        var query = $$"""
            query Content{{filter.VariableDeclarations}} {
              _Content({{filter.WhereArgument}}) {
                item { __typename {{MetadataFields}} _json }
              }
            }
            """;

        var response = await SendAsync(query, filter.Variables, previewToken, cancellationToken);
        var item = response.Data?["_Content"]?["item"];
        var metadata = item?["_metadata"];
        if (metadata == null)
        {
            return new ContentResult(response, null, []);
        }

        var values = item?["_json"] as JsonObject;
        var typeName = item?["__typename"]?.GetValue<string>();
        var names = await GetFieldNamesAsync(typeName, previewToken, cancellationToken);
        var properties = names
            .Union(values?.Select(x => x.Key) ?? [])
            .Where(IsProperty)
            .Select(name => new ContentProperty(name, values?[name]))
            .ToList();

        return new ContentResult(response, metadata, properties);
    }

    private async Task<IReadOnlyList<string>> GetFieldNamesAsync(string? typeName, string? previewToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return [];
        }

        var response = await SendAsync(TypeFieldsQuery, new Dictionary<string, string?> { ["name"] = typeName }, previewToken, cancellationToken);
        return response.Data?["__type"]?["fields"] is JsonArray fields
            ? fields.Select(x => x?["name"]?.GetValue<string>()).OfType<string>().ToList()
            : [];
    }

    private static bool IsProperty(string name) => !name.StartsWith('_');

    private async Task<GraphResult> SendAsync(string query, Dictionary<string, string?> variables, string? previewToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, GetEndpoint(previewToken));
        if (previewToken != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", previewToken);
        }

        var body = JsonSerializer.Serialize(new { query, variables });
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        return Parse((int)response.StatusCode, raw);
    }

    private string GetEndpoint(string? previewToken)
    {
        var endpoint = options.Value.Endpoint;
        return previewToken == null && !string.IsNullOrEmpty(options.Value.SingleKey)
            ? $"{endpoint}?auth={Uri.EscapeDataString(options.Value.SingleKey)}"
            : endpoint;
    }

    private static GraphResult Parse(int statusCode, string raw)
    {
        JsonNode? json;
        try
        {
            json = JsonNode.Parse(raw);
        }
        catch (JsonException)
        {
            return new GraphResult(statusCode, null, $"Graph returned HTTP {statusCode} with a non-JSON body", raw);
        }

        var errors = json?["errors"] as JsonArray;
        var error = errors is { Count: > 0 }
            ? string.Join("; ", errors.Select(x => x?["message"]?.GetValue<string>()))
            : statusCode >= 400 ? $"Graph returned HTTP {statusCode}" : null;

        return new GraphResult(statusCode, json?["data"], error, json?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? raw);
    }

    private class ContentFilter(string? key = null, string? version = null, string? locale = null)
    {
        private readonly (string Name, string? Value)[] _filters =
            new[] { ("key", key), ("version", version), ("locale", locale) }
                .Where(x => !string.IsNullOrEmpty(x.Item2))
                .ToArray();

        public string VariableDeclarations => _filters.Length == 0
            ? ""
            : $"({string.Join(", ", _filters.Select(x => $"${x.Name}: String"))})";

        public string WhereArgument => _filters.Length == 0
            ? ""
            : $"where: {{ _metadata: {{ {string.Join(", ", _filters.Select(x => $"{x.Name}: {{ eq: ${x.Name} }}"))} }} }}";

        public Dictionary<string, string?> Variables => _filters.ToDictionary(x => x.Name, x => x.Value);
    }
}
