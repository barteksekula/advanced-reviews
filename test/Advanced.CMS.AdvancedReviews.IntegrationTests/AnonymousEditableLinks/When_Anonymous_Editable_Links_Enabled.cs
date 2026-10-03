using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using Advanced.CMS.ApprovalReviews;
using Advanced.CMS.ExternalReviews;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.Options;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.AnonymousEditableLinks;

[Collection(IntegrationTestCollection.Name)]
public class When_Anonymous_Editable_Links_Enabled(When_Anonymous_Editable_Links_Enabled.TestFixture fixture)
    : IClassFixture<CommonFixture>,
      IClassFixture<AnonymousEditableLinksEnabledFixture>,
      IClassFixture<When_Anonymous_Editable_Links_Enabled.TestFixture>
{
    private const string PageName = "anonymous editable page";

    public class TestFixture(SiteFixture siteFixture) : IAsyncLifetime
    {
        public IContentRepository ContentRepository { get; } = siteFixture.Services.GetInstance<IContentRepository>();
        public IExternalReviewLinksRepository LinksRepository { get; } = siteFixture.Services.GetInstance<IExternalReviewLinksRepository>();
        public IApprovalReviewsRepository ReviewsRepository { get; } = siteFixture.Services.GetInstance<IApprovalReviewsRepository>();
        public IOptions<ExternalReviewOptions> Options { get; } = siteFixture.Services.GetInstance<IOptions<ExternalReviewOptions>>();
        public HttpClient Client { get; } = siteFixture.CreateClientWithoutRedirects();

        public StandardPage Page { get; private set; }

        public Task InitializeAsync()
        {
            Page = ContentRepository.CreatePage(PageName).WithoutEveryoneAccess();
            return Task.CompletedTask;
        }

        public async Task DisposeAsync() => await ContentRepository.CleanupAsync(Page);
    }

    [Fact]
    public async Task Anonymous_User_Sees_Editable_Review_Page()
    {
        var link = CreateEditableLink();

        var response = await fixture.Client.GetAsync(link.LinkUrl);

        Assert.StartsWith("/advanced-reviews/edit/", link.LinkUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("reviews-editor", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Anonymous_User_Can_Load_Page_Resources()
    {
        var html = await (await fixture.Client.GetAsync(CreateEditableLink().LinkUrl)).Content.ReadAsStringAsync();

        var cssUrl = Regex.Match(html, "rel=\"stylesheet\" href=\"([^\"]+)\"").Groups[1].Value;
        var scriptUrl = Regex.Match(html, "script.src = \"([^\"]+)\"").Groups[1].Value;
        var avatarUrl = Regex.Match(html, "data-avatar-url=\"([^\"]+)\"").Groups[1].Value;
        var iframeUrl = Regex.Match(html, "id=\"editableIframe\" src=\"([^\"]+)\"").Groups[1].Value;

        await AssertOk(cssUrl, "text/css");
        await AssertOk(scriptUrl, "text/javascript");
        await AssertOk(avatarUrl + "/external", "image/png");
        var iframeResponse = await AssertOk(iframeUrl, "text/html");
        Assert.Contains(PageName, await iframeResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Anonymous_User_Can_Add_And_Remove_Own_Pin()
    {
        var link = CreateEditableLink();

        var addResponse = await AddPin(link.Token, null, PinData("first"));
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var id = await ReadId(addResponse);

        var replyResponse = await AddPin(link.Token, id, PinData("first", "reply"));
        Assert.Equal(HttpStatusCode.OK, replyResponse.StatusCode);

        var removeResponse = await RemovePin(link.Token, id);
        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        Assert.DoesNotContain(fixture.ReviewsRepository.Load(link.ContentLink), x => x.Id == id);
    }

    [Fact]
    public async Task Anonymous_User_Can_Reply_But_Not_Change_Or_Remove_Editor_Pin()
    {
        var link = CreateEditableLink();
        var editorPin = fixture.ReviewsRepository.Update(link.ContentLink, new ReviewLocation { Data = PinData("editor comment") });

        Assert.Equal(HttpStatusCode.BadRequest, (await AddPin(link.Token, editorPin.Id, PinData("rewritten comment"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await RemovePin(link.Token, editorPin.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AddPin(link.Token, editorPin.Id, PinData("editor comment", "reply"))).StatusCode);
    }

    [Fact]
    public async Task Pins_From_Other_Links_Do_Not_Expose_Their_Token()
    {
        var otherLink = CreateEditableLink();
        await AddPin(otherLink.Token, null, PinData("other reviewer"));

        var html = await (await fixture.Client.GetAsync(CreateEditableLink().LinkUrl)).Content.ReadAsStringAsync();

        Assert.Contains("other reviewer", html);
        Assert.DoesNotContain(otherLink.Token, html);
    }

    [Fact]
    public async Task Reviewer_Can_Remove_Own_Pins_But_Not_Pins_From_Other_Links_Or_Without_Token()
    {
        var link = CreateEditableLink();
        var ownPinId = await ReadId(await AddPin(link.Token, null, PinData("own pin")));
        var otherLinkPinId = await ReadId(await AddPin(CreateEditableLink().Token, null, PinData("other link pin")));
        var pinWithoutToken = fixture.ReviewsRepository.Update(link.ContentLink, new ReviewLocation { Data = PinData("pin without token") });

        var html = await (await fixture.Client.GetAsync(link.LinkUrl)).Content.ReadAsStringAsync();
        var pins = ReadRemovableFlags(html);

        Assert.True(pins[ownPinId]);
        Assert.False(pins[otherLinkPinId]);
        Assert.False(pins[pinWithoutToken.Id]);
    }

    [Fact]
    public async Task View_Link_Token_Cannot_Add_Pin()
    {
        var viewLink = fixture.LinksRepository.AddLink(fixture.Page.ContentLink, false, TimeSpan.FromDays(1), null);

        Assert.Equal(HttpStatusCode.BadRequest, (await AddPin(viewLink.Token, null, PinData("first"))).StatusCode);
    }

    [Fact]
    public async Task Pin_Protected_Link_Requires_Pin_Code()
    {
        var link = CreateEditableLink();
        fixture.LinksRepository.UpdateLink(link.Token, null, "1234", null, null);

        var pageResponse = await fixture.Client.GetAsync(link.LinkUrl);
        Assert.Equal(HttpStatusCode.Redirect, pageResponse.StatusCode);
        Assert.Contains(fixture.Options.Value.PinCodeSecurity.ExternalReviewLoginUrl, pageResponse.Headers.Location.ToString());

        Assert.Equal(HttpStatusCode.Forbidden, (await AddPin(link.Token, null, PinData("first"))).StatusCode);
    }

    private ExternalReviewLink CreateEditableLink() =>
        fixture.LinksRepository.AddLink(fixture.Page.ContentLink, true, TimeSpan.FromDays(1), null);

    private async Task<HttpResponseMessage> AssertOk(string url, string contentType)
    {
        var response = await fixture.Client.GetAsync(WebUtility.HtmlDecode(url));
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{url} returned {response.StatusCode} {response.Headers.Location}");
        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
        return response;
    }

    private Task<HttpResponseMessage> AddPin(string token, string id, string data) =>
        fixture.Client.PostAsJsonAsync("/advanced-reviews/edit/AddPin", new { token, id, data });

    private Task<HttpResponseMessage> RemovePin(string token, string id) =>
        fixture.Client.PostAsJsonAsync("/advanced-reviews/edit/RemovePin", new { token, id });

    private static async Task<string> ReadId(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.EnumerateObject()
            .First(x => string.Equals(x.Name, "id", StringComparison.OrdinalIgnoreCase)).Value.GetString();
    }

    private static Dictionary<string, bool> ReadRemovableFlags(string html)
    {
        var pinsJson = WebUtility.HtmlDecode(Regex.Match(html, "data-pins=\"([^\"]*)\"").Groups[1].Value);
        using var document = JsonDocument.Parse(pinsJson);
        return document.RootElement.EnumerateArray().ToDictionary(
            x => x.GetProperty("id").GetString(),
            x => x.GetProperty("isRemovable").GetBoolean());
    }

    private static string PinData(string firstComment, params string[] replies) =>
        JsonSerializer.Serialize(new
        {
            propertyName = "PageName",
            isDone = false,
            firstComment = Comment(firstComment),
            comments = replies.Select(Comment)
        });

    private static object Comment(string text) =>
        new { author = "external", text, date = "2026-10-02T10:00:00.000Z", screenshot = (string)null };
}
