using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Headless.Sample;

internal record PreviewToken(string Claims, DateTimeOffset? Expires)
{
    public static PreviewToken Read(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return new PreviewToken("Not a JWT, the claims cannot be read", null);
        }

        try
        {
            var payload = JsonNode.Parse(Encoding.UTF8.GetString(FromBase64Url(parts[1])));
            var exp = payload?["exp"]?.GetValue<long>();
            return new PreviewToken(
                payload?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "",
                exp.HasValue ? DateTimeOffset.FromUnixTimeSeconds(exp.Value) : null);
        }
        catch (Exception exception) when (exception is FormatException or JsonException or InvalidOperationException)
        {
            return new PreviewToken($"The token payload cannot be read: {exception.Message}", null);
        }
    }

    private static byte[] FromBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
    }
}
